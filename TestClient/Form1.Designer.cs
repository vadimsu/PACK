namespace TestClient
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labelfirstIterationTime = new System.Windows.Forms.Label();
            this.labelAverageTime = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelfirstRxBytes = new System.Windows.Forms.Label();
            this.labelAverageRxBytes = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelReceivedBytes = new System.Windows.Forms.Label();
            this.labelReceivedOverallBytes = new System.Windows.Forms.Label();
            this.buttonStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(13, 13);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(89, 57);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "First iteration";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(275, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Average";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Time";
            // 
            // labelfirstIterationTime
            // 
            this.labelfirstIterationTime.AutoSize = true;
            this.labelfirstIterationTime.Location = new System.Drawing.Point(92, 83);
            this.labelfirstIterationTime.Name = "labelfirstIterationTime";
            this.labelfirstIterationTime.Size = new System.Drawing.Size(79, 13);
            this.labelfirstIterationTime.TabIndex = 4;
            this.labelfirstIterationTime.Text = "firstiterationtime";
            // 
            // labelAverageTime
            // 
            this.labelAverageTime.AutoSize = true;
            this.labelAverageTime.Location = new System.Drawing.Point(275, 83);
            this.labelAverageTime.Name = "labelAverageTime";
            this.labelAverageTime.Size = new System.Drawing.Size(79, 13);
            this.labelAverageTime.TabIndex = 5;
            this.labelAverageTime.Text = "firstiterationtime";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 128);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Rx bytes";
            // 
            // labelfirstRxBytes
            // 
            this.labelfirstRxBytes.AutoSize = true;
            this.labelfirstRxBytes.Location = new System.Drawing.Point(92, 127);
            this.labelfirstRxBytes.Name = "labelfirstRxBytes";
            this.labelfirstRxBytes.Size = new System.Drawing.Size(56, 13);
            this.labelfirstRxBytes.TabIndex = 7;
            this.labelfirstRxBytes.Text = "firstrxbytes";
            // 
            // labelAverageRxBytes
            // 
            this.labelAverageRxBytes.AutoSize = true;
            this.labelAverageRxBytes.Location = new System.Drawing.Point(278, 127);
            this.labelAverageRxBytes.Name = "labelAverageRxBytes";
            this.labelAverageRxBytes.Size = new System.Drawing.Size(79, 13);
            this.labelAverageRxBytes.TabIndex = 8;
            this.labelAverageRxBytes.Text = "averagerxbytes";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 173);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(48, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "received";
            // 
            // labelReceivedBytes
            // 
            this.labelReceivedBytes.AutoSize = true;
            this.labelReceivedBytes.Location = new System.Drawing.Point(92, 172);
            this.labelReceivedBytes.Name = "labelReceivedBytes";
            this.labelReceivedBytes.Size = new System.Drawing.Size(73, 13);
            this.labelReceivedBytes.TabIndex = 10;
            this.labelReceivedBytes.Text = "receivedbytes";
            // 
            // labelReceivedOverallBytes
            // 
            this.labelReceivedOverallBytes.AutoSize = true;
            this.labelReceivedOverallBytes.Location = new System.Drawing.Point(281, 171);
            this.labelReceivedOverallBytes.Name = "labelReceivedOverallBytes";
            this.labelReceivedOverallBytes.Size = new System.Drawing.Size(79, 13);
            this.labelReceivedOverallBytes.TabIndex = 11;
            this.labelReceivedOverallBytes.Text = "receivedoverall";
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(111, 12);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 12;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(371, 233);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.labelReceivedOverallBytes);
            this.Controls.Add(this.labelReceivedBytes);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.labelAverageRxBytes);
            this.Controls.Add(this.labelfirstRxBytes);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.labelAverageTime);
            this.Controls.Add(this.labelfirstIterationTime);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonStart);
            this.Name = "Form1";
            this.Text = "Test client";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelfirstIterationTime;
        private System.Windows.Forms.Label labelAverageTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelfirstRxBytes;
        private System.Windows.Forms.Label labelAverageRxBytes;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelReceivedBytes;
        private System.Windows.Forms.Label labelReceivedOverallBytes;
        private System.Windows.Forms.Button buttonStop;
    }
}

