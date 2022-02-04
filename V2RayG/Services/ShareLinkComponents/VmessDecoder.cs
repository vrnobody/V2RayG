using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using V2RayG.Resources.Resx;

namespace V2RayG.Services.ShareLinkComponents
{
    internal sealed class VmessDecoder :
        Apis.BaseClasses.ComponentOf<Codecs>,
        Apis.Interfaces.IShareLinkDecoder
    {
        Cache cache;

        public VmessDecoder(
            Cache cache)
        {
            this.cache = cache;
        }
        #region properties

        #endregion

        #region public methods
        public Tuple<JObject, JToken> Decode(string shareLink)
        {
            var vmess = Misc.Utils.VmessLink2Vmess(shareLink);
            if (vmess == null)
            {
                return null;
            }
            return Vmess2Config(vmess);
        }

        public string Encode(string config) =>
            ConfigString2Vmess(config)?.ToShareLink();

        public List<string> ExtractLinksFromText(string text) =>
            Misc.Utils.ExtractLinks(text, Apis.Models.Datas.Enums.LinkTypes.vmess);
        #endregion

        #region private methods
        Models.Datas.Vmess ConfigString2Vmess(string config)
        {
            var om = Models.Datas.OutboundMeta.FromConfig(config);
            if (om == null || om.protocol != "vmess")
            {
                return null;
            }

            Models.Datas.Vmess vmess = new Models.Datas.Vmess
            {
                v = "2",
                ps = om.alias,
                aid = "0"
            };

            // outbound
            vmess.add = om.address;
            vmess.port = om.port.ToString();
            vmess.id = om.uuid;

            // tls
            vmess.tls = om.security == "tls" ? "tls" : "";
            vmess.sni = om.tlsServerName;

            // stream
            switch (om.transport)
            {
                case "grpc":
                    vmess.net = "grpc";
                    vmess.path = om.grpcServName;
                    break;
                case "kcp":
                    vmess.net = "kcp";
                    break;
                case "ws":
                    vmess.net = "ws";
                    vmess.path = om.wsPath;
                    break;
                default:
                    // tcp
                    vmess.net = "tcp";
                    break;
            }
            return vmess;
        }


        Tuple<JObject, JToken> Vmess2Config(Models.Datas.Vmess vmess)
        {
            var om = new Models.Datas.OutboundMeta();
            om.protocol = "vmess";

            // stream
            var streamType = vmess.net?.ToLower();
            switch (streamType)
            {
                case "ws":
                    om.transport = "ws";
                    om.wsPath = vmess.path;
                    break;
                case "kcp":
                    om.transport = "kcp";
                    break;
                case "grpc":
                    om.transport = "grpc";
                    om.grpcServName = vmess.path;
                    break;
                default:
                    om.transport = "tcp";
                    break;
            }

            // tls
            om.security = vmess.tls?.ToLower() == "tls" ? "tls" : "none";
            om.tlsServerName = vmess.sni;


            // outbound
            om.address = vmess.add;
            om.port = Apis.Misc.Utils.Str2Int(vmess.port);
            om.uuid = vmess.id;

            var outb = om.ToOutbound(cache);

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            tpl["v2rayg"]["alias"] = vmess.ps;
            return new Tuple<JObject, JToken>(tpl, outb);
        }

        #endregion

        #region protected methods

        #endregion
    }
}
