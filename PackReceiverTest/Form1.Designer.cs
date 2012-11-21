namespace PackReceiverTest
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
            this.buttonChooseFile = new System.Windows.Forms.Button();
            this.checkBoxPackMode = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.labelFirstAttempt = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelAverage = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labelAttempts = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxRemoteIp = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxRemotePort = new System.Windows.Forms.TextBox();
            this.buttonStop = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.labelStatus = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelFirstAttemptSent = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.labelFirstAttemptReceived = new System.Windows.Forms.Label();
            this.labelAverageReceived = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.labelAverageSent = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.buttonClearChunks = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonChooseFile
            // 
            this.buttonChooseFile.Location = new System.Drawing.Point(13, 13);
            this.buttonChooseFile.Name = "buttonChooseFile";
            this.buttonChooseFile.Size = new System.Drawing.Size(75, 23);
            this.buttonChooseFile.TabIndex = 0;
            this.buttonChooseFile.Text = "Choose file";
            this.buttonChooseFile.UseVisualStyleBackColor = true;
            this.buttonChooseFile.Click += new System.EventHandler(this.buttonChooseFile_Click);
            // 
            // checkBoxPackMode
            // 
            this.checkBoxPackMode.AutoSize = true;
            this.checkBoxPackMode.Checked = true;
            this.checkBoxPackMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxPackMode.Location = new System.Drawing.Point(106, 49);
            this.checkBoxPackMode.Name = "checkBoxPackMode";
            this.checkBoxPackMode.Size = new System.Drawing.Size(83, 17);
            this.checkBoxPackMode.TabIndex = 1;
            this.checkBoxPackMode.Text = "PACK mode";
            this.checkBoxPackMode.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 131);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "First attempt";
            // 
            // labelFirstAttempt
            // 
            this.labelFirstAttempt.AutoSize = true;
            this.labelFirstAttempt.Location = new System.Drawing.Point(15, 162);
            this.labelFirstAttempt.Name = "labelFirstAttempt";
            this.labelFirstAttempt.Size = new System.Drawing.Size(86, 13);
            this.labelFirstAttempt.TabIndex = 3;
            this.labelFirstAttempt.Text = "First attempt time";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(117, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Average";
            // 
            // labelAverage
            // 
            this.labelAverage.AutoSize = true;
            this.labelAverage.Location = new System.Drawing.Point(116, 161);
            this.labelAverage.Name = "labelAverage";
            this.labelAverage.Size = new System.Drawing.Size(47, 13);
            this.labelAverage.TabIndex = 5;
            this.labelAverage.Text = "Average";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(230, 131);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Attempts";
            // 
            // labelAttempts
            // 
            this.labelAttempts.AutoSize = true;
            this.labelAttempts.Location = new System.Drawing.Point(233, 160);
            this.labelAttempts.Name = "labelAttempts";
            this.labelAttempts.Size = new System.Drawing.Size(48, 13);
            this.labelAttempts.TabIndex = 7;
            this.labelAttempts.Text = "Attempts";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Remote IP";
            // 
            // textBoxRemoteIp
            // 
            this.textBoxRemoteIp.Location = new System.Drawing.Point(16, 78);
            this.textBoxRemoteIp.Name = "textBoxRemoteIp";
            this.textBoxRemoteIp.Size = new System.Drawing.Size(131, 20);
            this.textBoxRemoteIp.TabIndex = 9;
            this.textBoxRemoteIp.Text = "127.0.0.1";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(198, 51);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Remote port";
            // 
            // textBoxRemotePort
            // 
            this.textBoxRemotePort.Location = new System.Drawing.Point(201, 78);
            this.textBoxRemotePort.Name = "textBoxRemotePort";
            this.textBoxRemotePort.Size = new System.Drawing.Size(100, 20);
            this.textBoxRemotePort.TabIndex = 11;
            this.textBoxRemotePort.Text = "5775";
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(221, 12);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 12;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(12, 361);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(37, 13);
            this.labelStatus.TabIndex = 13;
            this.labelStatus.Text = "Status";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 204);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(29, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "Sent";
            // 
            // labelFirstAttemptSent
            // 
            this.labelFirstAttemptSent.AutoSize = true;
            this.labelFirstAttemptSent.Location = new System.Drawing.Point(13, 236);
            this.labelFirstAttemptSent.Name = "labelFirstAttemptSent";
            this.labelFirstAttemptSent.Size = new System.Drawing.Size(29, 13);
            this.labelFirstAttemptSent.TabIndex = 15;
            this.labelFirstAttemptSent.Text = "Sent";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 272);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 13);
            this.label7.TabIndex = 16;
            this.label7.Text = "Received";
            // 
            // labelFirstAttemptReceived
            // 
            this.labelFirstAttemptReceived.AutoSize = true;
            this.labelFirstAttemptReceived.Location = new System.Drawing.Point(12, 313);
            this.labelFirstAttemptReceived.Name = "labelFirstAttemptReceived";
            this.labelFirstAttemptReceived.Size = new System.Drawing.Size(53, 13);
            this.labelFirstAttemptReceived.TabIndex = 17;
            this.labelFirstAttemptReceived.Text = "Received";
            // 
            // labelAverageReceived
            // 
            this.labelAverageReceived.AutoSize = true;
            this.labelAverageReceived.Location = new System.Drawing.Point(193, 314);
            this.labelAverageReceived.Name = "labelAverageReceived";
            this.labelAverageReceived.Size = new System.Drawing.Size(53, 13);
            this.labelAverageReceived.TabIndex = 21;
            this.labelAverageReceived.Text = "Received";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(193, 273);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 13);
            this.label9.TabIndex = 20;
            this.label9.Text = "Received";
            // 
            // labelAverageSent
            // 
            this.labelAverageSent.AutoSize = true;
            this.labelAverageSent.Location = new System.Drawing.Point(194, 237);
            this.labelAverageSent.Name = "labelAverageSent";
            this.labelAverageSent.Size = new System.Drawing.Size(29, 13);
            this.labelAverageSent.TabIndex = 19;
            this.labelAverageSent.Text = "Sent";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(194, 205);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(29, 13);
            this.label11.TabIndex = 18;
            this.label11.Text = "Sent";
            // 
            // buttonClearChunks
            // 
            this.buttonClearChunks.Location = new System.Drawing.Point(119, 11);
            this.buttonClearChunks.Name = "buttonClearChunks";
            this.buttonClearChunks.Size = new System.Drawing.Size(96, 23);
            this.buttonClearChunks.TabIndex = 22;
            this.buttonClearChunks.Text = "Clear chunks";
            this.buttonClearChunks.UseVisualStyleBackColor = true;
            this.buttonClearChunks.Click += new System.EventHandler(this.buttonClearChunks_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(308, 383);
            this.Controls.Add(this.buttonClearChunks);
            this.Controls.Add(this.labelAverageReceived);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.labelAverageSent);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.labelFirstAttemptReceived);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.labelFirstAttemptSent);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.textBoxRemotePort);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxRemoteIp);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.labelAttempts);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.labelAverage);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelFirstAttempt);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxPackMode);
            this.Controls.Add(this.buttonChooseFile);
            this.Name = "Form1";
            this.Text = "Pack Receiver Test";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonChooseFile;
        private System.Windows.Forms.CheckBox checkBoxPackMode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelFirstAttempt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelAverage;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelAttempts;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxRemoteIp;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxRemotePort;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelFirstAttemptSent;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelFirstAttemptReceived;
        private System.Windows.Forms.Label labelAverageReceived;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label labelAverageSent;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button buttonClearChunks;
    }
}

