using System;

namespace Client
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

        public Packet(Opcodes opcode, byte[] data)
        {
            if (data == null)
            {
                throw new NullReferenceException("data is null");
            }

            Opcode = opcode;
            Data = data;
        }
    }
}
