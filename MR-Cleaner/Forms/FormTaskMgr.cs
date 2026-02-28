using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MR_Cleaner.Forms
{
    public partial class FormTaskMgr : MetroFramework.Forms.MetroForm
    {
        Color selectColor = Color.FromArgb(200, 30, 30);
        Process[] allProcesses;

        public FormTaskMgr()
        {
            InitializeComponent();

            metroListView1.Columns.Add("Имя процесса");
            metroListView1.Columns.Add("PID");
            metroListView1.Columns.Add("Память (MB)");
            metroListView1.Columns.Add("Приоритет");

            metroListView1.View = View.Details;
            metroListView1.OwnerDraw = true;
            metroListView1.FullRowSelect = true;
            metroListView1.GridLines = false;
            metroListView1.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            metroListView1.DrawColumnHeader += DrawHeader;
            metroListView1.DrawItem += DrawItem;
            metroListView1.DrawSubItem += DrawSubItem;

            обновитьToolStripMenuItem.Click += (s, e) => LoadProcesses();
            убитьToolStripMenuItem.Click += (s, e) => KillProcess();

            metroTextBoxSearch.TextChanged += (s, e) => ApplySearch();

            this.Resize += (s, e) => ResizeColumns();
            this.Load += FormTaskMgr_Load;
        }

        private void FormTaskMgr_Load(object sender, EventArgs e)
        {
            LoadProcesses();
        }

        void ResizeColumns()
        {
            int w = metroListView1.ClientSize.Width;
            metroListView1.Columns[0].Width = (int)(w * 0.45);
            metroListView1.Columns[1].Width = (int)(w * 0.15);
            metroListView1.Columns[2].Width = (int)(w * 0.20);
            metroListView1.Columns[3].Width = (int)(w * 0.20);
        }

        void DrawHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 25)), e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, metroListView1.Font, e.Bounds, Color.White);
        }

        void DrawItem(object sender, DrawListViewItemEventArgs e)
        {
        }

        void DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Rectangle r = e.Bounds;
            bool selected = e.Item.Selected;

            if (selected)
                e.Graphics.FillRectangle(new SolidBrush(selectColor), r);
            else
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(17, 17, 17)), r);

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, metroListView1.Font, r, Color.White);
        }

        void LoadProcesses()
        {
            allProcesses = Process.GetProcesses();
            FillList(allProcesses);
        }

        void FillList(Process[] list)
        {
            metroListView1.BeginUpdate();
            metroListView1.Items.Clear();

            foreach (var p in list)
            {
                try
                {
                    var item = new ListViewItem(p.ProcessName);
                    item.SubItems.Add(p.Id.ToString());
                    item.SubItems.Add((p.WorkingSet64 / 1024 / 1024).ToString());
                    item.SubItems.Add(p.PriorityClass.ToString());
                    metroListView1.Items.Add(item);
                }
                catch { }
            }

            metroListView1.EndUpdate();
            ResizeColumns();
        }

        void ApplySearch()
        {
            if (allProcesses == null) return;

            string q = metroTextBoxSearch.Text.Trim().ToLower();

            if (q == "")
            {
                FillList(allProcesses);
                return;
            }

            var filtered = allProcesses
                .Where(p => p.ProcessName.ToLower().Contains(q))
                .ToArray();

            FillList(filtered);
        }

        void KillProcess()
        {
            if (metroListView1.SelectedItems.Count == 0) return;

            int pid = int.Parse(metroListView1.SelectedItems[0].SubItems[1].Text);

            try
            {
                Process.GetProcessById(pid).Kill();
                LoadProcesses();
            }
            catch
            {
            }
        }
    }
}
