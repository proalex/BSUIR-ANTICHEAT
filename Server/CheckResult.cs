using System;

namespace Server
{
    public class CheckResult
    {
        public readonly uint Number;
        public readonly int Id;
        public readonly Opcodes Opcode;
        public readonly byte[] Data;
        public readonly bool Exist;
        public readonly bool Kick;
        public readonly bool Log;

        public CheckResult(uint number, int id, Opcodes opcode, bool exist, bool kick, 
            bool log, byte[] data)
        {
            if (data == null)
            {
                throw new NullReferenceException("data is null");
            }

            Number = number;
            Opcode = opcode;
            Data = data;
            Id = id;
            Exist = exist;
            Kick = kick;
            Log = log;
        }
    }
}
