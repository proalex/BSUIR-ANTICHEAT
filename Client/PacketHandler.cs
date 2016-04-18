using System;
using System.Collections.Generic;
using System.Text;

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
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            ushort length = BitConverter.ToUInt16(request.Data, 0);
            string path = Encoding.UTF8.GetString(request.Data, 2, length);
            string hash = Checks.FileHash(path);
            Packet response = new Packet(Opcodes.FILE_HASH, Encoding.Default.GetBytes(hash));
            return response;
        }

        private Packet ModuleHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            ushort length = BitConverter.ToUInt16(request.Data, 0);
            string module = Encoding.UTF8.GetString(request.Data, 2, length);
            bool result = Checks.IsDllLoaded(_session.Game, module);
            Packet response = new Packet(Opcodes.MODULE, BitConverter.GetBytes(result));
            return response;
        }

        private Packet WindowHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            ushort length = BitConverter.ToUInt16(request.Data, 0);
            string window = Encoding.UTF8.GetString(request.Data, 2, length);
            bool result = Checks.FindWindow(window);
            Packet response = new Packet(Opcodes.WINDOW, BitConverter.GetBytes(result));
            return response;
        }

        private Packet MemoryHashHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            ushort length = BitConverter.ToUInt16(request.Data, 0);
            string moduleName = Encoding.UTF8.GetString(request.Data, 2, length);
            long offset = BitConverter.ToInt64(request.Data, length + 2);
            int size = BitConverter.ToInt32(request.Data, length + 10);
            bool result;
            string hash= "";

            if (length > 0)
            {
                result = Checks.ReadMemoryHash(_session.Game, moduleName, offset, size, ref hash);
            }
            else
            {
                result = Checks.ReadMemoryHash(_session.Game, null, offset, size, ref hash);
            }

            if (!result)
            {
                return null;
            }

            Packet response = new Packet(Opcodes.MEMORY_HASH, Encoding.Default.GetBytes(hash));
            return response;
        }

        private Packet MemoryPatternHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            ushort length = BitConverter.ToUInt16(request.Data, 0);

            if (length % 2 > 0 && length > 0)
            {
                return null;
            }

            PatternElement[] pattern = new PatternElement[length / 2];

            for (int i = 0; i < length / 2; i++)
            {
                pattern[i].data = request.Data[i * 2 + 2];
                pattern[i].check = BitConverter.ToBoolean(request.Data, i * 2 + 2);
            }

            uint result = Checks.FindPatternInMemory(_session.Game, pattern);
            Packet response = new Packet(Opcodes.MEMORY_PATTERN, BitConverter.GetBytes(result));
            return response;
        }
    }
}
