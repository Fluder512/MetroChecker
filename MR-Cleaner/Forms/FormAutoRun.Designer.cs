namespace MR_Cleaner.Forms
{
    partial class FormAutoRun
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.autoRunList = new MetroFramework.Controls.MetroListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colLocation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.refreshBtn = new MetroFramework.Controls.MetroButton();
            this.disableBtn = new MetroFramework.Controls.MetroButton();
            this.deleteBtn = new MetroFramework.Controls.MetroButton();
            this.SuspendLayout();
            // 
            // autoRunList
            // 
            this.autoRunList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colPath,
            this.colLocation});
            this.autoRunList.Dock = System.Windows.Forms.DockStyle.Top;
            this.autoRunList.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.autoRunList.FullRowSelect = true;
            this.autoRunList.Location = new System.Drawing.Point(20, 60);
            this.autoRunList.Name = "autoRunList";
            this.autoRunList.OwnerDraw = true;
            this.autoRunList.Size = new System.Drawing.Size(760, 300);
            this.autoRunList.Style = MetroFramework.MetroColorStyle.Orange;
            this.autoRunList.TabIndex = 0;
            this.autoRunList.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.autoRunList.UseCompatibleStateImageBehavior = false;
            this.autoRunList.UseSelectable = true;
            this.autoRunList.View = System.Windows.Forms.View.Details;
            this.autoRunList.BackColor = System.Drawing.Color.FromArgb(17, 17, 17);
            this.autoRunList.ForeColor = System.Drawing.Color.FromArgb(200, 200, 200);
            // 
            // colName
            // 
            this.colName.Text = "Имя файла";
            this.colName.Width = 200;
            // 
            // colPath
            // 
            this.colPath.Text = "Путь к файлу";
            this.colPath.Width = 350;
            // 
            // colLocation
            // 
            this.colLocation.Text = "Где находится";
            this.colLocation.Width = 200;
            // 
            // refreshBtn
            // 
            this.refreshBtn.Location = new System.Drawing.Point(23, 370);
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(120, 35);
            this.refreshBtn.TabIndex = 1;
            this.refreshBtn.Text = "Обновить";
            this.refreshBtn.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.refreshBtn.UseSelectable = true;
            this.refreshBtn.Click += new System.EventHandler(this.refreshBtn_Click);
            // 
            // disableBtn
            // 
            this.disableBtn.Location = new System.Drawing.Point(153, 370);
            this.disableBtn.Name = "disableBtn";
            this.disableBtn.Size = new System.Drawing.Size(120, 35);
            this.disableBtn.TabIndex = 2;
            this.disableBtn.Text = "Отключить";
            this.disableBtn.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.disableBtn.UseSelectable = true;
            this.disableBtn.Click += new System.EventHandler(this.disableBtn_Click);
            // 
            // deleteBtn
            // 
            this.deleteBtn.Location = new System.Drawing.Point(283, 370);
            this.deleteBtn.Name = "deleteBtn";
            this.deleteBtn.Size = new System.Drawing.Size(120, 35);
            this.deleteBtn.TabIndex = 3;
            this.deleteBtn.Text = "Удалить";
            this.deleteBtn.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.deleteBtn.UseSelectable = true;
            this.deleteBtn.Click += new System.EventHandler(this.deleteBtn_Click);
            // 
            // FormAutoRun
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 420);
            this.Controls.Add(this.deleteBtn);
            this.Controls.Add(this.disableBtn);
            this.Controls.Add(this.refreshBtn);
            this.Controls.Add(this.autoRunList);
            this.Name = "FormAutoRun";
            this.Style = MetroFramework.MetroColorStyle.Orange;
            this.Text = "Автозагрузка";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.Load += new System.EventHandler(this.FormAutoRun_Load);
            this.ResumeLayout(false);
        }

        #endregion

        private MetroFramework.Controls.MetroListView autoRunList;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ColumnHeader colLocation;
        private MetroFramework.Controls.MetroButton refreshBtn;
        private MetroFramework.Controls.MetroButton disableBtn;
        private MetroFramework.Controls.MetroButton deleteBtn;
    }
}