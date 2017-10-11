using System;
using System.Net;

namespace a
{
    class Program
    {
        //Code is dirty, who cares, it's C#.
        static void Main(string[] args)
        {
            Server server = new Server(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Press q to exit");


            while (true)
            {
                try
                {

                    if (Console.ReadKey().KeyChar == 'q')
                    {
                        server.Stop();
                        return;
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
