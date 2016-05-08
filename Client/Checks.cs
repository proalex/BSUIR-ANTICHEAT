using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Client
{
    public struct PatternElement
    {
        public byte Data;
        public bool Check;
    }

    public class Checks
    {
        const int MemCommit = 0x00001000;
        const int PageReadwrite = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicInformation
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public struct SystemInfo
        {
            public ushort ProcessorArchitecture;
            ushort _reserved;
            public uint PageSize;
            public IntPtr MinimumApplicationAddress;
            public IntPtr MaximumApplicationAddress;
            public IntPtr ActiveProcessorMask;
            public uint NumberOfProcessors;
            public uint ProcessorType;
            public uint AllocationGranularity;
            public ushort ProcessorLevel;
            public ushort ProcessorRevision;
        }

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, 
            out MemoryBasicInformation lpBuffer, uint dwLength);

        [DllImport("kernel32", SetLastError = true)]
        public static extern void GetSystemInfo(out SystemInfo lpSystemInfo);

        [DllImport("kernel32", SetLastError = true)]
        static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, 
            [Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static long FindPatternInMemory(GameProcess game, PatternElement[] pattern)
        {
            if (game == null)
            {
                throw new NullReferenceException("game is null");
            }

            if (pattern == null)
            {
                throw new NullReferenceException("pattern is null");
            }

            long result = 0;
            SystemInfo sysInfo = new SystemInfo();

            GetSystemInfo(out sysInfo);

            IntPtr procMinAddress = sysInfo.MinimumApplicationAddress;
            IntPtr procMaxAddress = sysInfo.MaximumApplicationAddress;
            long procMinAddressL = (long)procMinAddress;
            long procMaxAddressL = (long)procMaxAddress;
            MemoryBasicInformation memBasicInfo = new MemoryBasicInformation();
            IntPtr bytesRead;

            while (procMinAddressL < procMaxAddressL)
            {
                if (VirtualQueryEx(game.Handle, procMinAddress, out memBasicInfo,
                     (uint)Marshal.SizeOf(typeof(MemoryBasicInformation))) == 0)
                {
                    int i = Marshal.GetLastWin32Error();

                    if (i == 0)
                    {
                        break;
                    }

                    return -1;
                }

                if (memBasicInfo.Protect != 0 && memBasicInfo.State == MemCommit)
                {
                    byte[] buffer = new byte[memBasicInfo.RegionSize.ToInt64()];

                    ReadProcessMemory(game.Handle, (IntPtr)memBasicInfo.BaseAddress,
                        buffer, (uint)memBasicInfo.RegionSize.ToInt32(), out bytesRead);

                    int found = 0;

                    for (long i = 0; i < memBasicInfo.RegionSize.ToInt64(); i++)
                    {
                        byte data = buffer[i];

                        if (!pattern[found].Check || pattern[found].Data == data)
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

                procMinAddressL += memBasicInfo.RegionSize.ToInt64();
                procMinAddress = new IntPtr(procMinAddressL);
            }

            return result;
        }

        public static string FileHash(string filename)
        {
            if (filename == null)
            {
                throw new NullReferenceException("filename is null");
            }

            using (MD5 md5 = MD5.Create())
            {
                try
                {
                    using (FileStream stream = File.OpenRead(filename))
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                    }
                }
                catch (FileNotFoundException)
                {
                    return "";
                }
                catch (DirectoryNotFoundException)
                {
                    return "";
                }
            }
        }

        public static bool IsDllLoaded(GameProcess game, string hash)
        {
            if (game == null)
            {
                throw new NullReferenceException("game is null");
            }

            if (hash == null)
            {
                throw new NullReferenceException("hash is null");
            }

            game.Refresh();

            for (int i = 0; i < game.Modules.Count; i++)
            {
                ProcessModule module = game.Modules[i];

                if (FileHash(module.FileName) == hash)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool FindWindow(string caption)
        {
            if (caption == null)
            {
                throw new NullReferenceException("caption is null");
            }

            return FindWindow(null, caption).ToInt64() == 0 ? false : true;
        }

        public static bool ReadMemoryHash(GameProcess game, string moduleName, long offset,
            int size, ref string hash)
        {
            if (game == null)
            {
                throw new NullReferenceException("game is null");
            }

            byte[] buffer = new byte[size];

            if (!ReadMemory(game, moduleName, offset, size, buffer))
            {
                return false;
            }

            using (MD5 md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(buffer)).Replace("-", "");
            }

            return true;
        }

        public static bool ReadMemory(GameProcess game, string moduleName, long offset, 
            int size, byte[] buffer)
        {
            if (game == null)
            {
                throw new NullReferenceException("game is null");
            }

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
                    ProcessModule module = game.Modules[i];

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
