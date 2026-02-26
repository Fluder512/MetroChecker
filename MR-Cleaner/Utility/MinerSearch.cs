using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;

namespace MR_Cleaner.Utility
{
    internal class MinerSearch
    {
        private readonly int[] _PortList = new[] { 1111, 1112, 2020, 3333, 4028, 4040, 4141, 4444, 5555, 6633, 6666, 7001, 7777, 9980, 9999, 10191, 10343, 14433, 20009 };
        private readonly string[] _SuspNames = new[] { "xmrig", "minerd", "cpuminer", "ethminer", "claymore", "phoenix", "guiminer", "bfgminer", "cgminer", "nicehash", "nanominer", "lolminer", "teamredminer", "gminer", "t-rex", "nbminer", "kawpowminer", "unmineable", "flexpool", "2miners", "herominers", "minexmr", "xmr-stak", "xmrig-proxy", "xmrig-amd", "xmrig-cuda", "cryptonight", "randomx", "kawpow", "autolykos", "ethash", "progpow", "beamv3", "cuckoo", "cuckaroom", "cuckatoo", "ghostrider", "octopus", "etchash", "ubqhash", "firopow", "kheavyhash", "pyrinhash", "karlsenhash" };
        private readonly string[] _SuspPaths = new[] { "temp", "appdata", "local\\temp", "programdata", "users\\public", "windows\\temp", "recycle.bin" };

        public List<string> FoundMiners { get; private set; } = new List<string>();
        public List<string> FoundSuspicious { get; private set; } = new List<string>();

        public void Scan()
        {
            FoundMiners.Clear();
            FoundSuspicious.Clear();
            ScanProcesses();
            ScanNetworkConnections();
            ScanServices();
            ScanScheduledTasks();
            ScanStartup();
        }

        private void ScanProcesses()
        {
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    string name = proc.ProcessName.ToLower();
                    string path = proc.MainModule?.FileName?.ToLower() ?? "";
                    if (_SuspNames.Any(s => name.Contains(s)) || _SuspNames.Any(s => path.Contains(s)))
                    {
                        FoundMiners.Add($"Процесс: {proc.ProcessName} (ПИД: {proc.Id}) - {path}");
                        continue;
                    }
                    if (IsRunningFromSuspiciousPath(path))
                    {
                        FoundSuspicious.Add($"Подозрительный путь: {proc.ProcessName} - {path}");
                    }
                }
                catch { }
                finally { try { proc.Dispose(); } catch { } }
            }
        }

        private void ScanNetworkConnections()
        {
            var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            foreach (var conn in tcpConnections)
            {
                if (_PortList.Contains(conn.LocalEndPoint.Port) || _PortList.Contains(conn.RemoteEndPoint.Port))
                {
                    FoundSuspicious.Add($"Порт майнера: {conn.LocalEndPoint} -> {conn.RemoteEndPoint}");
                }
            }
        }

        private void ScanServices()
        {
            foreach (ServiceController svc in ServiceController.GetServices())
            {
                try
                {
                    string name = svc.ServiceName.ToLower();
                    string path = GetServicePath(svc.ServiceName)?.ToLower() ?? "";
                    if (_SuspNames.Any(s => name.Contains(s)) || _SuspNames.Any(s => path.Contains(s)))
                    {
                        FoundMiners.Add($"Service: {svc.ServiceName} - {path}");
                    }
                    else if (IsRunningFromSuspiciousPath(path))
                    {
                        FoundSuspicious.Add($"Подозрительный сервис: {svc.ServiceName} - {path}");
                    }
                }
                catch { }
            }
        }

        private void ScanScheduledTasks()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT Name, CommandLine FROM Win32_ScheduledJob");
                foreach (ManagementObject task in searcher.Get())
                {
                    string cmd = task["CommandLine"]?.ToString()?.ToLower() ?? "";
                    if (_SuspNames.Any(s => cmd.Contains(s)))
                    {
                        FoundMiners.Add($"Шедульный процесс: {task["Name"]} - {cmd}");
                    }
                    else if (IsRunningFromSuspiciousPath(cmd))
                    {
                        FoundSuspicious.Add($"Подозрительный процесс: {task["Name"]} - {cmd}");
                    }
                }
            }
            catch { }
            try
            {
                string tasksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "Tasks");
                if (Directory.Exists(tasksPath))
                {
                    foreach (var file in Directory.GetFiles(tasksPath, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            string content = File.ReadAllText(file);
                            if (_SuspNames.Any(s => content.ToLower().Contains(s)))
                            {
                                FoundMiners.Add($"Процесс файл: {file}");
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void ScanStartup()
        {
            string[] startupKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce"
            };
            foreach (string keyPath in startupKeys)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in key.GetValueNames())
                            {
                                string value = key.GetValue(valueName)?.ToString()?.ToLower() ?? "";
                                if (_SuspNames.Any(s => value.Contains(s)) || IsRunningFromSuspiciousPath(value))
                                {
                                    FoundSuspicious.Add($"Автозапуск: {valueName} = {value}");
                                }
                            }
                        }
                    }
                }
                catch { }
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in key.GetValueNames())
                            {
                                string value = key.GetValue(valueName)?.ToString()?.ToLower() ?? "";
                                if (_SuspNames.Any(s => value.Contains(s)) || IsRunningFromSuspiciousPath(value))
                                {
                                    FoundSuspicious.Add($"Автозапуск (CU): {valueName} = {value}");
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private bool IsRunningFromSuspiciousPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string lowerPath = path.ToLower();
            return _SuspPaths.Any(s => lowerPath.Contains(s));
        }

        private string GetServicePath(string serviceName)
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

        public string GetReport()
        {
            var report = new StringBuilder();
            report.AppendLine("=== Miner Search ===");
            report.AppendLine($"Время: {DateTime.Now}");
            report.AppendLine();
            if (FoundMiners.Count > 0)
            {
                report.AppendLine("[!!!] Найдено майнеров:");
                foreach (var m in FoundMiners) report.AppendLine($"  - {m}");
                report.AppendLine();
            }
            if (FoundSuspicious.Count > 0)
            {
                report.AppendLine("[!!] Подозрительные процессы:");
                foreach (var s in FoundSuspicious) report.AppendLine($"  - {s}");
                report.AppendLine();
            }
            if (FoundMiners.Count == 0 && FoundSuspicious.Count == 0)
            {
                report.AppendLine("[OK] Всё ок.");
            }
            report.AppendLine($"Всего майнеров: {FoundMiners.Count}");
            report.AppendLine($"Всего подозрительных: {FoundSuspicious.Count}");
            return report.ToString();
        }
    }
}