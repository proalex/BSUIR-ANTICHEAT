using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using Database;

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
            _handlers.Add(Opcodes.Ping, BasicHandler);
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

            bool result = response.Data.SequenceEqual(check.Data);

            if (result ^ check.Exist)
            {
                Console.WriteLine("IP: {0}:{1} Invalid result. Opcode: {2} Id: {3}",
                    session.RemoteIPEndPoint.Address, session.RemoteIPEndPoint.Port,
                    check.Opcode, check.Id);

                if (check.Log)
                {
                    Table<ViolationLog> violationLogTbl = Runner.DB.GetTable<ViolationLog>();
                    ViolationLog newLog = new ViolationLog()
                    {
                        CheckNumber = check.Id,
                        IP = session.RemoteIPEndPoint.Address.ToString(),
                        Type = (byte) check.Opcode,
                        Time = DateTime.Now
                    };

                    violationLogTbl.InsertOnSubmit(newLog);
                    Runner.DB.SubmitChanges();
                }

                if (check.Kick)
                {
                    return false;
                }
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
