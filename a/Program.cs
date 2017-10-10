using System;
using System.Net;

namespace a
{
    class Program
    {
        //Code is dirty, who cares, it's C#.

        static void Main(string[] args)
        {
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress curAdd in heserver.AddressList)
            {
                Server server = new Server(curAdd, 5000);
                server.Start();
                Console.WriteLine("Press q to exit");
                while (true)
                {
                    try
                    {

                        char c = (char)Console.ReadLine()[0];
                        if (c == 'q')
                        {
                            server.Stop();
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        //who cares ?
                    }
                }
            }

        }

    }
}
