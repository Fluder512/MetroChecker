using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MR_Cleaner.Utility
{
    internal class VCleaner
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        private const uint MEM_PRIVATE = 0x20000;

        private readonly List<string> _threats = new List<string>();
        private readonly List<string> _log = new List<string>();
        private int _scannedCount;
        private int _killedCount;

        private static readonly int _selfPid = Process.GetCurrentProcess().Id;
        private static readonly string _selfName = Process.GetCurrentProcess().ProcessName.ToLower();

        private static readonly string[] SuspiciousNames = {
            "svch0st", "svchost32", "svchost64", "lsass32", "winlogon32",
            "explorer32", "csrss32", "smss32", "wininit32", "services32",
            "spoolsv32", "taskhost32", "taskhostw32", "conhost32"
        };

        private static readonly string[] KnownMalwarePaths = {
            @"\AppData\Local\Temp\",
            @"\AppData\Roaming\Microsoft\Windows\",
            @"\Users\Public\",
            @"\ProgramData\Microsoft\Windows\",
        };

        public void ScanProcessesOnly(bool removeThreats = false, bool intensiveMode = false)
        {
            _threats.Clear();
            _log.Clear();
            _scannedCount = 0;
            _killedCount = 0;

            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                return;
            }

            if (intensiveMode)
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };
                Parallel.ForEach(processes, options, p => AnalyzeProcess(p, removeThreats, intensiveMode));
            }
            else
            {
                foreach (var p in processes)
                    AnalyzeProcess(p, removeThreats, intensiveMode);
            }
        }

        private bool IsSelfProcess(Process p)
        {
            try
            {
                return p.Id == _selfPid || p.ProcessName.ToLower() == _selfName;
            }
            catch
            {
                return false;
            }
        }

        private void AnalyzeProcess(Process process, bool removeThreats, bool intensiveMode)
        {
            try
            {
                if (IsSelfProcess(process))
                    return;

                System.Threading.Interlocked.Increment(ref _scannedCount);

                if (IsSystemCritical(process))
                    return;

                var suspicions = new List<string>();

                CheckProcessName(process, suspicions);
                CheckProcessPath(process, suspicions);
                CheckParentProcess(process, suspicions);

                if (intensiveMode)
                {
                    CheckRunPE(process, suspicions);
                    CheckSuspiciousMemory(process, suspicions);
                }

                CheckNetworkActivity(process, suspicions);

                if (suspicions.Count > 0)
                {
                    var msg = $"[THREAT] PID={process.Id} | {process.ProcessName} | {string.Join("; ", suspicions)}";
                    lock (_threats)
                    {
                        _threats.Add(msg);
                        _log.Add(msg);
                    }

                    if (removeThreats && !IsSelfProcess(process))
                    {
                        try
                        {
                            process.Kill();
                            System.Threading.Interlocked.Increment(ref _killedCount);
                            lock (_log)
                                _log.Add($"[KILLED] PID={process.Id} | {process.ProcessName}");
                        }
                        catch { }
                    }
                }
            }
            catch { }
            finally
            {
                try { process.Dispose(); } catch { }
            }
        }

        private bool IsSystemCritical(Process p)
        {
            try
            {
                var critical = new[] { "system", "idle", "smss", "csrss", "wininit", "services", "lsass", "winlogon" };
                return critical.Contains(p.ProcessName.ToLower());
            }
            catch
            {
                return true;
            }
        }

        private void CheckProcessName(Process p, List<string> suspicions)
        {
            try
            {
                var name = p.ProcessName.ToLower();
                if (SuspiciousNames.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    suspicions.Add("Подозрительное имя процесса (имитация системного)");
            }
            catch { }
        }

        private void CheckProcessPath(Process p, List<string> suspicions)
        {
            try
            {
                string path;
                try
                {
                    path = p.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    return;
                }

                if (string.IsNullOrEmpty(path))
                    return;

                foreach (var suspicious in KnownMalwarePaths)
                {
                    if (path.IndexOf(suspicious, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        suspicions.Add($"Запущен из подозрительного расположения: {suspicious}");
                        break;
                    }
                }

                if (!File.Exists(path))
                    suspicions.Add("Файл процесса не существует на диске");
            }
            catch { }
        }

        private void CheckParentProcess(Process p, List<string> suspicions)
        {
            try
            {
                var options = new System.Management.ObjectGetOptions();
                var scope = new System.Management.ManagementScope();
                scope.Options.Timeout = TimeSpan.FromSeconds(2);

                var wmi = new System.Management.ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {p.Id}");
                wmi.Options.Timeout = TimeSpan.FromSeconds(2);

                System.Management.ManagementObjectCollection results;
                try
                {
                    results = wmi.Get();
                }
                catch
                {
                    return;
                }

                foreach (System.Management.ManagementObject obj in results)
                {
                    try
                    {
                        var parentIdRaw = obj["ParentProcessId"];
                        if (parentIdRaw == null) continue;

                        var parentId = Convert.ToInt32(parentIdRaw);
                        if (parentId == _selfPid) continue;

                        Process parent;
                        try
                        {
                            parent = Process.GetProcessById(parentId);
                        }
                        catch
                        {
                            continue;
                        }

                        var unexpectedChildren = new Dictionary<string, string[]>
                        {
                            { "explorer", new[] { "cmd", "powershell", "wscript", "cscript", "mshta" } },
                            { "winword",  new[] { "cmd", "powershell", "wscript", "cscript" } },
                            { "excel",    new[] { "cmd", "powershell", "wscript", "cscript" } },
                        };

                        var parentName = parent.ProcessName.ToLower();
                        var childName = p.ProcessName.ToLower();

                        foreach (var kv in unexpectedChildren)
                        {
                            if (parentName == kv.Key && kv.Value.Contains(childName))
                                suspicions.Add($"Подозрительное дерево процессов: {parent.ProcessName} -> {p.ProcessName}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void CheckRunPE(Process p, List<string> suspicions)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, p.Id);
                if (hProcess == IntPtr.Zero) return;

                IntPtr address = IntPtr.Zero;
                int rwxRegions = 0;
                int privateExecRegions = 0;
                int iterations = 0;
                const int maxIterations = 2000;

                while (iterations++ < maxIterations)
                {
                    int result = VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION mbi, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>());
                    if (result == 0) break;

                    long regionSize = mbi.RegionSize.ToInt64();
                    if (regionSize <= 0) break;

                    if (mbi.State == MEM_COMMIT)
                    {
                        bool isExecWrite = mbi.Protect == PAGE_EXECUTE_READWRITE || mbi.Protect == PAGE_EXECUTE_WRITECOPY;
                        bool isPrivate = mbi.Type == MEM_PRIVATE;
                        bool isExecutable = (mbi.Protect & 0xF0) != 0;

                        if (isExecWrite) rwxRegions++;
                        if (isPrivate && isExecutable) privateExecRegions++;

                        if (isPrivate && isExecutable && regionSize > 0x10000)
                        {
                            if (ContainsMzHeader(hProcess, mbi.BaseAddress))
                                suspicions.Add("RunPE: PE-заголовок в приватной исполняемой памяти");
                        }
                    }

                    long next = address.ToInt64() + regionSize;
                    if (next <= address.ToInt64() || next >= 0x7FFFFFFF0000L) break;
                    address = new IntPtr(next);
                }

                if (rwxRegions > 5)
                    suspicions.Add($"Process Hollowing: много RWX-регионов ({rwxRegions})");

                if (privateExecRegions > 10)
                    suspicions.Add($"Подозрительные приватные исполняемые регионы ({privateExecRegions})");
            }
            catch { }
            finally
            {
                if (hProcess != IntPtr.Zero)
                {
                    try { CloseHandle(hProcess); } catch { }
                }
            }
        }

        private bool ContainsMzHeader(IntPtr hProcess, IntPtr baseAddress)
        {
            try
            {
                var buffer = new byte[2];
                ReadProcessMemory(hProcess, baseAddress, buffer, 2, out int read);
                return read == 2 && buffer[0] == 0x4D && buffer[1] == 0x5A;
            }
            catch { return false; }
        }

        private void CheckSuspiciousMemory(Process p, List<string> suspicions)
        {
            try
            {
                ProcessModuleCollection modules;
                try
                {
                    modules = p.Modules;
                }
                catch
                {
                    return;
                }

                var knownSuspiciousDlls = new[] { "meterpreter", "cobalt", "beacon", "inject", "hook32", "hook64" };

                foreach (ProcessModule m in modules)
                {
                    try
                    {
                        var name = m.ModuleName.ToLower();
                        foreach (var s in knownSuspiciousDlls)
                        {
                            if (name.Contains(s))
                                suspicions.Add($"Подозрительная DLL: {m.ModuleName}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void CheckNetworkActivity(Process p, List<string> suspicions)
        {
            try
            {
                var connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                var c2Ports = new HashSet<int> { 4444, 1337, 31337, 666, 9999, 12345, 54321 };

                var found = connections.Where(c =>
                {
                    try { return c.State == TcpState.Established && c2Ports.Contains(c.RemoteEndPoint.Port); }
                    catch { return false; }
                }).ToList();

                if (found.Any())
                    suspicions.Add($"Подозрительные соединения: {string.Join(", ", found.Select(c => c.RemoteEndPoint.ToString()))}");
            }
            catch { }
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Проверено процессов: {_scannedCount}");
            sb.AppendLine($"Угроз обнаружено: {_threats.Count}");
            sb.AppendLine($"Процессов завершено: {_killedCount}");
            sb.AppendLine();

            if (_threats.Count == 0)
            {
                sb.AppendLine("Угроз нету");
            }
            else
            {
                sb.AppendLine("--- Обнаруженные угрозы ---");
                foreach (var t in _threats)
                    sb.AppendLine(t);
            }

            return sb.ToString();
        }
    }
}