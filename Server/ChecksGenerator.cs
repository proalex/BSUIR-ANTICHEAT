using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Database;

namespace Server
{
    public class ChecksGenerator
    {
        private static ChecksGenerator _instance;
        public static ChecksGenerator Instance => 
            _instance ?? (_instance = new ChecksGenerator());
        private List<Object> _checks;

        private ChecksGenerator()
        {
            
        }

        public void LoadChecks(List<Object> checks)
        {
            if (_checks != null)
            {
                return;
            }
            if (checks == null)
            {
                throw new NullReferenceException("checks is null");
            }

            _checks = checks;
        }

        public Packet[] Generate(Session session)
        {
            if (session == null)
            {
                throw new NullReferenceException("session is null");
            }

            if (session.Timeout.ElapsedMilliseconds < Config.CheckInterval * 1000 &&
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
                if (_checks != null && _checks.Count > 0)
                {
                    var checksCount = Config.ChecksCount > _checks.Count() ? 
                        _checks.Count() : Config.ChecksCount;

                    for (int i = 0; i < checksCount; i++)
                    {
                        var check = _checks[session.NextCheckIndex(_checks.Count)];

                        if (check.GetType() == typeof(WindowChecks))
                        {
                            packets.Add(CheckWindow(session, check as WindowChecks));
                        }
                        else if (check.GetType() == typeof(ModuleChecks))
                        {
                            packets.Add(CheckModule(session, check as ModuleChecks));
                        }
                        else if (check.GetType() == typeof(FileChecks))
                        {
                            packets.Add(CheckFile(session, check as FileChecks));
                        }
                        else if (check.GetType() == typeof(MemoryChecks))
                        {
                            packets.Add(CheckMemory(session, check as MemoryChecks));
                        }
                        else if (check.GetType() == typeof(MemoryPatterns))
                        {
                            packets.Add(MemoryPattern(session, check as MemoryPatterns));
                        }
                    }
                }
                else
                {
                    packets.Add(Ping(session));
                }
            }

            return packets.ToArray();
        }

        private Packet CheckWindow(Session session, WindowChecks check)
        {
            Packet request = PacketBuilder.WindowCheck(session, check.Name);
            CheckResult result = new CheckResult(request.Number,
                check.Id, Opcodes.Window, check.Exist, check.Kick, true,
                BitConverter.GetBytes(true));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet CheckModule(Session session, ModuleChecks check)
        {
            Packet request = PacketBuilder.ModuleCheck(session, check.Hash);
            CheckResult result = new CheckResult(request.Number,
                check.Id, Opcodes.Module, check.Exist, check.Kick, true,
                BitConverter.GetBytes(true));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet CheckFile(Session session, FileChecks check)
        {
            Packet request = PacketBuilder.FileCheck(session, check.Path);
            CheckResult result = new CheckResult(request.Number,
                check.Id, Opcodes.FileHash, check.Exist, check.Kick, true,
                Encoding.UTF8.GetBytes(check.Hash));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet CheckMemory(Session session, MemoryChecks check)
        {
            Packet request;

            if (check.ModuleName.Length > 0)
            {
                request = PacketBuilder.MemoryCheck(session, check.ModuleName, check.Offset, check.Size);
            }
            else
            {
                request = PacketBuilder.MemoryCheck(session, check.Offset, check.Size);
            }

            CheckResult result = new CheckResult(request.Number,
                check.Id, Opcodes.MemoryHash, check.Exist, check.Kick, true,
                Encoding.UTF8.GetBytes(check.Hash));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet MemoryPattern(Session session, MemoryPatterns check)
        {
            PatternElement[] pattern = new PatternElement[check.Pattern.Length / 4];

            for (int i = 0; i < pattern.Length; i++)
            {
                pattern[i].Data = Convert.ToByte(check.Pattern.Substring(i * 4, 2), 16);
                pattern[i].Check = check.Pattern.Substring(i * 4 + 2, 2) != "00";
            }

            Packet request = PacketBuilder.MemoryPattern(session, pattern);
            CheckResult result = new CheckResult(request.Number,
                check.Id, Opcodes.MemoryPattern, check.Exist, check.Kick, true,
                BitConverter.GetBytes(check.Count));
            
            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet CheckGameHash(Session session)
        {
            Packet request = PacketBuilder.FileCheck(session, Config.ExeName);
            CheckResult result = new CheckResult(request.Number, 0, request.Opcode,
                true, true, false, Encoding.UTF8.GetBytes(Config.ExeHash));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet StartGame(Session session)
        {
            Packet request = PacketBuilder.StartGame(session, Config.ExeName);
            CheckResult result = new CheckResult(request.Number, 0, request.Opcode,
                true, true, false, BitConverter.GetBytes(true));

            session.RequestedChecks.Add(result);
            return request;
        }

        private Packet Ping(Session session)
        {
            Packet request = PacketBuilder.Ping(session);
            CheckResult result = new CheckResult(request.Number, 0, request.Opcode,
                true, true, false, BitConverter.GetBytes(true));

            session.RequestedChecks.Add(result);
            return request;
        }
    }
}
