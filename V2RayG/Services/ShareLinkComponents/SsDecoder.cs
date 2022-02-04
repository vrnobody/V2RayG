using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace V2RayG.Services.ShareLinkComponents
{
    internal sealed class SsDecoder :
        Apis.BaseClasses.ComponentOf<Codecs>,
        Apis.Interfaces.IShareLinkDecoder
    {
        Cache cache;

        public SsDecoder(Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            // ss://(base64)#tag or ss://(base64)
            var parts = shareLink.Split('#');
            if (parts.Length > 2 || parts.Length < 1)
            {
                return null;
            }

            string body = Misc.Utils.GetLinkBody(parts[0]);

            body = Misc.Utils.TranslateSIP002Body(body);
            Models.Datas.Shadowsocks ss = ParseSsLinkBody(body);
            if (ss == null)
            {
                return null;
            }

            var outbound = Ss2Outbound(ss);
            if (outbound == null)
            {
                return null;
            }

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
            {
                var name = Uri.UnescapeDataString(parts[1]);
                tpl["v2rayg"]["alias"] = name;
            }

            return new Tuple<JObject, JToken>(tpl, outbound);
        }


        public string Encode(string config)
        {
            var om = Models.Datas.OutboundMeta.FromConfig(config);

            if (om == null || (om.protocol != "shadowsocks" && om.protocol != "ss"))
            {
                return null;
            }

            var ss = new Models.Datas.Shadowsocks()
            {
                name = om.alias,
                method = om.method,
                password = om.password,
                hostname = om.address,
                port = om.port,
            };

            return ss.ToShareLink();
        }

        public List<string> ExtractLinksFromText(string text) =>
            Misc.Utils.ExtractLinks(
                text,
                Apis.Models.Datas.Enums.LinkTypes.ss);
        #endregion

        #region private methods
        public static Models.Datas.Shadowsocks ParseSsLinkBody(string body)
        {
            try
            {
                var ss = new Models.Datas.Shadowsocks();
                var plainText = Misc.Utils.Base64Decode(body);
                var parts = plainText.Split('@');
                var mp = parts[0].Split(':');
                if (parts[1].Length > 0 && mp[0].Length > 0 && mp[1].Length > 0)
                {
                    Apis.Misc.Utils.TryParseAddress(parts[1], out string hostname, out int port);
                    ss.method = mp[0];
                    ss.password = mp[1];
                    ss.hostname = hostname;
                    ss.port = port;
                }
                return ss;
            }
            catch { }
            return null;
        }

        JToken Ss2Outbound(Models.Datas.Shadowsocks ss)
        {
            var om = new Models.Datas.OutboundMeta();
            om.protocol = "shadowsocks";

            om.transport = "tcp";
            om.security = "none";

            om.address = ss.hostname;
            om.port = ss.port;
            om.method = ss.method;
            om.password = ss.password;

            return om.ToOutbound(cache);
        }
        #endregion

        #region protected methods

        #endregion
    }
}
