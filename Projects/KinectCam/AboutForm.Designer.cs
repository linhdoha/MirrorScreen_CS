namespace KinectCam
{
    partial class AboutForm
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.gbAuthors = new System.Windows.Forms.GroupBox();
            this.tbAuthors = new System.Windows.Forms.TextBox();
            this.cbMirrored = new System.Windows.Forms.CheckBox();
            this.cbDesktop = new System.Windows.Forms.CheckBox();
            this.gbOptions = new System.Windows.Forms.GroupBox();
            this.gbAuthors.SuspendLayout();
            this.gbOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.titleLabel.Location = new System.Drawing.Point(16, 7);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(475, 47);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "KinectCam ver. 2.2 (BI Version) ";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gbAuthors
            // 
            this.gbAuthors.Controls.Add(this.tbAuthors);
            this.gbAuthors.Location = new System.Drawing.Point(16, 170);
            this.gbAuthors.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbAuthors.Name = "gbAuthors";
            this.gbAuthors.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbAuthors.Size = new System.Drawing.Size(475, 86);
            this.gbAuthors.TabIndex = 7;
            this.gbAuthors.TabStop = false;
            this.gbAuthors.Text = "Authors";
            // 
            // tbAuthors
            // 
            this.tbAuthors.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbAuthors.Location = new System.Drawing.Point(8, 23);
            this.tbAuthors.Margin = new System.Windows.Forms.Padding(0);
            this.tbAuthors.Multiline = true;
            this.tbAuthors.Name = "tbAuthors";
            this.tbAuthors.ReadOnly = true;
            this.tbAuthors.Size = new System.Drawing.Size(459, 44);
            this.tbAuthors.TabIndex = 0;
            this.tbAuthors.Text = "VirtualCam and BaseClass created by Maxim Kartavenkov aka Sonic\r\nKinect Driver as" +
    " WebCam created by Piotr Sowa";
            // 
            // cbMirrored
            // 
            this.cbMirrored.AutoSize = true;
            this.cbMirrored.Location = new System.Drawing.Point(8, 23);
            this.cbMirrored.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbMirrored.Name = "cbMirrored";
            this.cbMirrored.Size = new System.Drawing.Size(83, 21);
            this.cbMirrored.TabIndex = 5;
            this.cbMirrored.Text = "Mirrored";
            this.cbMirrored.UseVisualStyleBackColor = true;
            this.cbMirrored.CheckedChanged += new System.EventHandler(this.cbMirrored_CheckedChanged);
            // 
            // cbDesktop
            // 
            this.cbDesktop.AutoSize = true;
            this.cbDesktop.Location = new System.Drawing.Point(8, 52);
            this.cbDesktop.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbDesktop.Name = "cbDesktop";
            this.cbDesktop.Size = new System.Drawing.Size(82, 21);
            this.cbDesktop.TabIndex = 6;
            this.cbDesktop.Text = "Desktop";
            this.cbDesktop.UseVisualStyleBackColor = true;
            this.cbDesktop.CheckedChanged += new System.EventHandler(this.cbDesktop_CheckedChanged);
            // 
            // gbOptions
            // 
            this.gbOptions.Controls.Add(this.cbDesktop);
            this.gbOptions.Controls.Add(this.cbMirrored);
            this.gbOptions.Location = new System.Drawing.Point(16, 58);
            this.gbOptions.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbOptions.Name = "gbOptions";
            this.gbOptions.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.gbOptions.Size = new System.Drawing.Size(475, 105);
            this.gbOptions.TabIndex = 6;
            this.gbOptions.TabStop = false;
            this.gbOptions.Text = "Options (only for current session)";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(507, 271);
            this.Controls.Add(this.gbAuthors);
            this.Controls.Add(this.gbOptions);
            this.Controls.Add(this.titleLabel);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "AboutForm";
            this.Text = "About";
            this.Title = "About";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.gbAuthors.ResumeLayout(false);
            this.gbAuthors.PerformLayout();
            this.gbOptions.ResumeLayout(false);
            this.gbOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.GroupBox gbAuthors;
        private System.Windows.Forms.TextBox tbAuthors;
        private System.Windows.Forms.CheckBox cbMirrored;
        private System.Windows.Forms.CheckBox cbDesktop;
        private System.Windows.Forms.GroupBox gbOptions;
    }
}