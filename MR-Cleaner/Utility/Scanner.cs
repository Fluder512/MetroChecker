

// MetroChecker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MR_Cleaner.Utility.Scanner
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using Microsoft.Win32;
using MR_Cleaner.Utility;

internal class Scanner
{
    public sealed class SystemScanResult
    {
        public bool TaskManagerEnabled { get; set; }

        public bool RegistryEditorEnabled { get; set; }

        public bool CommandPromptEnabled { get; set; }

        public bool RunDialogEnabled { get; set; }

        public bool ControlPanelEnabled { get; set; }

        public bool ShutdownFromStartEnabled { get; set; }

        public bool MmcEnabled { get; set; }

        public bool ViewOnDriveEnabled { get; set; }

        public bool DrivesVisibleEnabled { get; set; }

        public bool FindEnabled { get; set; }

        public bool ContextMenuEnabled { get; set; }

        public bool FolderOptionsEnabled { get; set; }

        public bool SecurityTabEnabled { get; set; }

        public bool FileMenuEnabled { get; set; }

        public bool CommonGroupsEnabled { get; set; }

        public bool StartMenuLogoffEnabled { get; set; }

        public bool ChangingWallpaperEnabled { get; set; }

        public bool WinKeysEnabled { get; set; }

        public bool SetTaskbarEnabled { get; set; }

        public bool LockWorkstationEnabled { get; set; }

        public bool ChangePasswordEnabled { get; set; }

        public bool TrayContextMenuEnabled { get; set; }

        public bool GroupPolicyUpdateEnabled { get; set; }

        public bool PowerOptionsEnabled { get; set; }

        public bool ContextMenusInStartEnabled { get; set; }

        public bool SystemRestoreEnabled { get; set; }

        public bool SystemConfigEnabled { get; set; }

        public bool LogoffEnabled { get; set; }

        public bool UsbStorEnabled { get; set; }

        public bool AppInitDllsClean { get; set; }

        public string AppInitDlls64 { get; set; } = string.Empty;

        public string AppInitDlls32 { get; set; } = string.Empty;

        public bool AppInitEnabled64 { get; set; }

        public bool AppInitEnabled32 { get; set; }

        public bool DefenderScanStarted { get; set; }

        public bool DefenderScanCompleted { get; set; }

        public string[] DefenderThreats { get; set; } = Array.Empty<string>();
    }

    private sealed class DefenderScanResult
    {
        public bool ScanStarted { get; set; }

        public bool ScanCompleted { get; set; }

        public string[] Threats { get; set; } = Array.Empty<string>();
    }

    public SystemScanResult Scan(bool runDefenderScan)
    {
        SystemScanResult systemScanResult = new SystemScanResult
        {
            TaskManagerEnabled = IsTaskManagerEnabled(),
            RegistryEditorEnabled = IsRegistryEditorEnabled(),
            CommandPromptEnabled = IsCommandPromptEnabled(),
            RunDialogEnabled = IsRunDialogEnabled(),
            ControlPanelEnabled = IsControlPanelEnabled(),
            ShutdownFromStartEnabled = IsShutdownFromStartEnabled(),
            MmcEnabled = IsMmcEnabled(),
            ViewOnDriveEnabled = IsViewOnDriveEnabled(),
            DrivesVisibleEnabled = AreDrivesVisible(),
            FindEnabled = IsFindEnabled(),
            ContextMenuEnabled = IsContextMenuEnabled(),
            FolderOptionsEnabled = AreFolderOptionsEnabled(),
            SecurityTabEnabled = IsSecurityTabEnabled(),
            FileMenuEnabled = IsFileMenuEnabled(),
            CommonGroupsEnabled = AreCommonGroupsEnabled(),
            StartMenuLogoffEnabled = IsStartMenuLogoffEnabled(),
            ChangingWallpaperEnabled = IsChangingWallpaperEnabled(),
            WinKeysEnabled = AreWinKeysEnabled(),
            SetTaskbarEnabled = IsSetTaskbarEnabled(),
            LockWorkstationEnabled = IsLockWorkstationEnabled(),
            ChangePasswordEnabled = IsChangePasswordEnabled(),
            TrayContextMenuEnabled = IsTrayContextMenuEnabled(),
            GroupPolicyUpdateEnabled = IsGroupPolicyUpdateEnabled(),
            PowerOptionsEnabled = ArePowerOptionsEnabled(),
            ContextMenusInStartEnabled = AreContextMenusInStartEnabled(),
            SystemRestoreEnabled = IsSystemRestoreEnabled(),
            SystemConfigEnabled = IsSystemConfigEnabled(),
            LogoffEnabled = IsLogoffEnabled(),
            UsbStorEnabled = IsUsbStorEnabled()
        };
        CheckAppInitDlls(systemScanResult);
        if (runDefenderScan)
        {
            DefenderScanResult defenderScanResult = RunDefenderFullScan();
            systemScanResult.DefenderScanStarted = defenderScanResult.ScanStarted;
            systemScanResult.DefenderScanCompleted = defenderScanResult.ScanCompleted;
            systemScanResult.DefenderThreats = defenderScanResult.Threats ?? Array.Empty<string>();
        }
        return systemScanResult;
    }

