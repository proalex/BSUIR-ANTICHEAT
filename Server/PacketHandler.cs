using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public delegate bool Handler(Session session, Packet response);

    public class PacketHandler
    {
        private Dictionary<Opcodes, Handler> _handlers = new Dictionary<Opcodes, Handler>();
        private static PacketHandler _instance;
        public static PacketHandler Instance => _instance ?? (_instance = new PacketHandler());

        private PacketHandler()
        {
            _handlers.Add(Opcodes.MemoryPattern, BasicHandler);
            _handlers.Add(Opcodes.FileHash, BasicHandler);
            _handlers.Add(Opcodes.Module, BasicHandler);
            _handlers.Add(Opcodes.Window, BasicHandler);
            _handlers.Add(Opcodes.MemoryHash, BasicHandler);
            _handlers.Add(Opcodes.StartGame, BasicHandler);
        }

        public bool Handle(Session session, Packet response)
        {
            if (response == null)
            {
                throw new NullReferenceException("response is null");
            }
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }

            return _handlers[response.Opcode].Invoke(session, response);
        }

        private bool BasicHandler(Session session, Packet response)
        {
            var queryCheck = session.RequestedChecks.Where(x => x.Number == response.Number);

            if (queryCheck.Count() != 1)
            {
                Console.WriteLine("IP: {0}:{1} Invalid check number.", 
                    session.RemoteIPEndPoint.Address, session.RemoteIPEndPoint.Port);
                return false;
            }

            var check = queryCheck.First();

            if (check.Opcode != response.Opcode)
            {
                Console.WriteLine("IP: {0}:{1} Invalid opcode.",
                    session.RemoteIPEndPoint.Address, session.RemoteIPEndPoint.Port);
                return false;
            }

            if (response.Data.Length != check.Data.Length || 
                response.Data.SequenceEqual(check.Data))
            {
                Console.WriteLine("IP: {0}:{1} Invalid result. Opcode {2}",
                    session.RemoteIPEndPoint.Address, session.RemoteIPEndPoint.Port,
                    check.Opcode);
                return false;
            }

            if (session.State == SessionState.GameHashUnchecked)
            {
                session.State = SessionState.GameHashChecked;
            }
            else if (session.State == SessionState.GameHashChecked)
            {
                session.State = SessionState.GameStarted;
            }

            session.RequestedChecks.Remove(check);
            return true;
        }
    }
}
