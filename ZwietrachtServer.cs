using ComputerUtils.Logging;
using ComputerUtils.Updating;
using ComputerUtils.Webserver;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Zwietracht.Models;

namespace Zwietracht
{
    public class ZwietrachtServer
    {
        public HttpServer server = new HttpServer();
        public Config config { get
            {
                return ZwietrachtEnvironment.config;
            } }
        public Dictionary<string, string> replace = new Dictionary<string, string>
        {
            {"{meta}", "<meta name=\"theme-color\" content=\"#63fac3\">\n<meta property=\"og:site_name\" content=\"Zwietracht\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<link rel=\"stylesheet\" href=\"/style.css\"><link href=\"https://fonts.googleapis.com/css?family=Open+Sans:400,400italic,700,700italic\" rel=\"stylesheet\" type=\"text/css\">" }
        };

        public Dictionary<string, Call> calls = new Dictionary<string, Call>();
        public Dictionary<string, Client> clients = new Dictionary<string, Client>();

        public string GetToken(ServerRequest request, bool send403 = true)
        {
            string token = request.context.Request.Headers["token"];
            if (token == null)
            {
                token = request.queryString["token"];
                if (token == null)
                {
                    token = request.cookies["token"] == null ? "" : request.cookies["token"].Value;
                    if (token == null)
                    {
                        if (send403) request.Send403();
                        return "";
                    }
                }
            }
            return token;
        }

        public bool IsUserAdmin(ServerRequest request, bool send403 = true)
        {
            return GetToken(request, send403) == config.masterToken;
        }


        public string HandleCallRequest(string[] req, string token)
        {
            if (req.Length < 4)
            {
                return "You must specify everything: 'token|call|channelId|audiobase64'";
            }
            User me = MongoDBInteractor.GetUserByToken(token);
            if (me == null)
            {
                return "You are ot registered as user";
            }
            Channel c;
            if (calls.ContainsKey(req[2])) c = calls[req[2]].channel;
            else
            {
                c = MongoDBInteractor.GetChannel(req[2]);
                calls.Add(req[2], new Call
                {
                    channel = c,
                });
            }
            if (c == null)
            {
                return "Fuck you";
            }
            if (!c.participants.Where(x => x.id == me.id).Any())
            {
                return "You are not part of this channel";
            }
            
            DateTime now = DateTime.Now;
            if (!calls[req[2]].clients.Where(x => x.userId == me.id).Any())
            {
                calls[req[2]].clients.Add(clients[me.id]);
            }
            List<Client> cs = calls[req[2]].clients;
            for (int i = 0; i < cs.Count; i++)
            {
                if (cs[i].recieved + new TimeSpan(0, 0, 5) < now)
                {
                    calls[req[2]].clients.RemoveAt(i);
                    cs = calls[req[2]].clients;
                    i--;
                } else if(cs[i].userId == me.id)
                {
                    calls[req[2]].clients[i].recieved = now;
                }
                else
                {
                    Logger.Log("Sending to " + cs[i].userId, LoggingType.Important);
                    clients[cs[i].userId].request.SendString(req[3]);
                }
            }
            
            return "";
            
        }

