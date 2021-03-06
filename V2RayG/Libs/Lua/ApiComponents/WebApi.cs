using System.Collections.Generic;
using GlobalApis = global::Apis;

namespace V2RayG.Libs.Lua.ApiComponents
{
    public sealed class WebApi :
        GlobalApis.BaseClasses.Disposable,
        GlobalApis.Interfaces.Services.IWebService
    {
        public string PatchHref(string url, string href) =>
            Misc.Utils.PatchHref(url, href);


        public List<string> ExtractLinks(
            string text,
            GlobalApis.Models.Datas.Enums.LinkTypes linkType) =>
            Misc.Utils.ExtractLinks(text, linkType);

        public string Search(string keywords, int first, int proxyPort, int timeout)
        {
            var url = Misc.Utils.GenSearchUrl(keywords, first);
            return Fetch(url, proxyPort, timeout);
        }

        public string Fetch(string url, int proxyPort, int timeout) =>
            Misc.Utils.Fetch(url, proxyPort, timeout);

        public bool Download(string url, string filename, int proxyPort, int timeout) =>
            Misc.Utils.DownloadFile(url, filename, proxyPort, timeout);
    }
}
