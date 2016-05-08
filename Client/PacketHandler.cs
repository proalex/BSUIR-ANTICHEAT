using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public delegate Packet Handler(Session session, Packet request);

    public class PacketHandler
    {
        private Dictionary<Opcodes, Handler> _handlers = new Dictionary<Opcodes, Handler>();
        private static PacketHandler _instance;
        
        public static PacketHandler Instance => _instance ?? (_instance = new PacketHandler());

        private PacketHandler()
        {
            _handlers.Add(Opcodes.MemoryPattern, MemoryPatternHandler);
            _handlers.Add(Opcodes.FileHash, FileHashHandler);
            _handlers.Add(Opcodes.Module, ModuleHandler);
            _handlers.Add(Opcodes.Window, WindowHandler);
            _handlers.Add(Opcodes.MemoryHash, MemoryHashHandler);
            _handlers.Add(Opcodes.StartGame, StartGameHandler);
            _handlers.Add(Opcodes.Ping, PingHandler);
        }

        public Packet Handle(Session session, Packet request)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            return _handlers[request.Opcode].Invoke(session, request);
        }

        private Packet PingHandler(Session session, Packet request)
        {
            return new Packet(Opcodes.Ping, BitConverter.GetBytes(true));
        }

        private Packet FileHashHandler(Session session, Packet request)
        {
            string hash = Checks.FileHash(Encoding.UTF8.GetString(request.Data));
            return new Packet(Opcodes.FileHash, Encoding.UTF8.GetBytes(hash));
        }

        private Packet ModuleHandler(Session session, Packet request)
        {
            string hash = Encoding.UTF8.GetString(request.Data);
            bool result = Checks.IsDllLoaded(session.Game, hash);
            return new Packet(Opcodes.Module, BitConverter.GetBytes(result));
        }

        private Packet WindowHandler(Session session, Packet request)
        {
            bool result = Checks.FindWindow(Encoding.UTF8.GetString(request.Data));
            return new Packet(Opcodes.Window, BitConverter.GetBytes(result));
        }

        private Packet MemoryHashHandler(Session session, Packet request)
        {
            ushort length = BitConverter.ToUInt16(request.Data, 0);
            string moduleName = Encoding.UTF8.GetString(request.Data, 2, length);
            long offset = BitConverter.ToInt64(request.Data, length + 2);
            int size = BitConverter.ToInt32(request.Data, length + 10);
            string hash= "";

            if (length > 0
                ? Checks.ReadMemoryHash(session.Game, moduleName, offset, size, ref hash)
                : Checks.ReadMemoryHash(session.Game, null, offset, size, ref hash))
            {
                return null;
            }

            return new Packet(Opcodes.MemoryHash, Encoding.UTF8.GetBytes(hash));
        }

        private Packet MemoryPatternHandler(Session session, Packet request)
        {
            PatternElement[] pattern = new PatternElement[request.Data.Length / 2];

            for (int i = 0; i < request.Data.Length / 2; i++)
            {
                pattern[i].Data = request.Data[i * 2];
                pattern[i].Check = BitConverter.ToBoolean(request.Data, i * 2 + 1);
            }

            long result = Checks.FindPatternInMemory(session.Game, pattern);

            if (result == -1)
            {
                return null;
            }

            return new Packet(Opcodes.MemoryPattern, BitConverter.GetBytes(result));
        }

        private Packet StartGameHandler(Session session, Packet request)
        {
            if (session.RunGame(Encoding.UTF8.GetString(request.Data)) == false)
            {
                return null;
            }

            return new Packet(Opcodes.StartGame, BitConverter.GetBytes(true));
        }
    }
}
