using Microsoft.Win32;
using MR_Cleaner.Forms;
using MR_Cleaner.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MR_Cleaner
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        public Form1()
        {
            InitializeComponent();
            this.ActiveControl = null;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ActiveControl = null;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.ActiveControl = null;
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (key != null)
                {
                    object value = key.GetValue("ConsentPromptBehaviorAdmin");
                    if (value != null)
                    {
                        int uacLevel = Convert.ToInt32(value);
                        if (uacLevel == 0)
                        {
                            key.SetValue("ConsentPromptBehaviorAdmin", 5, RegistryValueKind.DWord);
                            MetroFramework.MetroMessageBox.Show(this, "UAC был отключен. Установлено значение по умолчанию.", "UAC исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MetroFramework.MetroMessageBox.Show(this, "UAC настроен правильно.", "Проверка UAC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    key.Close();
                }
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("DisableTaskMgr");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("DisableTaskMgr");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("DisableTaskMgr", false);
                    if (keyLM != null) keyLM.DeleteValue("DisableTaskMgr", false);
                    MetroFramework.MetroMessageBox.Show(this, "Task Manager был отключен. Включено.", "TaskMgr исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Task Manager доступен.", "Проверка TaskMgr", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("DisableRegistryTools");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("DisableRegistryTools");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("DisableRegistryTools", false);
                    if (keyLM != null) keyLM.DeleteValue("DisableRegistryTools", false);
                    MetroFramework.MetroMessageBox.Show(this, "Regedit был отключен. Включено.", "Regedit исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Regedit доступен.", "Проверка Regedit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton4_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Policies\Microsoft\Windows\System", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("DisableCMD");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("DisableCMD");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("DisableCMD", false);
                    if (keyLM != null) keyLM.DeleteValue("DisableCMD", false);
                    MetroFramework.MetroMessageBox.Show(this, "CMD был отключен. Включено.", "CMD исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "CMD доступен.", "Проверка CMD", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton7_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("NoRun");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("NoRun");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("NoRun", false);
                    if (keyLM != null) keyLM.DeleteValue("NoRun", false);
                    MetroFramework.MetroMessageBox.Show(this, "Win + R был отключен. Включено.", "Win+R исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Win + R доступен.", "Проверка Win+R", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton8_Click(object sender, EventArgs e)
        {
            try
            {
                MetroFramework.MetroMessageBox.Show(this, "Запуск сканирования...\nЭто может занять несколько минут.", "MinerSearch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MinerSearch scanner = new MinerSearch();
                scanner.Scan();
                string report = scanner.GetReport();
                MetroFramework.MetroMessageBox.Show(this, report, "Результаты сканирования", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroLabel9_Click(object sender, EventArgs e)
        {
            FormAbout formAbout = new FormAbout();
            formAbout.ShowDialog();
        }

        private void metroButton9_Click(object sender, EventArgs e)
        {
            try
            {
                string result = "";
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", false);
                RegistryKey keyWow = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows", false);

                if (key != null)
                {
                    object appInitValue = key.GetValue("AppInit_DLLs");
                    object loadAppInitValue = key.GetValue("LoadAppInit_DLLs");

                    if (loadAppInitValue != null && Convert.ToInt32(loadAppInitValue) == 1)
                    {
                        if (appInitValue != null && !string.IsNullOrEmpty(appInitValue.ToString()))
                        {
                            result += $"AppInit_DLLs 64-бита: {appInitValue}\n";
                        }
                        else
                        {
                            result += "AppInit_DLLs 64-бита: нету\n";
                        }
                    }
                    else
                    {
                        result += "LoadAppInit_DLLs 64-бита: Отключен\n";
                    }
                    key.Close();
                }

                if (keyWow != null)
                {
                    object appInitValue = keyWow.GetValue("AppInit_DLLs");
                    object loadAppInitValue = keyWow.GetValue("LoadAppInit_DLLs");

                    if (loadAppInitValue != null && Convert.ToInt32(loadAppInitValue) == 1)
                    {
                        if (appInitValue != null && !string.IsNullOrEmpty(appInitValue.ToString()))
                        {
                            result += $"AppInit_DLLs 32-бита: {appInitValue}\n";
                        }
                        else
                        {
                            result += "AppInit_DLLs 32-бита: нету\n";
                        }
                    }
                    else
                    {
                        result += "LoadAppInit_DLLs 32-бита: Отключен\n";
                    }
                    keyWow.Close();
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = "Ошибка, не прочитал значение";
                }

                MetroFramework.MetroMessageBox.Show(this, result.Trim(), "Информация об AppInit_DLLs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            FormMBR formMBR = new FormMBR();
            formMBR.ShowDialog();
            this.ActiveControl = null;
        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            FormBackup formBackup = new FormBackup();
            formBackup.ShowDialog();
            this.ActiveControl = null;
        }

        private void metroButton11_Click(object sender, EventArgs e)
        {
            FormCPU formCPU = new FormCPU();
            formCPU.ShowDialog();
            this.ActiveControl = null;
        }

        private void metroButton10_Click(object sender, EventArgs e)
        {
            FormAutoRun formAutoRun = new FormAutoRun();
            formAutoRun.ShowDialog();
            this.ActiveControl = null;
        }

        private async void metroButton12_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult confirmResult = MetroFramework.MetroMessageBox.Show(
                    this,
                    "Запустить сканирование процессов?\n\n" +
                    "Поиск троянов в запущенных процессах\n" +
                    "Обнаружение RunPE и прочего\n" +
                    "Анализ оперативной памяти\n" +
                    "Проверка сетевых соединений\n\n",
                    "VCleaner - RunPE Scanner",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult != DialogResult.Yes)
                {
                    this.ActiveControl = null;
                    return;
                }

                DialogResult modeResult = MetroFramework.MetroMessageBox.Show(
                    this,
                    "Выберите режим сканирования:\n\n" +
                    "Intensive — интенсивная проверка\n" +
                    "Normal — экономный режим\n",
                    "Режим сканирования",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                bool intensiveMode = modeResult == DialogResult.Yes;

                MetroFramework.MetroMessageBox.Show(
                    this,
                    $"Сканирование процессов...\nРежим: {(intensiveMode ? "Intensive" : "Normal")}",
                    "VCleaner",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                VCleaner scanner = new VCleaner();

                await Task.Run(() => scanner.ScanProcessesOnly(removeThreats: true, intensiveMode: intensiveMode));

                string report = scanner.GetSummary();

                MetroFramework.MetroMessageBox.Show(
                    this,
                    report,
                    "VCleaner — Результаты",
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
            this.ActiveControl = null;
        }

        private async void metroButton13_Click(object sender, EventArgs e)
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

                var cleaner = new MemReduct();

                await Task.Run(() => cleaner.CleanMemory(includeSystem: false, cleanFileCache: true));

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

            this.ActiveControl = null;
        }

        private void metroButton14_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("NoControlPanel");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("NoControlPanel");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("NoControlPanel", false);
                    if (keyLM != null) keyLM.DeleteValue("NoControlPanel", false);
                    MetroFramework.MetroMessageBox.Show(this, "Панель управления была отключена. Включено.", "Панель управления исправлена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Панель управления доступна.", "Проверка панели управления", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton15_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("NoLogoff");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("NoLogoff");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("NoLogoff", false);
                    if (keyLM != null) keyLM.DeleteValue("NoLogoff", false);
                    MetroFramework.MetroMessageBox.Show(this, "Выход из системы был отключён. Включено.", "Logoff исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Выход из системы доступен.", "Проверка Logoff", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton16_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyCU = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
                bool disabled = false;
                if (keyCU != null)
                {
                    object val = keyCU.GetValue("NoClose");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (keyLM != null)
                {
                    object val = keyLM.GetValue("NoClose");
                    if (val != null && Convert.ToInt32(val) == 1) disabled = true;
                }
                if (disabled)
                {
                    if (keyCU != null) keyCU.DeleteValue("NoClose", false);
                    if (keyLM != null) keyLM.DeleteValue("NoClose", false);
                    MetroFramework.MetroMessageBox.Show(this, "Выключение ПК через Пуск было отключено. Включено.", "Выключение исправлено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Выключение ПК через Пуск доступно.", "Проверка выключения", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyCU != null) keyCU.Close();
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton17_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey keyLM = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\PowerManagement\Sleep", true);
                bool disabled = false;
                if (keyLM != null)
                {
                    object valAC = keyLM.GetValue("AllowStandbyStatesAC");
                    object valDC = keyLM.GetValue("AllowStandbyStatesDC");
                    if ((valAC != null && Convert.ToInt32(valAC) == 0) ||
                        (valDC != null && Convert.ToInt32(valDC) == 0))
                        disabled = true;
                }
                if (disabled)
                {
                    if (keyLM != null)
                    {
                        keyLM.DeleteValue("AllowStandbyStatesAC", false);
                        keyLM.DeleteValue("AllowStandbyStatesDC", false);
                    }
                    MetroFramework.MetroMessageBox.Show(this, "Режим сна был отключён. Включено.", "Сон исправлен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MetroFramework.MetroMessageBox.Show(this, "Режим сна доступен.", "Проверка режима сна", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (keyLM != null) keyLM.Close();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;

        }

        private void metroButton18_Click(object sender, EventArgs e)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR", true))
                {
                    if (key != null)
                    {
                        int value = (int)key.GetValue("Start", -1);
                        if (value != 3)
                        {
                            key.SetValue("Start", 3, Microsoft.Win32.RegistryValueKind.DWord);
                            MetroFramework.MetroMessageBox.Show(this, "USB был выключен, изменён на включён", "USB", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MetroFramework.MetroMessageBox.Show(this, "USB уже включён", "USB", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void metroButton19_Click(object sender, EventArgs e)
        {
            try
            {
                string[] paths = {
            Environment.ExpandEnvironmentVariables("%TEMP%"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch")
        };

                int count = 0;

                foreach (string path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (string file in Directory.GetFiles(path))
                        {
                            try
                            {
                                File.Delete(file);
                                count++;
                            }
                            catch { }
                        }
                    }
                }

                MetroFramework.MetroMessageBox.Show(this, $"Удалено файлов: {count}", "Очистка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.ActiveControl = null;
        }

        private void metroButton20_Click(object sender, EventArgs e)
        {
            FormTaskMgr formtaskMgr = new FormTaskMgr();
            formtaskMgr.ShowDialog(this);
            this.ActiveControl = null;
        }

        private void metroButton21_Click(object sender, EventArgs e)
        {
            FormNetstat formNetstat = new FormNetstat();
            formNetstat.ShowDialog(this);
            this.ActiveControl = null;
        }
    }
}