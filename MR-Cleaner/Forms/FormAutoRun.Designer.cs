namespace MR_Cleaner.Forms
{
    partial class FormAutoRun
    {
        private System.ComponentModel.IContainer components = null;
        private MetroFramework.Controls.MetroListView autoRunList;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colLocation;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem отключитьToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem удалитьToolStripMenuItem;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAutoRun));
            this.autoRunList = new MetroFramework.Controls.MetroListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLocation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.отключитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.удалитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.обновитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // autoRunList
            // 
            this.autoRunList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.autoRunList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.autoRunList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.autoRunList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colLocation,
            this.colPath});
            this.autoRunList.ContextMenuStrip = this.contextMenu;
            this.autoRunList.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.autoRunList.ForeColor = System.Drawing.Color.White;
            this.autoRunList.FullRowSelect = true;
            this.autoRunList.Location = new System.Drawing.Point(20, 60);
            this.autoRunList.Name = "autoRunList";
            this.autoRunList.OwnerDraw = true;
            this.autoRunList.Size = new System.Drawing.Size(1374, 698);
            this.autoRunList.Style = MetroFramework.MetroColorStyle.Red;
            this.autoRunList.TabIndex = 1;
            this.autoRunList.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.autoRunList.UseCompatibleStateImageBehavior = false;
            this.autoRunList.UseSelectable = true;
            this.autoRunList.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            this.colName.Text = "Имя";
            // 
            // colLocation
            // 
            this.colLocation.Text = "Источник";
            // 
            // colPath
            // 
            this.colPath.Text = "Путь";
            // 
            // contextMenu
            // 
            this.contextMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(17)))), ((int)(((byte)(17)))), ((int)(((byte)(17)))));
            this.contextMenu.ForeColor = System.Drawing.Color.White;
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.отключитьToolStripMenuItem,
            this.удалитьToolStripMenuItem,
            this.toolStripSeparator1,
            this.обновитьToolStripMenuItem});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.ShowImageMargin = false;
            this.contextMenu.Size = new System.Drawing.Size(112, 76);
            // 
            // отключитьToolStripMenuItem
            // 
            this.отключитьToolStripMenuItem.Name = "отключитьToolStripMenuItem";
            this.отключитьToolStripMenuItem.Size = new System.Drawing.Size(111, 22);
            this.отключитьToolStripMenuItem.Text = "Отключить";
            // 
            // удалитьToolStripMenuItem
            // 
            this.удалитьToolStripMenuItem.Name = "удалитьToolStripMenuItem";
            this.удалитьToolStripMenuItem.Size = new System.Drawing.Size(111, 22);
            this.удалитьToolStripMenuItem.Text = "Удалить";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(108, 6);
            // 
            // обновитьToolStripMenuItem
            // 
            this.обновитьToolStripMenuItem.Name = "обновитьToolStripMenuItem";
            this.обновитьToolStripMenuItem.Size = new System.Drawing.Size(111, 22);
            this.обновитьToolStripMenuItem.Text = "Обновить";
            // 
            // FormAutoRun
            // 
            this.ClientSize = new System.Drawing.Size(1414, 788);
            this.Controls.Add(this.autoRunList);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormAutoRun";
            this.Style = MetroFramework.MetroColorStyle.Red;
            this.Text = "Автозагрузка";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.Load += new System.EventHandler(this.FormAutoRun_Load);
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
