﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Runner
    {
        public static void Main(string[] args)
        {
            GameProcess game = new GameProcess("test.exe");
            game.Start();

            if (game.Running)
            {
                if (game.EnableDebugPrivilege())
                {
                    byte[] buffer = new byte[1];

                    if(Checks.ReadMemory(game, "ntdll.dll", 0, 1, buffer))
                    {
                        Console.WriteLine(String.Format("{0:X}", buffer[0]));
                    }

                    if (Checks.FindWindow("Cheat Engine 6.5"))
                    {
                        Console.WriteLine("Yep");
                    }

                    Console.WriteLine(Checks.FileHash("test.exe"));

                    string hash = "";

                    if (Checks.ReadMemoryHash(game, "ntdll.dll", 0, 1, ref hash))
                    {
                        Console.WriteLine(hash);
                    }

                    game.WaitForExit();
                }
            }
        }
    }
}
