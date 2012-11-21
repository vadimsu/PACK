namespace PackReceiverProxyEmulator
{
    partial class Statistics
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelPercentage = new System.Windows.Forms.Label();
            this.labelTotalSaved = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelTotalSent = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelTotalReceived = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.listBoxDebugInfo = new System.Windows.Forms.ListBox();
            this.buttonSaveAs = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // labelPercentage
            // 
            this.labelPercentage.AutoSize = true;
            this.labelPercentage.Location = new System.Drawing.Point(24, 257);
            this.labelPercentage.Name = "labelPercentage";
            this.labelPercentage.Size = new System.Drawing.Size(84, 13);
            this.labelPercentage.TabIndex = 39;
            this.labelPercentage.Text = "labelPercentage";
            // 
            // labelTotalSaved
            // 
            this.labelTotalSaved.AutoSize = true;
            this.labelTotalSaved.Location = new System.Drawing.Point(24, 222);
            this.labelTotalSaved.Name = "labelTotalSaved";
            this.labelTotalSaved.Size = new System.Drawing.Size(63, 13);
            this.labelTotalSaved.TabIndex = 38;
            this.labelTotalSaved.Text = "Total saved";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(19, 189);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 13);
            this.label5.TabIndex = 37;
            this.label5.Text = "Total saved";
            // 
            // labelTotalSent
            // 
            this.labelTotalSent.AutoSize = true;
            this.labelTotalSent.Location = new System.Drawing.Point(19, 160);
            this.labelTotalSent.Name = "labelTotalSent";
            this.labelTotalSent.Size = new System.Drawing.Size(50, 13);
            this.labelTotalSent.TabIndex = 36;
            this.labelTotalSent.Text = "total sent";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 125);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 35;
            this.label4.Text = "Total sent";
            // 
            // labelTotalReceived
            // 
            this.labelTotalReceived.AutoSize = true;
            this.labelTotalReceived.Location = new System.Drawing.Point(19, 89);
            this.labelTotalReceived.Name = "labelTotalReceived";
            this.labelTotalReceived.Size = new System.Drawing.Size(71, 13);
            this.labelTotalReceived.TabIndex = 34;
            this.labelTotalReceived.Text = "total received";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 59);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(75, 13);
            this.label6.TabIndex = 33;
            this.label6.Text = "Total received";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 36);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(49, 13);
            this.label7.TabIndex = 32;
            this.label7.Text = "Statistics";
            // 
            // listBoxDebugInfo
            // 
            this.listBoxDebugInfo.FormattingEnabled = true;
            this.listBoxDebugInfo.HorizontalScrollbar = true;
            this.listBoxDebugInfo.Location = new System.Drawing.Point(159, 4);
            this.listBoxDebugInfo.Name = "listBoxDebugInfo";
            this.listBoxDebugInfo.ScrollAlwaysVisible = true;
            this.listBoxDebugInfo.Size = new System.Drawing.Size(891, 290);
            this.listBoxDebugInfo.TabIndex = 40;
            // 
            // buttonSaveAs
            // 
            this.buttonSaveAs.Location = new System.Drawing.Point(22, 281);
            this.buttonSaveAs.Name = "buttonSaveAs";
            this.buttonSaveAs.Size = new System.Drawing.Size(75, 23);
            this.buttonSaveAs.TabIndex = 41;
            this.buttonSaveAs.Text = "Save As";
            this.buttonSaveAs.UseVisualStyleBackColor = true;
            this.buttonSaveAs.Click += new System.EventHandler(this.buttonSaveAs_Click);
            // 
            // Statistics
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonSaveAs);
            this.Controls.Add(this.listBoxDebugInfo);
            this.Controls.Add(this.labelPercentage);
            this.Controls.Add(this.labelTotalSaved);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.labelTotalSent);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.labelTotalReceived);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label7);
            this.Name = "Statistics";
            this.Size = new System.Drawing.Size(1075, 307);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelPercentage;
        private System.Windows.Forms.Label labelTotalSaved;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelTotalSent;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelTotalReceived;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ListBox listBoxDebugInfo;
        private System.Windows.Forms.Button buttonSaveAs;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}
