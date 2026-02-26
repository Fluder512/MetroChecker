using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            if (cleanFileCache)
                TryCleanFileCache();

            _afterMb = GetUsedMemoryMb();
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

            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) };

            Parallel.ForEach(processes, options, process =>
            {
                try
                {
                    try
                    {
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
                        }

                        string name;
                        try { name = process.ProcessName?.ToLowerInvariant(); }
                        catch { name = null; }

                        if (!includeSystem && (name == "system" || name == "idle" || name == "smss" || name == "csrss"))
                            return;

                        bool success = false;
                        try
                        {
                            success = EmptyWorkingSet(process.Handle);
                        }
                        catch
                        {
                            success = false;
                        }

                        if (!success)
                        {
                            try
                            {
                                SetProcessWorkingSetSize(process.Handle, new IntPtr(-1), new IntPtr(-1));
                                success = true;
                            }
                            catch { }
                        }

                        if (success)
                            System.Threading.Interlocked.Increment(ref _trimmedCount);
                        else
                            System.Threading.Interlocked.Increment(ref _failedCount);
                    }
                    finally
                    {
                        try { process.Dispose(); } catch { }
                    }
                }
                catch { }
            });
        }

        private void TryCleanFileCache()
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
                    return;

                if (!LookupPrivilegeValue(null, "SeIncreaseQuotaPrivilege", out LUID luid))
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
                SetSystemFileCacheSize(new IntPtr(-1), new IntPtr(-1), 0);
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