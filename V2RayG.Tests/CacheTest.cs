using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace V2RayG.Tests
{
    [TestClass]
    public class CacheTest
    {
        V2RayG.Services.Cache cache;

        public CacheTest()
        {
            cache = V2RayG.Services.Cache.Instance;
        }

        [TestMethod]
        public void GeneralCacheNormalTest()
        {

        }

        [TestMethod]
        public void HTMLFailTest()
        {
            Assert.ThrowsException<WebException>(() =>
            {
                var t = cache.html[""];
            });
        }

        [DataTestMethod]
        [DataRow("https://www.baidu.com/")]
        [DataRow("https://www.sogou.com/,https://www.baidu.com/")]
        public void HTMLNormalTest(string rawData)
        {
            var data = rawData.Split(',');
            var urls = new List<string>();
            var len = data.Length;
            for (var i = 0; i < 1000; i++)
            {
                urls.Add(data[i % len]);
            }
            var html = cache.html;
            html.Clear();

            try
            {
                Misc.Utils.ExecuteInParallel(urls, (url) =>
                {
                    return html[url];
                });
            }
            catch
            {
                Assert.Fail();
            }

            Assert.AreEqual<int>(data.Length, html.Count);
            html.Clear();
            Assert.AreEqual<int>(0, html.Count);
        }

        [DataTestMethod]
        [DataRow(@"vgc", @"{'alias': '','description': ''}")]
        public void LoadTplTest(string key, string expect)
        {
            var v = cache.tpl.LoadTemplate(key);
            var e = JObject.Parse(expect);
            Assert.AreEqual(true, JObject.DeepEquals(v, e));
        }

        [TestMethod]
        public void LoadMinConfigTest()
        {
            var min = cache.tpl.LoadMinConfig();
            var proto = Misc.Utils.GetValue<string>(min, "inbounds.0.protocol");
            Assert.AreEqual<string>("socks", proto);
        }
    }
}
