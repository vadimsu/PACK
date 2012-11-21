namespace PackSenderProxyEmulator
{
    partial class Form1
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
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.radioButtonRaw = new System.Windows.Forms.RadioButton();
            this.radioButtonHttp = new System.Windows.Forms.RadioButton();
            this.radioButtonLogNone = new System.Windows.Forms.RadioButton();
            this.radioButtonLogMedium = new System.Windows.Forms.RadioButton();
            this.radioButtonLogHigh = new System.Windows.Forms.RadioButton();
            this.labelRemotePort = new System.Windows.Forms.Label();
            this.textBoxRemotePort = new System.Windows.Forms.TextBox();
            this.textBoxLocalPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.radioButtonPackHttp = new System.Windows.Forms.RadioButton();
            this.radioButtonPackRaw = new System.Windows.Forms.RadioButton();
            this.radioButtonLogHigh2 = new System.Windows.Forms.RadioButton();
            this.radioButtonLogHigh3 = new System.Windows.Forms.RadioButton();
            this.buttonRefreshStatistics = new System.Windows.Forms.Button();
            this.statistics1 = new PackSenderProxyEmulator.Statistics();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(12, 266);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 12;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(134, 266);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 13;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // radioButtonRaw
            // 
            this.radioButtonRaw.AutoCheck = false;
            this.radioButtonRaw.AutoSize = true;
            this.radioButtonRaw.Checked = true;
            this.radioButtonRaw.Location = new System.Drawing.Point(26, 69);
            this.radioButtonRaw.Name = "radioButtonRaw";
            this.radioButtonRaw.Size = new System.Drawing.Size(47, 17);
            this.radioButtonRaw.TabIndex = 3;
            this.radioButtonRaw.TabStop = true;
            this.radioButtonRaw.Text = "Raw";
            this.radioButtonRaw.UseVisualStyleBackColor = true;
            this.radioButtonRaw.CheckedChanged += new System.EventHandler(this.radioButtonRaw_CheckedChanged);
            // 
            // radioButtonHttp
            // 
            this.radioButtonHttp.AutoCheck = false;
            this.radioButtonHttp.AutoSize = true;
            this.radioButtonHttp.Location = new System.Drawing.Point(26, 107);
            this.radioButtonHttp.Name = "radioButtonHttp";
            this.radioButtonHttp.Size = new System.Drawing.Size(45, 17);
            this.radioButtonHttp.TabIndex = 4;
            this.radioButtonHttp.Text = "Http";
            this.radioButtonHttp.UseVisualStyleBackColor = true;
            this.radioButtonHttp.CheckedChanged += new System.EventHandler(this.radioButtonHttp_CheckedChanged);
            // 
            // radioButtonLogNone
            // 
            this.radioButtonLogNone.AutoCheck = false;
            this.radioButtonLogNone.AutoSize = true;
            this.radioButtonLogNone.Location = new System.Drawing.Point(158, 69);
            this.radioButtonLogNone.Name = "radioButtonLogNone";
            this.radioButtonLogNone.Size = new System.Drawing.Size(70, 17);
            this.radioButtonLogNone.TabIndex = 7;
            this.radioButtonLogNone.Text = "Log none";
            this.radioButtonLogNone.UseVisualStyleBackColor = true;
            this.radioButtonLogNone.CheckedChanged += new System.EventHandler(this.radioButtonLogNone_CheckedChanged);
            // 
            // radioButtonLogMedium
            // 
            this.radioButtonLogMedium.AutoCheck = false;
            this.radioButtonLogMedium.AutoSize = true;
            this.radioButtonLogMedium.Location = new System.Drawing.Point(158, 107);
            this.radioButtonLogMedium.Name = "radioButtonLogMedium";
            this.radioButtonLogMedium.Size = new System.Drawing.Size(82, 17);
            this.radioButtonLogMedium.TabIndex = 8;
            this.radioButtonLogMedium.Text = "Log medium";
            this.radioButtonLogMedium.UseVisualStyleBackColor = true;
            this.radioButtonLogMedium.CheckedChanged += new System.EventHandler(this.radioButtonLogMedium_CheckedChanged);
            // 
            // radioButtonLogHigh
            // 
            this.radioButtonLogHigh.AutoCheck = false;
            this.radioButtonLogHigh.AutoSize = true;
            this.radioButtonLogHigh.Location = new System.Drawing.Point(158, 147);
            this.radioButtonLogHigh.Name = "radioButtonLogHigh";
            this.radioButtonLogHigh.Size = new System.Drawing.Size(66, 17);
            this.radioButtonLogHigh.TabIndex = 9;
            this.radioButtonLogHigh.TabStop = true;
            this.radioButtonLogHigh.Text = "Log high";
            this.radioButtonLogHigh.UseVisualStyleBackColor = true;
            this.radioButtonLogHigh.CheckedChanged += new System.EventHandler(this.radioButtonLogHigh_CheckedChanged);
            // 
            // labelRemotePort
            // 
            this.labelRemotePort.AutoSize = true;
            this.labelRemotePort.Location = new System.Drawing.Point(13, 13);
            this.labelRemotePort.Name = "labelRemotePort";
            this.labelRemotePort.Size = new System.Drawing.Size(65, 13);
            this.labelRemotePort.TabIndex = 7;
            this.labelRemotePort.Text = "Remote port";
            // 
            // textBoxRemotePort
            // 
            this.textBoxRemotePort.Location = new System.Drawing.Point(16, 43);
            this.textBoxRemotePort.Name = "textBoxRemotePort";
            this.textBoxRemotePort.Size = new System.Drawing.Size(100, 20);
            this.textBoxRemotePort.TabIndex = 1;
            this.textBoxRemotePort.Text = "8888";
            // 
            // textBoxLocalPort
            // 
            this.textBoxLocalPort.Location = new System.Drawing.Point(158, 43);
            this.textBoxLocalPort.Name = "textBoxLocalPort";
            this.textBoxLocalPort.Size = new System.Drawing.Size(100, 20);
            this.textBoxLocalPort.TabIndex = 2;
            this.textBoxLocalPort.Text = "7777";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(155, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Local port";
            // 
            // radioButtonPackHttp
            // 
            this.radioButtonPackHttp.AutoCheck = false;
            this.radioButtonPackHttp.AutoSize = true;
            this.radioButtonPackHttp.Location = new System.Drawing.Point(26, 147);
            this.radioButtonPackHttp.Name = "radioButtonPackHttp";
            this.radioButtonPackHttp.Size = new System.Drawing.Size(76, 17);
            this.radioButtonPackHttp.TabIndex = 5;
            this.radioButtonPackHttp.Text = "PACK Http";
            this.radioButtonPackHttp.UseVisualStyleBackColor = true;
            // 
            // radioButtonPackRaw
            // 
            this.radioButtonPackRaw.AutoCheck = false;
            this.radioButtonPackRaw.AutoSize = true;
            this.radioButtonPackRaw.Location = new System.Drawing.Point(26, 179);
            this.radioButtonPackRaw.Name = "radioButtonPackRaw";
            this.radioButtonPackRaw.Size = new System.Drawing.Size(78, 17);
            this.radioButtonPackRaw.TabIndex = 6;
            this.radioButtonPackRaw.Text = "PACK Raw";
            this.radioButtonPackRaw.UseVisualStyleBackColor = true;
            // 
            // radioButtonLogHigh2
            // 
            this.radioButtonLogHigh2.AutoCheck = false;
            this.radioButtonLogHigh2.AutoSize = true;
            this.radioButtonLogHigh2.Location = new System.Drawing.Point(158, 179);
            this.radioButtonLogHigh2.Name = "radioButtonLogHigh2";
            this.radioButtonLogHigh2.Size = new System.Drawing.Size(72, 17);
            this.radioButtonLogHigh2.TabIndex = 10;
            this.radioButtonLogHigh2.TabStop = true;
            this.radioButtonLogHigh2.Text = "Log high2";
            this.radioButtonLogHigh2.UseVisualStyleBackColor = true;
            // 
            // radioButtonLogHigh3
            // 
            this.radioButtonLogHigh3.AutoCheck = false;
            this.radioButtonLogHigh3.AutoSize = true;
            this.radioButtonLogHigh3.Checked = true;
            this.radioButtonLogHigh3.Location = new System.Drawing.Point(158, 213);
            this.radioButtonLogHigh3.Name = "radioButtonLogHigh3";
            this.radioButtonLogHigh3.Size = new System.Drawing.Size(72, 17);
            this.radioButtonLogHigh3.TabIndex = 11;
            this.radioButtonLogHigh3.TabStop = true;
            this.radioButtonLogHigh3.Text = "Log high3";
            this.radioButtonLogHigh3.UseVisualStyleBackColor = true;
            // 
            // buttonRefreshStatistics
            // 
            this.buttonRefreshStatistics.Location = new System.Drawing.Point(276, 421);
            this.buttonRefreshStatistics.Name = "buttonRefreshStatistics";
            this.buttonRefreshStatistics.Size = new System.Drawing.Size(217, 23);
            this.buttonRefreshStatistics.TabIndex = 16;
            this.buttonRefreshStatistics.Text = "Show outstanding transactions";
            this.buttonRefreshStatistics.UseVisualStyleBackColor = true;
            this.buttonRefreshStatistics.Click += new System.EventHandler(this.buttonRefreshStatistics_Click);
            // 
            // statistics1
            // 
            this.statistics1.DebugMode = true;
            this.statistics1.Location = new System.Drawing.Point(276, 31);
            this.statistics1.Name = "statistics1";
            this.statistics1.Size = new System.Drawing.Size(855, 366);
            this.statistics1.TabIndex = 17;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1132, 533);
            this.Controls.Add(this.statistics1);
            this.Controls.Add(this.buttonRefreshStatistics);
            this.Controls.Add(this.radioButtonLogHigh3);
            this.Controls.Add(this.radioButtonLogHigh2);
            this.Controls.Add(this.radioButtonPackRaw);
            this.Controls.Add(this.radioButtonPackHttp);
            this.Controls.Add(this.textBoxLocalPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxRemotePort);
            this.Controls.Add(this.labelRemotePort);
            this.Controls.Add(this.radioButtonLogHigh);
            this.Controls.Add(this.radioButtonLogMedium);
            this.Controls.Add(this.radioButtonLogNone);
            this.Controls.Add(this.radioButtonHttp);
            this.Controls.Add(this.radioButtonRaw);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Name = "Form1";
            this.Text = "Sender Proxy Emulator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.RadioButton radioButtonRaw;
        private System.Windows.Forms.RadioButton radioButtonHttp;
        private System.Windows.Forms.RadioButton radioButtonLogNone;
        private System.Windows.Forms.RadioButton radioButtonLogMedium;
        private System.Windows.Forms.RadioButton radioButtonLogHigh;
        private System.Windows.Forms.Label labelRemotePort;
        private System.Windows.Forms.TextBox textBoxRemotePort;
        private System.Windows.Forms.TextBox textBoxLocalPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButtonPackHttp;
        private System.Windows.Forms.RadioButton radioButtonPackRaw;
        private System.Windows.Forms.RadioButton radioButtonLogHigh2;
        private System.Windows.Forms.RadioButton radioButtonLogHigh3;
        private System.Windows.Forms.Button buttonRefreshStatistics;
        private Statistics statistics1;
    }
}

