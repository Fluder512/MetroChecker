using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Management;

namespace MR_Cleaner.Forms
{
    public partial class FormBackup : MetroFramework.Forms.MetroForm
    {
        private Dictionary<string, string> driveMapping = new Dictionary<string, string>();

        public FormBackup()
        {
            InitializeComponent();
            this.ActiveControl = null;
        }

        private void FormBackup_Load(object sender, EventArgs e)
        {
            try
            {
                ManagementObjectSearcher diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject diskDrive in diskDriveSearcher.Get())
                {
                    string deviceId = diskDrive["DeviceID"].ToString();
                    string model = diskDrive["Model"].ToString();
                    string size = diskDrive["Size"].ToString();

                    string sizeGB = "Unknown";
                    if (!string.IsNullOrEmpty(size))
                    {
                        long bytes = long.Parse(size);
                        sizeGB = (bytes / (1024 * 1024 * 1024)).ToString() + " GB";
                    }

                    string displayText = $"{deviceId} - {model} ({sizeGB})";

                    ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");

                    foreach (ManagementObject partition in partitionSearcher.Get())
                    {
                        ManagementObjectSearcher logicalDiskSearcher = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");

                        foreach (ManagementObject logicalDisk in logicalDiskSearcher.Get())
                        {
                            string driveLetter = logicalDisk["DeviceID"].ToString();
                            displayText += $" [{driveLetter}]";
                        }
                    }

                    metroComboBox1.Items.Add(displayText);
                    driveMapping[displayText] = deviceId;
                }

                if (metroComboBox1.Items.Count > 0)
                {
                    metroComboBox1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка при получении списка дисков: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (metroComboBox1.SelectedIndex < 0)
                {
                    MetroFramework.MetroMessageBox.Show(this, "Выберите диск для backup.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.ActiveControl = null;
                    return;
                }

                string selectedItem = metroComboBox1.SelectedItem.ToString();

                if (!driveMapping.ContainsKey(selectedItem))
                {
                    MetroFramework.MetroMessageBox.Show(this, "Не удалось определить диск.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.ActiveControl = null;
                    return;
                }

                string devicePath = driveMapping[selectedItem];

                byte[] mbrData = new byte[512];
                int totalRead = 0;

                using (FileStream fs = new FileStream(devicePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    while (totalRead < 512)
                    {
                        byte[] buffer = new byte[512 - totalRead];
                        int bytesRead = fs.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        Buffer.BlockCopy(buffer, 0, mbrData, totalRead, bytesRead);
                        totalRead += bytesRead;
                    }
                }

                if (totalRead != 512)
                {
                    MetroFramework.MetroMessageBox.Show(this, $"Не удалось прочитать полный MBR. Прочитано байт: {totalRead}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.ActiveControl = null;
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                int driveNumber = GetDriveNumberFromDeviceId(devicePath);
                saveFileDialog.FileName = $"MBR_Backup_Drive{driveNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.smbr";
                saveFileDialog.Filter = "MBR Backup Files|*.smbr|All Files|*.*";
                saveFileDialog.Title = "Выберите место для сохранения backup";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllBytes(saveFileDialog.FileName, mbrData);
                    MetroFramework.MetroMessageBox.Show(this, $"Backup успешно создан:\n{saveFileDialog.FileName}\nРазмер: {mbrData.Length} байт", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MetroFramework.MetroMessageBox.Show(this, "Требуется запуск от имени администратора.\nЗапустите программу как Administrator.", "Ошибка доступа", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private int GetDriveNumberFromDeviceId(string deviceId)
        {
            try
            {
                string[] parts = deviceId.Split('\\');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].ToLower().Contains("physicaldrive") && i + 1 < parts.Length)
                    {
                        if (int.TryParse(parts[i + 1], out int number))
                        {
                            return number;
                        }
                    }
                }
            }
            catch { }
            return -1;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.ActiveControl = null;
        }
    }
}