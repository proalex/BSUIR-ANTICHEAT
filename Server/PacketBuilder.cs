using System;
using System.Text;

namespace Server
{
    public struct PatternElement
    {
        public byte Data;
        public bool Check;
    }

    public static class PacketBuilder
    {
        public static Packet BuildFileHash(Session session, string path)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (path == null)
            {
                throw new NullReferenceException("path is null");
            }

            return new Packet(Opcodes.FileHash, Encoding.UTF8.GetBytes(path), 
                session.nextCheckNumber());
        }

        public static Packet BuildStartGame(Session session, string path)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (path == null)
            {
                throw new NullReferenceException("path is null");
            }

            return new Packet(Opcodes.StartGame, Encoding.UTF8.GetBytes(path), 
                session.nextCheckNumber());
        }

        public static Packet BuildModuleGame(Session session, string hash)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (hash == null)
            {
                throw new NullReferenceException("hash is null");
            }

            return new Packet(Opcodes.Module, Encoding.UTF8.GetBytes(hash), 
                session.nextCheckNumber());
        }

        public static Packet BuildWindow(Session session, string caption)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (caption == null)
            {
                throw new NullReferenceException("caption is null");
            }

            return new Packet(Opcodes.Window, Encoding.UTF8.GetBytes(caption),
                session.nextCheckNumber());
        }

        public static Packet BuildMemoryPattern(Session session, PatternElement[] pattern)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (pattern == null)
            {
                throw new NullReferenceException("pattern is null");
            }

            byte[] data = new byte[pattern.Length*2];

            for (int i = 0; i < pattern.Length; i++)
            {
                data[i*2] = pattern[i].Data;
                data[i*2 + 1] = pattern[i].Check ? (byte)1 : (byte)0;
            }

            return new Packet(Opcodes.MemoryPattern, data, session.nextCheckNumber());
        }

        public static Packet BuildMemoryHash(Session session, string moduleName,
            long offset, int size)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (moduleName == null)
            {
                throw new NullReferenceException("moduleName is null");
            }

            byte[] moduleNameData = Encoding.UTF8.GetBytes(moduleName);
            byte[] data = new byte[moduleNameData.Length + 12];
            byte[] lengthData = BitConverter.GetBytes((ushort) moduleNameData.Length);
            byte[] offsetData = BitConverter.GetBytes(offset);
            byte[] sizeData = BitConverter.GetBytes(size);
            int index = 0;

            Array.Copy(lengthData, data, lengthData.Length);
            index += 2;
            Array.Copy(moduleNameData, 0, data, index, moduleNameData.Length);
            index += moduleNameData.Length;
            Array.Copy(offsetData, 0, data, index, offsetData.Length);
            index += offsetData.Length;
            Array.Copy(sizeData, 0, data, index, sizeData.Length);
            return new Packet(Opcodes.MemoryHash, data, session.nextCheckNumber());
        }
    }
}
