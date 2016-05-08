using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Client
{
    public class GameProcess
    {
        private static uint _standardRightsRequired = 0x000F0000;
        private static uint _standardRightsRead = 0x00020000;
        private static uint _tokenAssignPrimary = 0x0001;
        private static uint _tokenDuplicate = 0x0002;
        private static uint _tokenImpersonate = 0x0004;
        private static uint _tokenQuery = 0x0008;
        private static uint _tokenQuerySource = 0x0010;
        private static uint _tokenAdjustPrivileges = 0x0020;
        private static uint _tokenAdjustGroups = 0x0040;
        private static uint _tokenAdjustDefault = 0x0080;
        private static uint _tokenAdjustSessionid = 0x0100;
        private static uint _tokenRead = (_standardRightsRead | _tokenQuery);
        private static uint _tokenAllAccess = (_standardRightsRequired | _tokenAssignPrimary |
            _tokenDuplicate | _tokenImpersonate | _tokenQuery | _tokenQuerySource |
            _tokenAdjustPrivileges | _tokenAdjustGroups | _tokenAdjustDefault |
            _tokenAdjustSessionid);
        public const string SeAssignprimarytokenName = "SeAssignPrimaryTokenPrivilege";
        public const string SeAuditName = "SeAuditPrivilege";
        public const string SeBackupName = "SeBackupPrivilege";
        public const string SeChangeNotifyName = "SeChangeNotifyPrivilege";
        public const string SeCreateGlobalName = "SeCreateGlobalPrivilege";
        public const string SeCreatePagefileName = "SeCreatePagefilePrivilege";
        public const string SeCreatePermanentName = "SeCreatePermanentPrivilege";
        public const string SeCreateSymbolicLinkName = "SeCreateSymbolicLinkPrivilege";
        public const string SeCreateTokenName = "SeCreateTokenPrivilege";
        public const string SeDebugName = "SeDebugPrivilege";
        public const string SeEnableDelegationName = "SeEnableDelegationPrivilege";
        public const string SeImpersonateName = "SeImpersonatePrivilege";
        public const string SeIncBasePriorityName = "SeIncreaseBasePriorityPrivilege";
        public const string SeIncreaseQuotaName = "SeIncreaseQuotaPrivilege";
        public const string SeIncWorkingSetName = "SeIncreaseWorkingSetPrivilege";
        public const string SeLoadDriverName = "SeLoadDriverPrivilege";
        public const string SeLockMemoryName = "SeLockMemoryPrivilege";
        public const string SeMachineAccountName = "SeMachineAccountPrivilege";
        public const string SeManageVolumeName = "SeManageVolumePrivilege";
        public const string SeProfSingleProcessName = "SeProfileSingleProcessPrivilege";
        public const string SeRelabelName = "SeRelabelPrivilege";
        public const string SeRemoteShutdownName = "SeRemoteShutdownPrivilege";
        public const string SeRestoreName = "SeRestorePrivilege";
        public const string SeSecurityName = "SeSecurityPrivilege";
        public const string SeShutdownName = "SeShutdownPrivilege";
        public const string SeSyncAgentName = "SeSyncAgentPrivilege";
        public const string SeSystemEnvironmentName = "SeSystemEnvironmentPrivilege";
        public const string SeSystemProfileName = "SeSystemProfilePrivilege";
        public const string SeSystemtimeName = "SeSystemtimePrivilege";
        public const string SeTakeOwnershipName = "SeTakeOwnershipPrivilege";
        public const string SeTcbName = "SeTcbPrivilege";
        public const string SeTimeZoneName = "SeTimeZonePrivilege";
        public const string SeTrustedCredmanAccessName = "SeTrustedCredManAccessPrivilege";
        public const string SeUndockName = "SeUndockPrivilege";
        public const string SeUnsolicitedInputName = "SeUnsolicitedInputPrivilege";

        [StructLayout(LayoutKind.Sequential)]
        public struct Luid
        {
            public UInt32 LowPart;
            public Int32 HighPart;
        }

        public const UInt32 SePrivilegeEnabledByDefault = 0x00000001;
        public const UInt32 SePrivilegeEnabled = 0x00000002;
        public const UInt32 SePrivilegeRemoved = 0x00000004;
        public const UInt32 SePrivilegeUsedForAccess = 0x80000000;

        [StructLayout(LayoutKind.Sequential)]
        public struct TokenPrivileges
        {
            public UInt32 PrivilegeCount;
            public Luid Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LuidAndAttributes
        {
            public Luid Luid;
            public UInt32 Attributes;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr processHandle,
            UInt32 desiredAccess, out IntPtr tokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName,
            out Luid lpLuid);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
           [MarshalAs(UnmanagedType.Bool)]bool disableAllPrivileges,
           ref TokenPrivileges newState,
           UInt32 zero,
           IntPtr null1,
           IntPtr null2);

        private Process _process;

        public ProcessModuleCollection Modules
        {
            get { return Running ? _process.Modules : null; }
        }

        public IntPtr BaseAddress
        {
            get { return Running ? _process.MainModule.BaseAddress : IntPtr.Zero; }
        }

        public IntPtr Handle
        {
            get { return _process.Handle; }
        }

        public bool Running
        {
            get { return !_process.HasExited; }
        }

        public GameProcess(string path)
        {
            _process = new Process();
            _process.StartInfo.FileName = path;
        }

        public void Start()
        {
            _process.Start();
        }

        public void WaitForExit()
        {
            if (Running)
            {
                _process.WaitForExit();
            }
        }

        public void Refresh()
        {
            _process.Refresh();
        }

        public void Kill()
        {
            if (Running)
            {
                _process.Kill();
            }
        }

        public bool EnableDebugPrivilege()
        {
            IntPtr hToken;
            Luid luidSeDebugNameValue;
            TokenPrivileges tkpPrivileges;

            if (!Running)
            {
                return false;
            }

            if (!OpenProcessToken(_process.Handle, _tokenAdjustPrivileges | _tokenQuery, out hToken))
            {
                return false;
            }

            if (!LookupPrivilegeValue(null, SeDebugName, out luidSeDebugNameValue))
            {
                return false;
            }

            tkpPrivileges.PrivilegeCount = 1;
            tkpPrivileges.Luid = luidSeDebugNameValue;
            tkpPrivileges.Attributes = SePrivilegeEnabled;

            if (!AdjustTokenPrivileges(hToken, false, ref tkpPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
            {
                return false;
            }

            CloseHandle(hToken);
            return true;
        }
    }
}
