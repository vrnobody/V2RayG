using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using GlobalApis = global::Apis;

namespace Luna.Models.Apis.SysCmpos
{
    public class HttpServer :
        GlobalApis.BaseClasses.Disposable,
        GlobalApis.Interfaces.Lua.IRunnable
    {
        HttpListener serv;

        private readonly GlobalApis.Interfaces.Lua.ILuaMailBox inbox;
        private readonly GlobalApis.Interfaces.Lua.ILuaMailBox outbox;

        public HttpServer(
            string url,
            GlobalApis.Interfaces.Lua.ILuaMailBox inbox,
            GlobalApis.Interfaces.Lua.ILuaMailBox outbox)
        {
            this.inbox = inbox;
            this.outbox = outbox;
            serv = new HttpListener();
            serv.Prefixes.Add(url);
        }

        #region public methods
        public void Start()
        {
            try
            {
                Stop();
                serv.Start();
                HandleConnOut();
                HandleConnIn();
            }
            catch { }
        }

        public void Stop()
        {
            try
            {
                serv.Stop();
            }
            catch { }
        }
        #endregion

        #region protected 
        protected override void Cleanup()
        {
            Stop();
        }
        #endregion

        #region private methods
        const int MaxContextLen = 10240;

        ConcurrentDictionary<string, HttpListenerContext> contexts = new ConcurrentDictionary<string, HttpListenerContext>();

        void HandleConnOut()
        {
            GlobalApis.Misc.Utils.RunInBackground(() =>
            {
                try
                {
                    while (true)
                    {
                        var mail = outbox.Wait();
                        if (mail == null)
                        {
                            break;
                        }

                        if (!contexts.TryRemove(mail.title, out var ctx))
                        {
                            continue;
                        }

                        HandleOneConnOut(ctx, mail.GetContent());
                    }
                }
                catch { }
            });
        }

        void HandleOneConnOut(HttpListenerContext ctx, string content)
        {
            GlobalApis.Misc.Utils.RunInBackground(() =>
            {
                try
                {
                    var resp = ctx.Response;
                    var encoding = ctx.Request.ContentEncoding;
                    var buff = encoding.GetBytes(content ?? "");
                    resp.ContentLength64 = buff.Length;
                    using (var s = resp.OutputStream)
                    {
                        s.Write(buff, 0, buff.Length);
                    }
                    resp.Close();
                }
                catch { }
            });
        }

        void HandleConnIn()
        {
            GlobalApis.Misc.Utils.RunInBackground(() =>
            {
                try
                {
                    while (serv.IsListening)
                    {
                        if (contexts.Keys.Count > MaxContextLen)
                        {
                            GlobalApis.Misc.Utils.Sleep(100);
                            continue;
                        }

                        var ctx = serv.GetContext();
                        try
                        {
                            HandleOneConnection(ctx);
                        }
                        catch { }
                    }
                }
                catch { };
            });
        }

        int HttpMethodToCode(string method)
        {
            switch (method)
            {
                case "POST":
                    return 1;
                default:
                    return 0;
            }
        }

        void HandleOneConnection(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var code = HttpMethodToCode(req.HttpMethod);

            string text;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                text = reader.ReadToEnd();
            }

            var id = Guid.NewGuid().ToString();
            contexts.TryAdd(id, ctx);
            inbox.Send(inbox.GetAddress(), code, id, true, text ?? "");
        }

        #endregion


    }
}
