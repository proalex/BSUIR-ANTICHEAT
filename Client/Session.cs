using System;
using System.Net.Sockets;

namespace Client
{
    public class Session
    {
        private GameProcess _game;
        private bool _gameStarted;

        public GameProcess Game
        {
            get { return _game; }
        }

        public bool GameStarted => _gameStarted;

        public Session(Socket socket)
        {
            if (socket == null)
            {
                throw new NullReferenceException("socket is null");
            }
        }

        public bool RunGame(string path)
        {
            if (path == null)
            {
                throw new NullReferenceException("path is null");
            }

            if (_game != null && _game.Running)
            {
                return false;
            }

            GameProcess game = new GameProcess(path);

            game.Start();

            if(!game.EnableDebugPrivilege())
            {
                game.Kill();
                return false;
            }

            _game = game;
            _gameStarted = true;
            return true;
        }

        public void Stop()
        {
            if (_game != null)
            {
                _game.Kill();
            }
        }
    }
}
