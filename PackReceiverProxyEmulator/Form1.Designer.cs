namespace PackReceiverProxyEmulator
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
            this.radioButtonLogNone = new System.Windows.Forms.RadioButton();
            this.radioButtonLogMedium = new System.Windows.Forms.RadioButton();
            this.radioButtonLogHigh = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.textBoxRemoteIp = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxLocalPort = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxPackMode = new System.Windows.Forms.CheckBox();
            this.radioButtonLogHigh2 = new System.Windows.Forms.RadioButton();
            this.radioButtonLogHigh3 = new System.Windows.Forms.RadioButton();
            this.buttonRefreshStatistics = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.buttonFlush = new System.Windows.Forms.Button();
            this.statistics1 = new PackReceiverProxyEmulator.Statistics();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(13, 28);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(135, 27);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 1;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // radioButtonLogNone
            // 
            this.radioButtonLogNone.AutoSize = true;
            this.radioButtonLogNone.Location = new System.Drawing.Point(12, 199);
            this.radioButtonLogNone.Name = "radioButtonLogNone";
            this.radioButtonLogNone.Size = new System.Drawing.Size(70, 17);
            this.radioButtonLogNone.TabIndex = 2;
            this.radioButtonLogNone.Text = "Log none";
            this.radioButtonLogNone.UseVisualStyleBackColor = true;
            this.radioButtonLogNone.CheckedChanged += new System.EventHandler(this.radioButtonLogNone_CheckedChanged);
            // 
            // radioButtonLogMedium
            // 
            this.radioButtonLogMedium.AutoSize = true;
            this.radioButtonLogMedium.Location = new System.Drawing.Point(12, 222);
            this.radioButtonLogMedium.Name = "radioButtonLogMedium";
            this.radioButtonLogMedium.Size = new System.Drawing.Size(82, 17);
            this.radioButtonLogMedium.TabIndex = 3;
            this.radioButtonLogMedium.Text = "Log medium";
            this.radioButtonLogMedium.UseVisualStyleBackColor = true;
            this.radioButtonLogMedium.CheckedChanged += new System.EventHandler(this.radioButtonLogMedium_CheckedChanged);
            // 
            // radioButtonLogHigh
            // 
            this.radioButtonLogHigh.AutoSize = true;
            this.radioButtonLogHigh.Location = new System.Drawing.Point(12, 245);
            this.radioButtonLogHigh.Name = "radioButtonLogHigh";
            this.radioButtonLogHigh.Size = new System.Drawing.Size(66, 17);
            this.radioButtonLogHigh.TabIndex = 4;
            this.radioButtonLogHigh.Text = "Log high";
            this.radioButtonLogHigh.UseVisualStyleBackColor = true;
            this.radioButtonLogHigh.CheckedChanged += new System.EventHandler(this.radioButtonLogHigh_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Port";
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(135, 61);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(100, 20);
            this.textBoxPort.TabIndex = 6;
            this.textBoxPort.Text = "7777";
            // 
            // textBoxRemoteIp
            // 
            this.textBoxRemoteIp.Location = new System.Drawing.Point(135, 97);
            this.textBoxRemoteIp.Name = "textBoxRemoteIp";
            this.textBoxRemoteIp.Size = new System.Drawing.Size(100, 20);
            this.textBoxRemoteIp.TabIndex = 8;
            this.textBoxRemoteIp.Text = "127.0.0.1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 104);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Remote IP";
            // 
            // textBoxLocalPort
            // 
            this.textBoxLocalPort.Location = new System.Drawing.Point(135, 139);
            this.textBoxLocalPort.Name = "textBoxLocalPort";
            this.textBoxLocalPort.Size = new System.Drawing.Size(100, 20);
            this.textBoxLocalPort.TabIndex = 10;
            this.textBoxLocalPort.Text = "6666";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 146);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Local Port";
            // 
            // checkBoxPackMode
            // 
            this.checkBoxPackMode.AutoSize = true;
            this.checkBoxPackMode.Location = new System.Drawing.Point(135, 185);
            this.checkBoxPackMode.Name = "checkBoxPackMode";
            this.checkBoxPackMode.Size = new System.Drawing.Size(83, 17);
            this.checkBoxPackMode.TabIndex = 11;
            this.checkBoxPackMode.Text = "PACK mode";
            this.checkBoxPackMode.UseVisualStyleBackColor = true;
            this.checkBoxPackMode.CheckedChanged += new System.EventHandler(this.checkBoxPackMode_CheckedChanged);
            // 
            // radioButtonLogHigh2
            // 
            this.radioButtonLogHigh2.AutoCheck = false;
            this.radioButtonLogHigh2.AutoSize = true;
            this.radioButtonLogHigh2.Location = new System.Drawing.Point(13, 268);
            this.radioButtonLogHigh2.Name = "radioButtonLogHigh2";
            this.radioButtonLogHigh2.Size = new System.Drawing.Size(72, 17);
            this.radioButtonLogHigh2.TabIndex = 12;
            this.radioButtonLogHigh2.Text = "Log high2";
            this.radioButtonLogHigh2.UseVisualStyleBackColor = true;
            // 
            // radioButtonLogHigh3
            // 
            this.radioButtonLogHigh3.AutoCheck = false;
            this.radioButtonLogHigh3.AutoSize = true;
            this.radioButtonLogHigh3.Location = new System.Drawing.Point(12, 291);
            this.radioButtonLogHigh3.Name = "radioButtonLogHigh3";
            this.radioButtonLogHigh3.Size = new System.Drawing.Size(72, 17);
            this.radioButtonLogHigh3.TabIndex = 13;
            this.radioButtonLogHigh3.Text = "Log high3";
            this.radioButtonLogHigh3.UseVisualStyleBackColor = true;
            // 
            // buttonRefreshStatistics
            // 
            this.buttonRefreshStatistics.Location = new System.Drawing.Point(344, 310);
            this.buttonRefreshStatistics.Name = "buttonRefreshStatistics";
            this.buttonRefreshStatistics.Size = new System.Drawing.Size(198, 23);
            this.buttonRefreshStatistics.TabIndex = 25;
            this.buttonRefreshStatistics.Text = "Show outstanding transactions";
            this.buttonRefreshStatistics.UseVisualStyleBackColor = true;
            this.buttonRefreshStatistics.Click += new System.EventHandler(this.buttonRefreshStatistics_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(135, 236);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(62, 13);
            this.label8.TabIndex = 30;
            this.label8.Text = "Precentage";
            // 
            // buttonFlush
            // 
            this.buttonFlush.Location = new System.Drawing.Point(135, 291);
            this.buttonFlush.Name = "buttonFlush";
            this.buttonFlush.Size = new System.Drawing.Size(75, 23);
            this.buttonFlush.TabIndex = 32;
            this.buttonFlush.Text = "Flush";
            this.buttonFlush.UseVisualStyleBackColor = true;
            this.buttonFlush.Click += new System.EventHandler(this.buttonFlush_Click);
            // 
            // statistics1
            // 
            this.statistics1.DebugMode = true;
            this.statistics1.Location = new System.Drawing.Point(279, -1);
            this.statistics1.Name = "statistics1";
            this.statistics1.Size = new System.Drawing.Size(1077, 305);
            this.statistics1.TabIndex = 31;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1362, 336);
            this.Controls.Add(this.buttonFlush);
            this.Controls.Add(this.statistics1);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.buttonRefreshStatistics);
            this.Controls.Add(this.radioButtonLogHigh3);
            this.Controls.Add(this.radioButtonLogHigh2);
            this.Controls.Add(this.checkBoxPackMode);
            this.Controls.Add(this.textBoxLocalPort);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxRemoteIp);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.radioButtonLogHigh);
            this.Controls.Add(this.radioButtonLogMedium);
            this.Controls.Add(this.radioButtonLogNone);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Name = "Form1";
            this.Text = "Receiver Proxy Emulator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.RadioButton radioButtonLogNone;
        private System.Windows.Forms.RadioButton radioButtonLogMedium;
        private System.Windows.Forms.RadioButton radioButtonLogHigh;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.TextBox textBoxRemoteIp;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxLocalPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxPackMode;
        private System.Windows.Forms.RadioButton radioButtonLogHigh2;
        private System.Windows.Forms.RadioButton radioButtonLogHigh3;
        private System.Windows.Forms.Button buttonRefreshStatistics;
        private System.Windows.Forms.Label label8;
        private Statistics statistics1;
        private System.Windows.Forms.Button buttonFlush;
    }
}

