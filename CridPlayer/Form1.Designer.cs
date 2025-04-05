namespace CridPlayer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            CridViewer = new LibVLCSharp.WinForms.VideoView();
            button1 = new Button();
            adxBtn = new Button();
            flyleafHost1 = new FlyleafLib.Controls.WinForms.FlyleafHost();
            richTextBox1 = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)CridViewer).BeginInit();
            SuspendLayout();
            // 
            // CridViewer
            // 
            CridViewer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            CridViewer.BackColor = Color.Black;
            CridViewer.Location = new Point(12, 59);
            CridViewer.MediaPlayer = null;
            CridViewer.Name = "CridViewer";
            CridViewer.Size = new Size(492, 347);
            CridViewer.TabIndex = 0;
            CridViewer.Text = "videoView1";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button1.Location = new Point(615, 393);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 1;
            button1.TabStop = false;
            button1.Text = "Load Crid";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // adxBtn
            // 
            adxBtn.Location = new Point(599, 26);
            adxBtn.Name = "adxBtn";
            adxBtn.Size = new Size(110, 23);
            adxBtn.TabIndex = 3;
            adxBtn.TabStop = false;
            adxBtn.Text = "Load Crid ADX";
            adxBtn.UseVisualStyleBackColor = true;
            adxBtn.Visible = false;
            adxBtn.Click += adxBtn_Click;
            // 
            // flyleafHost1
            // 
            flyleafHost1.AllowDrop = true;
            flyleafHost1.BackColor = Color.Transparent;
            flyleafHost1.DragMove = true;
            flyleafHost1.IsFullScreen = false;
            flyleafHost1.KeyBindings = true;
            flyleafHost1.Location = new Point(559, 90);
            flyleafHost1.Name = "flyleafHost1";
            flyleafHost1.OpenOnDrop = false;
            flyleafHost1.PanMoveOnCtrl = true;
            flyleafHost1.PanRotateOnShiftWheel = true;
            flyleafHost1.PanZoomOnCtrlWheel = true;
            flyleafHost1.Player = null;
            flyleafHost1.Size = new Size(150, 150);
            flyleafHost1.SwapDragEnterOnShift = true;
            flyleafHost1.SwapOnDrop = true;
            flyleafHost1.TabIndex = 4;
            flyleafHost1.ToggleFullScreenOnDoubleClick = true;
            flyleafHost1.Visible = false;
            // 
            // richTextBox1
            // 
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            richTextBox1.BackColor = SystemColors.InactiveCaptionText;
            richTextBox1.ForeColor = SystemColors.Window;
            richTextBox1.Location = new Point(529, 59);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(248, 193);
            richTextBox1.TabIndex = 5;
            richTextBox1.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(richTextBox1);
            Controls.Add(flyleafHost1);
            Controls.Add(adxBtn);
            Controls.Add(button1);
            Controls.Add(CridViewer);
            Name = "Form1";
            Text = "CRID Viewer";
            ((System.ComponentModel.ISupportInitialize)CridViewer).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private LibVLCSharp.WinForms.VideoView CridViewer;
        private Button button1;
        private Button adxBtn;
        private FlyleafLib.Controls.WinForms.FlyleafHost flyleafHost1;
        private RichTextBox richTextBox1;
    }
}
