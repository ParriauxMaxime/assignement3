using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;

namespace a
{
    class Server
    {
        private TcpListener _tcpListener;
        private List<Thread> _threadList;
        private List<Client> _clientList;
        public Int32 port { get; }
        private IPAddress address { get; }
        private Thread _mainThread;
        private bool _isRunning = false;

        public Server(IPAddress address, Int32 port)
        {
            Console.WriteLine("Server is starting in a moment");
            this.port = port;
            this.address = address;
            _tcpListener = new TcpListener(address, port);
            _tcpListener.Start();
            _threadList = new List<Thread>();
            _clientList = new List<Client>();
            _mainThread = new Thread(runServer);
        }

        public void Start()
        {
            this._isRunning = true;
            _mainThread.Start();
        }
        protected void runServer()
        {
            Console.WriteLine("Server started. Waiting for connection.");
            Thread thread = null;
            while (this._isRunning)
            {
                if (_tcpListener.Pending())
                {
                    thread = new Thread(acceptConnection);
                    _threadList.Add(thread);
                    thread.Start(_tcpListener.AcceptTcpClient());
                }
            }
        }

        protected void acceptConnection(object client)
        {
            Console.WriteLine("Accepted Connection");
            Client c = new Client((TcpClient)client);
            _clientList.Add(c);
            Console.WriteLine("Successful connection of client n°{0}.", c.Index);
            Console.WriteLine(c.Read());
            c.Write("l");
            Thread.Sleep(5000);
            c.Write("Hello world !");
            this.killConnection(c);
        }

        protected void killConnection(Client cl)
        {
            int number = cl.Index;
            cl.Close();
            _clientList.Remove(cl);
            Console.WriteLine("Client n°{0} disconnected.", number);
        }

        public void Stop()
        {
            this._isRunning = false;
            try
            {
                foreach (Client t in _clientList)
                {
                    this.killConnection(t);
                }
                foreach (Thread t in _threadList)
                {
                    t.Join(0);
                }
            }
            catch (Exception) {
                //Who cares ?
            }
            _threadList.Clear();
            _mainThread.Join(0);
        }
    }
}