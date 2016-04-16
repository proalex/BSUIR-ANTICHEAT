using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Checks
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

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
