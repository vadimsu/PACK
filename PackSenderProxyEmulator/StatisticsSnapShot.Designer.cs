namespace PackSenderProxyEmulator
{
    partial class StatisticsSnapShot
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
            this.statistics1 = new PackSenderProxyEmulator.Statistics();
            this.SuspendLayout();
            // 
            // statistics1
            // 
            this.statistics1.DebugMode = true;
            this.statistics1.Location = new System.Drawing.Point(12, 12);
            this.statistics1.Name = "statistics1";
            this.statistics1.Size = new System.Drawing.Size(901, 380);
            this.statistics1.TabIndex = 0;
            // 
            // StatisticsSnapShot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 404);
            this.Controls.Add(this.statistics1);
            this.Name = "StatisticsSnapShot";
            this.Text = "StatisticsSnapShot";
            this.ResumeLayout(false);

        }

        #endregion

        private Statistics statistics1;
    }
}