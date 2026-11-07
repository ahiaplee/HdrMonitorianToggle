using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HdrMonitorianToggleGUI
{
    public partial class MainWindow : Form
    {
        private const string MonitorianProcessName = "Monitorian";
        private const string MonitorianFullPath = @"C:\Program Files (x86)\Monitorian\Monitorian.exe";
        private List<MonitorDetail> monitors_;

        private class MonitorDetail
        {
            public int DisplayIndex { get; set; }
            public string Name { get; set; } = string.Empty;
            public uint TargetId { get; set; }
            public LUID AdapterId { get; set; }
            public bool IsHdrSupported { get; set; }
            public bool IsHdrEnabled { get; set; }
        }

        private void WriteLogLine(string text)
        {
            logListBox.Items.Add(text);
            logListBox.TopIndex = logListBox.Items.Count - 1;
        }

        private static List<MonitorDetail> GetAvailableMonitors()
        {
            var list = new List<MonitorDetail>();

            int status = GetDisplayConfigBufferSizes(QDC_ONLY_SPECIFIED_PATHS, out uint pathCount, out uint modeCount);
            if (status != 0) return list;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            status = QueryDisplayConfig(QDC_ONLY_SPECIFIED_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (status != 0) return list;

            for (int i = 0; i < paths.Length; i++)
            {
                var target = paths[i].targetInfo;

                var hdrInfo = GetAdvancedColorInfo(target.id, target.adapterId);
                bool isSupported = (hdrInfo.value & 0x1) != 0;
                bool isEnabled = (hdrInfo.value & 0x2) != 0;

                if (isSupported)
                {
                    string monitorFriendlyName = GetMonitorFriendlyName(target.id, target.adapterId);

                    list.Add(new MonitorDetail
                    {
                        DisplayIndex = i + 1,
                        Name = string.IsNullOrWhiteSpace(monitorFriendlyName) ? $"Display {i + 1}" : monitorFriendlyName,
                        TargetId = target.id,
                        AdapterId = target.adapterId,
                        IsHdrSupported = isSupported,
                        IsHdrEnabled = isEnabled
                    });
                }
            }

            return list;
        }

        private void WaitForMonitorReady(uint targetId, LUID adapterId, int timeoutSeconds = 10)
        {
            var stopwatch = Stopwatch.StartNew();

            // Initial brief delay to let Windows begin the driver reset
            Thread.Sleep(1000);

            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                int status = GetDisplayConfigBufferSizes(QDC_ONLY_SPECIFIED_PATHS, out uint pathCount, out uint modeCount);
                if (status == 0)
                {
                    var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                    var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

                    if (QueryDisplayConfig(QDC_ONLY_SPECIFIED_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) == 0)
                    {
                        foreach (var path in paths)
                        {
                            if (path.targetInfo.id == targetId &&
                                path.targetInfo.adapterId.LowPart == adapterId.LowPart &&
                                path.targetInfo.adapterId.HighPart == adapterId.HighPart)
                            {
                                // Verify target is active, available, and HDR is confirmed OFF
                                if (path.targetInfo.targetAvailable)
                                {
                                    var colorInfo = GetAdvancedColorInfo(targetId, adapterId);
                                    bool isHdrStillOn = (colorInfo.value & 0x2) != 0;

                                    if (!isHdrStillOn)
                                    {
                                        WriteLogLine(" Connected!");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(400);
            }

            WriteLogLine(" Timeout reached, continuing anyway.");
        }

        private void LaunchMonitorianUnelevated()
        {
            if (!File.Exists(MonitorianFullPath))
            {
                WriteLogLine($"Executable not found at: {MonitorianFullPath}");
                return;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{MonitorianFullPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                WriteLogLine($"Failed to start Monitorian: {ex.Message}");
            }
        }

        private void CloseMonitorian()
        {
            var processes = Process.GetProcessesByName(MonitorianProcessName);
            foreach (var p in processes)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(1000);
                }
                catch { }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            refreshMonitorList();
        }

        private void refreshMonitorList()
        {
            WriteLogLine("Refreshing Monitor List...");
            monitorsListBox.Items.Clear();

            // 1. Discover all connected displays and inspect HDR status
            monitors_ = GetAvailableMonitors();
            if (monitors_.Count == 0)
            {
                monitorsListBox.Items.Add("No HDR capable Monitors found");
                return;
            }

            WriteLogLine($"Found {monitors_.Count} HDR capable monitors");

            for (int i = 0; i < monitors_.Count; i++)
            {
                var mon = monitors_[i];
                // ConsoleColor stateColor = mon.IsHdrEnabled ? ConsoleColor.Green : ConsoleColor.Yellow;
                string stateText = mon.IsHdrEnabled ? "[HDR ON]" : "[HDR OFF]";
                monitorsListBox.Items.Add($" [{i + 1}] Display {mon.DisplayIndex}: {mon.Name} {stateText}");
                // Console.ForegroundColor = stateColor;
                // WriteLogLine(stateText);
                // Console.ResetColor();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }


        #region Native Display API Interop

        private const uint QDC_ONLY_SPECIFIED_PATHS = 0x00000002;
        private const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;
        private const uint DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
        private const uint DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO request);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME request);

        [DllImport("user32.dll")]
        private static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE request);

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public uint scanLineOrdering;
            public bool targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
            public uint id;
            public LUID adapterId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] modeInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public uint type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
            public int colorEncoding;
            public int bitsPerColorChannel;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public uint outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        private static DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO GetAdvancedColorInfo(uint targetId, LUID adapterId)
        {
            var req = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
            req.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
            req.header.size = (uint)Marshal.SizeOf(req);
            req.header.adapterId = adapterId;
            req.header.id = targetId;

            DisplayConfigGetDeviceInfo(ref req);
            return req;
        }

        private static string GetMonitorFriendlyName(uint targetId, LUID adapterId)
        {
            var req = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            req.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            req.header.size = (uint)Marshal.SizeOf(req);
            req.header.adapterId = adapterId;
            req.header.id = targetId;

            int res = DisplayConfigGetDeviceInfo(ref req);
            if (res == 0 && !string.IsNullOrWhiteSpace(req.monitorFriendlyDeviceName))
            {
                return req.monitorFriendlyDeviceName;
            }
            return string.Empty;
        }

        private static void SetHdrState(uint targetId, LUID adapterId, bool enable)
        {
            var req = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
            req.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
            req.header.size = (uint)Marshal.SizeOf(req);
            req.header.adapterId = adapterId;
            req.header.id = targetId;
            req.value = enable ? 1u : 0u;

            DisplayConfigSetDeviceInfo(ref req);
        }

        #endregion

        private void monitorsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateSelectionLabels();
        }

        private void updateSelectionLabels()
        {
            if (monitorsListBox.SelectedIndex < 0)
            {
                selectMonitorLabel.Text = "Select One";
                statusLabel.Text = "";
                return;
            }

            var selectedMonitor = monitors_[monitorsListBox.SelectedIndex];
            selectMonitorLabel.Text = $"Selected: [{selectedMonitor.DisplayIndex}] Display {selectedMonitor.DisplayIndex}: {selectedMonitor.Name}";

            string stateText = selectedMonitor.IsHdrEnabled ? "HDR ON" : "HDR OFF";

            statusLabel.ForeColor = selectedMonitor.IsHdrEnabled ? System.Drawing.Color.Green : System.Drawing.Color.Red;
            statusLabel.Text = $"{stateText}";
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {

        }

        private void clearLogBtn_Click(object sender, EventArgs e)
        {
            logListBox.Items.Clear();
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            refreshMonitorList();
        }

        private void hdrToggleBtn_Click(object sender, EventArgs e)
        {
            hdrToggleBtn.Enabled = false;
            refreshBtn.Enabled = false;
            toggleHDR();
            refreshMonitorList();
            updateSelectionLabels();

            hdrToggleBtn.Enabled = true;
            refreshBtn.Enabled = true;
        }

        private void toggleHDR()
        {
            if (monitors_.Count < 1)
            {
                return;
            }


            var selectedMonitor = monitors_[monitorsListBox.SelectedIndex];
            if (selectedMonitor.IsHdrEnabled)
            {
                WriteLogLine($"Disabling HDR on {selectedMonitor.Name}...");
                SetHdrState(selectedMonitor.TargetId, selectedMonitor.AdapterId, false);

                // Actively poll until the display hardware reconnects and reports SDR ready
                Console.Write("Waiting for monitor and graphics driver to reconnect");
                WaitForMonitorReady(selectedMonitor.TargetId, selectedMonitor.AdapterId);

                // Small grace period for DDC/CI I2C bus initialization
                Thread.Sleep(1500);

                WriteLogLine("\nLaunching Monitorian...");
                LaunchMonitorianUnelevated();
            }
            else
            {
                WriteLogLine("Closing Monitorian...");
                CloseMonitorian();

                WriteLogLine($"Enabling HDR on {selectedMonitor.Name}...");
                SetHdrState(selectedMonitor.TargetId, selectedMonitor.AdapterId, true);
            }
        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }
    }
}
