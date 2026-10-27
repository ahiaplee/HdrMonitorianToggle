using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HdrMonitorianToggle
{
    internal class Program
    {
        private const string MonitorianProcessName = "Monitorian";
        private const string MonitorianFullPath = @"C:\Program Files (x86)\Monitorian\Monitorian.exe";

        private class MonitorDetail
        {
            public int DisplayIndex { get; set; }
            public string Name { get; set; } = string.Empty;
            public uint TargetId { get; set; }
            public LUID AdapterId { get; set; }
            public bool IsHdrSupported { get; set; }
            public bool IsHdrEnabled { get; set; }
        }

        static void Main(string[] args)
        {
            // 1. Discover all connected displays and inspect HDR status
            var hdrMonitors = GetAvailableMonitors();

            if (hdrMonitors.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No HDR-capable monitors found.");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey(true);
                return;
            }

            // 2. Display the interactive menu
            Console.WriteLine("========================================");
            Console.WriteLine("        HDR CAPABLE MONITORS           ");
            Console.WriteLine("========================================");
            for (int i = 0; i < hdrMonitors.Count; i++)
            {
                var mon = hdrMonitors[i];
                string stateText = mon.IsHdrEnabled ? "[HDR ON]" : "[HDR OFF]";
                ConsoleColor stateColor = mon.IsHdrEnabled ? ConsoleColor.Green : ConsoleColor.Yellow;

                Console.Write($" [{i + 1}] Display {mon.DisplayIndex}: {mon.Name} - ");
                Console.ForegroundColor = stateColor;
                Console.WriteLine(stateText);
                Console.ResetColor();
            }
            Console.WriteLine("========================================");

            // 3. Prompt user selection
            MonitorDetail selectedMonitor;
            while (true)
            {
                Console.Write($"Select a monitor (1-{hdrMonitors.Count}): ");
                string? input = Console.ReadLine();

                if (int.TryParse(input, out int selection) && selection >= 1 && selection <= hdrMonitors.Count)
                {
                    selectedMonitor = hdrMonitors[selection - 1];
                    break;
                }
                Console.WriteLine("Invalid selection. Please try again.");
            }

            Console.WriteLine();

            // 4. Execute toggle logic
            if (selectedMonitor.IsHdrEnabled)
            {
                Console.WriteLine($"Disabling HDR on {selectedMonitor.Name}...");
                SetHdrState(selectedMonitor.TargetId, selectedMonitor.AdapterId, false);

                // Actively poll until the display hardware reconnects and reports SDR ready
                Console.Write("Waiting for monitor and graphics driver to reconnect");
                WaitForMonitorReady(selectedMonitor.TargetId, selectedMonitor.AdapterId);

                // Small grace period for DDC/CI I2C bus initialization
                Thread.Sleep(1500);

                Console.WriteLine("\nLaunching Monitorian...");
                LaunchMonitorianUnelevated();
            }
            else
            {
                Console.WriteLine("Closing Monitorian...");
                CloseMonitorian();

                Console.WriteLine($"Enabling HDR on {selectedMonitor.Name}...");
                SetHdrState(selectedMonitor.TargetId, selectedMonitor.AdapterId, true);
            }

            // 5. Completion and exit prompt
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nOperation completed successfully.");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

        private static void WaitForMonitorReady(uint targetId, LUID adapterId, int timeoutSeconds = 10)
        {
            var stopwatch = Stopwatch.StartNew();

            // Initial brief delay to let Windows begin the driver reset
            Thread.Sleep(1000);

            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
            {
                Console.Write(".");

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
                                        Console.WriteLine(" Connected!");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(400);
            }

            Console.WriteLine(" Timeout reached, continuing anyway.");
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

        private static void LaunchMonitorianUnelevated()
        {
            if (!File.Exists(MonitorianFullPath))
            {
                Console.WriteLine($"Executable not found at: {MonitorianFullPath}");
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
                Console.WriteLine($"Failed to start Monitorian: {ex.Message}");
            }
        }

        private static void CloseMonitorian()
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
    }
}