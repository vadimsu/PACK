namespace PackSenderTest
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
            this.buttonGo = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.textBoxMyPort = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxMyIp = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonClearChunks = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonGo
            // 
            this.buttonGo.Location = new System.Drawing.Point(22, 12);
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.Size = new System.Drawing.Size(75, 23);
            this.buttonGo.TabIndex = 1;
            this.buttonGo.Text = "Go";
            this.buttonGo.UseVisualStyleBackColor = true;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // textBoxMyPort
            // 
            this.textBoxMyPort.Location = new System.Drawing.Point(166, 83);
            this.textBoxMyPort.Name = "textBoxMyPort";
            this.textBoxMyPort.Size = new System.Drawing.Size(100, 20);
            this.textBoxMyPort.TabIndex = 15;
            this.textBoxMyPort.Text = "5775";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(166, 56);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "My port";
            // 
            // textBoxMyIp
            // 
            this.textBoxMyIp.Location = new System.Drawing.Point(22, 83);
            this.textBoxMyIp.Name = "textBoxMyIp";
            this.textBoxMyIp.Size = new System.Drawing.Size(131, 20);
            this.textBoxMyIp.TabIndex = 13;
            this.textBoxMyIp.Text = "192.168.2.102";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "My IP";
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(22, 135);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(37, 13);
            this.labelStatus.TabIndex = 16;
            this.labelStatus.Text = "Status";
            // 
            // buttonClearChunks
            // 
            this.buttonClearChunks.Location = new System.Drawing.Point(166, 12);
            this.buttonClearChunks.Name = "buttonClearChunks";
            this.buttonClearChunks.Size = new System.Drawing.Size(96, 23);
            this.buttonClearChunks.TabIndex = 23;
            this.buttonClearChunks.Text = "Clear chunks";
            this.buttonClearChunks.UseVisualStyleBackColor = true;
            this.buttonClearChunks.Click += new System.EventHandler(this.buttonClearChunks_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.buttonClearChunks);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.textBoxMyPort);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBoxMyIp);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonGo);
            this.Name = "Form1";
            this.Text = "Pack Sender Test";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonGo;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox textBoxMyPort;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxMyIp;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Button buttonClearChunks;
    }
}

