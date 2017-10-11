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
        }

        void handleRequest(object clientObj)
        {
            if (clientObj is null) return;
            if (!(clientObj is TcpClient client)) return;

            try
            {
                string requestString = Read(client);
                dynamic json = JsonConvert.DeserializeObject<dynamic>(requestString);

                ConstructResponse(json, out Response response);

                Send(client, response.ToJson());

                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        private void ConstructResponse(dynamic json, out Response response)
        {
            response = new Response();

            try
            {
                string method = json.method;
                string path   = json.path;
                int    date   = json.date;

                string lastToken = Path.GetFileName(path);

                if (method == "read")
                {
                    if (lastToken == "categories")
                    {
                        response.Status = Response.StatusCode.Ok;
                        response.Body = Category.Data;
                    }
                    else if (int.TryParse(lastToken, out int id))
                    {
                        var cat = Category.Data.Find(x => x.Id == id);

                        if (cat != null)
                        {
                            response.Status = Response.StatusCode.Ok;
                            response.Body = cat;
                        }
                        // Category not found
                        else
                        {
                            response.Status = Response.StatusCode.NotFound;
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (method == "create")
                {
                    if (lastToken != "categories")
                    {
                        throw new Exception();
                    }

                    dynamic input = JsonConvert.DeserializeObject<dynamic>(json.body);
                    Category newCat = Category.Create(input.name);

                    response.Status = Response.StatusCode.Created;
                    response.Body = newCat;
                }
            }
            catch
            {
                response.Status = Response.StatusCode.BadRequest;
                response.Reasons.Add("Bad Request");
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

            Console.WriteLine($"Writing {data}");
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
        public object Body = null;
        private StatusCode _status;

        public StatusCode Status
        {
            get { return _status; }
            set
            {
                _status = value;
                // Automatically give friendly names to status codes
                // If it's not a BadRequest
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

        public string ToJson()
        {
            // Prepare the Status String according to the assignment's paper
            string statusString = (int)Status + " ";
            statusString += string.Join(", ", Reasons.ToArray());

            // Include body in the answer if it's not null
            // ignore it otherwise
            object response;
            if (Body != null)
                response = new
                {
                    status = statusString,
                    body = JsonConvert.SerializeObject(Body)
                };
            else
                response = new
                {
                    status = statusString
                };

            return JsonConvert.SerializeObject(response);
        }

    }
}