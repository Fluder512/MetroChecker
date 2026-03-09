using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
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

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ulAf, TCP_TABLE_CLASS tableClass, uint reserved);

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

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res1;
            public ushort e_oemid;
            public ushort e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;
            public int e_lfanew;
        }

        private struct PeInfo
        {
            public bool IsValid;
            public bool Is64;
            public ushort Machine;
            public ushort NumberOfSections;
            public ushort Magic;
            public uint AddressOfEntryPoint;
            public ulong ImageBase;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public ushort Subsystem;
            public uint SectionAlignment;
            public uint FileAlignment;
            public List<PeSection> Sections;
        }

        private struct PeSection
        {
            public string Name;
            public uint VirtualAddress;
            public uint VirtualSize;
            public uint SizeOfRawData;
            public uint PointerToRawData;
            public uint Characteristics;
        }

        private sealed class TcpConnectionRecord
        {
            public int Pid;
            public IPEndPoint LocalEndPoint;
            public IPEndPoint RemoteEndPoint;
            public TcpState State;
        }

        private enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }

        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_PRIVATE = 0x20000;
        private const uint MEM_IMAGE = 0x1000000;

        private const uint PAGE_EXECUTE = 0x10;
        private const uint PAGE_EXECUTE_READ = 0x20;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
        private const uint PAGE_GUARD = 0x100;
        private const uint PAGE_NOACCESS = 0x01;

        private const int ProcessBasicInformationClass = 0;
        private const int AF_INET = 2;

        private static readonly int SelfPid = Process.GetCurrentProcess().Id;
        private static readonly string SelfName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();

        private static readonly HashSet<string> CriticalProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system","idle","smss","csrss","wininit","services","lsass","winlogon","registry","fontdrvhost"
        };

        private static readonly HashSet<string> SuspiciousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svch0st","svchost32","svchost64","lsass32","winlogon32",
            "explorer32","csrss32","smss32","wininit32","services32",
            "spoolsv32","taskhost32","taskhostw32","conhost32",
            "chrome32","firefox32","edge32"
        };

        private static readonly string[] KnownMalwarePaths =
        {
            @"\appdata\local\temp\",
            @"\appdata\roaming\microsoft\windows\",
            @"\users\public\",
            @"\programdata\microsoft\windows\",
            @"\programdata\",
            @"\windows\temp\"
        };

        private static readonly string[] SuspiciousDlls =
        {
            "meterpreter","cobalt","beacon","inject","hook32","hook64",
            "keylogger","stealer","grabber","rat","remote","shellcode"
        };

        private static readonly Dictionary<string, string[]> UnexpectedChildren = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "explorer", new[] { "cmd","powershell","wscript","cscript","mshta","regsvr32","rundll32","pwsh" } },
            { "winword",  new[] { "cmd","powershell","wscript","cscript","mshta","rundll32","pwsh" } },
            { "excel",    new[] { "cmd","powershell","wscript","cscript","mshta","rundll32","pwsh" } },
            { "outlook",  new[] { "cmd","powershell","wscript","cscript","mshta","pwsh" } }
        };

        private static readonly HashSet<int> C2Ports = new HashSet<int> { 4444, 1337, 31337, 666, 9999, 12345, 54321, 8081, 8082, 9001 };

        private static readonly HashSet<string> DotNetProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "devenv","msbuild","dotnet","dnspy","ilspy","jetbrains",
            "rider","vstest","testhost","csc","vbc","fsc"
        };

        private ConcurrentBag<string> _threatsBag;
        private ConcurrentBag<string> _logBag;
        private int _scannedCount;
        private int _killedCount;

        private Dictionary<int, int> _parentMap;
        private Dictionary<int, List<TcpConnectionRecord>> _tcpMap;

        public List<string> Threats { get; } = new List<string>();
        public List<string> Log { get; } = new List<string>();

        public void ScanProcessesOnly(bool removeThreats = false, bool intensiveMode = false)
        {
            _threatsBag = new ConcurrentBag<string>();
            _logBag = new ConcurrentBag<string>();
            _scannedCount = 0;
            _killedCount = 0;

            _parentMap = BuildParentMap();
            _tcpMap = BuildTcpMap();

            Process[] processes;
            try { processes = Process.GetProcesses(); }
            catch { return; }

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            };

            Parallel.ForEach(processes, options, p => AnalyzeProcess(p, removeThreats, intensiveMode));

            Threats.Clear();
            Log.Clear();
            Threats.AddRange(_threatsBag.Distinct().OrderBy(x => x));
            Log.AddRange(_logBag.Distinct().OrderBy(x => x));
        }

        private void AnalyzeProcess(Process process, bool removeThreats, bool intensiveMode)
        {
            try
            {
                if (IsSelf(process)) return;
                if (process.HasExited) return;

                Interlocked.Increment(ref _scannedCount);

                if (IsSystemCritical(process)) return;

                var suspicions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            finally
            {
                try { process.Dispose(); } catch { }
            }
        }

        private static bool IsSelf(Process p)
        {
            try { return p.Id == SelfPid || p.ProcessName.Equals(SelfName, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        private static bool IsSystemCritical(Process p)
        {
            try
            {
                if (CriticalProcesses.Contains(p.ProcessName))
                    return true;

                if (p.SessionId == 0 && CriticalProcesses.Contains(p.ProcessName))
                    return true;

                return false;
            }
            catch { return true; }
        }

        private static bool IsDotNetOrIDE(Process p)
        {
            try
            {
                string name = p.ProcessName.ToLowerInvariant();
                if (DotNetProcesses.Any(d => name.Contains(d)))
                    return true;

                string path = string.Empty;
                try { path = p.MainModule?.FileName ?? string.Empty; }
                catch { }

                if (string.IsNullOrEmpty(path)) return false;

                string pathLower = path.ToLowerInvariant();
                return pathLower.Contains("visual studio") ||
                       pathLower.Contains("jetbrains") ||
                       pathLower.Contains("microsoft.net") ||
                       pathLower.Contains(@"\dotnet\");
            }
            catch { return false; }
        }

        private static void CheckProcessName(Process p, HashSet<string> suspicions)
        {
            try
            {
                if (SuspiciousNames.Contains(p.ProcessName))
                    suspicions.Add("Подозрительное имя процесса (имитация системного)");
            }
            catch { }
        }

        private static void CheckProcessPath(Process p, HashSet<string> suspicions)
        {
            try
            {
                string path;
                try { path = p.MainModule?.FileName ?? string.Empty; }
                catch { return; }

                if (string.IsNullOrEmpty(path)) return;

                string lower = path.ToLowerInvariant();

                foreach (var malwarePath in KnownMalwarePaths)
                {
                    if (lower.Contains(malwarePath))
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

        private void CheckParentProcess(Process p, HashSet<string> suspicions)
        {
            try
            {
                if (_parentMap == null) return;
                if (!_parentMap.TryGetValue(p.Id, out int parentId)) return;
                if (parentId <= 0 || parentId == SelfPid) return;

                Process parent;
                try { parent = Process.GetProcessById(parentId); }
                catch { return; }

                using (parent)
                {
                    string parentName = parent.ProcessName.ToLowerInvariant();
                    string childName = p.ProcessName.ToLowerInvariant();

                    foreach (var kv in UnexpectedChildren)
                    {
                        if (string.Equals(parentName, kv.Key, StringComparison.OrdinalIgnoreCase) && kv.Value.Contains(childName))
                        {
                            suspicions.Add($"Подозрительное дерево процессов: {parent.ProcessName} -> {p.ProcessName}");
                        }
                    }
                }
            }
            catch { }
        }

        private void CheckNetworkActivity(Process p, HashSet<string> suspicions)
        {
            try
            {
                if (_tcpMap == null) return;
                if (!_tcpMap.TryGetValue(p.Id, out var list)) return;
                if (list == null || list.Count == 0) return;

                var suspicious = new List<string>();

                foreach (var c in list)
                {
                    try
                    {
                        if (c.State != TcpState.Established)
                            continue;

                        int port = c.RemoteEndPoint.Port;
                        if (!C2Ports.Contains(port))
                            continue;

                        var ip = c.RemoteEndPoint.Address;
                        if (IsPrivateIp(ip))
                            continue;

                        suspicious.Add($"{c.LocalEndPoint} -> {c.RemoteEndPoint}");
                    }
                    catch { }
                }

                if (suspicious.Count > 0)
                    suspicions.Add($"Подозрительные C2-соединения: {string.Join(", ", suspicious.Distinct())}");
            }
            catch { }
        }

        private static bool IsPrivateIp(IPAddress ip)
        {
            try
            {
                if (IPAddress.IsLoopback(ip))
                    return true;

                if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                    return false;

                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 10) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                return false;
            }
            catch { return false; }
        }

        private static void CheckRunPE(Process p, HashSet<string> suspicions)
        {
            if (IsDotNetOrIDE(p)) return;

            string diskPath = string.Empty;
            try { diskPath = p.MainModule?.FileName ?? string.Empty; }
            catch { return; }

            if (string.IsNullOrEmpty(diskPath) || !File.Exists(diskPath)) return;

            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION, false, p.Id);
                if (hProcess == IntPtr.Zero) return;

                IntPtr mainModuleBase;
                try { mainModuleBase = p.MainModule.BaseAddress; }
                catch { return; }

                int score = 0;

                if (TryGetPebImageBase(hProcess, out IntPtr pebImageBase))
                {
                    if (pebImageBase != IntPtr.Zero && pebImageBase != mainModuleBase)
                        score += 4;
                }

                if (TryQueryMemory(hProcess, mainModuleBase, out MEMORY_BASIC_INFORMATION mbi))
                {
                    if ((mbi.State & MEM_COMMIT) != MEM_COMMIT)
                        score += 2;

                    if ((mbi.Type & MEM_IMAGE) != MEM_IMAGE)
                        score += 5;
                }

                if (!TryReadPeFromMemory(hProcess, mainModuleBase, out PeInfo memPe))
                    return;

                if (!TryReadPeFromDisk(diskPath, out PeInfo diskPe))
                    return;

                if (memPe.Machine != diskPe.Machine)
                    score += 5;

                if (memPe.Magic != diskPe.Magic)
                    score += 4;

                if (memPe.NumberOfSections != diskPe.NumberOfSections)
                    score += 2;

                if (memPe.AddressOfEntryPoint != diskPe.AddressOfEntryPoint)
                    score += 4;

                if (memPe.SizeOfImage != diskPe.SizeOfImage)
                    score += 2;

                if (memPe.SizeOfHeaders != diskPe.SizeOfHeaders)
                    score += 1;

                if (memPe.Subsystem != diskPe.Subsystem)
                    score += 1;

                if (memPe.SectionAlignment != diskPe.SectionAlignment)
                    score += 1;

                if (memPe.FileAlignment != diskPe.FileAlignment)
                    score += 1;

                score += CompareSections(memPe, diskPe);

                int epDiff = CompareEntryPointBytes(hProcess, mainModuleBase, memPe, diskPath, diskPe);
                if (epDiff >= 24)
                    score += 5;
                else if (epDiff >= 8)
                    score += 2;

                int privateExec = CountExecutablePrivateRegions(hProcess, 4);
                if (privateExec >= 2)
                    score += 3;
                else if (privateExec == 1)
                    score += 1;

                if (score >= 7)
                    suspicions.Add("RunPE: PE-заголовок в памяти сильно отличается от файла на диске");
            }
            catch { }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    try { CloseHandle(hProcess); } catch { }
            }
        }

        private static bool TryGetPebImageBase(IntPtr hProcess, out IntPtr imageBase)
        {
            imageBase = IntPtr.Zero;

            try
            {
                var pbi = new PROCESS_BASIC_INFORMATION();
                int retLen;
                int nt = NtQueryInformationProcess(hProcess, ProcessBasicInformationClass, ref pbi, Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out retLen);
                if (nt != 0 || pbi.PebBaseAddress == IntPtr.Zero)
                    return false;

                int offset = IntPtr.Size == 8 ? 0x10 : 0x08;
                byte[] buffer = new byte[IntPtr.Size];
                if (!ReadMemoryExact(hProcess, IntPtr.Add(pbi.PebBaseAddress, offset), buffer, buffer.Length))
                    return false;

                imageBase = IntPtr.Size == 8
                    ? new IntPtr(BitConverter.ToInt64(buffer, 0))
                    : new IntPtr(BitConverter.ToInt32(buffer, 0));

                return imageBase != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryQueryMemory(IntPtr hProcess, IntPtr address, out MEMORY_BASIC_INFORMATION mbi)
        {
            mbi = default;
            try
            {
                return VirtualQueryEx(hProcess, address, out mbi, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()) != 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadPeFromMemory(IntPtr hProcess, IntPtr baseAddress, out PeInfo pe)
        {
            pe = default;
            pe.Sections = new List<PeSection>();

            try
            {
                byte[] header = new byte[0x2000];
                if (!ReadProcessMemory(hProcess, baseAddress, header, header.Length, out int bytesRead))
                    return false;

                if (bytesRead < 512)
                    return false;

                return TryParsePe(header, out pe);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadPeFromDisk(string path, out PeInfo pe)
        {
            pe = default;
            pe.Sections = new List<PeSection>();

            try
            {
                byte[] data;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    int readLen = (int)Math.Min(fs.Length, 0x4000);
                    if (readLen < 512)
                        return false;

                    data = new byte[readLen];
                    int total = 0;
                    while (total < readLen)
                    {
                        int read = fs.Read(data, total, readLen - total);
                        if (read <= 0) break;
                        total += read;
                    }

                    if (total < 512)
                        return false;
                }

                return TryParsePe(data, out pe);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParsePe(byte[] data, out PeInfo pe)
        {
            pe = default;
            pe.Sections = new List<PeSection>();

            try
            {
                if (data == null || data.Length < 0x200)
                    return false;

                if (data[0] != 0x4D || data[1] != 0x5A)
                    return false;

                int e_lfanew = BitConverter.ToInt32(data, 0x3C);
                if (e_lfanew <= 0 || e_lfanew + 0x108 >= data.Length)
                    return false;

                uint peSig = BitConverter.ToUInt32(data, e_lfanew);
                if (peSig != 0x00004550)
                    return false;

                ushort machine = BitConverter.ToUInt16(data, e_lfanew + 4);
                ushort numberOfSections = BitConverter.ToUInt16(data, e_lfanew + 6);
                ushort sizeOfOptionalHeader = BitConverter.ToUInt16(data, e_lfanew + 20);

                int optionalHeader = e_lfanew + 24;
                ushort magic = BitConverter.ToUInt16(data, optionalHeader);
                bool is64 = magic == 0x20b;
                if (!is64 && magic != 0x10b)
                    return false;

                uint addressOfEntryPoint = BitConverter.ToUInt32(data, optionalHeader + 16);
                ulong imageBase = is64
                    ? BitConverter.ToUInt64(data, optionalHeader + 24)
                    : BitConverter.ToUInt32(data, optionalHeader + 28);

                uint sectionAlignment = BitConverter.ToUInt32(data, optionalHeader + 32);
                uint fileAlignment = BitConverter.ToUInt32(data, optionalHeader + 36);
                uint sizeOfImage = BitConverter.ToUInt32(data, optionalHeader + 56);
                uint sizeOfHeaders = BitConverter.ToUInt32(data, optionalHeader + 60);
                ushort subsystem = BitConverter.ToUInt16(data, optionalHeader + 68);

                int sectionTable = optionalHeader + sizeOfOptionalHeader;
                if (sectionTable < 0 || sectionTable >= data.Length)
                    return false;

                var sections = new List<PeSection>();
                int maxSections = Math.Min(numberOfSections, (ushort)96);

                for (int i = 0; i < maxSections; i++)
                {
                    int off = sectionTable + i * 40;
                    if (off + 40 > data.Length)
                        break;

                    string name = Encoding.ASCII.GetString(data, off, 8).TrimEnd('\0', ' ');
                    uint virtualSize = BitConverter.ToUInt32(data, off + 8);
                    uint virtualAddress = BitConverter.ToUInt32(data, off + 12);
                    uint sizeOfRawData = BitConverter.ToUInt32(data, off + 16);
                    uint pointerToRawData = BitConverter.ToUInt32(data, off + 20);
                    uint characteristics = BitConverter.ToUInt32(data, off + 36);

                    sections.Add(new PeSection
                    {
                        Name = name,
                        VirtualAddress = virtualAddress,
                        VirtualSize = virtualSize,
                        SizeOfRawData = sizeOfRawData,
                        PointerToRawData = pointerToRawData,
                        Characteristics = characteristics
                    });
                }

                pe = new PeInfo
                {
                    IsValid = true,
                    Is64 = is64,
                    Machine = machine,
                    NumberOfSections = numberOfSections,
                    Magic = magic,
                    AddressOfEntryPoint = addressOfEntryPoint,
                    ImageBase = imageBase,
                    SizeOfImage = sizeOfImage,
                    SizeOfHeaders = sizeOfHeaders,
                    Subsystem = subsystem,
                    SectionAlignment = sectionAlignment,
                    FileAlignment = fileAlignment,
                    Sections = sections
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int CompareSections(PeInfo memPe, PeInfo diskPe)
        {
            int score = 0;

            try
            {
                int common = Math.Min(memPe.Sections.Count, diskPe.Sections.Count);

                for (int i = 0; i < common; i++)
                {
                    var m = memPe.Sections[i];
                    var d = diskPe.Sections[i];

                    if (!string.Equals(m.Name, d.Name, StringComparison.Ordinal))
                        score += 2;

                    if (m.VirtualAddress != d.VirtualAddress)
                        score += 2;

                    if (m.VirtualSize != d.VirtualSize)
                        score += 1;

                    if (m.SizeOfRawData != d.SizeOfRawData)
                        score += 1;

                    if (m.Characteristics != d.Characteristics)
                        score += 2;
                }

                if (memPe.Sections.Count != diskPe.Sections.Count)
                    score += 2;
            }
            catch { }

            return score;
        }

        private static int CompareEntryPointBytes(IntPtr hProcess, IntPtr moduleBase, PeInfo memPe, string diskPath, PeInfo diskPe)
        {
            try
            {
                const int bytesToCompare = 64;

                if (memPe.AddressOfEntryPoint == 0)
                    return 0;

                IntPtr epAddress = IntPtr.Add(moduleBase, (int)memPe.AddressOfEntryPoint);

                byte[] memBytes = new byte[bytesToCompare];
                if (!ReadMemoryExact(hProcess, epAddress, memBytes, bytesToCompare))
                    return 0;

                uint fileOffset = RvaToFileOffset(diskPe, diskPe.AddressOfEntryPoint);
                if (fileOffset == 0)
                    return 0;

                using (var fs = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (fileOffset + bytesToCompare > fs.Length)
                        return 0;

                    fs.Position = fileOffset;
                    byte[] diskBytes = new byte[bytesToCompare];
                    int total = 0;
                    while (total < bytesToCompare)
                    {
                        int read = fs.Read(diskBytes, total, bytesToCompare - total);
                        if (read <= 0) break;
                        total += read;
                    }

                    if (total < bytesToCompare)
                        return 0;

                    int diff = 0;
                    for (int i = 0; i < bytesToCompare; i++)
                    {
                        if (memBytes[i] != diskBytes[i])
                            diff++;
                    }

                    return diff;
                }
            }
            catch
            {
                return 0;
            }
        }

        private static uint RvaToFileOffset(PeInfo pe, uint rva)
        {
            try
            {
                if (pe.Sections == null || pe.Sections.Count == 0)
                {
                    if (rva < pe.SizeOfHeaders)
                        return rva;
                    return 0;
                }

                foreach (var s in pe.Sections)
                {
                    uint start = s.VirtualAddress;
                    uint size = Math.Max(s.VirtualSize, s.SizeOfRawData);
                    uint end = start + size;

                    if (rva >= start && rva < end)
                        return s.PointerToRawData + (rva - start);
                }

                if (rva < pe.SizeOfHeaders)
                    return rva;

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int CountExecutablePrivateRegions(IntPtr hProcess, int stopAfter)
        {
            IntPtr address = IntPtr.Zero;
            int found = 0;

            try
            {
                while (true)
                {
                    MEMORY_BASIC_INFORMATION mbi;
                    int result = VirtualQueryEx(hProcess, address, out mbi, (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>());
                    if (result == 0) break;

                    bool committed = (mbi.State & MEM_COMMIT) == MEM_COMMIT;
                    bool isPrivate = (mbi.Type & MEM_PRIVATE) == MEM_PRIVATE;
                    bool executable =
                        (mbi.Protect & PAGE_EXECUTE) == PAGE_EXECUTE ||
                        (mbi.Protect & PAGE_EXECUTE_READ) == PAGE_EXECUTE_READ ||
                        (mbi.Protect & PAGE_EXECUTE_READWRITE) == PAGE_EXECUTE_READWRITE ||
                        (mbi.Protect & PAGE_EXECUTE_WRITECOPY) == PAGE_EXECUTE_WRITECOPY;
                    bool badProtect =
                        (mbi.Protect & PAGE_GUARD) == PAGE_GUARD ||
                        (mbi.Protect & PAGE_NOACCESS) == PAGE_NOACCESS;
                    {
                        found++;
                        if (found >= stopAfter)
                            break;
                    }

                    long next = mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64();
                    if (next <= 0 || next <= address.ToInt64()) break;
                    address = new IntPtr(next);
                }
            }
            catch
            {
            }

            return found;
        }

        private static bool ReadMemoryExact(IntPtr hProcess, IntPtr address, byte[] buffer, int size)
        {
            try
            {
                if (buffer == null || buffer.Length < size)
                    return false;

                if (!ReadProcessMemory(hProcess, address, buffer, size, out int read))
                    return false;

                return read == size;
            }
            catch
            {
                return false;
            }
        }

        private static void CheckSuspiciousMemory(Process p, HashSet<string> suspicions)
        {
            try
            {
                ProcessModuleCollection modules;
                try { modules = p.Modules; }
                catch { modules = null; }

                if (modules != null)
                {
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

                ScanExecutablePrivateMemory(p, suspicions);
            }
            catch { }
        }

        private static void ScanExecutablePrivateMemory(Process p, HashSet<string> suspicions)
        {
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ, false, p.Id);
                if (hProcess == IntPtr.Zero) return;

                int found = CountExecutablePrivateRegions(hProcess, 3);

                if (found > 0)
                    suspicions.Add($"Исполняемые приватные области памяти: {found}");
            }
            catch { }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    try { CloseHandle(hProcess); } catch { }
            }
        }

        private static Dictionary<int, int> BuildParentMap()
        {
            var map = new Dictionary<int, int>();

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process"))
                {
                    searcher.Options.Timeout = TimeSpan.FromSeconds(5);
                    using (var results = searcher.Get())
                    {
                        foreach (ManagementObject obj in results)
                        {
                            try
                            {
                                int pid = Convert.ToInt32(obj["ProcessId"]);
                                int ppid = Convert.ToInt32(obj["ParentProcessId"]);
                                map[pid] = ppid;
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }

            return map;
        }

        private static Dictionary<int, List<TcpConnectionRecord>> BuildTcpMap()
        {
            var result = new Dictionary<int, List<TcpConnectionRecord>>();
            IntPtr buffer = IntPtr.Zero;

            try
            {
                int size = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref size, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (size <= 0)
                    return result;

                buffer = Marshal.AllocHGlobal(size);
                uint ret = GetExtendedTcpTable(buffer, ref size, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0)
                    return result;

                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = IntPtr.Add(buffer, 4);
                int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();

                for (int i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(rowPtr);

                    var rec = new TcpConnectionRecord
                    {
                        Pid = unchecked((int)row.owningPid),
                        LocalEndPoint = new IPEndPoint(new IPAddress(row.localAddr), ConvertPort(row.localPort)),
                        RemoteEndPoint = new IPEndPoint(new IPAddress(row.remoteAddr), ConvertPort(row.remotePort)),
                        State = (TcpState)row.state
                    };

                    if (!result.TryGetValue(rec.Pid, out var list))
                    {
                        list = new List<TcpConnectionRecord>();
                        result[rec.Pid] = list;
                    }

                    list.Add(rec);
                    rowPtr = IntPtr.Add(rowPtr, rowSize);
                }
            }
            catch { }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }

            return result;
        }

        private static int ConvertPort(uint port)
        {
            byte[] bytes = BitConverter.GetBytes(port);
            return (bytes[0] << 8) + bytes[1];
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