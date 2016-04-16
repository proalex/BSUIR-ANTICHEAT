using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Checks
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string sClass, string sWindow);

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

        public static bool FindWindow(string caption)
        {
            return FindWindow(null, caption) == 0 ? false : true;
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
                buffer, size, out read);

            if (read.ToInt64() != size)
            {
                return false;
            }

            return true;
        }
    }
}
