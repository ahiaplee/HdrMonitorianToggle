namespace HdrMonitorianToggleGUI
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.monitorsListBox = new System.Windows.Forms.ListBox();
            this.hdrCapableMonitorsLabel = new System.Windows.Forms.Label();
            this.selectMonitorLabel = new System.Windows.Forms.Label();
            this.hdrToggleBtn = new System.Windows.Forms.Button();
            this.logListBox = new System.Windows.Forms.ListBox();
            this.clearLogBtn = new System.Windows.Forms.Button();
            this.logLabel = new System.Windows.Forms.Label();
            this.refreshBtn = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.statusFixLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // monitorsListBox
            // 
            this.monitorsListBox.FormattingEnabled = true;
            this.monitorsListBox.Location = new System.Drawing.Point(12, 62);
            this.monitorsListBox.Name = "monitorsListBox";
            this.monitorsListBox.Size = new System.Drawing.Size(277, 95);
            this.monitorsListBox.TabIndex = 0;
            this.monitorsListBox.SelectedIndexChanged += new System.EventHandler(this.monitorsListBox_SelectedIndexChanged);
            // 
            // hdrCapableMonitorsLabel
            // 
            this.hdrCapableMonitorsLabel.AutoSize = true;
            this.hdrCapableMonitorsLabel.Location = new System.Drawing.Point(9, 9);
            this.hdrCapableMonitorsLabel.Name = "hdrCapableMonitorsLabel";
            this.hdrCapableMonitorsLabel.Size = new System.Drawing.Size(116, 13);
            this.hdrCapableMonitorsLabel.TabIndex = 1;
            this.hdrCapableMonitorsLabel.Text = "HDR Capable Monitors";
            this.hdrCapableMonitorsLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // selectMonitorLabel
            // 
            this.selectMonitorLabel.AutoSize = true;
            this.selectMonitorLabel.Location = new System.Drawing.Point(12, 33);
            this.selectMonitorLabel.Name = "selectMonitorLabel";
            this.selectMonitorLabel.Size = new System.Drawing.Size(60, 13);
            this.selectMonitorLabel.TabIndex = 2;
            this.selectMonitorLabel.Text = "Select One";
            // 
            // hdrToggleBtn
            // 
            this.hdrToggleBtn.Location = new System.Drawing.Point(295, 62);
            this.hdrToggleBtn.Name = "hdrToggleBtn";
            this.hdrToggleBtn.Size = new System.Drawing.Size(75, 23);
            this.hdrToggleBtn.TabIndex = 3;
            this.hdrToggleBtn.Text = "Toggle";
            this.hdrToggleBtn.UseVisualStyleBackColor = true;
            this.hdrToggleBtn.Click += new System.EventHandler(this.hdrToggleBtn_Click);
            // 
            // logListBox
            // 
            this.logListBox.FormattingEnabled = true;
            this.logListBox.Location = new System.Drawing.Point(12, 191);
            this.logListBox.Name = "logListBox";
            this.logListBox.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.logListBox.Size = new System.Drawing.Size(376, 108);
            this.logListBox.TabIndex = 4;
            // 
            // clearLogBtn
            // 
            this.clearLogBtn.Location = new System.Drawing.Point(313, 305);
            this.clearLogBtn.Name = "clearLogBtn";
            this.clearLogBtn.Size = new System.Drawing.Size(75, 23);
            this.clearLogBtn.TabIndex = 5;
            this.clearLogBtn.Text = "Clear Log";
            this.clearLogBtn.UseVisualStyleBackColor = true;
            this.clearLogBtn.Click += new System.EventHandler(this.clearLogBtn_Click);
            // 
            // logLabel
            // 
            this.logLabel.AutoSize = true;
            this.logLabel.Location = new System.Drawing.Point(12, 175);
            this.logLabel.Name = "logLabel";
            this.logLabel.Size = new System.Drawing.Size(25, 13);
            this.logLabel.TabIndex = 6;
            this.logLabel.Text = "Log";
            this.logLabel.Click += new System.EventHandler(this.label3_Click);
            // 
            // refreshBtn
            // 
            this.refreshBtn.Location = new System.Drawing.Point(295, 91);
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(75, 23);
            this.refreshBtn.TabIndex = 8;
            this.refreshBtn.Text = "Refresh";
            this.refreshBtn.UseVisualStyleBackColor = true;
            this.refreshBtn.Click += new System.EventHandler(this.refreshBtn_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(60, 46);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(40, 13);
            this.statusLabel.TabIndex = 9;
            this.statusLabel.Text = "           ";
            // 
            // statusFixLabel
            // 
            this.statusFixLabel.AutoSize = true;
            this.statusFixLabel.Location = new System.Drawing.Point(12, 46);
            this.statusFixLabel.Name = "statusFixLabel";
            this.statusFixLabel.Size = new System.Drawing.Size(37, 13);
            this.statusFixLabel.TabIndex = 10;
            this.statusFixLabel.Text = "Status";
            this.statusFixLabel.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 336);
            this.Controls.Add(this.statusFixLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.refreshBtn);
            this.Controls.Add(this.logLabel);
            this.Controls.Add(this.clearLogBtn);
            this.Controls.Add(this.logListBox);
            this.Controls.Add(this.hdrToggleBtn);
            this.Controls.Add(this.selectMonitorLabel);
            this.Controls.Add(this.hdrCapableMonitorsLabel);
            this.Controls.Add(this.monitorsListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainWindow";
            this.Text = "HDR Monitorian Toggle GUI";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox monitorsListBox;
        private System.Windows.Forms.Label hdrCapableMonitorsLabel;
        private System.Windows.Forms.Label selectMonitorLabel;
        private System.Windows.Forms.Button hdrToggleBtn;
        private System.Windows.Forms.ListBox logListBox;
        private System.Windows.Forms.Button clearLogBtn;
        private System.Windows.Forms.Label logLabel;
        private System.Windows.Forms.Button refreshBtn;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label statusFixLabel;
    }
}

