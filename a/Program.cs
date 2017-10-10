using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;

namespace a
{
    class Program
    {
        static private TcpListener _tcpListener;
        static private List<Thread> _threadList;
        static void Main(string[] args)
        {
            Console.WriteLine("Server is starting in a moment");
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            foreach (IPAddress curAdd in heserver.AddressList) {
               _tcpListener = new TcpListener(curAdd, 5000);
            }
            _tcpListener.Start();
            _threadList = new List<Thread>();
            var th = new Thread(loopForConnection);
            th.Start();
        }

        private static void loopForConnection() {
            Console.WriteLine("Server started. Waiting for connection.");
            Thread thread = null;
            while(true) {
                if (_tcpListener.Pending()) {
                    thread = new Thread(acceptConnection);
                    _threadList.Add(thread);
                    thread.Start(_tcpListener.AcceptTcpClient());
                }
            }
        }

        private static void acceptConnection(object client) {
            Console.WriteLine("Accepted Connection");
            TcpClient cl = (TcpClient)client;
            Console.WriteLine("Successful connection");
            Thread.Sleep(5000);
            cl.Close();
        }
    }

}
