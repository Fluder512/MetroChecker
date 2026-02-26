using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
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

        public FormAutoRun()
        {
            InitializeComponent();
            this.ActiveControl = null;
            SetupDarkListView();
        }

        private void SetupDarkListView()
        {
            autoRunList.BackColor = Color.FromArgb(17, 17, 17);
            autoRunList.ForeColor = Color.FromArgb(200, 200, 200);
        }

        private void FormAutoRun_Load(object sender, EventArgs e)
        {
            LoadAutoRunEntries();
            this.ActiveControl = null;
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
                var item = new ListViewItem(new[] {
                    entry.Name.Length > 40 ? entry.Name.Substring(0, 40) + "..." : entry.Name,
                    entry.Path.Length > 60 ? entry.Path.Substring(0, 60) + "..." : entry.Path,
                    entry.Location
                });
                item.Tag = entry;
                item.BackColor = Color.FromArgb(17, 17, 17);
                item.ForeColor = Color.FromArgb(200, 200, 200);
                autoRunList.Items.Add(item);
            }

            this.ActiveControl = null;
        }

        private void ScanRegistryRunKeys()
        {
            string[] runKeys = new[]
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
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            object value = key.GetValue(valueName);
                            if (value != null)
                            {
                                string path = value.ToString();
                                string fileName = Path.GetFileNameWithoutExtension(path);
                                if (string.IsNullOrEmpty(fileName)) fileName = valueName;

                                entries.Add(new AutoRunEntry
                                {
                                    Name = fileName,
                                    Path = path,
                                    Location = $"Реестр ({hiveName})",
                                    RegistryKey = $@"{baseKey.Name}\{subKey}",
                                    ValueName = valueName
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanWinlogonKeys()
        {
            string winlogonPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(winlogonPath))
                {
                    if (key != null)
                    {
                        string userinit = key.GetValue("Userinit")?.ToString();
                        if (!string.IsNullOrEmpty(userinit))
                        {
                            string[] programs = userinit.Split(',');
                            foreach (string prog in programs)
                            {
                                string trimmed = prog.Trim();
                                if (!string.IsNullOrEmpty(trimmed) && !trimmed.ToLower().Contains("userinit.exe"))
                                {
                                    entries.Add(new AutoRunEntry
                                    {
                                        Name = Path.GetFileNameWithoutExtension(trimmed),
                                        Path = trimmed,
                                        Location = "Winlogon Userinit",
                                        RegistryKey = $@"HKLM\{winlogonPath}",
                                        ValueName = "Userinit"
                                    });
                                }
                            }
                        }

                        string shell = key.GetValue("Shell")?.ToString();
                        if (!string.IsNullOrEmpty(shell) && !shell.ToLower().Equals("explorer.exe"))
                        {
                            entries.Add(new AutoRunEntry
                            {
                                Name = Path.GetFileNameWithoutExtension(shell),
                                Path = shell,
                                Location = "Winlogon Shell",
                                RegistryKey = $@"HKLM\{winlogonPath}",
                                ValueName = "Shell"
                            });
                        }

                        string notify = key.GetValue("Notify")?.ToString();
                        if (!string.IsNullOrEmpty(notify))
                        {
                            entries.Add(new AutoRunEntry
                            {
                                Name = "Notify DLL",
                                Path = notify,
                                Location = "Winlogon Notify",
                                RegistryKey = $@"HKLM\{winlogonPath}",
                                ValueName = "Notify"
                            });
                        }

                        using (var notifyKey = key.OpenSubKey("Notify"))
                        {
                            if (notifyKey != null)
                            {
                                foreach (string subKeyName in notifyKey.GetSubKeyNames())
                                {
                                    using (var subKey = notifyKey.OpenSubKey(subKeyName))
                                    {
                                        if (subKey != null)
                                        {
                                            string dllName = subKey.GetValue("DLLName")?.ToString();
                                            if (!string.IsNullOrEmpty(dllName))
                                            {
                                                entries.Add(new AutoRunEntry
                                                {
                                                    Name = subKeyName,
                                                    Path = dllName,
                                                    Location = "Winlogon Notify SubKey",
                                                    RegistryKey = $@"HKLM\{winlogonPath}\Notify\{subKeyName}",
                                                    ValueName = "DLLName"
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanStartupFolders()
        {
            string[] startupPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup))
            };

            foreach (string startupPath in startupPaths)
            {
                if (Directory.Exists(startupPath))
                {
                    foreach (string file in Directory.GetFiles(startupPath, "*", SearchOption.TopDirectoryOnly))
                    {
                        entries.Add(new AutoRunEntry
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Path = file,
                            Location = "Папка автозагрузки"
                        });
                    }

                    foreach (string dir in Directory.GetDirectories(startupPath, "*", SearchOption.TopDirectoryOnly))
                    {
                        entries.Add(new AutoRunEntry
                        {
                            Name = Path.GetFileName(dir),
                            Path = dir,
                            Location = "Папка автозагрузки"
                        });
                    }
                }
            }
        }

        private void ScanScheduledTasks()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT Name, CommandLine, Description FROM Win32_ScheduledJob");
                foreach (ManagementObject task in searcher.Get())
                {
                    string cmd = task["CommandLine"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        entries.Add(new AutoRunEntry
                        {
                            Name = task["Name"]?.ToString() ?? "Unknown",
                            Path = cmd,
                            Location = "Планировщик задач",
                            TaskName = task["Name"]?.ToString()
                        });
                    }
                }
            }
            catch { }

            try
            {
                var taskSearcher = new ManagementObjectSearcher("root\\Microsoft\\Windows\\TaskScheduler", "SELECT * FROM MSFT_ScheduledTask");
                foreach (ManagementObject task in taskSearcher.Get())
                {
                    string taskName = task["TaskName"]?.ToString();
                    if (!string.IsNullOrEmpty(taskName) && !taskName.StartsWith("{"))
                    {
                        entries.Add(new AutoRunEntry
                        {
                            Name = taskName,
                            Path = "Task Scheduler",
                            Location = "Планировщик задач (WMI)",
                            TaskName = taskName
                        });
                    }
                }
            }
            catch { }
        }

        private void ScanServices()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT Name, DisplayName, PathName, StartMode FROM Win32_Service WHERE StartMode='Auto'");
                foreach (ManagementObject svc in searcher.Get())
                {
                    string path = svc["PathName"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(path) && path != "System32\\")
                    {
                        entries.Add(new AutoRunEntry
                        {
                            Name = svc["DisplayName"]?.ToString() ?? svc["Name"]?.ToString(),
                            Path = path.Replace("\"", ""),
                            Location = "Служба (Авто)",
                            IsService = true,
                            ServiceName = svc["Name"]?.ToString()
                        });
                    }
                }
            }
            catch { }
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            LoadAutoRunEntries();
        }

        private void disableBtn_Click(object sender, EventArgs e)
        {
            if (autoRunList.SelectedItems.Count == 0)
            {
                MetroFramework.MetroMessageBox.Show(this, "Выберите элемент для отключения.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.ActiveControl = null;
                return;
            }

            foreach (ListViewItem item in autoRunList.SelectedItems)
            {
                if (item.Tag is AutoRunEntry entry)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(entry.RegistryKey) && !string.IsNullOrEmpty(entry.ValueName))
                        {
                            if (entry.RegistryKey.Contains("Winlogon"))
                            {
                                if (entry.ValueName == "Userinit")
                                {
                                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
                                    {
                                        if (key != null)
                                        {
                                            string currentValue = key.GetValue("Userinit")?.ToString() ?? "";
                                            string[] programs = currentValue.Split(',');
                                            var filtered = programs.Where(p => !p.Trim().Equals(entry.Path.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
                                            key.SetValue("Userinit", string.Join(",", filtered), RegistryValueKind.String);
                                        }
                                    }
                                }
                                else if (entry.ValueName == "Shell")
                                {
                                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
                                    {
                                        if (key != null)
                                        {
                                            key.SetValue("Shell", "explorer.exe", RegistryValueKind.String);
                                        }
                                    }
                                }
                                else if (entry.ValueName == "Notify" || entry.Location.Contains("Notify"))
                                {
                                    using (var key = Registry.LocalMachine.OpenSubKey(entry.RegistryKey.Replace(@"HKLM\", ""), true))
                                    {
                                        if (key != null && !string.IsNullOrEmpty(entry.ValueName))
                                        {
                                            key.DeleteValue(entry.ValueName, false);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string keyPath = entry.RegistryKey;
                                RegistryKey baseKey = keyPath.StartsWith("HKEY_LOCAL_MACHINE") ? Registry.LocalMachine : Registry.CurrentUser;
                                string subKey = keyPath.Substring(keyPath.IndexOf('\\') + 1);

                                using (var key = baseKey.OpenSubKey(subKey, true))
                                {
                                    if (key != null)
                                    {
                                        key.DeleteValue(entry.ValueName, false);
                                    }
                                }
                            }
                        }
                        else if (entry.IsService && !string.IsNullOrEmpty(entry.ServiceName))
                        {
                            using (var sc = new ServiceController(entry.ServiceName))
                            {
                                if (sc.Status != ServiceControllerStatus.Stopped)
                                {
                                    sc.Stop();
                                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                                }
                            }
                            Process.Start("sc", $"config {entry.ServiceName} start= disabled");
                        }
                        else if (entry.Location == "Папка автозагрузки" && File.Exists(entry.Path))
                        {
                            string newPath = entry.Path + ".disabled";
                            File.Move(entry.Path, newPath);
                        }
                        else if (!string.IsNullOrEmpty(entry.TaskName) && entry.Location.Contains("Планировщик"))
                        {
                            Process.Start("schtasks", $"/delete /tn \"{entry.TaskName}\" /f");
                        }
                    }
                    catch (Exception ex)
                    {
                        MetroFramework.MetroMessageBox.Show(this, $"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            LoadAutoRunEntries();
            MetroFramework.MetroMessageBox.Show(this, "Элементы отключены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.ActiveControl = null;
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            if (autoRunList.SelectedItems.Count == 0)
            {
                MetroFramework.MetroMessageBox.Show(this, "Выберите элемент для удаления.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.ActiveControl = null;
                return;
            }

            DialogResult result = MetroFramework.MetroMessageBox.Show(this, "Вы уверены что хотите удалить выбранные элементы?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                this.ActiveControl = null;
                return;
            }

            foreach (ListViewItem item in autoRunList.SelectedItems)
            {
                if (item.Tag is AutoRunEntry entry)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(entry.RegistryKey) && !string.IsNullOrEmpty(entry.ValueName))
                        {
                            if (entry.RegistryKey.Contains("Winlogon"))
                            {
                                if (entry.ValueName == "Userinit")
                                {
                                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
                                    {
                                        if (key != null)
                                        {
                                            string currentValue = key.GetValue("Userinit")?.ToString() ?? "";
                                            string[] programs = currentValue.Split(',');
                                            var filtered = programs.Where(p => !p.Trim().Equals(entry.Path.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
                                            key.SetValue("Userinit", string.Join(",", filtered), RegistryValueKind.String);
                                        }
                                    }
                                }
                                else if (entry.ValueName == "Shell")
                                {
                                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
                                    {
                                        if (key != null)
                                        {
                                            key.SetValue("Shell", "explorer.exe", RegistryValueKind.String);
                                        }
                                    }
                                }
                                else if (entry.ValueName == "Notify" || entry.Location.Contains("Notify"))
                                {
                                    using (var key = Registry.LocalMachine.OpenSubKey(entry.RegistryKey.Replace(@"HKLM\", ""), true))
                                    {
                                        if (key != null && !string.IsNullOrEmpty(entry.ValueName))
                                        {
                                            key.DeleteValue(entry.ValueName, false);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string keyPath = entry.RegistryKey;
                                RegistryKey baseKey = keyPath.StartsWith("HKEY_LOCAL_MACHINE") ? Registry.LocalMachine : Registry.CurrentUser;
                                string subKey = keyPath.Substring(keyPath.IndexOf('\\') + 1);

                                using (var key = baseKey.OpenSubKey(subKey, true))
                                {
                                    if (key != null)
                                    {
                                        key.DeleteValue(entry.ValueName, false);
                                    }
                                }
                            }
                        }

                        if (File.Exists(entry.Path) && entry.Location == "Папка автозагрузки")
                        {
                            File.Delete(entry.Path);
                        }

                        if (!string.IsNullOrEmpty(entry.TaskName) && entry.Location.Contains("Планировщик"))
                        {
                            Process.Start("schtasks", $"/delete /tn \"{entry.TaskName}\" /f");
                        }
                    }
                    catch (Exception ex)
                    {
                        MetroFramework.MetroMessageBox.Show(this, $"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            LoadAutoRunEntries();
            MetroFramework.MetroMessageBox.Show(this, "Элементы удалены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.ActiveControl = null;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.ActiveControl = null;
        }
    }
}