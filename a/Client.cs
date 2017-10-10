using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace a
{
    class Client
    {
        private TcpClient TcpClient { get; set; }
        public int Index { get; }

        private NetworkStream _networkStream;
        static int referenceIndex = 0;
        public Client(TcpClient client)
        {
            Index = referenceIndex;
            TcpClient = client;
            referenceIndex++;
            _networkStream = client.GetStream();
            _networkStream.ReadTimeout = 100;
        }

        public void Close()
        {
            _networkStream.Close();
            TcpClient.Close();
        }

        public string Read()
        {
            Byte[] data = new Byte[256];
            int i;
            string msg = "";
            if (_networkStream.CanRead)
            {
                try {
                    while ((i = _networkStream.Read(data, 0, data.Length)) != 0)
                    {
                        msg += System.Text.Encoding.ASCII.GetString(data, 0, i);
                    }
                    return msg;
                }
                catch (System.IO.IOException) {
                    return msg;
                }
            }
            return msg;
        }
        public int Write(string msg)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
            if (_networkStream.CanWrite)
            {
                _networkStream.Write(data, 0, msg.Length);
                return 0;
            }
            return 1;
        }
    }
}