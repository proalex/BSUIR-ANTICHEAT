using System;
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

            if (game.IsRunning())
            {
                game.WaitForExit();
            }
        }
    }
}
