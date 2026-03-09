using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MR_Cleaner.Utility;

namespace MR_Cleaner.Forms
{
    public partial class FormRAM : MetroFramework.Forms.MetroForm
    {
        private Timer updateTimer;
        private PerformanceCounter ramCounter;

        public FormRAM()
        {
            InitializeComponent();
            this.ActiveControl = null;
        }

        private void FormRAM_Load(object sender, EventArgs e)
        {
            ramLabel.ForeColor = Color.White;
            ramLabel.UseCustomForeColor = true;

            usedMbLabel.ForeColor = Color.White;
            usedMbLabel.UseCustomForeColor = true;

            totalMbLabel.ForeColor = Color.White;
            totalMbLabel.UseCustomForeColor = true;

            try
            {
                ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use", true);
                ramCounter.NextValue();

                updateTimer = new Timer();
                updateTimer.Interval = 1000;
                updateTimer.Tick += UpdateTimer_Tick;
                updateTimer.Start();

                UpdateRamInfo();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(
                    this,
                    "Ошибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            this.ActiveControl = null;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateRamInfo();
        }

        private void UpdateRamInfo()
        {
            float percent = 0f;

            try
            {
                percent = ramCounter.NextValue();
            }
            catch
            {
            }

            var info = new Microsoft.VisualBasic.Devices.ComputerInfo();

            ulong totalBytes = info.TotalPhysicalMemory;
            ulong availableBytes = info.AvailablePhysicalMemory;
            ulong usedBytes = totalBytes - availableBytes;

            long totalMb = (long)(totalBytes / 1024 / 1024);
            long usedMb = (long)(usedBytes / 1024 / 1024);
            long freeMb = (long)(availableBytes / 1024 / 1024);

            ramLabel.Text = $"Общая нагрузка ОЗУ: {percent:F0}%";
            usedMbLabel.Text = $"Используется: {usedMb:N0} MB | Свободно: {freeMb:N0} MB";
            totalMbLabel.Text = $"Всего памяти: {totalMb:N0} MB";
        }

        private async void cleanButton_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult confirmResult = MetroFramework.MetroMessageBox.Show(
                    this,
                    "Действительно ли вы хотите очистить оперативную память?",
                    "MemReduct",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult != DialogResult.Yes)
                {
                    this.ActiveControl = null;
                    return;
                }

                cleanButton.Enabled = false;

                var cleaner = new MemReduct();

                await Task.Run(() => cleaner.CleanMemory(includeSystem: false, cleanFileCache: true));

                UpdateRamInfo();

                string report = cleaner.GetSummary();

                MetroFramework.MetroMessageBox.Show(
                    this,
                    report,
                    "MemReduct",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(
                    this,
                    "Ошибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                cleanButton.Enabled = true;
            }

            this.ActiveControl = null;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.ActiveControl = null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Tick -= UpdateTimer_Tick;
                updateTimer.Dispose();
                updateTimer = null;
            }

            if (ramCounter != null)
            {
                ramCounter.Dispose();
                ramCounter = null;
            }

            base.OnFormClosing(e);
        }
    }
}