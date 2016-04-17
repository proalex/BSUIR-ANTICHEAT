using System;

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

                    if (Checks.IsDllLoaded(game, "097AA1113BF9C60994C2425C7547B760"))
                    {
                        Console.WriteLine("yep2");
                    }

                    PatternElement[] pattern = new PatternElement[2];

                    pattern[0].check = true;
                    pattern[0].data = 0x90;
                    pattern[1].check = true;
                    pattern[1].data = 0x90;
                    Console.WriteLine(Checks.FindPatternInMemory(game, pattern));
                    game.WaitForExit();
                }
            }
        }
    }
}
