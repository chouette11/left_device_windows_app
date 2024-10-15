namespace LeftDeviceWindows
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
            pictureBox1 = new PictureBox();
            label1 = new Label();
            menuStrip1 = new MenuStrip();
            表示ToolStripMenuItem = new ToolStripMenuItem();
            クライアントウィンドウToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(290, 58);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(200, 200);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(157, 305);
            label1.Name = "label1";
            label1.Size = new Size(489, 25);
            label1.TabIndex = 2;
            label1.Text = "スマホで左手デバイスのモバイルアプリでQRコードを読み取ってください";
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { 表示ToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 33);
            menuStrip1.TabIndex = 3;
            menuStrip1.Text = "menuStrip1";
            // 
            // 表示ToolStripMenuItem
            // 
            表示ToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { クライアントウィンドウToolStripMenuItem });
            表示ToolStripMenuItem.Name = "表示ToolStripMenuItem";
            表示ToolStripMenuItem.Size = new Size(64, 29);
            表示ToolStripMenuItem.Text = "表示";
            // 
            // クライアントウィンドウToolStripMenuItem
            // 
            クライアントウィンドウToolStripMenuItem.Name = "クライアントウィンドウToolStripMenuItem";
            クライアントウィンドウToolStripMenuItem.Size = new Size(270, 34);
            クライアントウィンドウToolStripMenuItem.Text = "クライアントウィンドウ";
            クライアントウィンドウToolStripMenuItem.Click += クライアントウィンドウToolStripMenuItem_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private PictureBox pictureBox1;
        private Label label1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem 表示ToolStripMenuItem;
        private ToolStripMenuItem クライアントウィンドウToolStripMenuItem;
    }
}
