using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class ChecksGenerator
    {
        private static ChecksGenerator _instance;
        public static ChecksGenerator Instance => 
            _instance ?? (_instance = new ChecksGenerator());
        private const string _exeName = "test.exe";
        private const string _exeHash = "301437983FECBEDFD493D813F5ECECAE";

        private ChecksGenerator()
        {
            
        }

        public Packet[] Generate(Session session)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }

            if (session.Timeout.ElapsedMilliseconds * 1000 < 30 &&
                session.State == SessionState.GameStarted)
            {
                return new Packet[0];
            }

            List<Packet> packets = new List<Packet>();

            if (session.State == SessionState.GameHashUnchecked)
            {
                packets.Add(CheckGameHash(session));
            }
            else if (session.State == SessionState.GameHashChecked)
            {
                packets.Add(StartGame(session));
            }
            else if (session.State == SessionState.GameStarted)
            {

            }

            return packets.ToArray();
        }

        private Packet CheckGameHash(Session session)
        {
            Packet request = PacketBuilder.BuildFileHash(session, _exeName);
            CheckResult result = new CheckResult(request.Number, request.Opcode,
                Encoding.UTF8.GetBytes(_exeHash));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet StartGame(Session session)
        {
            Packet request = PacketBuilder.BuildStartGame(session, _exeName);
            CheckResult result = new CheckResult(request.Number, request.Opcode,
                BitConverter.GetBytes(true));

            session.RequestedChecks.Add(result);
            return request;
        }
    }
}
