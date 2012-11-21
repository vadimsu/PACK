namespace PackTesting
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.buttonOpenFile = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.checkBoxDoUntilStopped = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelFirstIteration = new System.Windows.Forms.Label();
            this.labelAverage = new System.Windows.Forms.Label();
            this.checkBoxPause = new System.Windows.Forms.CheckBox();
            this.labelIterations = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // buttonOpenFile
            // 
            this.buttonOpenFile.Location = new System.Drawing.Point(13, 30);
            this.buttonOpenFile.Name = "buttonOpenFile";
            this.buttonOpenFile.Size = new System.Drawing.Size(75, 23);
            this.buttonOpenFile.TabIndex = 0;
            this.buttonOpenFile.Text = "Open file";
            this.buttonOpenFile.UseVisualStyleBackColor = true;
            this.buttonOpenFile.Click += new System.EventHandler(this.buttonOpenFile_Click);
            // 
            // listView1
            // 
            this.listView1.Location = new System.Drawing.Point(13, 80);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(720, 170);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // checkBoxDoUntilStopped
            // 
            this.checkBoxDoUntilStopped.AutoSize = true;
            this.checkBoxDoUntilStopped.Location = new System.Drawing.Point(94, 35);
            this.checkBoxDoUntilStopped.Name = "checkBoxDoUntilStopped";
            this.checkBoxDoUntilStopped.Size = new System.Drawing.Size(103, 17);
            this.checkBoxDoUntilStopped.TabIndex = 2;
            this.checkBoxDoUntilStopped.Text = "Do until stopped";
            this.checkBoxDoUntilStopped.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(287, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "First iteration";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(487, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Average";
            // 
            // labelFirstIteration
            // 
            this.labelFirstIteration.AutoSize = true;
            this.labelFirstIteration.Location = new System.Drawing.Point(290, 39);
            this.labelFirstIteration.Name = "labelFirstIteration";
            this.labelFirstIteration.Size = new System.Drawing.Size(0, 13);
            this.labelFirstIteration.TabIndex = 5;
            this.labelFirstIteration.Tag = "finishStamp";
            // 
            // labelAverage
            // 
            this.labelAverage.AutoSize = true;
            this.labelAverage.Location = new System.Drawing.Point(490, 35);
            this.labelAverage.Name = "labelAverage";
            this.labelAverage.Size = new System.Drawing.Size(0, 13);
            this.labelAverage.TabIndex = 6;
            // 
            // checkBoxPause
            // 
            this.checkBoxPause.AutoSize = true;
            this.checkBoxPause.Location = new System.Drawing.Point(203, 34);
            this.checkBoxPause.Name = "checkBoxPause";
            this.checkBoxPause.Size = new System.Drawing.Size(56, 17);
            this.checkBoxPause.TabIndex = 7;
            this.checkBoxPause.Text = "Pause";
            this.checkBoxPause.UseVisualStyleBackColor = true;
            // 
            // labelIterations
            // 
            this.labelIterations.AutoSize = true;
            this.labelIterations.Location = new System.Drawing.Point(620, 36);
            this.labelIterations.Name = "labelIterations";
            this.labelIterations.Size = new System.Drawing.Size(0, 13);
            this.labelIterations.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(617, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Iterations";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(745, 262);
            this.Controls.Add(this.labelIterations);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.checkBoxPause);
            this.Controls.Add(this.labelAverage);
            this.Controls.Add(this.labelFirstIteration);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxDoUntilStopped);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.buttonOpenFile);
            this.Name = "Form1";
            this.Text = "PACK testing";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button buttonOpenFile;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.CheckBox checkBoxDoUntilStopped;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelFirstIteration;
        private System.Windows.Forms.Label labelAverage;
        private System.Windows.Forms.CheckBox checkBoxPause;
        private System.Windows.Forms.Label labelIterations;
        private System.Windows.Forms.Label label4;
    }
}

