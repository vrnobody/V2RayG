using Newtonsoft.Json.Linq;
using System;
using System.Windows.Forms;

namespace V2RayG.Controllers.ConfigerComponet
{
    class TabEditor : ConfigerComponentController
    {
        private readonly ComboBox cboxLogLevel;
        private readonly ComboBox cboxInbProto;
        private readonly ComboBox cboxOutbProto;
        private readonly ComboBox cboxOutbTransport;
        private readonly ComboBox cboxOutbSecurity;

        public TabEditor(
            ComboBox cboxLogLevel,
            Button btnLogLevelInsert,

            ComboBox cboxInbProto,
            Button btnInbInsert,

            ComboBox cboxOutbProto,
            ComboBox cboxOutbTransport,
            ComboBox cboxOutbSecurity,
            Button btnOutbInsert)
        {
            this.cboxLogLevel = cboxLogLevel;
            this.cboxInbProto = cboxInbProto;
            this.cboxOutbProto = cboxOutbProto;
            this.cboxOutbTransport = cboxOutbTransport;
            this.cboxOutbSecurity = cboxOutbSecurity;

            InitCtrls();
            BindCtrlsEvent(btnLogLevelInsert, btnInbInsert, btnOutbInsert);
        }

        #region properties


        #endregion

        #region private method
        void InitCtrls()
        {
            cboxLogLevel.SelectedIndex = 2; // warning
            cboxInbProto.SelectedIndex = 0; // socks
            cboxOutbProto.SelectedIndex = 0; // vmess
            cboxOutbTransport.SelectedIndex = 2; // ws
            cboxOutbSecurity.SelectedIndex = 1; // tls
        }
        void BindCtrlsEvent(
            Button btnLogLevelInsert,
            Button btnInbInsert,
            Button btnOutbInsert)
        {
            btnLogLevelInsert.Click += (s, a) =>
            {
                container.InjectConfigHelper(() =>
                {
                    var tpl = container.cache.tpl.LoadTemplate(@"cfgEdtLogLevel");
                    tpl["error"]["level"] = cboxLogLevel.Text;
                    container.config["log"] = tpl;
                });
            };

            btnInbInsert.Click += (s, a) =>
            {
                container.InjectConfigHelper(() =>
                {
                    var proto = cboxInbProto.Text;

                    var tpl = container.cache.tpl.LoadTemplate(@"inbSimSock");

                    tpl["protocol"] = proto;

                    if (proto == "http")
                    {
                        tpl["port"] = 8080;
                    }

                    if (!(container.config["inbounds"] is JArray))
                    {
                        container.config["inbounds"] = JArray.Parse(@"[]");
                    }
                    if ((container.config["inbounds"] as JArray).Count < 1)
                    {
                        (container.config["inbounds"] as JArray)
                            .Add(JObject.Parse(@"{}"));
                    }
                    container.config["inbounds"][0] = tpl;
                });
            };

            btnOutbInsert.Click += (s, a) =>
            {
                container.InjectConfigHelper(() =>
                {
                    JToken tpl = CreateOutboundCfg();
                    if (!(container.config["outbounds"] is JArray))
                    {
                        container.config["outbounds"] = JArray.Parse(@"[]");
                    }
                    if ((container.config["outbounds"] as JArray).Count < 1)
                    {
                        (container.config["outbounds"] as JArray)
                            .Add(JObject.Parse(@"{}"));
                    }
                    container.config["outbounds"][0] = tpl;
                });
            };
        }

        private JToken CreateOutboundCfg()
        {
            var tpl = container.cache.tpl.LoadTemplate(@"cfgEdtOutb");

            var proto = cboxOutbProto.Text;
            tpl["protocol"] = proto;
            switch (proto)
            {
                case "vless":
                case "vmess":
                    tpl["settings"]["uuid"] = Guid.NewGuid().ToString();
                    break;
                case "trojan":
                    tpl["settings"]["password"] = @"123456";
                    break;
                case "shadowsocks":
                    tpl["settings"]["password"] = @"123456";
                    tpl["settings"]["method"] = @"aes-256-gcm";
                    break;
                default:
                    break;
            }

            var stream = cboxOutbTransport.Text;
            tpl["streamSettings"]["transport"] = stream;
            switch (stream)
            {
                case "kcp":
                    // 填入seed会报错
                    // tpl["streamSettings"]["transportSettings"]["seed"] = @"";
                    break;
                case "ws":
                    tpl["streamSettings"]["transportSettings"]["path"] = @"/";
                    break;
                case "grpc":
                    tpl["streamSettings"]["transportSettings"]["serviceName"] = @"";
                    break;
                default:
                    break;
            }

            var tls = cboxOutbSecurity.Text;
            tpl["streamSettings"]["security"] = tls;
            switch (tls)
            {
                case "tls":
                    tpl["streamSettings"]["securitySettings"]["serverName"] = @"";
                    break;
                default:
                    break;
            }

            return tpl;
        }
        #endregion

        #region public method
        public override void Update(JObject config)
        { }
        #endregion
    }
}
