using System;
using Client;

namespace FileHash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hash (hash): " + Checks.FileHash("hash"));
            Console.ReadLine();
        }
    }
}
