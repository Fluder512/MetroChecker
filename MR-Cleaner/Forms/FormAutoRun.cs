using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Windows.Forms;

namespace MR_Cleaner.Forms
{
    public partial class FormAutoRun : MetroFramework.Forms.MetroForm
    {
        private class AutoRunEntry
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Location { get; set; }
            public string RegistryKey { get; set; }
            public string ValueName { get; set; }
            public string TaskName { get; set; }
            public bool IsService { get; set; }
            public string ServiceName { get; set; }
        }

        private List<AutoRunEntry> entries = new List<AutoRunEntry>();
        Color selectColor = Color.FromArgb(200, 30, 30);

        public FormAutoRun()
        {
            InitializeComponent();
            SetupListView();
            this.Resize += (s, e) => ResizeColumns();
        }

        private void SetupListView()
        {
            autoRunList.OwnerDraw = true;
            autoRunList.DrawColumnHeader += DrawHeader;
            autoRunList.DrawItem += DrawItem;
            autoRunList.DrawSubItem += DrawSubItem;
            autoRunList.FullRowSelect = true;
            autoRunList.GridLines = false;
            autoRunList.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            отключитьToolStripMenuItem.Click += (s, e) => DisableSelected();
            удалитьToolStripMenuItem.Click += (s, e) => DeleteSelected();
            обновитьToolStripMenuItem.Click += (s, e) => LoadAutoRunEntries();
        }

        private void DrawHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 25)), e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, autoRunList.Font, e.Bounds, Color.White);
        }

        private void DrawItem(object sender, DrawListViewItemEventArgs e)
        {
        }

        private void DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Rectangle r = e.Bounds;
            bool selected = e.Item.Selected;

            if (selected)
                e.Graphics.FillRectangle(new SolidBrush(selectColor), r);
            else
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(17, 17, 17)), r);

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, autoRunList.Font, r, Color.White);
        }

        private void ResizeColumns()
        {
            int w = autoRunList.ClientSize.Width;
            autoRunList.Columns[0].Width = (int)(w * 0.25);
            autoRunList.Columns[1].Width = (int)(w * 0.25);
            autoRunList.Columns[2].Width = (int)(w * 0.50);
        }

        private void FormAutoRun_Load(object sender, EventArgs e)
        {
            LoadAutoRunEntries();
        }

        private void LoadAutoRunEntries()
        {
            entries.Clear();
            autoRunList.Items.Clear();

            ScanRegistryRunKeys();
            ScanStartupFolders();
            ScanScheduledTasks();
            ScanServices();
            ScanWinlogonKeys();

            foreach (var entry in entries)
            {
                var item = new ListViewItem(new[]
                {
                    entry.Name,
                    entry.Location,
                    entry.Path
                });

                item.Tag = entry;
                autoRunList.Items.Add(item);
            }

            ResizeColumns();
        }

        private void ScanRegistryRunKeys()
        {
            string[] runKeys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunServices",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunServicesOnce",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce"
            };

            foreach (string keyPath in runKeys)
            {
                ScanRegistryKey(Registry.LocalMachine, keyPath, "HKLM");
                ScanRegistryKey(Registry.CurrentUser, keyPath, "HKCU");
            }
        }

        private void ScanRegistryKey(RegistryKey baseKey, string subKey, string hiveName)
        {
            try
            {
                using (var key = baseKey.OpenSubKey(subKey))
                {
                    if (key == null) return;

                    foreach (string valueName in key.GetValueNames())
                    {
                        string value = key.GetValue(valueName)?.ToString();
                        if (value == null) continue;

                        entries.Add(new AutoRunEntry
                        {
                            Name = valueName,
                            Path = value,
                            Location = $"Реестр ({hiveName})",
                            RegistryKey = $@"{baseKey.Name}\{subKey}",
                            ValueName = valueName
                        });
                    }
                }
            }
            catch { }
        }

        private void ScanStartupFolders()
        {
            string[] paths =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };

            foreach (string path in paths)
            {
                if (!Directory.Exists(path)) continue;

                foreach (string file in Directory.GetFiles(path))
                {
                    entries.Add(new AutoRunEntry
                    {
                        Name = Path.GetFileName(file),
                        Path = file,
                        Location = "Папка автозагрузки"
                    });
                }
            }
        }

        private void ScanScheduledTasks()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ScheduledJob");
                foreach (ManagementObject task in searcher.Get())
                {
                    string cmd = task["CommandLine"]?.ToString();
                    if (string.IsNullOrEmpty(cmd)) continue;

                    entries.Add(new AutoRunEntry
                    {
                        Name = task["Name"]?.ToString(),
                        Path = cmd,
                        Location = "Планировщик задач"
                    });
                }
            }
            catch { }
        }

        private void ScanServices()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE StartMode='Auto'");
                foreach (ManagementObject svc in searcher.Get())
                {
                    string path = svc["PathName"]?.ToString();
                    if (string.IsNullOrEmpty(path)) continue;

                    entries.Add(new AutoRunEntry
                    {
                        Name = svc["DisplayName"]?.ToString(),
                        Path = path.Replace("\"", ""),
                        Location = "Служба (Авто)"
                    });
                }
            }
            catch { }
        }

        private void ScanWinlogonKeys()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    if (key == null) return;

                    string shell = key.GetValue("Shell")?.ToString();
                    if (!string.IsNullOrEmpty(shell) && shell.ToLower() != "explorer.exe")
                    {
                        entries.Add(new AutoRunEntry
                        {
                            Name = "Shell",
                            Path = shell,
                            Location = "Winlogon Shell"
                        });
                    }
                }
            }
            catch { }
        }

        private void DisableSelected()
        {
        }

        private void DeleteSelected()
        {
        }
    }
}
