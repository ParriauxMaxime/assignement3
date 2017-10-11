using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace a
{
    class Server
    {
        public Int32 port { get; }
        public IPAddress address { get; }

        private TcpListener _tcpListener;
        private bool _isRunning = false;

        public Server(IPAddress address, Int32 port)
        {
            Console.WriteLine("Server is starting in a moment");
            this.port = port;
            this.address = address;
            _tcpListener = new TcpListener(address, port);
        }

        public void Start()
        {
            this._isRunning = true;

            try
            {
                _tcpListener.Start();
            }
            catch
            {
                Console.WriteLine("Could not start TCP listener!");
                return;
            }

            // FIXME: use cancellationtoken to stop
            Task.Factory.StartNew(() =>
            {
                while (this._isRunning)
                {
                    var client = _tcpListener.AcceptTcpClient();
                    Console.WriteLine("Connected");

                    var thread = new Thread(handleRequest);
                    thread.Start(client);
                }
            }, TaskCreationOptions.LongRunning);

            Console.WriteLine("anan");
        }

        void handleRequest(object clientObj)
        {
            if (clientObj is null) return;
            if (!(clientObj is TcpClient client)) return;

            try
            {
                string requestString = Read(client);
                dynamic json = JsonConvert.DeserializeObject<dynamic>(requestString);

                Response response = new Response();

                if (json.method == "read") {
                    string path = json.path;
                    string end = Path.GetFileName(path);


                    if (end == "categories")
                    {
                        response.Status = Response.StatusCode.Ok;
                        response.Body = Category.Data;

                    } 
                    else if (int.TryParse(end, out var id))
                    {
                        response.Status = Response.StatusCode.Ok;
                        var cat = Category.Data.Find(x => x.Id == id);
                        response.Body = cat;
                    }
                }

                Send(client, response.ToJson());

                client.GetStream().Close();
                client.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        public void Stop()
        {
            this._isRunning = false;
        }

        static void Send(NetworkStream strm, string data)
        {
            var response = Encoding.UTF8.GetBytes(data);
            strm.Write(response, 0, response.Length);
        }

        static string Read(NetworkStream strm, int size)
        {
            byte[] buffer = new byte[size];
            var bytesRead = strm.Read(buffer, 0, buffer.Length);
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return request;
        }

        static string Read(TcpClient client)
        {
            return Read(client.GetStream(), client.ReceiveBufferSize);
        }

        static void Send(TcpClient client, string data)
        {
            Send(client.GetStream(), data);
        }
    }

    class Request
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
    }

    class Response
    {
        public enum StatusCode
        {
            Ok = 1,
            Created,
            Updated,
            BadRequest,
            NotFound,
            Error
        };

        public List<string> Reasons = new List<string>();
        private StatusCode _status;

        public StatusCode Status
        {
            get { return _status; }
            set
            {
                _status = value;
                switch (value)
                {
                    case StatusCode.Ok:
                        Reasons.Add("Ok");
                        break;
                    case StatusCode.Created:
                        Reasons.Add("Created");
                        break;
                    case StatusCode.Updated:
                        Reasons.Add("Updated");
                        break;
                    case StatusCode.NotFound:
                        Reasons.Add("Not Found");
                        break;
                    case StatusCode.Error:
                        Reasons.Add("Error");
                        break;
                    case StatusCode.BadRequest:
                        break;
                }
            }
        }
        

        public object Body;

        public string ToJson()
        {
            string statusString = Status.ToString() + " ";
            statusString = string.Join(", ", Reasons.ToArray());

            var response = new
            {
                status = statusString,
                body = JsonConvert.SerializeObject(Body)
            };
            return JsonConvert.SerializeObject(response);
        }

    }
}