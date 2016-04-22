using System;
using Client;

namespace FileHash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hash (test.exe): " + Checks.FileHash("test.exe"));
            Console.ReadLine();
        }
    }
}
