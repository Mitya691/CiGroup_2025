using System;
using System.Threading;
using System.Net.Sockets;

namespace Sharp7
{

    class MsgSocket
    {
        private Socket _tcpSocket;
        private int _readTimeout = 2000;
        private int _writeTimeout = 2000;
        private int _connectTimeout = 1000;
        private int _lastError;
        public MsgSocket()
        {
        }

        ~MsgSocket()
        {
            Close();
        }

        public void Close()
        {
            if (_tcpSocket != null)
            {
                _tcpSocket.Dispose();
                _tcpSocket = null;
            }
        }

        private void CreateSocket()
        {
            _tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _tcpSocket.NoDelay = true;
        }

        private void TcpPing(string host, int port)
        {
            // To Ping the PLC an Asynchronous socket is used rather then an ICMP packet.
            // This allows the use also across Internet and Firewalls (obviously the port must be opened)           
            _lastError = 0;
            Socket pingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {

                IAsyncResult result = pingSocket.BeginConnect(host, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(_connectTimeout, true);

                if (!success)
                {
                    _lastError = S7Consts.ErrTcpConnectionFailed;
                }
            }
            catch
            {
                _lastError = S7Consts.ErrTcpConnectionFailed;
            }

            pingSocket.Close();
        }

        public int Connect(string host, int port)
        {
            _lastError = 0;
            if (!Connected)
            {
                TcpPing(host, port);
                if (_lastError == 0)
                    try
                    {
                        CreateSocket();
                        _tcpSocket.Connect(host, port);
                    }
                    catch
                    {
                        _lastError = S7Consts.ErrTcpConnectionFailed;
                    }
            }
            return _lastError;
        }

        private int WaitForData(int size, int timeout)
        {
            bool expired = false;
            int sizeAvail;
            int elapsed = Environment.TickCount;
            _lastError = 0;
            try
            {
                sizeAvail = _tcpSocket.Available;
                while ((sizeAvail < size) && (!expired))
                {
                    Thread.Sleep(2);
                    sizeAvail = _tcpSocket.Available;
                    expired = Environment.TickCount - elapsed > timeout;
                    // If timeout we clean the buffer
                    if (expired && (sizeAvail > 0))
                    {
                        try
                        {
                            byte[] flush = new byte[sizeAvail];
                            _tcpSocket.Receive(flush, 0, sizeAvail, SocketFlags.None);
                        }
                        catch
                        {
                            _lastError = S7Consts.ErrTcpDataReceive;
                        }
                    }
                }
            }
            catch
            {
                _lastError = S7Consts.ErrTcpDataReceive;
            }
            if (expired)
            {
                _lastError = S7Consts.ErrTcpDataReceive;
            }
            return _lastError;
        }

        public int Receive(byte[] buffer, int start, int size)
        {

            int bytesRead = 0;
            _lastError = WaitForData(size, _readTimeout);
            if (_lastError == 0)
            {
                try
                {
                    bytesRead = _tcpSocket.Receive(buffer, start, size, SocketFlags.None);
                }
                catch
                {
                    _lastError = S7Consts.ErrTcpDataReceive;
                }
                if (bytesRead == 0) // Connection Reset by the peer
                {
                    _lastError = S7Consts.ErrTcpDataReceive;
                    Close();
                }
            }
            return _lastError;
        }

        public int Send(byte[] buffer, int size)
        {
            _lastError = 0;
            try
            {
                _tcpSocket.Send(buffer, size, SocketFlags.None);
            }
            catch
            {
                _lastError = S7Consts.ErrTcpDataSend;
                Close();
            }
            return _lastError;
        }

        public bool Connected
        {
            get
            {
                return (_tcpSocket != null) && (_tcpSocket.Connected);
            }
        }

        public int ReadTimeout
        {
            get
            {
                return _readTimeout;
            }
            set
            {
                _readTimeout = value;
            }
        }

        public int WriteTimeout
        {
            get
            {
                return _writeTimeout;
            }
            set
            {
                _writeTimeout = value;
            }

        }
        public int ConnectTimeout
        {
            get
            {
                return _connectTimeout;
            }
            set
            {
                _connectTimeout = value;
            }
        }
    }
}