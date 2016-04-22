using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Server
{
    public enum SessionState : byte
    {
        GameHashUnchecked,
        GameHashChecked,
        GameStarted
    }

    public class Session
    {
        public SessionState State;
        public readonly IPEndPoint RemoteIPEndPoint;
        public readonly List<CheckResult> RequestedChecks = new List<CheckResult>();
        public readonly Stopwatch Timeout = new Stopwatch();
        private ushort _checkNumber;

        public Session(IPEndPoint remoteIPEndPoint)
        {
            if (remoteIPEndPoint == null)
            {
                throw new NullReferenceException("remoteIPEndPoint is null");
            }

            RemoteIPEndPoint = remoteIPEndPoint;
        }

        public ushort nextCheckNumber()
        {
            return _checkNumber++;
        }
    }
}
