using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

        private static readonly int SelfPid = Process.GetCurrentProcess().Id;
        private static readonly string SelfName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();

        private static readonly HashSet<string> CriticalProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system", "idle", "smss", "csrss", "wininit", "services", "lsass", "winlogon"
        };

        private static readonly HashSet<string> SuspiciousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svch0st", "svchost32", "svchost64", "lsass32", "winlogon32",
            "explorer32", "csrss32", "smss32", "wininit32", "services32",
            "spoolsv32", "taskhost32", "taskhostw32", "conhost32"
        };

        private static readonly string[] KnownMalwarePaths =
        {
            @"\AppData\Local\Temp\",
            @"\AppData\Roaming\Microsoft\Windows\",
            @"\Users\Public\",
            @"\ProgramData\Microsoft\Windows\"
        };

        private static readonly string[] SuspiciousDlls =
        {
            "meterpreter", "cobalt", "beacon", "inject", "hook32", "hook64"
        };

        private static readonly Dictionary<string, string[]> UnexpectedChildren = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "explorer", new[] { "cmd", "powershell", "wscript", "cscript", "mshta" } },
            { "winword",  new[] { "cmd", "powershell", "wscript", "cscript" } },
            { "excel",    new[] { "cmd", "powershell", "wscript", "cscript" } }
        };

        private static readonly HashSet<int> C2Ports = new HashSet<int> { 4444, 1337, 31337, 666, 9999, 12345, 54321 };

        private ConcurrentBag<string> _threatsBag;
        private ConcurrentBag<string> _logBag;
        private int _scannedCount;
        private int _killedCount;

        private TcpConnectionInformation[] _tcpSnapshot;

        public List<string> Threats { get; } = new List<string>();
        public List<string> Log { get; } = new List<string>();

        public void ScanProcessesOnly(bool removeThreats = false, bool intensiveMode = false)
        {
            _threatsBag = new ConcurrentBag<string>();
            _logBag = new ConcurrentBag<string>();
            _scannedCount = 0;
            _killedCount = 0;

            try { _tcpSnapshot = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections(); }
            catch { _tcpSnapshot = new TcpConnectionInformation[0]; }

            Process[] processes;
            try { processes = Process.GetProcesses(); }
            catch { return; }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            Parallel.ForEach(processes, options, p => AnalyzeProcess(p, removeThreats, intensiveMode));

            Threats.Clear();
            Log.Clear();
            Threats.AddRange(_threatsBag);
            Log.AddRange(_logBag);
        }

        private void AnalyzeProcess(Process process, bool removeThreats, bool intensiveMode)
        {
            try
            {
                if (IsSelf(process)) return;

                Interlocked.Increment(ref _scannedCount);

                if (IsSystemCritical(process)) return;

                var suspicions = new List<string>();

                CheckProcessName(process, suspicions);
                CheckProcessPath(process, suspicions);
                CheckParentProcess(process, suspicions);
                CheckNetworkActivity(process, suspicions);

                if (intensiveMode)
                {
                    CheckRunPE(process, suspicions);
                    CheckSuspiciousMemory(process, suspicions);
                }

                if (suspicions.Count == 0) return;

                string msg = $"[THREAT] PID={process.Id} | {process.ProcessName} | {string.Join("; ", suspicions)}";
                _threatsBag.Add(msg);
                _logBag.Add(msg);

                if (removeThreats)
                {
                    try
                    {
                        process.Kill();
                        Interlocked.Increment(ref _killedCount);
                        _logBag.Add($"[KILLED] PID={process.Id} | {process.ProcessName}");
                    }
                    catch { }
                }
            }
            catch { }
            finally { try { process.Dispose(); } catch { } }
        }

        private static bool IsSelf(Process p)
        {
            try { return p.Id == SelfPid || p.ProcessName.ToLowerInvariant() == SelfName; }
            catch { return false; }
        }

        private static bool IsSystemCritical(Process p)
        {
            try { return CriticalProcesses.Contains(p.ProcessName); }
            catch { return true; }
        }

        private static void CheckProcessName(Process p, List<string> suspicions)
        {
            try
            {
                if (SuspiciousNames.Contains(p.ProcessName))
                    suspicions.Add("Подозрительное имя процесса (имитация системного)");
            }
            catch { }
        }

        private static void CheckProcessPath(Process p, List<string> suspicions)
        {
            try
            {
                string path;
                try { path = p.MainModule?.FileName ?? string.Empty; }
                catch { return; }

                if (string.IsNullOrEmpty(path)) return;

                foreach (var malwarePath in KnownMalwarePaths)
                {
                    if (path.IndexOf(malwarePath, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        suspicions.Add($"Запущен из подозрительного расположения: {malwarePath}");
                        break;
                    }
                }

                if (!File.Exists(path))
                    suspicions.Add("Файл процесса не существует на диске");
            }
            catch { }
        }

        private static void CheckParentProcess(Process p, List<string> suspicions)
        {
            try
            {
                using (var wmi = new ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {p.Id}"))
                {
                    wmi.Options.Timeout = TimeSpan.FromSeconds(2);

                    ManagementObjectCollection results;
                    try { results = wmi.Get(); }
                    catch { return; }

                    foreach (ManagementObject obj in results)
                    {
                        try
                        {
                            var raw = obj["ParentProcessId"];
                            if (raw == null) continue;

                            int parentId = Convert.ToInt32(raw);
                            if (parentId == SelfPid) continue;

                            Process parent;
                            try { parent = Process.GetProcessById(parentId); }
                            catch { continue; }

                            string parentName = parent.ProcessName.ToLowerInvariant();
                            string childName = p.ProcessName.ToLowerInvariant();

                            foreach (var kv in UnexpectedChildren)
                            {
                                if (string.Equals(parentName, kv.Key, StringComparison.OrdinalIgnoreCase)
                                    && kv.Value.Contains(childName))
                                {
                                    suspicions.Add($"Подозрительное дерево процессов: {parent.ProcessName} -> {p.ProcessName}");
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void CheckNetworkActivity(Process p, List<string> suspicions)
        {
            try
            {
                var found = _tcpSnapshot
                    .Where(c =>
                    {
                        try { return c.State == TcpState.Established && C2Ports.Contains(c.RemoteEndPoint.Port); }
                        catch { return false; }
                    })
                    .ToList();

                if (found.Count > 0)
                    suspicions.Add($"Подозрительные соединения: {string.Join(", ", found.Select(c => c.RemoteEndPoint.ToString()))}");
            }
            catch { }
        }

        private static void CheckRunPE(Process p, List<string> suspicions)
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
                    int result = VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION mbi,
                        (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
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
                    try { CloseHandle(hProcess); } catch { }
            }
        }

        private static bool ContainsMzHeader(IntPtr hProcess, IntPtr baseAddress)
        {
            try
            {
                var buffer = new byte[2];
                ReadProcessMemory(hProcess, baseAddress, buffer, 2, out int read);
                return read == 2 && buffer[0] == 0x4D && buffer[1] == 0x5A;
            }
            catch { return false; }
        }

        private static void CheckSuspiciousMemory(Process p, List<string> suspicions)
        {
            try
            {
                ProcessModuleCollection modules;
                try { modules = p.Modules; }
                catch { return; }

                foreach (ProcessModule m in modules)
                {
                    try
                    {
                        string name = m.ModuleName.ToLowerInvariant();
                        foreach (var s in SuspiciousDlls)
                        {
                            if (name.Contains(s))
                            {
                                suspicions.Add($"Подозрительная DLL: {m.ModuleName}");
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Проверено процессов: {_scannedCount}");
            sb.AppendLine($"Угроз обнаружено: {Threats.Count}");
            sb.AppendLine($"Процессов завершено: {_killedCount}");
            sb.AppendLine();

            if (Threats.Count == 0)
            {
                sb.AppendLine("Угроз нету");
            }
            else
            {
                sb.AppendLine("--- Обнаруженные угрозы ---");
                foreach (var t in Threats)
                    sb.AppendLine(t);
            }

            return sb.ToString();
        }
    }
}