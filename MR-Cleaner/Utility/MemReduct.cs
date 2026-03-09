using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MR_Cleaner.Utility
{
    internal sealed class MemReduct
    {
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemFileCacheSize(IntPtr MinimumFileCacheSize, IntPtr MaximumFileCacheSize, uint Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        private static extern uint NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

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

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
        private const uint TOKEN_QUERY = 0x08;
        private const uint SE_PRIVILEGE_ENABLED = 0x02;

        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private const int SystemMemoryListInformation = 80;
        private const int MemoryCaptureAccessedBits = 1;
        private const int MemoryFlushModifiedList = 3;
        private const int MemoryPurgeStandbyList = 4;
        private const int MemoryPurgeLowPriorityStandbyList = 5;
        private const int MemoryCombineLists = 6;

        private const uint FILE_CACHE_MAX_HARD_ENABLE = 0x00000001;
        private const uint FILE_CACHE_MIN_HARD_ENABLE = 0x00000002;

        private static readonly HashSet<string> SystemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system",
            "idle",
            "smss",
            "csrss",
            "wininit",
            "winlogon",
            "services",
            "lsass",
            "lsm",
            "svchost",
            "dwm",
            "registry",
            "fontdrvhost",
            "memory compression",
            "searchindexer"
        };

        private int _trimmedCount;
        private int _failedCount;
        private long _usedBeforeMb;
        private long _usedAfterMb;
        private long _availableBeforeMb;
        private long _availableAfterMb;

        public void CleanMemory(bool includeSystem = false, bool cleanFileCache = true)
        {
            _trimmedCount = 0;
            _failedCount = 0;

            ReadMemorySnapshot(out _usedBeforeMb, out _availableBeforeMb);

            IntPtr token = IntPtr.Zero;
            try
            {
                token = OpenPrivileges();
                if (token != IntPtr.Zero)
                {
                    EnablePrivilege(token, "SeDebugPrivilege");
                    EnablePrivilege(token, "SeIncreaseQuotaPrivilege");
                    EnablePrivilege(token, "SeProfileSingleProcessPrivilege");
                }

                ForceGarbageCollection();

                TrimAllProcesses(includeSystem, 2);

                ForceGarbageCollection();

                if (cleanFileCache)
                    TryCleanFileCache();

                TryPurgeMemoryLists(2);

                ForceGarbageCollection();
                TrimCurrentProcess();
            }
            finally
            {
                if (token != IntPtr.Zero)
                    CloseHandle(token);
            }

            ReadMemorySnapshot(out _usedAfterMb, out _availableAfterMb);
        }

        private static IntPtr OpenPrivileges()
        {
            return OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr token)
                ? token
                : IntPtr.Zero;
        }

        private static bool EnablePrivilege(IntPtr token, string name)
        {
            if (token == IntPtr.Zero)
                return false;

            if (!LookupPrivilegeValue(null, name, out LUID luid))
                return false;

            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                }
            };

            return AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }

        private void ForceGarbageCollection()
        {
            GCSettingsHelper.CompactLargeObjectHeapOnce();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GCSettingsHelper.CompactLargeObjectHeapOnce();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            TrimCurrentProcess();
        }

        private void TrimCurrentProcess()
        {
            IntPtr current = GetCurrentProcess();
            TryTrimHandle(current);
        }

        private void TrimAllProcesses(bool includeSystem, int passes)
        {
            for (int pass = 0; pass < passes; pass++)
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
                int maxParallelism = Math.Max(1, Environment.ProcessorCount);

                Parallel.ForEach(
                    processes,
                    new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
                    process =>
                    {
                        try
                        {
                            if (!process.HasExited)
                                TrimProcess(process, includeSystem, currentPid);
                        }
                        catch
                        {
                            Interlocked.Increment(ref _failedCount);
                        }
                        finally
                        {
                            try
                            {
                                process.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    });
            }
        }

        private void TrimProcess(Process process, bool includeSystem, int currentPid)
        {
            int pid;
            try
            {
                pid = process.Id;
            }
            catch
            {
                Interlocked.Increment(ref _failedCount);
                return;
            }

            if (pid == currentPid || pid <= 4)
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
                try
                {
                    name = process.ProcessName;
                }
                catch
                {
                    return;
                }

                if (SystemProcesses.Contains(name))
                    return;
            }

            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION | PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ, false, pid);
                if (handle == IntPtr.Zero)
                {
                    Interlocked.Increment(ref _failedCount);
                    return;
                }

                if (TryTrimHandle(handle))
                    Interlocked.Increment(ref _trimmedCount);
                else
                    Interlocked.Increment(ref _failedCount);
            }
            finally
            {
                if (handle != IntPtr.Zero)
                    CloseHandle(handle);
            }
        }

        private static bool TryTrimHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return false;

            bool ok = false;

            try
            {
                if (SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1)))
                    ok = true;
            }
            catch
            {
            }

            try
            {
                if (EmptyWorkingSet(handle))
                    ok = true;
            }
            catch
            {
            }

            try
            {
                if (SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1)))
                    ok = true;
            }
            catch
            {
            }

            return ok;
        }

        private void TryCleanFileCache()
        {
            try
            {
                SetSystemFileCacheSize(IntPtr.Zero, IntPtr.Zero, FILE_CACHE_MIN_HARD_ENABLE | FILE_CACHE_MAX_HARD_ENABLE);
            }
            catch { }

            try
            {
                SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0);
            }
            catch { }
        }

        private void TryPurgeMemoryLists(int rounds)
        {
            for (int i = 0; i < rounds; i++)
            {
                PurgeList(MemoryCaptureAccessedBits);
                PurgeList(MemoryFlushModifiedList);
                PurgeList(MemoryPurgeStandbyList);
                PurgeList(MemoryPurgeLowPriorityStandbyList);
                PurgeList(MemoryCombineLists);
            }
        }

        private static void PurgeList(int command)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(ptr, command);
                NtSetSystemInformation(SystemMemoryListInformation, ptr, sizeof(int));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        private static void ReadMemorySnapshot(out long usedMb, out long availableMb)
        {
            var state = new MEMORYSTATUSEX
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            if (GlobalMemoryStatusEx(ref state))
            {
                usedMb = (long)((state.ullTotalPhys - state.ullAvailPhys) >> 20);
                availableMb = (long)(state.ullAvailPhys >> 20);
                return;
            }

            usedMb = 0;
            availableMb = 0;
        }

        public string GetSummary()
        {
            long freedUsedMb = Math.Max(0, _usedBeforeMb - _usedAfterMb);
            long gainedAvailableMb = Math.Max(0, _availableAfterMb - _availableBeforeMb);

            var sb = new StringBuilder();
            sb.AppendLine($"Обработано процессов: {_trimmedCount}");
            sb.AppendLine($"Не удалось обработать: {_failedCount}");
            sb.AppendLine($"Использовалось до очистки: {_usedBeforeMb} MB");
            sb.AppendLine($"Использовалось после очистки: {_usedAfterMb} MB");
            sb.AppendLine($"Доступно до очистки: {_availableBeforeMb} MB");
            sb.AppendLine($"Доступно после очистки: {_availableAfterMb} MB");
            return sb.ToString();
        }

        private static class GCSettingsHelper
        {
            public static void CompactLargeObjectHeapOnce()
            {
                try
                {
                    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                }
                catch
                {
                }
            }
        }
    }
}