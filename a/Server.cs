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
                Console.WriteLine($"Got {requestString}");
                Request json = JsonConvert.DeserializeObject<Request>(requestString);

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

        private void ConstructResponse(Request json, out Response response)
        {
            response = new Response();

            try
            {
                // These fields are Required
                // Fail early (with try-catch) 
                // if we can't access them

                string method = json.Method ?? "";
                string path = json.Path ?? "";
                int date;

                if (json.Method == null)
                    response.AddError("Missing Method");
                if (json.Path == null && method != "echo") // Edge case for echo
                    response.AddError("Missing Resource");
                if (json.Date == null)
                    response.AddError("Missing Date");
                if (!int.TryParse(json.Date, out date))
                    response.AddError("Illegal Date");
                
                // Get the last segment of path
                string lastToken = Path.GetFileName(path);

                // Stupid syntax for such a simple thing
                int? id = int.TryParse(lastToken, out int i) ? (int?)i : null;

                // Respond according to the method
                if (method == "read")
                {
                    // Return all categories
                    if (lastToken == "categories")
                    {
                        response.Status = Response.StatusCode.Ok;
                        response.Body = Category.Data;
                    }
                    // Return specific category with an id
                    else if (id != null)
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
                    // Invalid request
                    else
                    {
                        response.AddError("Bad Request");
                    }
                }
                else if (method == "create")
                {
                    bool error = false;

                    // Do not allow the user to give an id
                    if (lastToken != "categories")
                    {
                        response.AddError("Bad Request");
                        error = true;
                    }
                    if (json.Body == null)
                    {
                        response.AddError("Missing Body");
                        error = true;
                    }

                    if (!error)
                    {
                        // Create a new category with the given name
                        // And return the newly created object

                        dynamic input = JsonConvert.DeserializeObject<dynamic>(json.Body);
                        Category newCat = Category.Create((string)input.name);

                        response.Status = Response.StatusCode.Created;
                        response.Body = newCat;
                    }
                }
                else if (method == "update")
                {
                    bool error = false;

                    // Should give an id
                    if (id == null)
                    {
                        response.AddError("Bad Request");
                        error = true;
                    }
                    if (json.Body == null)
                    {
                        response.AddError("Missing Body");
                        error = true;
                    }

                    if (!error)
                    {
                        var cat = JsonConvert.DeserializeObject<Category>(json.Body);
                        cat.Id = id.Value;

                        // Database was correctly updated
                        if (Category.Update(cat))
                        {
                            response.Status = Response.StatusCode.Updated;
                        }
                        // Item does not exist
                        else
                        {
                            response.Status = Response.StatusCode.NotFound;
                        }
                    }
                }
                else if (method == "delete")
                {
                    // Should give an id
                    if (id == null) 
                    {
                        response.AddError("Bad Request");
                    }
                    else
                    {
                        // Database was correctly updated
                        if (Category.Delete(id.Value))
                        {
                            response.Status = Response.StatusCode.Ok;
                        }
                        // Item does not exist
                        else
                        {
                            response.Status = Response.StatusCode.NotFound;
                        }
                    }
                }
                else if (method == "echo")
                {
                    if (json.Body == null)
                    {
                        response.AddError("Missing Body");
                    }
                    else
                    {
                        response.Status = Response.StatusCode.Ok;
                        response.Body = json.Body;
                    }
                }
                // Invalid method
                else
                {
                    response.AddError("Illegal Method");
                }
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                response.AddError("Illegal Body");
            }
            // Something was wrong
            catch (Exception e)
            {
                Console.WriteLine(e);
                response.AddError("Really Bad Request");
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
                if (_status == StatusCode.BadRequest)
                    return;
                
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

        public void AddError(string reason)
        {
            Status = StatusCode.BadRequest;
            Reasons.Add(reason);
        }

        public string ToJson()
        {
            // Prepare the Status String according to the assignment's paper
            string statusString = (int)Status + " ";
            statusString += string.Join(", ", Reasons.ToArray());

            // Include body in the answer if it's not null
            // ignore it otherwise
            var response = new
            {
                status = statusString,
                body = Body is string || Body is null ? Body : 
                    JsonConvert.SerializeObject(Body)
            };

            return JsonConvert.SerializeObject(response);
        }

    }
}