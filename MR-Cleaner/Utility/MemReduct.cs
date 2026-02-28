using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MR_Cleaner.Utility
{
    internal class MemReduct
    {
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        private static extern bool SetSystemFileCacheSize(IntPtr MinimumFileCacheSize, IntPtr MaximumFileCacheSize, uint Flags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("ntdll.dll")]
        private static extern uint NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint SE_PRIVILEGE_ENABLED = 0x0002;
        private const int SystemMemoryListInformation = 80;
        private const int MemoryPurgeStandbyList = 4;
        private const int MemoryFlushModifiedList = 3;

        private static readonly string[] SystemProcessNames =
        {
            "system", "idle", "smss", "csrss", "wininit", "winlogon",
            "services", "lsass", "lsm", "svchost", "dwm"
        };

        private int _trimmedCount;
        private int _failedCount;
        private long _beforeMb;
        private long _afterMb;

        public void CleanMemory(bool includeSystem = false, bool cleanFileCache = false)
        {
            _trimmedCount = 0;
            _failedCount = 0;
            _beforeMb = GetUsedMemoryMb();

            TrimAllProcesses(includeSystem);

            ForceGarbageCollection();

            if (cleanFileCache)
                TryCleanFileCache();

            TryPurgeStandbyList();

            _afterMb = GetUsedMemoryMb();
        }

        private void ForceGarbageCollection()
        {
            for (int i = 0; i < 2; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
            }

            SetProcessWorkingSetSize(GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1));
        }

        private void TrimAllProcesses(bool includeSystem)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                return;
            }

            int currentPid = Process.GetCurrentProcess().Id;
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            Parallel.ForEach(processes, options, process =>
            {
                try
                {
                    TrimProcess(process, includeSystem, currentPid);
                }
                catch { }
                finally
                {
                    try { process.Dispose(); } catch { }
                }
            });
        }

        private void TrimProcess(Process process, bool includeSystem, int currentPid)
        {
            try
            {
                if (process.Id == currentPid)
                    return;

                if (process.Id <= 4)
                    return;

                if (!includeSystem)
                {
                    try
                    {
                        if (process.SessionId == 0)
                            return;
                    }
                    catch
                    {
                        return;
                    }

                    string name;
                    try { name = process.ProcessName?.ToLowerInvariant(); }
                    catch { return; }

                    if (IsSystemProcess(name))
                        return;
                }

                bool success = TryTrimProcess(process.Handle);

                if (success)
                    Interlocked.Increment(ref _trimmedCount);
                else
                    Interlocked.Increment(ref _failedCount);
            }
            catch
            {
                Interlocked.Increment(ref _failedCount);
            }
        }

        private bool TryTrimProcess(IntPtr handle)
        {
            try
            {
                if (EmptyWorkingSet(handle))
                    return true;
            }
            catch { }

            try
            {
                SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
                return true;
            }
            catch { }

            return false;
        }

        private bool IsSystemProcess(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;

            foreach (var sys in SystemProcessNames)
            {
                if (name == sys)
                    return true;
            }

            return false;
        }

        private void TryCleanFileCache()
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
                    return;

                EnablePrivilege(token, "SeIncreaseQuotaPrivilege");
                EnablePrivilege(token, "SeProfileSingleProcessPrivilege");

                SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0);
            }
            catch { }
            finally
            {
                if (token != IntPtr.Zero)
                    try { CloseHandle(token); } catch { }
            }
        }

        private void EnablePrivilege(IntPtr token, string privilegeName)
        {
            if (!LookupPrivilegeValue(null, privilegeName, out LUID luid))
                return;

            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                }
            };

            AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }

        private void TryPurgeStandbyList()
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
                    return;

                EnablePrivilege(token, "SeProfileSingleProcessPrivilege");

                var commandFlush = MemoryFlushModifiedList;
                var ptrFlush = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(ptrFlush, commandFlush);
                NtSetSystemInformation(SystemMemoryListInformation, ptrFlush, sizeof(int));
                Marshal.FreeHGlobal(ptrFlush);

                var commandPurge = MemoryPurgeStandbyList;
                var ptrPurge = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(ptrPurge, commandPurge);
                NtSetSystemInformation(SystemMemoryListInformation, ptrPurge, sizeof(int));
                Marshal.FreeHGlobal(ptrPurge);
            }
            catch { }
            finally
            {
                if (token != IntPtr.Zero)
                    try { CloseHandle(token); } catch { }
            }
        }

        private long GetUsedMemoryMb()
        {
            try
            {
                var status = new MEMORYSTATUSEX();
                status.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
                if (GlobalMemoryStatusEx(ref status))
                    return (long)((status.ullTotalPhys - status.ullAvailPhys) / 1024 / 1024);
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public string GetSummary()
        {
            long freed = Math.Max(0, _beforeMb - _afterMb);
            var sb = new StringBuilder();
            sb.AppendLine($"Обработано процессов: {_trimmedCount}");
            sb.AppendLine($"Не удалось обработать: {_failedCount}");
            sb.AppendLine($"Память до очистки: {_beforeMb} MB");
            sb.AppendLine($"Память после очистки: {_afterMb} MB");
            sb.AppendLine($"Освобождено: ~{freed} MB");
            return sb.ToString();
        }
    }
}