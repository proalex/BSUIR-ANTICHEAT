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
        private int _checkIndex = 0;

        public Session(IPEndPoint remoteIPEndPoint)
        {
            if (remoteIPEndPoint == null)
            {
                throw new NullReferenceException("remoteIPEndPoint is null");
            }

            RemoteIPEndPoint = remoteIPEndPoint;
        }

        public ushort NextCheckNumber()
        {
            return _checkNumber++;
        }

        public int NextCheckIndex(int checksCount)
        {
            if (_checkIndex >= checksCount - 1)
            {
                _checkIndex = 0;
            }
            else
            {
                _checkIndex++;
            }

            return _checkIndex;
        }
    }
}
