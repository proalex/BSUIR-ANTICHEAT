using System;

namespace Client
{
    public enum Opcodes : byte
    {
        MEMORY_PATTERN,
        FILE_HASH,
        MODULE,
        WINDOW,
        MEMORY_HASH,
        START_GAME
    }

    public class Packet
    {
        private Opcodes _opcode;
        private byte[] _data;

        public Opcodes Opcode
        {
            get { return _opcode; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public Packet(Opcodes opcode, byte[] data)
        {
            if (data == null)
            {
                throw new NullReferenceException("data is null");
            }

            _opcode = opcode;
            _data = data;
        }
    }
}
