namespace CridPlayer
{
    partial class OpenCridForm
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
            CRIDListBox = new ListBox();
            DirectoryTxt = new TextBox();
            OpenFileBtn = new Button();
            FileDetailsTxt = new RichTextBox();
            OpenCridBtn = new Button();
            SavelistBtn = new Button();
            SuspendLayout();
            // 
            // CRIDListBox
            // 
            CRIDListBox.FormattingEnabled = true;
            CRIDListBox.ItemHeight = 15;
            CRIDListBox.Location = new Point(38, 86);
            CRIDListBox.Name = "CRIDListBox";
            CRIDListBox.Size = new Size(288, 334);
            CRIDListBox.TabIndex = 0;
            CRIDListBox.SelectedIndexChanged += CRIDListBox_SelectedIndexChanged;
            // 
            // DirectoryTxt
            // 
            DirectoryTxt.Location = new Point(38, 41);
            DirectoryTxt.Name = "DirectoryTxt";
            DirectoryTxt.Size = new Size(687, 23);
            DirectoryTxt.TabIndex = 1;
            DirectoryTxt.TextChanged += DirectoryTxt_TextChanged;
            // 
            // OpenFileBtn
            // 
            OpenFileBtn.Location = new Point(731, 41);
            OpenFileBtn.Name = "OpenFileBtn";
            OpenFileBtn.Size = new Size(36, 23);
            OpenFileBtn.TabIndex = 2;
            OpenFileBtn.Text = "...";
            OpenFileBtn.UseVisualStyleBackColor = true;
            OpenFileBtn.Click += OpenFileBtn_Click;
            // 
            // FileDetailsTxt
            // 
            FileDetailsTxt.Location = new Point(378, 86);
            FileDetailsTxt.Name = "FileDetailsTxt";
            FileDetailsTxt.Size = new Size(372, 299);
            FileDetailsTxt.TabIndex = 3;
            FileDetailsTxt.Text = "";
            // 
            // OpenCridBtn
            // 
            OpenCridBtn.Location = new Point(439, 401);
            OpenCridBtn.Name = "OpenCridBtn";
            OpenCridBtn.Size = new Size(75, 23);
            OpenCridBtn.TabIndex = 4;
            OpenCridBtn.Text = "Select";
            OpenCridBtn.UseVisualStyleBackColor = true;
            OpenCridBtn.Click += OpenCridBtn_Click;
            // 
            // SavelistBtn
            // 
            SavelistBtn.Location = new Point(572, 401);
            SavelistBtn.Name = "SavelistBtn";
            SavelistBtn.Size = new Size(153, 23);
            SavelistBtn.TabIndex = 5;
            SavelistBtn.Text = "Save current list to File";
            SavelistBtn.UseVisualStyleBackColor = true;
            SavelistBtn.Click += SavelistBtn_Click;
            // 
            // OpenCridForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(SavelistBtn);
            Controls.Add(OpenCridBtn);
            Controls.Add(FileDetailsTxt);
            Controls.Add(OpenFileBtn);
            Controls.Add(DirectoryTxt);
            Controls.Add(CRIDListBox);
            Name = "OpenCridForm";
            Text = "Open CRID";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox CRIDListBox;
        private TextBox DirectoryTxt;
        private Button OpenFileBtn;
        private RichTextBox FileDetailsTxt;
        private Button OpenCridBtn;
        private Button SavelistBtn;
    }
}