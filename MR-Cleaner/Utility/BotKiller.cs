using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MR_Cleaner.Utility
{
    internal class BotKiller
    {
        private static int _processesTerminated;
        private static int _filesDeleted;
        private static int _mutexesDetected;
        private static readonly object _lock = new object();
        public static List<string> DetectedThreats { get; } = new List<string>();
        public static List<string> TerminatedProcesses { get; } = new List<string>();

        public static int ProcessesTerminated => _processesTerminated;
        public static int FilesDeleted => _filesDeleted;
        public static int MutexesDetected => _mutexesDetected;

        public static void ResetStats()
        {
            _processesTerminated = 0;
            _filesDeleted = 0;
            _mutexesDetected = 0;
            lock (_lock)
            {
                DetectedThreats.Clear();
                TerminatedProcesses.Clear();
            }
        }

        public static string GetReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"BotKiller Report - {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Убито Процессов: {_processesTerminated}");
            sb.AppendLine($"Файлов Удалено: {_filesDeleted}");
            sb.AppendLine($"Мутексов задетекчено: {_mutexesDetected}");
            lock (_lock)
            {
                if (DetectedThreats.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("Detected threats:");
                    foreach (var t in DetectedThreats.Distinct())
                        sb.AppendLine($"  • {t}");
                }
                if (TerminatedProcesses.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("Terminated processes:");
                    foreach (var p in TerminatedProcesses.Distinct())
                        sb.AppendLine($"  • {p}");
                }
            }
            return sb.ToString();
        }

