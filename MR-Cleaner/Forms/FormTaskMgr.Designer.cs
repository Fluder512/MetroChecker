namespace MR_Cleaner.Forms
{
    partial class FormTaskMgr
    {
        private System.ComponentModel.IContainer components = null;
        private MetroFramework.Controls.MetroListView metroListView1;
        private MetroFramework.Controls.MetroTextBox metroTextBoxSearch;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem убитьToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem обновитьToolStripMenuItem;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTaskMgr));
            this.metroListView1 = new MetroFramework.Controls.MetroListView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.убитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.обновитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.metroTextBoxSearch = new MetroFramework.Controls.MetroTextBox();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // metroListView1
            // 
            this.metroListView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.metroListView1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.metroListView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.metroListView1.ContextMenuStrip = this.contextMenuStrip1;
            this.metroListView1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.metroListView1.ForeColor = System.Drawing.Color.White;
            this.metroListView1.FullRowSelect = true;
            this.metroListView1.Location = new System.Drawing.Point(23, 63);
            this.metroListView1.Name = "metroListView1";
            this.metroListView1.OwnerDraw = true;
            this.metroListView1.Size = new System.Drawing.Size(618, 430);
            this.metroListView1.TabIndex = 0;
            this.metroListView1.UseCompatibleStateImageBehavior = false;
            this.metroListView1.UseSelectable = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.contextMenuStrip1.ForeColor = System.Drawing.Color.White;
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.убитьToolStripMenuItem,
            this.toolStripSeparator1,
            this.обновитьToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.ShowImageMargin = false;
            this.contextMenuStrip1.Size = new System.Drawing.Size(104, 54);
            // 
            // убитьToolStripMenuItem
            // 
            this.убитьToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.убитьToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.убитьToolStripMenuItem.Name = "убитьToolStripMenuItem";
            this.убитьToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.убитьToolStripMenuItem.Text = "Убить";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(100, 6);
            // 
            // обновитьToolStripMenuItem
            // 
            this.обновитьToolStripMenuItem.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.обновитьToolStripMenuItem.ForeColor = System.Drawing.Color.White;
            this.обновитьToolStripMenuItem.Name = "обновитьToolStripMenuItem";
            this.обновитьToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.обновитьToolStripMenuItem.Text = "Обновить";
            // 
            // metroTextBoxSearch
            // 
            this.metroTextBoxSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.metroTextBoxSearch.CustomButton.Image = null;
            this.metroTextBoxSearch.CustomButton.Location = new System.Drawing.Point(596, 1);
            this.metroTextBoxSearch.CustomButton.Name = "";
            this.metroTextBoxSearch.CustomButton.Size = new System.Drawing.Size(21, 21);
            this.metroTextBoxSearch.CustomButton.Style = MetroFramework.MetroColorStyle.Blue;
            this.metroTextBoxSearch.CustomButton.TabIndex = 1;
            this.metroTextBoxSearch.CustomButton.Theme = MetroFramework.MetroThemeStyle.Light;
            this.metroTextBoxSearch.CustomButton.UseSelectable = true;
            this.metroTextBoxSearch.CustomButton.Visible = false;
            this.metroTextBoxSearch.Lines = new string[0];
            this.metroTextBoxSearch.Location = new System.Drawing.Point(23, 505);
            this.metroTextBoxSearch.MaxLength = 32767;
            this.metroTextBoxSearch.Name = "metroTextBoxSearch";
            this.metroTextBoxSearch.PasswordChar = '\0';
            this.metroTextBoxSearch.PromptText = "Поиск процесса...";
            this.metroTextBoxSearch.ScrollBars = System.Windows.Forms.ScrollBars.None;
            this.metroTextBoxSearch.SelectedText = "";
            this.metroTextBoxSearch.SelectionLength = 0;
            this.metroTextBoxSearch.SelectionStart = 0;
            this.metroTextBoxSearch.ShortcutsEnabled = true;
            this.metroTextBoxSearch.Size = new System.Drawing.Size(618, 23);
            this.metroTextBoxSearch.Style = MetroFramework.MetroColorStyle.Red;
            this.metroTextBoxSearch.TabIndex = 1;
            this.metroTextBoxSearch.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.metroTextBoxSearch.UseStyleColors = true;
            this.metroTextBoxSearch.WaterMark = "Поиск процесса...";
            this.metroTextBoxSearch.WaterMarkColor = System.Drawing.Color.FromArgb(((int)(((byte)(109)))), ((int)(((byte)(109)))), ((int)(((byte)(109)))));
            this.metroTextBoxSearch.WaterMarkFont = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Pixel);
            // 
            // FormTaskMgr
            // 
            this.ClientSize = new System.Drawing.Size(664, 560);
            this.Controls.Add(this.metroTextBoxSearch);
            this.Controls.Add(this.metroListView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormTaskMgr";
            this.Style = MetroFramework.MetroColorStyle.Red;
            this.Text = "Диспетчер задач";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
