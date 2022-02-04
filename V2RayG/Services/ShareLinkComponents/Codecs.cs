using Newtonsoft.Json.Linq;

namespace V2RayG.Services.ShareLinkComponents
{
    public sealed class Codecs :
        Apis.BaseClasses.ComponentOf<Codecs>
    {
        Settings setting;
        Cache cache;

        public Codecs() { }

        #region public methods
        public string Encode<TDecoder>(string config)
            where TDecoder :
                Apis.BaseClasses.ComponentOf<Codecs>,
                Apis.Interfaces.IShareLinkDecoder
        => GetChild<TDecoder>()?.Encode(config);


        public string Decode(
            string shareLink,
            Apis.Interfaces.IShareLinkDecoder decoder)
        {
            try
            {
                var tuple = decoder.Decode(shareLink);
                return GenerateConfing(tuple);
            }
            catch { }

            return null;
        }

        public string Decode<TDecoder>(string shareLink)
            where TDecoder :
                Apis.BaseClasses.ComponentOf<Codecs>,
                Apis.Interfaces.IShareLinkDecoder
        {
            var tuple = GetChild<TDecoder>()?.Decode(shareLink);
            return GenerateConfing(tuple);
        }

        public void Run(
            Cache cache,
            Settings setting)
        {
            this.setting = setting;
            this.cache = cache;

            var ssDecoder = new SsDecoder(cache);
            var v2cfgDecoder = new V2cfgDecoder();
            var vmessDecoder = new VmessDecoder(cache);
            var trojanDecoder = new TrojanDecoder(cache);
            var vlessDecoder = new VlessDecoder(cache);

            AddChild(vlessDecoder);
            AddChild(trojanDecoder);
            AddChild(ssDecoder);
            AddChild(v2cfgDecoder);
            AddChild(vmessDecoder);
        }
        #endregion

        #region private methods
        private string GenerateConfing(System.Tuple<JObject, JToken> tuple)
        {
            if (tuple == null)
            {
                return null;
            }

            // special case for v2cfg:// ...
            if (tuple.Item2 == null)
            {
                return Misc.Utils.Config2String(tuple.Item1);
            }

            return InjectOutboundIntoTemplate(tuple.Item1, tuple.Item2);
        }


        string InjectOutboundIntoTemplate(JObject template, JToken outbound)
        {

            var inb = Misc.Utils.CreateJObject(
                "inbounds.0",
                cache.tpl.LoadTemplate("inbSimSock"));

            var outb = Misc.Utils.CreateJObject(
                "outbounds.0",
                outbound);

            Misc.Utils.MergeJson(template, inb);
            Misc.Utils.MergeJson(template, outb);
            return Misc.Utils.Config2String(template as JObject);
        }
        #endregion
    }
}