    public void FixAll()
    {
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableTaskMgr");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableTaskMgr");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableRegistryTools");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableRegistryTools");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "DisableCMD");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\System", "DisableCMD");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRun");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRun");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoControlPanel");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoControlPanel");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoClose");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoClose");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\MMC", "RestrictToPermittedSnapins");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\MMC", "RestrictToPermittedSnapins");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewOnDrive");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewOnDrive");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDrives");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDrives");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFind");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFind");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewContextMenu");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewContextMenu");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFolderOptions");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFolderOptions");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSecurityTab");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSecurityTab");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFileMenu");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFileMenu");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoCommonGroups");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoCommonGroups");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "StartMenuLogOff");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "StartMenuLogOff");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoChangingWallPaper");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoChangingWallPaper");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoWinKeys");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoWinKeys");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSetTaskbar");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSetTaskbar");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableLockWorkstation");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableLockWorkstation");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableChangePassword");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableChangePassword");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoTrayContextMenu");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoTrayContextMenu");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "DenyUsersFromMachGP");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\System", "DenyUsersFromMachGP");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HidePowerOptions");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HidePowerOptions");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "DisableContextMenusInStart");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "DisableContextMenusInStart");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableConfig");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableConfig");
        RemovePolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLogoff");
        RemovePolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLogoff");
        FixUsbStor();
        FixAppInitDlls();
    }

    private void CheckAppInitDlls(SystemScanResult result)
    {
        try
        {
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows", writable: false))
            {
                if (registryKey != null)
                {
                    object value = registryKey.GetValue("LoadAppInit_DLLs");
                    if (result.AppInitEnabled64 = value != null && Convert.ToInt32(value) == 1)
                    {
                        result.AppInitDlls64 = registryKey.GetValue("AppInit_DLLs")?.ToString() ?? string.Empty;
                    }
                }
            }
            using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows NT\\CurrentVersion\\Windows", writable: false))
            {
                if (registryKey2 != null)
                {
                    object value2 = registryKey2.GetValue("LoadAppInit_DLLs");
                    if (result.AppInitEnabled32 = value2 != null && Convert.ToInt32(value2) == 1)
                    {
                        result.AppInitDlls32 = registryKey2.GetValue("AppInit_DLLs")?.ToString() ?? string.Empty;
                    }
                }
            }
            result.AppInitDllsClean = (!result.AppInitEnabled64 || string.IsNullOrEmpty(result.AppInitDlls64)) && (!result.AppInitEnabled32 || string.IsNullOrEmpty(result.AppInitDlls32));
        }
        catch
        {
            result.AppInitDllsClean = true;
        }
    }

    private void FixAppInitDlls()
    {
        try
        {
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows", writable: true))
            {
                if (registryKey != null)
                {
                    registryKey.SetValue("AppInit_DLLs", "", RegistryValueKind.String);
                    registryKey.SetValue("LoadAppInit_DLLs", 0, RegistryValueKind.DWord);
                }
            }
            RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows NT\\CurrentVersion\\Windows", writable: true);
            if (registryKey2 != null)
            {
                registryKey2.SetValue("AppInit_DLLs", "", RegistryValueKind.String);
                registryKey2.SetValue("LoadAppInit_DLLs", 0, RegistryValueKind.DWord);
            }
        }
        catch
        {
        }
    }

    private bool IsTaskManagerEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableTaskMgr") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableTaskMgr") == 0;
    }

    private bool IsRegistryEditorEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableRegistryTools") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableRegistryTools") == 0;
    }

    private bool IsCommandPromptEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "DisableCMD") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\System", "DisableCMD") == 0;
    }

    private bool IsRunDialogEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRun") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRun") == 0;
    }

    private bool IsControlPanelEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoControlPanel") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoControlPanel") == 0;
    }

    private bool IsShutdownFromStartEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoClose") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoClose") == 0;
    }

    private bool IsMmcEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\MMC", "RestrictToPermittedSnapins") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\MMC", "RestrictToPermittedSnapins") == 0;
    }

    private bool IsViewOnDriveEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewOnDrive") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewOnDrive") == 0;
    }

    private bool AreDrivesVisible()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDrives") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDrives") == 0;
    }

    private bool IsFindEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFind") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFind") == 0;
    }

    private bool IsContextMenuEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewContextMenu") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoViewContextMenu") == 0;
    }

    private bool AreFolderOptionsEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFolderOptions") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFolderOptions") == 0;
    }

    private bool IsSecurityTabEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSecurityTab") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSecurityTab") == 0;
    }

    private bool IsFileMenuEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFileMenu") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoFileMenu") == 0;
    }

    private bool AreCommonGroupsEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoCommonGroups") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoCommonGroups") == 0;
    }

    private bool IsStartMenuLogoffEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "StartMenuLogOff") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "StartMenuLogOff") == 0;
    }

    private bool IsChangingWallpaperEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoChangingWallPaper") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoChangingWallPaper") == 0;
    }

    private bool AreWinKeysEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoWinKeys") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoWinKeys") == 0;
    }

    private bool IsSetTaskbarEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSetTaskbar") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSetTaskbar") == 0;
    }

    private bool IsLockWorkstationEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableLockWorkstation") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableLockWorkstation") == 0;
    }

    private bool IsChangePasswordEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableChangePassword") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableChangePassword") == 0;
    }

    private bool IsTrayContextMenuEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoTrayContextMenu") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoTrayContextMenu") == 0;
    }

    private bool IsGroupPolicyUpdateEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\System", "DenyUsersFromMachGP") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows\\System", "DenyUsersFromMachGP") == 0;
    }

    private bool ArePowerOptionsEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HidePowerOptions") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HidePowerOptions") == 0;
    }

    private bool AreContextMenusInStartEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "DisableContextMenusInStart") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "DisableContextMenusInStart") == 0;
    }

    private bool IsSystemRestoreEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableSR") == 0;
    }

    private bool IsSystemConfigEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableConfig") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableConfig") == 0;
    }

    private bool IsLogoffEnabled()
    {
        return GetPolicyValue(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLogoff") == 0 && GetPolicyValue(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLogoff") == 0;
    }

    private bool IsUsbStorEnabled()
    {
        try
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            RegistryKey registryKey2 = registryKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\USBSTOR", writable: false);
            if (registryKey2 == null)
            {
                return true;
            }
            object value = registryKey2.GetValue("Start");
            if (value == null)
            {
                return true;
            }
            int result;
            int num = ((value is int num2) ? num2 : (int.TryParse(value.ToString(), out result) ? result : 3));
            return num != 4;
        }
        catch
        {
            return true;
        }
    }

    private void FixUsbStor()
    {
        try
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
            RegistryKey registryKey2 = registryKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\USBSTOR", writable: true);
            if (registryKey2 != null)
            {
                object value = registryKey2.GetValue("Start");
                int result;
                int num = ((value is int num2) ? num2 : (int.TryParse(value?.ToString(), out result) ? result : 3));
                if (num == 4)
                {
                    registryKey2.SetValue("Start", 3, RegistryValueKind.DWord);
                }
            }
        }
        catch
        {
        }
    }

    private int GetPolicyValue(RegistryHive hive, string subKey, string valueName)
    {
        try
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            RegistryKey registryKey2 = registryKey.OpenSubKey(subKey, writable: false);
            if (registryKey2 == null)
            {
                return 0;
            }
            object value = registryKey2.GetValue(valueName);
            if (value == null)
            {
                return 0;
            }
            if (value is int result)
            {
                return result;
            }
            if (int.TryParse(value.ToString(), out var result2))
            {
                return result2;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private void RemovePolicyValue(RegistryHive hive, string subKey, string valueName)
    {
        try
        {
            RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            RegistryKey registryKey2 = registryKey.OpenSubKey(subKey, writable: true);
            if (registryKey2 != null && registryKey2.GetValue(valueName) != null)
            {
                registryKey2.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch
        {
        }
    }

    private DefenderScanResult RunDefenderFullScan()
    {
        DefenderScanResult defenderScanResult = new DefenderScanResult();
        string text = ResolveDefenderCmdPath();
        if (string.IsNullOrWhiteSpace(text) || !File.Exists(text))
        {
            return defenderScanResult;
        }
        Process process = new Process();
        process.StartInfo.FileName = text;
        process.StartInfo.Arguments = "-Scan -ScanType 2";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        try
        {
            process.Start();
            defenderScanResult.ScanStarted = true;
            process.WaitForExit();
            defenderScanResult.ScanCompleted = true;
            defenderScanResult.Threats = GetDefenderThreats();
        }
        catch
        {
            defenderScanResult.ScanStarted = false;
            defenderScanResult.ScanCompleted = false;
            defenderScanResult.Threats = Array.Empty<string>();
        }
        return defenderScanResult;
    }

    private string ResolveDefenderCmdPath()
    {
        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string text = Path.Combine(folderPath, "Windows Defender", "MpCmdRun.exe");
        if (File.Exists(text))
        {
            return text;
        }
        string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string path = Path.Combine(folderPath2, "Microsoft", "Windows Defender", "Platform");
        if (!Directory.Exists(path))
        {
            return null;
        }
        try
        {
            string text2 = (from d in Directory.GetDirectories(path)
                            orderby d descending
                            select d).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(text2))
            {
                return null;
            }
            string text3 = Path.Combine(text2, "MpCmdRun.exe");
            if (File.Exists(text3))
            {
                return text3;
            }
        }
        catch
        {
        }
        return null;
    }

    private string[] GetDefenderThreats()
    {
        try
        {
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpThreatDetection");
            string[] array = (from ManagementObject m in managementObjectSearcher.Get()
                              select m["ThreatName"]?.ToString() into s
                              where !string.IsNullOrWhiteSpace(s)
                              select s).Distinct().ToArray();
            return (array.Length == 0) ? Array.Empty<string>() : array;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
