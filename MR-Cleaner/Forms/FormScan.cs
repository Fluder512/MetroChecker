using MR_Cleaner.Utility;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MR_Cleaner.Forms
{
    public partial class FormScan : MetroFramework.Forms.MetroForm
    {
        private readonly Scanner _scanner = new Scanner();
        private readonly VCleaner _vCleaner = new VCleaner();
        private readonly MinerSearch _minerSearch = new MinerSearch();
        private readonly MemReduct _memReduct = new MemReduct();

        private CancellationTokenSource _progressCts;

        public FormScan()
        {
            InitializeComponent();
            this.ActiveControl = null;
            metroButton1.UseSelectable = false;
        }

        private async void metroButton1_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            metroButton1.Enabled = false;
            metroButton1.Text = "Сканирование...";
            metroProgressBar1.Value = 0;

            _progressCts = new CancellationTokenSource();
            var progressTask = AnimateProgressBar(_progressCts.Token);

            Scanner.SystemScanResult scanResult = null;
            long deletedFiles = 0;
            long deletedBytes = 0;
            long failedFiles = 0;

            await Task.Run(() =>
            {
                scanResult = _scanner.Scan(false);
                _scanner.FixAll();

                BotKiller.Execute();

                _vCleaner.ScanProcessesOnly(removeThreats: true, intensiveMode: true);

                _minerSearch.Scan();

                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? winDir;

                string[] folders =
                {
                    Path.GetTempPath(),
                    Path.Combine(winDir, "Temp"),
                    Path.Combine(winDir, "Prefetch"),
                    Path.Combine(winDir, "SoftwareDistribution", "Download"),
                    Path.Combine(localAppData, "Temp"),
                    Path.Combine(localAppData, "Microsoft", "Windows", "INetCache"),
                    Path.Combine(localAppData, "Microsoft", "Windows", "Temporary Internet Files"),
                    Path.Combine(localAppData, "Microsoft", "Windows", "WER"),
                    Path.Combine(localAppData, "CrashDumps"),
                    Path.Combine(systemRoot, "Logs"),
                };

                Parallel.ForEach(folders, folder =>
                {
                    if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;
                    try
                    {
                        foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                        {
                            try
                            {
                                long size = new FileInfo(file).Length;
                                File.Delete(file);
                                Interlocked.Increment(ref deletedFiles);
                                Interlocked.Add(ref deletedBytes, size);
                            }
                            catch
                            {
                                Interlocked.Increment(ref failedFiles);
                            }
                        }

                        foreach (string dir in Directory.GetDirectories(folder))
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                });

                _memReduct.CleanMemory(includeSystem: false, cleanFileCache: true);
            });

            _progressCts.Cancel();
            await progressTask;

            metroProgressBar1.Value = 100;

            var sb = new StringBuilder();
            sb.AppendLine($"Дата сканирования: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("=== Системные ограничения ===");
            bool hasRestrictions =
                !scanResult.TaskManagerEnabled ||
                !scanResult.RegistryEditorEnabled ||
                !scanResult.CommandPromptEnabled ||
                !scanResult.RunDialogEnabled ||
                !scanResult.ControlPanelEnabled ||
                !scanResult.ShutdownFromStartEnabled ||
                !scanResult.MmcEnabled ||
                !scanResult.ViewOnDriveEnabled ||
                !scanResult.DrivesVisibleEnabled ||
                !scanResult.FindEnabled ||
                !scanResult.ContextMenuEnabled ||
                !scanResult.FolderOptionsEnabled ||
                !scanResult.SecurityTabEnabled ||
                !scanResult.FileMenuEnabled ||
                !scanResult.CommonGroupsEnabled ||
                !scanResult.StartMenuLogoffEnabled ||
                !scanResult.ChangingWallpaperEnabled ||
                !scanResult.WinKeysEnabled ||
                !scanResult.SetTaskbarEnabled ||
                !scanResult.LockWorkstationEnabled ||
                !scanResult.ChangePasswordEnabled ||
                !scanResult.TrayContextMenuEnabled ||
                !scanResult.GroupPolicyUpdateEnabled ||
                !scanResult.PowerOptionsEnabled ||
                !scanResult.ContextMenusInStartEnabled ||
                !scanResult.SystemRestoreEnabled ||
                !scanResult.SystemConfigEnabled ||
                !scanResult.LogoffEnabled ||
                !scanResult.UsbStorEnabled;

            if (hasRestrictions)
            {
                sb.AppendLine("Обнаружены и сняты системные ограничения:");
                if (!scanResult.TaskManagerEnabled) sb.AppendLine("  - Диспетчер задач был отключён");
                if (!scanResult.RegistryEditorEnabled) sb.AppendLine("  - Regedit был отключён");
                if (!scanResult.CommandPromptEnabled) sb.AppendLine("  - CMD был отключён");
                if (!scanResult.RunDialogEnabled) sb.AppendLine("  - Окно Win + R было отключено");
                if (!scanResult.ControlPanelEnabled) sb.AppendLine("  - Панель управления была отключена");
                if (!scanResult.ShutdownFromStartEnabled) sb.AppendLine("  - Выключение из Пуска было отключено");
                if (!scanResult.MmcEnabled) sb.AppendLine("  - MMC был ограничен");
                if (!scanResult.ViewOnDriveEnabled) sb.AppendLine("  - Просмотр дисков был ограничен");
                if (!scanResult.DrivesVisibleEnabled) sb.AppendLine("  - Диски были скрыты");
                if (!scanResult.FindEnabled) sb.AppendLine("  - Поиск был отключён");
                if (!scanResult.ContextMenuEnabled) sb.AppendLine("  - Контекстное меню было отключено");
                if (!scanResult.FolderOptionsEnabled) sb.AppendLine("  - Параметры папок были отключены");
                if (!scanResult.SecurityTabEnabled) sb.AppendLine("  - Вкладка Безопасность была скрыта");
                if (!scanResult.FileMenuEnabled) sb.AppendLine("  - Меню Файл было отключено");
                if (!scanResult.CommonGroupsEnabled) sb.AppendLine("  - Общие группы были отключены");
                if (!scanResult.StartMenuLogoffEnabled) sb.AppendLine("  - Выход из системы в Пуске был отключён");
                if (!scanResult.ChangingWallpaperEnabled) sb.AppendLine("  - Смена обоев была запрещена");
                if (!scanResult.WinKeysEnabled) sb.AppendLine("  - Win-клавиши были отключены");
                if (!scanResult.SetTaskbarEnabled) sb.AppendLine("  - Изменение панели задач было запрещено");
                if (!scanResult.LockWorkstationEnabled) sb.AppendLine("  - Блокировка рабочего места была отключена");
                if (!scanResult.ChangePasswordEnabled) sb.AppendLine("  - Смена пароля была отключена");
                if (!scanResult.TrayContextMenuEnabled) sb.AppendLine("  - Контекстное меню трея было отключено");
                if (!scanResult.GroupPolicyUpdateEnabled) sb.AppendLine("  - Обновление групповых политик было ограничено");
                if (!scanResult.PowerOptionsEnabled) sb.AppendLine("  - Пункты питания были отключены");
                if (!scanResult.ContextMenusInStartEnabled) sb.AppendLine("  - Контекстные меню в Пуске были отключены");
                if (!scanResult.SystemRestoreEnabled) sb.AppendLine("  - Восстановление системы было отключено");
                if (!scanResult.SystemConfigEnabled) sb.AppendLine("  - Настройка восстановления системы была отключена");
                if (!scanResult.LogoffEnabled) sb.AppendLine("  - Выход из системы был отключён");
                if (!scanResult.UsbStorEnabled) sb.AppendLine("  - USB был заблокирован");
            }
            else
            {
                sb.AppendLine("Системных ограничений не обнаружено.");
            }

            sb.AppendLine();
            sb.AppendLine("=== AppInit_DLLs ===");
            sb.AppendLine(scanResult.AppInitEnabled64
                ? (string.IsNullOrEmpty(scanResult.AppInitDlls64) ? "64-бит: включён, DLL нет" : $"64-бит: {scanResult.AppInitDlls64} — очищено")
                : "64-бит: отключён");
            sb.AppendLine(scanResult.AppInitEnabled32
                ? (string.IsNullOrEmpty(scanResult.AppInitDlls32) ? "32-бит: включён, DLL нет" : $"32-бит: {scanResult.AppInitDlls32} — очищено")
                : "32-бит: отключён");

            sb.AppendLine();
            sb.AppendLine("=== BotKiller Report ===");
            sb.AppendLine(BotKiller.GetReport());

            sb.AppendLine("=== Анализ процессов ===");
            sb.AppendLine(_vCleaner.GetSummary());

            sb.AppendLine("=== Поиск майнеров ===");
            sb.AppendLine(_minerSearch.GetReport());

            sb.AppendLine("=== Очистка временных файлов ===");
            string freed = deletedBytes >= 1024L * 1024 * 1024
                ? $"{deletedBytes / (1024.0 * 1024 * 1024):F1} GB"
                : deletedBytes >= 1024 * 1024
                    ? $"{deletedBytes / (1024.0 * 1024):F1} MB"
                    : deletedBytes >= 1024
                        ? $"{deletedBytes / 1024.0:F1} KB"
                        : $"{deletedBytes} B";
            sb.AppendLine($"Удалено файлов: {deletedFiles}");
            sb.AppendLine($"Освобождено: {freed}");
            sb.AppendLine($"Недоступны: {failedFiles}");

            sb.AppendLine();
            sb.AppendLine("=== Очистка памяти ===");
            sb.AppendLine(_memReduct.GetSummary());

            string reportText = sb.ToString();

            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"MR_Cleaner_Report_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.txt";
                string reportPath = Path.Combine(desktop, fileName);
                File.WriteAllText(reportPath, reportText, Encoding.UTF8);
            }
            catch
            {
            }

            int botkillerThreats = BotKiller.DetectedThreats.Count;
            MetroFramework.MetroMessageBox.Show(
                this,
                botkillerThreats > 0
                    ? $"Сканирование завершено.\n\n[!] BotKiller: обнаружено угроз: {botkillerThreats}\n[+] Нейтрализовано процессов: {BotKiller.ProcessesTerminated}\n[+] Удалено файлов: {BotKiller.FilesDeleted}\n\nОтчёт на рабочем столе."
                    : "Сканирование завершено, отчёт на рабочем столе.",
                "Готово",
                MessageBoxButtons.OK,
                botkillerThreats > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            metroButton1.Text = "Сканировать";
            metroButton1.Enabled = true;
        }

        private async Task AnimateProgressBar(CancellationToken token)
        {
            int target = 90;
            while (!token.IsCancellationRequested && metroProgressBar1.Value < target)
            {
                await Task.Delay(80, token).ContinueWith(_ => { });
                if (token.IsCancellationRequested) break;
                Invoke((Action)(() =>
                {
                    if (metroProgressBar1.Value < target)
                        metroProgressBar1.Value = Math.Min(metroProgressBar1.Value + 1, target);
                }));
            }
        }
    }
}