#pragma warning disable CS0649, CS0169
        public class Win32API
        {
            public enum ObjectInformationClass
            {
                ObjectBasicInformation,
                ObjectNameInformation,
                ObjectTypeInformation,
                ObjectAllTypesInformation,
                ObjectHandleInformation
            }

            [Flags]
            public enum ProcessAccessFlags : uint
            {
                All = 0x1F0FFFu,
                Terminate = 1u,
                CreateThread = 2u,
                VMOperation = 8u,
                VMRead = 0x10u,
                VMWrite = 0x20u,
                DupHandle = 0x40u,
                SetInformation = 0x200u,
                QueryInformation = 0x400u,
                Synchronize = 0x100000u
            }

            public struct OBJECT_BASIC_INFORMATION
            {
                public int Attributes;
                public int GrantedAccess;
                public int HandleCount;
                public int PointerCount;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int NameInformationLength;
                public int TypeInformationLength;
                public int SecurityDescriptorLength;
                public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;
            }

            public struct OBJECT_TYPE_INFORMATION
            {
                public UNICODE_STRING Name;
                public int ObjectCount;
                public int HandleCount;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int Reserved4;
                public int PeakObjectCount;
                public int PeakHandleCount;
                public int Reserved5;
                public int Reserved6;
                public int Reserved7;
                public int Reserved8;
                public int InvalidAttributes;
                public GENERIC_MAPPING GenericMapping;
                int ValidAccess;
                byte Unknown;
                byte MaintainHandleDatabase;
                int PoolType;
                int PagedPoolUsage;
                int NonPagedPoolUsage;
            }

            public struct OBJECT_NAME_INFORMATION
            {
                public UNICODE_STRING Name;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct UNICODE_STRING
            {
                public ushort Length;
                public ushort MaximumLength;
                public IntPtr Buffer;
            }

            public struct GENERIC_MAPPING
            {
                public int GenericRead;
                public int GenericWrite;
                public int GenericExecute;
                public int GenericAll;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SYSTEM_HANDLE_INFORMATION
            {
                public int ProcessID;
                public byte ObjectTypeNumber;
                public byte Flags;
                public ushort Handle;
                public int Object_Pointer;
                public uint GrantedAccess;
            }

            public const int MAX_PATH = 260;
            public const uint STATUS_INFO_LENGTH_MISMATCH = 3221225476u;
            public const int DUPLICATE_SAME_ACCESS = 2;
            public const int DUPLICATE_CLOSE_SOURCE = 1;

            [DllImport("ntdll.dll")]
            public static extern int NtQueryObject(IntPtr ObjectHandle, int ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, ref int returnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

            [DllImport("ntdll.dll")]
            public static extern uint NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int returnLength);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr OpenMutex(uint desiredAccess, bool inheritHandle, string name);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll")]
            public static extern int CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, ushort hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentProcess();

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        }
#pragma warning restore CS0649, CS0169

        public class Win32Processes
        {
            private const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
            private const uint STATUS_INFO_LENGTH_MISMATCH = 3221225476u;

            public static string getObjectTypeName(Win32API.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
            {
                IntPtr hSourceProcessHandle = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
                IntPtr lpTargetHandle = IntPtr.Zero;
                Win32API.OBJECT_BASIC_INFORMATION oBJECT_BASIC_INFORMATION = default;
                IntPtr zero = IntPtr.Zero;
                Win32API.OBJECT_TYPE_INFORMATION oBJECT_TYPE_INFORMATION = default;
                IntPtr zero2 = IntPtr.Zero;
                int returnLength = 0;
                if (!Win32API.DuplicateHandle(hSourceProcessHandle, shHandle.Handle, Win32API.GetCurrentProcess(), out lpTargetHandle, 0u, false, 2u))
                {
                    return null;
                }
                zero = Marshal.AllocHGlobal(Marshal.SizeOf(oBJECT_BASIC_INFORMATION));
                Win32API.NtQueryObject(lpTargetHandle, 0, zero, Marshal.SizeOf(oBJECT_BASIC_INFORMATION), ref returnLength);
                oBJECT_BASIC_INFORMATION = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(zero, typeof(Win32API.OBJECT_BASIC_INFORMATION));
                Marshal.FreeHGlobal(zero);
                zero2 = Marshal.AllocHGlobal(oBJECT_BASIC_INFORMATION.TypeInformationLength);
                returnLength = oBJECT_BASIC_INFORMATION.TypeInformationLength;
                while (Win32API.NtQueryObject(lpTargetHandle, 2, zero2, returnLength, ref returnLength) == -1073741820)
                {
                    Marshal.FreeHGlobal(zero2);
                    zero2 = Marshal.AllocHGlobal(returnLength);
                }
                oBJECT_TYPE_INFORMATION = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(zero2, typeof(Win32API.OBJECT_TYPE_INFORMATION));
                string result = Marshal.PtrToStringUni(!Is64Bits() ? oBJECT_TYPE_INFORMATION.Name.Buffer : new IntPtr(Convert.ToInt64(oBJECT_TYPE_INFORMATION.Name.Buffer.ToString(), 10) >> 32), oBJECT_TYPE_INFORMATION.Name.Length >> 1);
                Marshal.FreeHGlobal(zero2);
                Win32API.CloseHandle(lpTargetHandle);
                return result;
            }

            public static string getObjectName(Win32API.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
            {
                IntPtr hSourceProcessHandle = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
                IntPtr lpTargetHandle = IntPtr.Zero;
                Win32API.OBJECT_BASIC_INFORMATION oBJECT_BASIC_INFORMATION = default;
                IntPtr zero = IntPtr.Zero;
                Win32API.OBJECT_NAME_INFORMATION oBJECT_NAME_INFORMATION = default;
                IntPtr zero2 = IntPtr.Zero;
                int returnLength = 0;
                IntPtr zero3 = IntPtr.Zero;
                if (!Win32API.DuplicateHandle(hSourceProcessHandle, shHandle.Handle, Win32API.GetCurrentProcess(), out lpTargetHandle, 0u, false, 2u))
                {
                    return null;
                }
                zero = Marshal.AllocHGlobal(Marshal.SizeOf(oBJECT_BASIC_INFORMATION));
                Win32API.NtQueryObject(lpTargetHandle, 0, zero, Marshal.SizeOf(oBJECT_BASIC_INFORMATION), ref returnLength);
                oBJECT_BASIC_INFORMATION = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(zero, typeof(Win32API.OBJECT_BASIC_INFORMATION));
                Marshal.FreeHGlobal(zero);
                returnLength = oBJECT_BASIC_INFORMATION.NameInformationLength;
                zero2 = Marshal.AllocHGlobal(returnLength);
                while (Win32API.NtQueryObject(lpTargetHandle, 1, zero2, returnLength, ref returnLength) == -1073741820)
                {
                    Marshal.FreeHGlobal(zero2);
                    zero2 = Marshal.AllocHGlobal(returnLength);
                }
                oBJECT_NAME_INFORMATION = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(zero2, typeof(Win32API.OBJECT_NAME_INFORMATION));
                zero3 = !Is64Bits() ? oBJECT_NAME_INFORMATION.Name.Buffer : new IntPtr(Convert.ToInt64(oBJECT_NAME_INFORMATION.Name.Buffer.ToString(), 10) >> 32);
                if (zero3 != IntPtr.Zero)
                {
                    byte[] destination = new byte[returnLength];
                    try
                    {
                        Marshal.Copy(zero3, destination, 0, returnLength);
                        return Marshal.PtrToStringUni(Is64Bits() ? new IntPtr(zero3.ToInt64()) : new IntPtr(zero3.ToInt32()));
                    }
                    catch (AccessViolationException)
                    {
                        return null;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(zero2);
                        Win32API.CloseHandle(lpTargetHandle);
                    }
                }
                return null;
            }

            public static List<Win32API.SYSTEM_HANDLE_INFORMATION> GetHandles(Process process = null, string IN_strObjectTypeName = null, string IN_strObjectName = null)
            {
                int num = 65536;
                IntPtr intPtr = Marshal.AllocHGlobal(num);
                int returnLength = 0;
                IntPtr zero = IntPtr.Zero;
                while (Win32API.NtQuerySystemInformation(16, intPtr, num, ref returnLength) == 3221225476u)
                {
                    num = returnLength;
                    Marshal.FreeHGlobal(intPtr);
                    intPtr = Marshal.AllocHGlobal(returnLength);
                }
                byte[] destination = new byte[returnLength];
                Marshal.Copy(intPtr, destination, 0, returnLength);
                long num2 = 0L;
                if (Is64Bits())
                {
                    num2 = Marshal.ReadInt64(intPtr);
                    zero = new IntPtr(intPtr.ToInt64() + 8);
                }
                else
                {
                    num2 = Marshal.ReadInt32(intPtr);
                    zero = new IntPtr(intPtr.ToInt32() + 4);
                }
                List<Win32API.SYSTEM_HANDLE_INFORMATION> list = new List<Win32API.SYSTEM_HANDLE_INFORMATION>();
                for (long num3 = 0L; num3 < num2; num3++)
                {
                    Win32API.SYSTEM_HANDLE_INFORMATION sYSTEM_HANDLE_INFORMATION = default;
                    if (Is64Bits())
                    {
                        sYSTEM_HANDLE_INFORMATION = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(zero, typeof(Win32API.SYSTEM_HANDLE_INFORMATION));
                        zero = new IntPtr(zero.ToInt64() + Marshal.SizeOf(sYSTEM_HANDLE_INFORMATION) + 8);
                    }
                    else
                    {
                        zero = new IntPtr(zero.ToInt64() + Marshal.SizeOf(sYSTEM_HANDLE_INFORMATION));
                        sYSTEM_HANDLE_INFORMATION = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(zero, typeof(Win32API.SYSTEM_HANDLE_INFORMATION));
                    }
                    if ((process == null || sYSTEM_HANDLE_INFORMATION.ProcessID == process.Id) && (IN_strObjectTypeName == null || getObjectTypeName(sYSTEM_HANDLE_INFORMATION, Process.GetProcessById(sYSTEM_HANDLE_INFORMATION.ProcessID)) == IN_strObjectTypeName) && (IN_strObjectName == null || getObjectName(sYSTEM_HANDLE_INFORMATION, Process.GetProcessById(sYSTEM_HANDLE_INFORMATION.ProcessID)) == IN_strObjectName))
                    {
                        list.Add(sYSTEM_HANDLE_INFORMATION);
                    }
                }
                Marshal.FreeHGlobal(intPtr);
                return list;
            }

            public static bool Is64Bits()
            {
                return Marshal.SizeOf(typeof(IntPtr)) == 8;
            }
        }

        public static class Paths
        {
            public static readonly string[] CheckedPaths = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Environment.GetFolderPath(Environment.SpecialFolder.Cookies),
                Environment.GetFolderPath(Environment.SpecialFolder.History),
                Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
                Environment.GetFolderPath(Environment.SpecialFolder.Recent),
                Environment.GetFolderPath(Environment.SpecialFolder.SendTo),
                Environment.GetFolderPath(Environment.SpecialFolder.Templates),
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonTemplates),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "config", "systemprofile"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "config", "systemprofile", "AppData", "Local"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "config", "systemprofile", "AppData", "Roaming"),
                Path.GetTempPath(),
                Path.Combine(Path.GetTempPath(), "Low"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Microsoft", "Windows", "INetCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Microsoft", "Windows", "Temporary Internet Files"),
                "C:\\",
                "C:\\Users\\",
                "C:\\ProgramData\\",
                "C:\\Windows\\Tasks\\",
                "C:\\Windows\\SysWOW64\\",
                "C:\\Windows\\System32\\",
                "C:\\Windows\\Prefetch\\",
                "C:\\Windows\\Fonts\\",
                "C:\\Windows\\Installer\\",
                "C:\\Windows\\Debug\\",
                "C:\\Windows\\Cursors\\",
                "C:\\Windows\\Help\\",
                "C:\\Windows\\addins\\",
                "C:\\Windows\\assembly\\",
                "C:\\Windows\\Globalization\\",
                "C:\\Windows\\IME\\",
                "C:\\Windows\\Inf\\",
                "C:\\Windows\\Logs\\",
                "C:\\Windows\\Media\\",
                "C:\\Windows\\Registration\\",
                "C:\\Windows\\Resources\\",
                "C:\\Windows\\SchCache\\",
                "C:\\Windows\\Security\\",
                "C:\\Windows\\ShellExperiences\\",
                "C:\\Windows\\ShellComponents\\",
                "C:\\Windows\\Speech\\",
                "C:\\Windows\\SystemApps\\",
                "C:\\Windows\\Temp\\",
                "C:\\Windows\\WinSxS\\",
                "C:\\PerfLogs\\",
                "C:\\Recovery\\",
                "C:\\Intel\\",
                "C:\\AMD\\",
                "C:\\NVIDIA\\",
                "C:\\Drivers\\",
                "C:\\SWSetup\\",
                "C:\\HP\\",
                "C:\\Dell\\",
                "C:\\Lenovo\\",
                "C:\\Program Files (x86)\\",
                "C:\\Program Files\\",
                "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\",
                "C:\\Users\\Public\\AppData\\Local\\",
                "C:\\Users\\Public\\AppData\\Roaming\\"
            };
        }

        private static readonly string[] SafeProcesses = new string[]
        {
            "c:\\windows\\system32\\wbem\\wmiprvse.exe", "c:\\windows\\system32\\svchost.exe", "c:\\windows\\system32\\lsass.exe",
            "c:\\windows\\system32\\csrss.exe", "c:\\windows\\system32\\wininit.exe", "c:\\windows\\system32\\services.exe",
            "c:\\windows\\system32\\smss.exe", "c:\\windows\\system32\\dwm.exe", "c:\\windows\\system32\\ntoskrnl.exe",
            "c:\\windows\\system32\\cmd.exe", "c:\\windows\\system32\\powershell.exe", "c:\\windows\\system32\\conhost.exe",
            "c:\\windows\\system32\\ctfmon.exe", "c:\\windows\\system32\\winlogon.exe", "c:\\windows\\system32\\spoolsv.exe",
            "c:\\windows\\system32\\wudfhost.exe", "c:\\windows\\system32\\mousocoreworker.exe", "c:\\windows\\system32\\rundll32.exe",
            "c:\\windows\\system32\\dllhost.exe", "c:\\windows\\system32\\fontdrvhost.exe", "c:\\windows\\system32\\dashost.exe",
            "c:\\windows\\system32\\aggregatorhost.exe", "c:\\windows\\system32\\wlanext.exe", "c:\\windows\\system32\\elanfpservice.exe",
            "c:\\windows\\system32\\searchindexer.exe", "c:\\windows\\system32\\securityhealthsystray.exe", "c:\\windows\\system32\\sihost.exe",
            "c:\\windows\\system32\\runtimebroker.exe", "c:\\windows\\system32\\notepad.exe", "c:\\windows\\system32\\musnotifyicon.exe",
            "c:\\windows\\system32\\comppkgsrv.exe", "c:\\windows\\system32\\searchfilterhost.exe", "c:\\windows\\system32\\taskmgr.exe",
            "c:\\windows\\system32\\taskhostw.exe", "c:\\windows\\system32\\applicationframehost.exe", "c:\\windows\\syswow64\\dllhost.exe",
            "c:\\windows\\explorer.exe", "c:\\windows\\regedit.exe"
        };

        private static readonly string[] WhitelistNames = new string[]
        {
            "tlauncher.exe", "java.exe", "discord.exe", "update.exe", "browser.exe",
            "chrome.exe", "opera.exe", "firefox.exe", "telegram.exe"
        };

        public static void Execute()
        {
            ResetStats();
            new Thread(TempClear).Start();
            new Thread(RemoveDefenderExclusions).Start();
            RunBotKiller();
        }

        public static void TempClear()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath)) return;
                foreach (string file in Directory.GetFiles(tempPath, "*", SearchOption.TopDirectoryOnly))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); Interlocked.Increment(ref _filesDeleted); } catch { }
                }
                foreach (string dir in Directory.GetDirectories(tempPath))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }

        public static void RunBotKiller()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process");
                var processes = searcher.Get().Cast<ManagementObject>().ToList();
                Parallel.ForEach(processes, new ParallelOptions { MaxDegreeOfParallelism = 3 }, proc =>
                {
                    try
                    {
                        var execPathObj = proc["ExecutablePath"];
                        if (execPathObj == null) return;
                        string path = execPathObj.ToString();
                        if (string.IsNullOrEmpty(path) || !ShouldScan(path)) return;
                        int pid = Convert.ToInt32(proc["ProcessId"]);
                        if (pid <= 0 || pid == Process.GetCurrentProcess().Id) return;
                        Process process = null;
                        try { process = Process.GetProcessById(pid); } catch { return; }
                        bool isMalicious = false;
                        string threatReason = null;
                        if (path.IndexOf("xdwd", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            isMalicious = true;
                            threatReason = "XDWD prefix detected";
                        }
                        else if (!IsWindowVisible(process.MainWindowTitle) && (IsFileHidden(path) || IsSuspiciousPath(path) || path.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)))
                        {
                            isMalicious = true;
                            threatReason = "Suspicious path/hidden file";
                        }
                        else if (HasMaliciousMutex(process, out string mutexInfo))
                        {
                            isMalicious = true;
                            threatReason = $"Malicious mutex: {mutexInfo}";
                        }
                        if (isMalicious)
                        {
                            string procName = Path.GetFileName(path);
                            lock (_lock)
                            {
                                DetectedThreats.Add($"{procName} - {threatReason}");
                                TerminatedProcesses.Add(path);
                            }
                            try { process.Kill(); Interlocked.Increment(ref _processesTerminated); } catch { }
                            RemoveFile(path);
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }

        private static bool ShouldScan(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string lower = path.ToLowerInvariant();
            foreach (string safe in SafeProcesses)
            {
                if (lower == safe) return false;
            }
            if (lower.Contains("c:\\windows\\system32\\driverstore\\filerepository\\") ||
                lower.Contains("c:\\windows\\systemapps"))
            {
                return false;
            }
            return true;
        }

        private static bool IsSuspiciousPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string fileName = Path.GetFileName(path).ToLowerInvariant();
            string currentPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (path.Equals(currentPath, StringComparison.OrdinalIgnoreCase)) return false;
            foreach (string name in WhitelistNames)
            {
                if (fileName == name) return false;
            }
            string tempPath = Path.GetTempPath().ToLowerInvariant();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).ToLowerInvariant();
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToLowerInvariant();
            string netPath = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\", "Windows\\Microsoft.NET").ToLowerInvariant();
            string lower = path.ToLowerInvariant();
            if (lower.StartsWith(tempPath) || lower.StartsWith(appData) || lower.StartsWith(userProfile) ||
                fileName == "wscript.exe" || lower.StartsWith(netPath))
            {
                return true;
            }
            return false;
        }

        private static bool HasMaliciousMutex(Process process, out string detectedMutex)
        {
            detectedMutex = null;
            try
            {
                string[] mutexes = GetProcessMutexes(process);
                foreach (string mutex in mutexes)
                {
                    if (mutex.Length == 16 ||
                        mutex.StartsWith("DCR", StringComparison.OrdinalIgnoreCase) ||
                        mutex.StartsWith("Client", StringComparison.OrdinalIgnoreCase))
                    {
                        Interlocked.Increment(ref _mutexesDetected);
                        detectedMutex = mutex;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static string[] GetProcessMutexes(Process process)
        {
            List<string> list = new List<string>();
            foreach (var handle in Win32Processes.GetHandles(process, "Mutant"))
            {
                string name = Win32Processes.getObjectName(handle, Process.GetProcessById(handle.ProcessID));
                if (!string.IsNullOrEmpty(name) && name.StartsWith("\\Sessions\\1\\BaseNamedObjects\\") && !name.StartsWith("\\Sessions\\1\\BaseNamedObjects\\SM0:"))
                {
                    list.Add(name.Replace("\\Sessions\\1\\BaseNamedObjects\\", ""));
                }
            }
            return list.ToArray();
        }

        private static bool IsFileHidden(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                return (File.GetAttributes(path) & (FileAttributes.Hidden | FileAttributes.System)) != 0;
            }
            catch { }
            return false;
        }

        private static bool IsWindowVisible(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;
            try
            {
                IntPtr hwnd = Win32API.FindWindow(null, title);
                return hwnd != IntPtr.Zero && Win32API.IsWindowVisible(hwnd);
            }
            catch { }
            return false;
        }

        private static void RemoveFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                RemoveRegistryPersistence("Software\\Microsoft\\Windows\\CurrentVersion\\Run", filePath);
                RemoveRegistryPersistence("Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce", filePath);
                Thread.Sleep(100);
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                    Interlocked.Increment(ref _filesDeleted);
                }
            }
            catch { }
        }

        private static void RemoveRegistryPersistence(string regPath, string payload)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    if (key != null)
                    {
                        foreach (string name in key.GetValueNames())
                        {
                            if (key.GetValue(name)?.ToString() == payload)
                            {
                                key.DeleteValue(name);
                            }
                        }
                    }
                }
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return;
                using (var key = Registry.LocalMachine.OpenSubKey(regPath, true))
                {
                    if (key == null) return;
                    foreach (string name in key.GetValueNames())
                    {
                        if (key.GetValue(name)?.ToString() == payload)
                        {
                            key.DeleteValue(name);
                        }
                    }
                }
            }
            catch { }
        }

        public static void RemoveDefenderExclusions()
        {
            try
            {
                string[] computerIds = GetDefenderComputerIds();
                foreach (string id in computerIds)
                {
                    try
                    {
                        using (var mo = new ManagementObject($"root\\Microsoft\\Windows\\Defender:MSFT_MpPreference.ComputerID='{id}'"))
                        {
                            var parameters = mo.GetMethodParameters("Remove");
                            parameters["ExclusionPath"] = GetExclusionPaths(id);
                            parameters["ExclusionProcess"] = GetExclusionProcesses(id);
                            mo.InvokeMethod("Remove", parameters, null);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static string[] GetDefenderComputerIds()
        {
            List<string> list = new List<string>();
            try
            {
                foreach (ManagementObject mo in new ManagementObjectSearcher("root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpPreference").Get())
                {
                    if (mo["ComputerID"] != null)
                    {
                        list.Add(mo["ComputerID"].ToString());
                    }
                }
            }
            catch { }
            return list.ToArray();
        }

        private static string[] GetExclusionPaths(string computerId)
        {
            try
            {
                foreach (ManagementObject mo in new ManagementObjectSearcher("root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpPreference").Get())
                {
                    if (mo["ComputerID"]?.ToString() != computerId) continue;
                    string currentPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    List<string> exclusions = new List<string>();
                    if (mo["ExclusionPath"] is string[] paths)
                    {
                        foreach (string p in paths)
                        {
                            if (!string.IsNullOrEmpty(p) && !currentPath.Contains(p))
                            {
                                exclusions.Add(p);
                            }
                        }
                    }
                    return exclusions.ToArray();
                }
            }
            catch { }
            return new string[0];
        }

        private static string[] GetExclusionProcesses(string computerId)
        {
            try
            {
                foreach (ManagementObject mo in new ManagementObjectSearcher("root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpPreference").Get())
                {
                    if (mo["ComputerID"]?.ToString() != computerId) continue;
                    string currentPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    List<string> exclusions = new List<string>();
                    if (mo["ExclusionProcess"] is string[] procs)
                    {
                        foreach (string p in procs)
                        {
                            if (!string.IsNullOrEmpty(p) && !currentPath.Contains(p))
                            {
                                exclusions.Add(p);
                            }
                        }
                    }
                    return exclusions.ToArray();
                }
            }
            catch { }
            return new string[0];
        }
    }
}