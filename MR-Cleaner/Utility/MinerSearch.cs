using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MR_Cleaner.Utility
{
    internal class MinerSearch
    {
        private static readonly HashSet<int> PortSet = new HashSet<int>
        {
            1111,1112,2020,3333,4028,4040,4141,4444,5555,
            6633,6666,7001,7777,9980,9999,10191,10343,14433,20009
        };

        private static readonly HashSet<string> SuspNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xmrig","minerd","cpuminer","ethminer","claymore","phoenix",
            "guiminer","bfgminer","cgminer","nicehash","nanominer","lolminer",
            "teamredminer","gminer","t-rex","nbminer","kawpowminer","unmineable",
            "flexpool","2miners","herominers","minexmr","xmr-stak","xmrig-proxy",
            "xmrig-amd","xmrig-cuda","cryptonight","randomx","kawpow","autolykos",
            "ethash","progpow","beamv3","cuckoo","cuckaroom","cuckatoo",
            "ghostrider","octopus","etchash","ubqhash","firopow",
            "kheavyhash","pyrinhash","karlsenhash"
        };

        private static readonly string[] SuspPaths =
        {
            "temp","appdata","local\\temp","programdata",
            "users\\public","windows\\temp","recycle.bin"
        };

        private static readonly string[] StartupKeys =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce"
        };

        public List<string> FoundMiners { get; } = new List<string>();
        public List<string> FoundSuspicious { get; } = new List<string>();

        private ConcurrentBag<string> _minersBag;
        private ConcurrentBag<string> _suspBag;

        public void Scan()
        {
            _minersBag = new ConcurrentBag<string>();
            _suspBag = new ConcurrentBag<string>();

            var tasks = new[]
            {
                Task.Run(() => ScanProcesses()),
                Task.Run(() => ScanNetworkConnections()),
                Task.Run(() => ScanServices()),
                Task.Run(() => ScanScheduledTasks()),
                Task.Run(() => ScanStartup())
            };

            Task.WaitAll(tasks);

            FoundMiners.Clear();
            FoundSuspicious.Clear();
            FoundMiners.AddRange(_minersBag);
            FoundSuspicious.AddRange(_suspBag);

            SaveReportToDesktop();
        }

        private void ScanProcesses()
        {
            var processes = Process.GetProcesses();
            Parallel.ForEach(processes, proc =>
            {
                try
                {
                    string name = proc.ProcessName;
                    string lowerName = name.ToLowerInvariant();

                    string path = string.Empty;
                    try { path = proc.MainModule?.FileName?.ToLowerInvariant() ?? string.Empty; } catch { }

                    if (ContainsSuspName(lowerName) || ContainsSuspName(path))
                    {
                        _minersBag.Add($"Процесс: {name} (PID: {proc.Id}) - {path}");
                        return;
                    }

                    if (IsRunningFromSuspiciousPath(path))
                        _suspBag.Add($"Подозрительный путь: {name} - {path}");
                }
                catch { }
                finally { try { proc.Dispose(); } catch { } }
            });
        }

        private void ScanNetworkConnections()
        {
            try
            {
                var connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                foreach (var conn in connections)
                {
                    if (PortSet.Contains(conn.LocalEndPoint.Port) || PortSet.Contains(conn.RemoteEndPoint.Port))
                        _suspBag.Add($"Порт майнера: {conn.LocalEndPoint} -> {conn.RemoteEndPoint}");
                }
            }
            catch { }
        }

        private void ScanServices()
        {
            try
            {
                foreach (var svc in ServiceController.GetServices())
                {
                    try
                    {
                        string name = svc.ServiceName;
                        string lower = name.ToLowerInvariant();
                        string path = GetServiceImagePath(name)?.ToLowerInvariant() ?? string.Empty;

                        if (ContainsSuspName(lower) || ContainsSuspName(path))
                            _minersBag.Add($"Сервис: {name} - {path}");
                        else if (IsRunningFromSuspiciousPath(path))
                            _suspBag.Add($"Подозрительный сервис: {name} - {path}");
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ScanScheduledTasks()
        {
            ScanScheduledTasksWmi();
            ScanScheduledTasksFiles();
        }

        private void ScanScheduledTasksWmi()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, CommandLine FROM Win32_ScheduledJob"))
                {
                    foreach (ManagementObject task in searcher.Get())
                    {
                        try
                        {
                            string cmd = task["CommandLine"]?.ToString()?.ToLowerInvariant() ?? string.Empty;
                            string taskName = task["Name"]?.ToString() ?? string.Empty;

                            if (ContainsSuspName(cmd))
                                _minersBag.Add($"Задание (WMI): {taskName} - {cmd}");
                            else if (IsRunningFromSuspiciousPath(cmd))
                                _suspBag.Add($"Подозрительное задание (WMI): {taskName} - {cmd}");
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void ScanScheduledTasksFiles()
        {
            try
            {
                string tasksPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "System32", "Tasks");

                if (!Directory.Exists(tasksPath))
                    return;

                var files = Directory.GetFiles(tasksPath, "*", SearchOption.AllDirectories);
                Parallel.ForEach(files, file =>
                {
                    try
                    {
                        string content = File.ReadAllText(file).ToLowerInvariant();
                        if (ContainsSuspName(content))
                            _minersBag.Add($"Задание (файл): {file}");
                    }
                    catch { }
                });
            }
            catch { }
        }

        private void ScanStartup()
        {
            foreach (string keyPath in StartupKeys)
            {
                ScanStartupKey(Registry.LocalMachine, keyPath, "HKLM");
                ScanStartupKey(Registry.CurrentUser, keyPath, "HKCU");
            }
        }

        private void ScanStartupKey(RegistryKey hive, string keyPath, string hiveLabel)
        {
            try
            {
                using (var key = hive.OpenSubKey(keyPath))
                {
                    if (key == null) return;

                    foreach (string valueName in key.GetValueNames())
                    {
                        string value = key.GetValue(valueName)?.ToString()?.ToLowerInvariant() ?? string.Empty;

                        if (ContainsSuspName(value))
                            _suspBag.Add($"Автозапуск [{hiveLabel}]: {valueName} = {value}");
                        else if (IsRunningFromSuspiciousPath(value))
                            _suspBag.Add($"Подозрительный автозапуск [{hiveLabel}]: {valueName} = {value}");
                    }
                }
            }
            catch { }
        }

        private static bool ContainsSuspName(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return SuspNames.Any(s => input.Contains(s));
        }

        private static bool IsRunningFromSuspiciousPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return SuspPaths.Any(s => path.Contains(s));
        }

        private static string GetServiceImagePath(string serviceName)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}"))
                {
                    return key?.GetValue("ImagePath")?.ToString();
                }
            }
            catch { return null; }
        }

        private void SaveReportToDesktop()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string fileName = $"MetroMinerSearch_{DateTime.Now:dd-MM-yyyy_HH-mm}.txt";
            string fullPath = Path.Combine(desktop, fileName);

            File.WriteAllText(fullPath, GetReport(), Encoding.UTF8);
        }

        public string GetReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Metro Miner Search Report ===");
            sb.AppendLine($"Дата: {DateTime.Now}");
            sb.AppendLine();

            if (FoundMiners.Count > 0)
            {
                sb.AppendLine("[!!!] Найдены майнеры:");
                foreach (var m in FoundMiners) sb.AppendLine($"  - {m}");
                sb.AppendLine();
            }

            if (FoundSuspicious.Count > 0)
            {
                sb.AppendLine("[!!] Подозрительные элементы:");
                foreach (var s in FoundSuspicious) sb.AppendLine($"  - {s}");
                sb.AppendLine();
            }

            if (FoundMiners.Count == 0 && FoundSuspicious.Count == 0)
                sb.AppendLine("[OK] Угроз не обнаружено.");

            sb.AppendLine($"Всего майнеров: {FoundMiners.Count}");
            sb.AppendLine($"Всего подозрительных: {FoundSuspicious.Count}");

            return sb.ToString();
        }
    }
}
