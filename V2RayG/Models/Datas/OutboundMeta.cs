using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace V2RayG.Models.Datas
{
    public class OutboundMeta
    {
        public string alias = string.Empty;

        public string protocol = string.Empty;

        public string address = string.Empty;
        public int port = 0;

        public string uuid = string.Empty; // vless vmess
        public string password = string.Empty; // ss trojan
        public string method = string.Empty; // ss 

        // tls
        public string security = "none"; // none tls
        public string tlsServerName = string.Empty; // json key: serverName

        // stream
        public string transport = "tcp"; // mkcp tcp ws grpc
        public string wsPath = string.Empty; // json key: path
        public string grpcServName = string.Empty; // json key: serviceName
        public string kcpSeed = string.Empty; // json key: seed

        public OutboundMeta()
        { }

        #region properties

        #endregion

        #region public methods
        static public OutboundMeta FromConfig(string config)
        {
            if (!TryParseConfig(config, out JObject json))
            {
                return null;
            }

            var GetStr = Misc.Utils.GetStringByPrefixAndKeyHelper(json);

            // outbound
            var om = new OutboundMeta();

            om.alias = GetStr("v2rayg", "alias");

            var root = "outbounds.0";
            om.protocol = GetStr(root, "protocol");
            om.address = GetStr(root, "settings.address");
            om.port = Apis.Misc.Utils.Str2Int(GetStr(root, "settings.port"));
            om.uuid = GetStr(root, "settings.uuid");
            om.password = GetStr(root, "settings.password");
            om.method = GetStr(root, "settings.method");

            root = root + ".streamSettings";

            // tls
            om.security = GetStr(root, "security") ?? "none";
            om.tlsServerName = GetStr(root, "securitySettings.serverName");

            // stream
            om.transport = GetStr(root, "transport") ?? "tcp";
            om.wsPath = GetStr(root, "transportSettings.path");
            om.grpcServName = GetStr(root, "transportSettings.serviceName");
            om.kcpSeed = GetStr(root, "transportSettings.seed");

            return om;
        }

        public JToken ToOutbound(Services.Cache cache)
        {
            var o = cache.tpl.LoadTemplate("outboundMeta");

            // outbound
            o["protocol"] = protocol;
            o["settings"]["address"] = address;
            o["settings"]["port"] = port;
            if (!string.IsNullOrEmpty(uuid))
            {
                o["settings"]["uuid"] = uuid;
            }
            if (!string.IsNullOrEmpty(password))
            {
                o["settings"]["password"] = password;
            }
            if (!string.IsNullOrEmpty(method))
            {
                o["settings"]["method"] = method;
            }

            // tls
            o["streamSettings"]["security"] = security;
            if (security == "tls" && !string.IsNullOrEmpty(tlsServerName))
            {
                o["streamSettings"]["securitySettings"]["serverName"] = tlsServerName;
            }

            // stream
            o["streamSettings"]["transport"] = transport;
            if (!string.IsNullOrEmpty(wsPath))
            {
                o["streamSettings"]["transportSettings"]["path"] = wsPath;
            }
            if (!string.IsNullOrEmpty(grpcServName))
            {
                o["streamSettings"]["transportSettings"]["serviceName"] = grpcServName;
            }
            if (!string.IsNullOrEmpty(kcpSeed))
            {
                o["streamSettings"]["transportSettings"]["seed"] = kcpSeed;
            }
            return o;
        }
        #endregion

        #region private methods
        static bool TryParseConfig(string config, out JObject json)
        {
            json = null;
            try
            {
                json = JObject.Parse(config);
                return json != null;
            }
            catch { }
            return false;
        }
        #endregion

        #region protected methods

        #endregion
    }
}