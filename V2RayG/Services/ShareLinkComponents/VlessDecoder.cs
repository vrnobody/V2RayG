using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace V2RayG.Services.ShareLinkComponents
{
    internal sealed class VlessDecoder :
        Apis.BaseClasses.ComponentOf<Codecs>,
        Apis.Interfaces.IShareLinkDecoder
    {
        readonly Services.Cache cache;
        public VlessDecoder(Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            try
            {
                var om = ParseVlessUrl(shareLink);
                if (om != null)
                {
                    var outbound = om.ToOutbound(cache);
                    var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
                    tpl["v2rayg"]["alias"] = om.alias;
                    return new Tuple<JObject, JToken>(tpl, outbound);
                }
            }
            catch { }
            return null;
        }


        public string Encode(string config)
        {
            var om = Models.Datas.OutboundMeta.FromConfig(config);
            if (om == null || om.protocol != @"vless")
            {
                return null;
            }

            var ps = new Dictionary<string, string>();

            // tls
            ps["security"] = om.security;
            if (om.security == "tls" && !string.IsNullOrWhiteSpace(om.tlsServerName))
            {
                ps["sni"] = om.tlsServerName;
            }

            //stream
            ps["type"] = om.transport;
            switch (om.transport)
            {
                case "grpc":
                    ps["serviceName"] = om.grpcServName;
                    break;
                case "ws":
                    if (!string.IsNullOrWhiteSpace(om.wsPath))
                    {
                        ps["path"] = om.wsPath;
                    }
                    break;
                case "kcp":
                    if (!string.IsNullOrWhiteSpace(om.kcpSeed))
                    {
                        ps["seed"] = om.kcpSeed;
                    }
                    break;
                default:
                    break;
            }

            var pms = ps
                .Select(kv => string.Format("{0}={1}", kv.Key, Uri.EscapeDataString(kv.Value)))
                .ToList();

            var url = string.Format(
                "{0}://{1}@{2}:{3}?{4}#{5}",
                om.protocol,
                Uri.EscapeDataString(om.uuid),
                Uri.EscapeDataString(om.address),
                om.port,
                string.Join("&", pms),
                Uri.EscapeDataString(om.alias));
            return url;
        }

        public List<string> ExtractLinksFromText(string text) =>
           Misc.Utils.ExtractLinks(
               text,
               Apis.Models.Datas.Enums.LinkTypes.vless);
        #endregion

        #region private methods


        Models.Datas.OutboundMeta ParseVlessUrl(string url)
        {
            var proto = "vless";
            var header = proto + "://";

            if (!url.StartsWith(header))
            {
                return null;
            }

            // 抄袭自： https://github.com/musva/V2RayW/commit/e54f387e8d8181da833daea8464333e41f0f19e6 GPLv3
            List<string> parts = url
                .Substring(header.Length)
                .Split(new char[6] { ':', '@', '?', '&', '#', '=' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Uri.UnescapeDataString(s))
                .ToList();

            if (parts.Count < 5)
            {
                return null;
            }

            var om = new Models.Datas.OutboundMeta();
            om.alias = parts.Last();
            om.protocol = proto;
            om.address = parts[1];
            om.port = Apis.Misc.Utils.Str2Int(parts[2]);
            om.uuid = parts[0];

            string GetValue(string key, string def)
            {
                return parts.Contains(key) ? parts[parts.IndexOf(key) + 1] : def;
            }

            om.security = GetValue("security", "none");
            om.tlsServerName = GetValue("sni", parts[1]);

            om.transport = GetValue("type", "tcp");
            switch (om.transport)
            {
                case "grpc":
                    om.grpcServName = GetValue("serviceName", @"");
                    break;
                case "ws":
                    om.wsPath = GetValue("path", "/");
                    break;
                case "tcp":
                    break;
                case "kcp":
                    om.kcpSeed = GetValue("seed", "");
                    break;
                default:
                    break;
            }

            return om;
        }
        #endregion

        #region protected methods

        #endregion
    }
}
