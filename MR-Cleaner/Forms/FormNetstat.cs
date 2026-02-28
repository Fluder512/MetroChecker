using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MR_Cleaner.Forms
{
    public partial class FormNetstat : MetroFramework.Forms.MetroForm
    {
        Color selectColor = Color.FromArgb(200, 30, 30);

        public FormNetstat()
        {
            InitializeComponent();
            SetupListView();
            this.Load += FormNetstat_Load;
            this.Resize += (s, e) => ResizeColumns();
            обновитьToolStripMenuItem.Click += (s, e) => LoadNetstat();
        }

        void FormNetstat_Load(object sender, EventArgs e)
        {
            LoadNetstat();
        }

        void SetupListView()
        {
            metroListView1.OwnerDraw = true;
            metroListView1.FullRowSelect = true;
            metroListView1.GridLines = false;
            metroListView1.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            metroListView1.DrawColumnHeader += DrawHeader;
            metroListView1.DrawItem += DrawItem;
            metroListView1.DrawSubItem += DrawSubItem;

            metroListView1.Columns.Add("Протокол");
            metroListView1.Columns.Add("Локальный адрес");
            metroListView1.Columns.Add("Внешний адрес");
            metroListView1.Columns.Add("Состояние");
            metroListView1.Columns.Add("PID");
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

        void ResizeColumns()
        {
            int w = metroListView1.ClientSize.Width;
            metroListView1.Columns[0].Width = (int)(w * 0.15);
            metroListView1.Columns[1].Width = (int)(w * 0.30);
            metroListView1.Columns[2].Width = (int)(w * 0.30);
            metroListView1.Columns[3].Width = (int)(w * 0.15);
            metroListView1.Columns[4].Width = (int)(w * 0.10);
        }

        void LoadNetstat()
        {
            metroListView1.BeginUpdate();
            metroListView1.Items.Clear();

            Process p = new Process();
            p.StartInfo.FileName = "netstat.exe";
            p.StartInfo.Arguments = "-ano";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var lines = output.Split('\n')
                .Where(l => l.Trim().StartsWith("TCP") || l.Trim().StartsWith("UDP"))
                .ToArray();

            foreach (var line in lines)
            {
                var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                string proto = parts[0];
                string local = parts[1];
                string remote = parts[2];
                string state = proto == "UDP" ? "-" : parts[3];
                string pid = proto == "UDP" ? parts[3] : parts[4];

                var item = new ListViewItem(proto);
                item.SubItems.Add(local);
                item.SubItems.Add(remote);
                item.SubItems.Add(state);
                item.SubItems.Add(pid);

                metroListView1.Items.Add(item);
            }

            metroListView1.EndUpdate();
            ResizeColumns();
        }
    }
}
