using System;
using System.Collections.Generic;
using System.Text;

namespace Client
{
    public delegate Packet Handler(Packet request);

    public class PacketHandler
    {
        private Session _session;
        private Dictionary<Opcodes, Handler> _handlers = new Dictionary<Opcodes, Handler>();

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
            _handlers.Add(Opcodes.START_GAME, StartGameHandler);
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

            var length = BitConverter.ToUInt16(request.Data, 0);
            var path = Encoding.UTF8.GetString(request.Data, 2, length);
            var hash = Checks.FileHash(path);
            var response = new Packet(Opcodes.FILE_HASH, Encoding.Default.GetBytes(hash));
            return response;
        }

        private Packet ModuleHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            var length = BitConverter.ToUInt16(request.Data, 0);
            var module = Encoding.UTF8.GetString(request.Data, 2, length);
            var result = Checks.IsDllLoaded(_session.Game, module);
            var response = new Packet(Opcodes.MODULE, BitConverter.GetBytes(result));
            return response;
        }

        private Packet WindowHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            var length = BitConverter.ToUInt16(request.Data, 0);
            var window = Encoding.UTF8.GetString(request.Data, 2, length);
            var result = Checks.FindWindow(window);
            var response = new Packet(Opcodes.WINDOW, BitConverter.GetBytes(result));
            return response;
        }

        private Packet MemoryHashHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            var length = BitConverter.ToUInt16(request.Data, 0);
            var moduleName = Encoding.UTF8.GetString(request.Data, 2, length);
            var offset = BitConverter.ToInt64(request.Data, length + 2);
            var size = BitConverter.ToInt32(request.Data, length + 10);
            var hash= "";

            if (length > 0
                ? Checks.ReadMemoryHash(_session.Game, moduleName, offset, size, ref hash)
                : Checks.ReadMemoryHash(_session.Game, null, offset, size, ref hash))
            {
                return null;
            }

            var response = new Packet(Opcodes.MEMORY_HASH, Encoding.Default.GetBytes(hash));
            return response;
        }

        private Packet MemoryPatternHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            var length = BitConverter.ToUInt16(request.Data, 0);

            if (length % 2 > 0 && length > 0)
            {
                return null;
            }

            var pattern = new PatternElement[length / 2];

            for (var i = 0; i < length / 2; i++)
            {
                pattern[i].data = request.Data[i * 2 + 2];
                pattern[i].check = BitConverter.ToBoolean(request.Data, i * 2 + 2);
            }

            var result = Checks.FindPatternInMemory(_session.Game, pattern);

            if (result == -1)
            {
                return null;
            }

            var response = new Packet(Opcodes.MEMORY_PATTERN, BitConverter.GetBytes(result));
            return response;
        }

        private Packet StartGameHandler(Packet request)
        {
            if (request == null)
            {
                throw new NullReferenceException("request is null");
            }

            var length = BitConverter.ToUInt16(request.Data, 0);
            var path = Encoding.UTF8.GetString(request.Data, 2, length);

            if (_session.RunGame(path) == false)
            {
                return null;
            }

            var response = new Packet(Opcodes.START_GAME, BitConverter.GetBytes(true));
            return response;
        }
    }
}
