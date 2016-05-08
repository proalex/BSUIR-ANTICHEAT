using System;

namespace Server
{
    public enum Opcodes : byte
    {
        MemoryPattern,
        FileHash,
        Module,
        Window,
        MemoryHash,
        StartGame,
        Ping
    }

    public class Packet
    {
        public readonly Opcodes Opcode;
        public readonly byte[] Data;
        public readonly ushort Number;

        public Packet(Opcodes opcode, byte[] data, ushort number)
        {
            if (data == null)
            {
                throw new NullReferenceException("data is null");
            }

            Opcode = opcode;
            Data = data;
            Number = number;
        }
    }
}
