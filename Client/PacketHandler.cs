using System;
using System.Collections.Generic;

namespace Client
{
    public delegate Packet Handler(Packet request);

    public class PacketHandler
    {
        private Session _session;
        private Dictionary<Opcodes, Handler> _handlers;

        public PacketHandler(Session session)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }

            _session = session;
            _handlers.Add(Opcodes.MEMORY_PATTERN, MemoryPatternHandler);
            _handlers.Add(Opcodes.FILE_HASH, FileHashHandler);
            _handlers.Add(Opcodes.MODULE, ModuleHandler);
            _handlers.Add(Opcodes.WINDOW, WindowHandler);
            _handlers.Add(Opcodes.MEMORY_HASH, MemoryHashHandler);
        }

        public Packet Handle(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            return _handlers[request.Opcode].Invoke(request);
        }

        private Packet FileHashHandler(Packet request)
        {

        }

        private Packet ModuleHandler(Packet request)
        {

        }

        private Packet WindowHandler(Packet request)
        {

        }

        private Packet MemoryHashHandler(Packet request)
        {

        }

        private Packet MemoryPatternHandler(Packet request)
        {

        }
    }
}
