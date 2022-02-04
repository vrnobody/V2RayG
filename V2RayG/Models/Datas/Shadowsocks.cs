using System;

namespace V2RayG.Models.Datas
{
    public class Shadowsocks
    {
        public string name = string.Empty;
        public string method = string.Empty;
        public string password = string.Empty;
        public string hostname = string.Empty;
        public int port = 0;

        public Shadowsocks()
        { }

        public string ToShareLink()
        {
            var ss = this;
            if (ss == null)
            {
                return null;
            }

            var auth = string.Format("{0}:{1}", ss.method, ss.password);
            var userinfo = Misc.Utils.Base64Encode(auth)
                .Replace("=", "")
                .Replace('+', '-')
                .Replace('/', '_');

            var body = string.Format(
                "{0}@{1}:{2}#{3}",
                userinfo,
                Uri.EscapeDataString(ss.hostname),
                ss.port,
                Uri.EscapeDataString(ss.name));

            return Misc.Utils.AddLinkPrefix(
                body,
                Apis.Models.Datas.Enums.LinkTypes.ss);
        }

    }
}
