using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Client
{
    public struct PatternElement
    {
        public byte data;
        public bool check;
    }

    public class Checks
    {
        const int MEM_COMMIT = 0x00001000;
        const int PAGE_READWRITE = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, 
            out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32", SetLastError = true)]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32", SetLastError = true)]
        static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, 
            [Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static int FindPatternInMemory(GameProcess game, PatternElement[] pattern)
        {
            int result = 0;

            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            IntPtr proc_min_address = sys_info.minimumApplicationAddress;
            IntPtr proc_max_address = sys_info.maximumApplicationAddress;
            long proc_min_address_l = (long)proc_min_address;
            long proc_max_address_l = (long)proc_max_address;
            MEMORY_BASIC_INFORMATION mem_basic_info = new MEMORY_BASIC_INFORMATION();

            IntPtr bytesRead;

            while (proc_min_address_l < proc_max_address_l)
            {
                if (VirtualQueryEx(game.Handle, proc_min_address, out mem_basic_info,
                     (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                {
                    int i = Marshal.GetLastWin32Error();

                    if (i == 0)
                    {
                        break;
                    }

                    return -1;
                }

                if (mem_basic_info.Protect == PAGE_READWRITE 
                    && mem_basic_info.State == MEM_COMMIT)
                {
                    byte[] buffer = new byte[mem_basic_info.RegionSize.ToInt64()];

                    ReadProcessMemory(game.Handle, (IntPtr)mem_basic_info.BaseAddress,
                        buffer, (uint)mem_basic_info.RegionSize.ToInt32(), out bytesRead);

                    int found = 0;

                    for (long i = 0; i < mem_basic_info.RegionSize.ToInt64(); i++)
                    {
                        byte data = buffer[i];

                        if (!pattern[found].check || pattern[found].data == data)
                        {
                            found++;
                        }
                        else
                        {
                            found = 0;
                        }

                        if (found == pattern.Length)
                        {
                            found = 0;
                            result++;
                        }
                    }
                }

                proc_min_address_l += mem_basic_info.RegionSize.ToInt64();
                proc_min_address = new IntPtr(proc_min_address_l);
            }

            return result;
        }

        public static string FileHash(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                }
            }
        }

        public static bool IsDllLoaded(GameProcess game, string hash)
        {
            for (int i = 0; i < game.Modules.Count; i++)
            {
                var module = game.Modules[i];

                if (FileHash(module.FileName) == hash)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool FindWindow(string caption)
        {
            return FindWindow(null, caption).ToInt64() == 0 ? false : true;
        }

        public static bool ReadMemoryHash(GameProcess game, string moduleName, long offset,
            int size, ref string hash)
        {
            byte[] buffer = new byte[size];

            if (!ReadMemory(game, moduleName, offset, size, buffer))
            {
                return false;
            }

            using (var md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", "");
            }

            return true;
        }

        public static bool ReadMemory(GameProcess game, string moduleName, long offset, 
            int size, byte[] buffer)
        {
            if (!game.Running)
            {
                return false;
            }

            IntPtr baseAddress = IntPtr.Zero;

            if (moduleName == null)
            {
                baseAddress = game.BaseAddress;
            }
            else
            {
                for (int i = 0; i < game.Modules.Count; i++)
                {
                    var module = game.Modules[i];

                    if (module.ModuleName == moduleName)
                    {
                        baseAddress = module.BaseAddress;
                        break;
                    }
                }

                if (baseAddress == IntPtr.Zero)
                {
                    return false;
                }
            }

            IntPtr read;

            ReadProcessMemory(game.Handle, (IntPtr)(offset + baseAddress.ToInt64()),
                buffer, (uint) size, out read);

            if (read.ToInt64() != size)
            {
                return false;
            }

            return true;
        }
    }
}