        public void StartServer()
        {
            server = new HttpServer();
            string frontend = "frontend" + Path.DirectorySeparatorChar;
            server.DefaultCacheValidityInSeconds = 0;
            MongoDBInteractor.Init();
            server.AddWSRoute("/", new Action<SocketServerRequest>(request =>
            {
                string[] req = request.bodyString.Split('|');
                Logger.Log(request.bodyString);

                if (req.Length < 2)
                {
                    request.SendString("You must send a command: 'token|command|arg1|arg2|arg3|...'");
                    return;
                }
                string token = req[0];
                User u = MongoDBInteractor.GetUserByToken(token);
                clients[u.id] = new Client
                {
                    userId = u.id,
                    request = request,
                };
                switch (req[1])
                {
                    case "messages":
                        WSQueryString queryString = new WSQueryString(req);
                        if (req.Length < 3)
                        {
                            request.SendString("You must specify a channel: 'token|messages|channelId'");
                            return;
                        }
                        request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetMessages(req[2], int.Parse(queryString.Get("before") ?? "-1"), int.Parse(queryString.Get("after") ?? "-1"), int.Parse(queryString.Get("count") ?? "100"), token)));
                        break;
                }
            }));
            server.AddRoute("POST", "api/v1/call", new Func<ServerRequest, bool>(request =>
            {
                string[] req = request.bodyString.Split('|');
                if (req.Length < 2)
                {
                    request.SendString("no");
                    return true;
                }
                request.SendString(HandleCallRequest(req, req[0]));
                return true;
            }));
            // Messages
            server.AddRoute("GET", "/api/v1/messages/", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetMessages(request.pathDiff, int.Parse(request.queryString.Get("before") ?? "-1"), int.Parse(request.queryString.Get("after") ?? "-1"), int.Parse(request.queryString.Get("count") ?? "100"))));
                return true;
            }), true);
            server.AddRoute("POST", "/api/v1/messages/", new Func<ServerRequest, bool>(request =>
            {
                MongoDBInteractor.AddMessage(JsonSerializer.Deserialize<Message>(request.bodyString), GetToken(request), request.pathDiff);
                request.SendString("");
                return true;
            }), true);
            server.AddRoute("POST", "/api/v1/createchannel/", new Func<ServerRequest, bool>(request =>
            {
                MongoDBInteractor.CreateChannel(JsonSerializer.Deserialize<Channel>(request.bodyString), GetToken(request));
                request.SendString("");
                return true;
            }));
            server.AddRoute("GET", "/api/v1/me/", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.Me(GetToken(request))));
                return true;
            }));

            server.AddRoute("GET", "/cdn/", new Func<ServerRequest, bool>(request =>
            {
                string[] req = request.pathDiff.Trim('/').Split("/");
                if (req.Length < 3)
                {
                    request.SendString("needs channel/message/filename");
                    return true;
                }
                if (request.bodyString.Contains("."))
                {
                    request.SendString("dots are not allowed in cdn urls due to security concerns", "text/plain", 400);
                    return true;
                }
                string requestedFile = ZwietrachtEnvironment.dataDir + "files" + Path.DirectorySeparatorChar + req[0] + Path.DirectorySeparatorChar + req[1] + Path.DirectorySeparatorChar + req[2];
                Logger.Log(requestedFile);
                request.SendFile(requestedFile);
                return true;
            }), true);

            // Users
            server.AddRoute("POST", "/api/v1/createuser", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(UserSystem.ProcessCreateUser(JsonSerializer.Deserialize<LoginRequest>(request.bodyString))));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/login", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(UserSystem.ProcessLogin(JsonSerializer.Deserialize<LoginRequest>(request.bodyString))));
                return true;
            }));

            server.AddRoute("POST", "/api/updateserver/", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                Update u = new Update();
                u.changelog = request.queryString.Get("changelog");
                config.updates.Insert(0, u);
                config.Save();
                request.SendString("Starting update");
                Updater.StartUpdateNetApp(request.bodyBytes, Path.GetFileName(Assembly.GetExecutingAssembly().Location), ZwietrachtEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("GET", "/amiadmin", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString("yes");
                return true;
            }));
            server.AddRouteFile("/", frontend + "index.html", replace, true, true, true);
            server.AddRouteFile("/app", frontend + "app.html", replace, true, true, true);
            server.AddRoute("GET", "/admin", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendFile(frontend + "admin.html", replace);
                return true;
            }), true, true, true, true);
            server.AddRouteFile("/admin", frontend + "admin.html", replace, true, true, true);
            server.AddRouteFile("/style.css", frontend + "style.css", replace, true, true, true);
            server.AddRouteFile("/script.js", frontend + "script.js", replace, true, true, true);
            server.StartServer(config.port);
        }
    }
}