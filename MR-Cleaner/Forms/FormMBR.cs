using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MR_Cleaner.Forms
{
    public partial class FormMBR : MetroFramework.Forms.MetroForm
    {
        private Dictionary<string, string> driveMapping = new Dictionary<string, string>();

        public FormMBR()
        {
            InitializeComponent();
            this.ActiveControl = null;
        }

        private void FormMBR_Load(object sender, EventArgs e)
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
                    MetroFramework.MetroMessageBox.Show(this, "Выберите диск для восстановления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.ActiveControl = null;
                    return;
                }

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "MBR Backup Files|*.smbr|All Files|*.*";
                openFileDialog.Title = "Выберите файл backup MBR";

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                {
                    this.ActiveControl = null;
                    return;
                }

                byte[] mbrData = File.ReadAllBytes(openFileDialog.FileName);

                if (mbrData.Length != 512)
                {
                    MetroFramework.MetroMessageBox.Show(this, $"Неверный размер файла backup. Ожидается 512 байт, получено: {mbrData.Length}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.ActiveControl = null;
                    return;
                }

                DialogResult confirmResult = MetroFramework.MetroMessageBox.Show(this, "ВНИМАНИЕ! Это действие перезапишет MBR выбранного диска!\nПродолжить?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirmResult != DialogResult.Yes)
                {
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

                using (FileStream fs = new FileStream(devicePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    fs.Write(mbrData, 0, 512);
                }

                MetroFramework.MetroMessageBox.Show(this, $"MBR успешно восстановлен из:\n{openFileDialog.FileName}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.ActiveControl = null;
        }
    }
}