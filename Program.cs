using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace c_sharp_http_example
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var regex = new Regex("(http(s)?:\\/\\/)([a-z0-9\\w]+\\.*)+[a-z0-9]{2,4}(:)[0-9]*(/)");

            Console.WriteLine("Enter listening URL: ");
            var listeningUrl = Console.ReadLine();

            while (!regex.IsMatch(listeningUrl))
            {
                Console.WriteLine("Wrong format. You could try \"http://localhost:3000/\"");
                listeningUrl = Console.ReadLine();
            }

            Console.WriteLine("Enter requestUrl URL: ");
            var requestUrl = Console.ReadLine();

            while (!regex.IsMatch(requestUrl))
            {
                Console.WriteLine("Wrong format. You could try \"http://localhost:3000/\"");
                requestUrl = Console.ReadLine();
            }

            var server = new Server(listeningUrl, requestUrl);

            Console.WriteLine("If you want to exit, enter \'exit\'");
            Console.WriteLine("If you want to GET data, enter \'get\'");
            Console.WriteLine("If you want to POST data, enter \'post\'");
            var command = Console.ReadLine();

            while (command != "exit")
            {
                if (command == "get")
                {
                    Console.WriteLine("Enter your request");
                    var req = Console.ReadLine();

                    Console.WriteLine("Enter your router");
                    var router = Console.ReadLine();

                    Console.WriteLine("Enter your mediaType");
                    var mediaType = Console.ReadLine();

                    server.Get(req, router, mediaType).Wait();

                    command = "";
                }

                if (command == "post")
                {
                    Console.WriteLine("Enter your request");
                    var req = Console.ReadLine();

                    Console.WriteLine("Enter your router");
                    var router = Console.ReadLine();

                    Console.WriteLine("Enter your mediaType");
                    var mediaType = Console.ReadLine();

                    server.Post(req, router, mediaType).Wait();

                    command = "";
                }

                command = Console.ReadLine();
            }
        }

        public class Server
        {
            public string LISTENING_URL;
            public string REQ_URL;

            public Server(string listeningUrl = "http://localhost:3000/", string requestUrl = "http://localhost:3001/")
            {
                LISTENING_URL = listeningUrl;
                REQ_URL = requestUrl;

                var listener = new HttpListener();
                listener.Prefixes.Add(LISTENING_URL);

                listener.Start();
                Console.WriteLine($"Trace: Listening on {LISTENING_URL}");

                ThreadPool.QueueUserWorkItem((state) =>
                {
                    while (listener.IsListening)
                    {
                        // Wait for request.
                        var context = listener.GetContext();

                        //
                        ThreadPool.QueueUserWorkItem((ctx) =>
                        {
                            HandleRequest((HttpListenerContext)ctx);
                        }, context);
                    }
                });
            }

            public static void HandleRequest(HttpListenerContext context)
            {
                var req = context.Request;
                var res = context.Response;

                // Read the req.
                string resData = "";
                var route = req.RawUrl;

                if (req.HttpMethod == "GET")
                {
                    Console.WriteLine($"Requesting to route '{route}'");
                    var resObj = new { message = "You made a GET request!", timestamp = DateTime.Now };
                    resData = JsonConvert.SerializeObject(resObj);

                }

                if (req.HttpMethod == "POST")
                {
                    Console.WriteLine($"Requesting to route '{route}'");
                    using (var body = req.InputStream)
                    {
                        using (var reader = new StreamReader(body, req.ContentEncoding))
                        {
                            string reqData = reader.ReadToEnd();
                            var reqObj = new { data = reqData, message = "You made a POST request!", timestamp = DateTime.Now };

                            resData = JsonConvert.SerializeObject(reqObj);
                        }
                    }
                }

                // Send res.
                var buffer = Encoding.UTF8.GetBytes(resData);
                res.ContentLength64 = buffer.Length;
                res.OutputStream.Write(buffer, 0, buffer.Length);
                res.Close();
            }

            public async Task Get(string message, string router, string mediaType = "")
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var url = REQ_URL + router;
                        var res = await client.GetAsync(url);

                        StringContent content;

                        if (mediaType == "")
                        {
                            content = new StringContent(message, Encoding.UTF8);
                        }
                        else
                        {
                            content = new StringContent(message, Encoding.UTF8, mediaType);
                        }

                        if (res.IsSuccessStatusCode)
                        {
                            var resBody = await res.Content.ReadAsStringAsync();
                            Console.WriteLine($"Response from {url}:\n{resBody}");
                        }
                        else
                        {
                            Console.WriteLine($"Error: Failed to request {url}...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }

                }
            }

            public async Task Post(string message, string router, string mediaType = "")
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var url = REQ_URL + router;

                        StringContent content;

                        if (mediaType == "")
                        {
                            content = new StringContent(message, Encoding.UTF8);
                        }
                        else
                        {
                            content = new StringContent(message, Encoding.UTF8, mediaType);
                        }

                        var res = await client.PostAsync(url, content);

                        if (res.IsSuccessStatusCode)
                        {
                            var resBody = await res.Content.ReadAsStringAsync();
                            Console.WriteLine($"Response from {url}:\n{resBody}");
                        }
                        else
                        {
                            Console.WriteLine($"Error: Failed to request {url}...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }
                }
            }
        }
    }
}
