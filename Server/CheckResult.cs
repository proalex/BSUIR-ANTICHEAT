using System;

namespace Server
{
    public class CheckResult
    {
        public readonly uint Number;
        public readonly Opcodes Opcode;
        public readonly byte[] Data;

        public CheckResult(uint number, Opcodes opcode, byte[] data)
        {
            if (data == null)
            {
                throw new NullReferenceException("data is null");
            }

            Number = number;
            Opcode = opcode;
            Data = data;
        }
    }
}
