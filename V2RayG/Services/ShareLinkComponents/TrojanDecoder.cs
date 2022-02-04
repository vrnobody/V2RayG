using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace V2RayG.Services.ShareLinkComponents
{
    internal sealed class TrojanDecoder :
        Apis.BaseClasses.ComponentOf<Codecs>,
        Apis.Interfaces.IShareLinkDecoder
    {
        readonly Services.Cache cache;
        public TrojanDecoder(Services.Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            /* 
             * trojan://password@remote_host:remote_port
             * in which the password is url-encoded in case it contains illegal characters.
             */

            try
            {
                var om = ParseTrojanUrl(shareLink);
                if (om != null)
                {
                    var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
                    tpl["v2rayg"]["alias"] = om.alias;

                    var outbound = om.ToOutbound(cache);
                    return new Tuple<JObject, JToken>(tpl, outbound);

                }
            }
            catch { }
            return null;
        }


        public string Encode(string config)
        {
            throw new NotImplementedException();
        }

        public List<string> ExtractLinksFromText(string text) =>
           Misc.Utils.ExtractLinks(
               text,
               Apis.Models.Datas.Enums.LinkTypes.trojan);
        #endregion

        #region private methods
        Models.Datas.OutboundMeta ParseTrojanUrl(string link)
        {
            var proto = "trojan";
            var header = proto + "://";

            if (!link.StartsWith(header))
            {
                return null;
            }

            var parts = link.Split('#');
            var url = parts[0];
            var name = string.Empty;
            if (parts.Length >= 2)
            {
                name = Uri.UnescapeDataString(parts[1]);
            }

            var port = url.Split(':').LastOrDefault();
            if (string.IsNullOrEmpty(port))
            {
                return null;
            }

            var body = url.Substring(header.Length, url.Length - header.Length - port.Length - 1);
            var pa = body.Split('@');
            if (pa.Length != 2)
            {
                return null;
            }
            var password = pa[0];
            var hostname = pa[1];

            var om = new Models.Datas.OutboundMeta();
            om.protocol = proto;
            om.alias = name;
            om.address = hostname;
            om.port = Apis.Misc.Utils.Str2Int(port);
            om.password = Uri.UnescapeDataString(password);

            om.security = "tls";
            om.transport = "tcp";

            return om;
        }
        #endregion

        #region protected methods

        #endregion
    }
}
