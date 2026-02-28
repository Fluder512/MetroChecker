namespace MR_Cleaner.Forms
{
    partial class FormNetstat
    {
        private System.ComponentModel.IContainer components = null;
        private MetroFramework.Controls.MetroListView metroListView1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
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
            this.metroListView1 = new MetroFramework.Controls.MetroListView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.обновитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();

            this.metroListView1.BackColor = System.Drawing.Color.FromArgb(17, 17, 17);
            this.metroListView1.ForeColor = System.Drawing.Color.White;
            this.metroListView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.metroListView1.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.metroListView1.FullRowSelect = true;
            this.metroListView1.Location = new System.Drawing.Point(23, 63);
            this.metroListView1.Name = "metroListView1";
            this.metroListView1.OwnerDraw = true;
            this.metroListView1.Size = new System.Drawing.Size(788, 487);
            this.metroListView1.Style = MetroFramework.MetroColorStyle.Red;
            this.metroListView1.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.metroListView1.UseCompatibleStateImageBehavior = false;
            this.metroListView1.UseSelectable = true;
            this.metroListView1.View = System.Windows.Forms.View.Details;
            this.metroListView1.ContextMenuStrip = this.contextMenuStrip1;
            this.metroListView1.Anchor = ((System.Windows.Forms.AnchorStyles)
                (((System.Windows.Forms.AnchorStyles.Top |
                   System.Windows.Forms.AnchorStyles.Bottom) |
                   System.Windows.Forms.AnchorStyles.Left) |
                   System.Windows.Forms.AnchorStyles.Right));

            this.contextMenuStrip1.BackColor = System.Drawing.Color.FromArgb(17, 17, 17);
            this.contextMenuStrip1.ForeColor = System.Drawing.Color.White;
            this.contextMenuStrip1.ShowImageMargin = false;
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.обновитьToolStripMenuItem});

            this.обновитьToolStripMenuItem.Text = "Обновить";

            this.ClientSize = new System.Drawing.Size(834, 573);
            this.Controls.Add(this.metroListView1);
            this.Name = "FormNetstat";
            this.Style = MetroFramework.MetroColorStyle.Red;
            this.Text = "Сетевые подключения";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;

            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
