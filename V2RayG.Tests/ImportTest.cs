using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


namespace V2RayG.Tests
{
    [TestClass]
    public class ImportTest
    {
        [DataTestMethod]
        [DataRow(@"{'a':null}", @"{'a':''}", false)]
        [DataRow(
            @"{'a':1,'b':[{'a':{'c':['1','2','3'],'b':1}},{'b':1}],'c':3}",
            @"{'a':1,'b':[{'a':{'c':{'1':'2'}}}]}",
            false)]
        [DataRow(
            @"{'a':1,'b':[{'a':{'c':['1','2','3'],'b':1}},{'b':1}],'c':3}",
            @"{'a':1,'b':[{'a':{'c':['1','2']}}]}",
            true)]
        [DataRow(@"{'a':'1'}", @"{'a':'1'}", true)]
        [DataRow(
            @"{'a':1,'b':[{'a':{'a':1,'b':1}},{'b':1}],'c':3}",
            @"{'a':1,'b':[{'a':{'c':1}}]}",
            false)]
        [DataRow(
            @"{'a':1,'b':[{'a':{'a':1,'b':1}},{'b':1}],'c':3}",
            @"{'a':1,'b':[{'b':1}]}",
            true)]
        [DataRow(@"{'a':1,'b':2,'c':3}", @"{'a':1,'b':2}", true)]
        [DataRow(@"{'a':1,'b':2,'d':3}", @"{'a':1,'b':2,'c':3}", false)]
        [DataRow(@"{}", @"{}", true)]
        public void ContainsTest(string main, string sub, bool expect)
        {
            var m = JObject.Parse(main);
            var s = JObject.Parse(sub);
            Assert.AreEqual<bool>(expect, Misc.Utils.Contains(m, s));
        }

        [DataTestMethod]
        [DataRow(".")]
        [DataRow("a.1")]
        [DataRow("b.")]
        public void CreateJObjectFailTest(string path)
        {
            Assert.ThrowsException<KeyNotFoundException>(() =>
            {
                Misc.Utils.CreateJObject(path);
            });
        }

        [DataTestMethod]
        [DataRow("a.0.b", @"{}", @"{a:[{b:{}}]}")]
        [DataRow("a.0.b", @"{c:1}", @"{a:[{b:{c:1}}]}")]
        [DataRow("a", @"[{c:1}]", @"{'a':[{c:1}]}")]
        [DataRow("", @"[{c:1}]", @"{}")]
        public void CreateJObjectWithChildTest(string path, string child, string expect)
        {
            var c = JToken.Parse(child);
            var result = Misc.Utils.CreateJObject(path, c);
            var e = JObject.Parse(expect);
            Assert.AreEqual<bool>(true, JObject.DeepEquals(result, e));
        }

        [DataTestMethod]
        [DataRow("a.0.b", @"{a:[{b:{}}]}")]
        [DataRow("a", @"{'a':{}}")]
        [DataRow("a.b", @"{'a':{'b':{}}}")]
        [DataRow("a.b.c.d.e", @"{'a':{'b':{'c':{'d':{'e':{}}}}}}")]
        [DataRow("", @"{}")]
        public void CreateJObjectNormalTest(string path, string expect)
        {
            var result = Misc.Utils.CreateJObject(path);
            var e = JObject.Parse(expect);
            Assert.AreEqual<bool>(true, JObject.DeepEquals(result, e));
        }


        [DataTestMethod]
        [DataRow(@"{'a':1}", "b")]
        [DataRow(@"{}", "")]
        public void TryExtractJObjectPartFailTest(string json, string path)
        {
            var stat = Misc.Utils.TryExtractJObjectPart(JObject.Parse(json), path, out JObject part);
            Assert.AreEqual(false, stat);
            Assert.AreEqual(null, part);
        }

        [DataTestMethod]
        [DataRow(@"{'a':{'b':{'c':[]}},'b':1}", "a", @"{'a':{'b':{'c':[]}}}")]
        [DataRow(@"{'a':1,'b':1}", "a", @"{'a':1}")]
        [DataRow(@"{'a':1}", "a", @"{'a':1}")]
        public void TryExtractJObjectPartNormalTest(string json, string path, string expect)
        {
            var source = JObject.Parse(json);
            var stat = Misc.Utils.TryExtractJObjectPart(source, path, out JObject part);
            var e = JObject.Parse(expect);

            Assert.AreEqual(true, stat);
            Assert.AreEqual<bool>(true, JObject.DeepEquals(e, part));
        }

        [DataTestMethod]
        [DataRow("a.b.", "a.b", "")]
        [DataRow("a.b.c", "a.b", "c")]
        [DataRow(".", "", "")]
        [DataRow(".b", "", "b")]
        [DataRow("", "", "")]
        public void PathParseTest(string path, string parent, string key)
        {
            var v = Misc.Utils.ParsePathIntoParentAndKey(path);
            Assert.AreEqual<string>(parent, v.Item1);
            Assert.AreEqual<string>(key, v.Item2);

        }

        [DataTestMethod]
        [DataRow(
            @"{routing:{rule:[{a:[1,3]}]}}",
            @"{routing:{rule:[{a:[2]}]}}",
            @"{routing:{rule:[{a:[2]},{a:[1,3]}]}}")]
        [DataRow(
            @"{routing:{rule:[{b:2},{a:[1,2,3,{c:1}]}]}}",
            @"{routing:{rule:[{a:[1,2,3,{c:1}]},{c:2}]}}",
            @"{routing:{rule:[{a:[1,2,3,{c:1}]},{c:2},{b:2}]}}")]
        [DataRow(
            @"{routing:{rule:[{b:2},{a:1}]}}",
            @"{routing:{rule:[{a:1}]}}",
            @"{routing:{rule:[{a:1},{b:2}]}}")]
        [DataRow(
            @"{'inbounds':[{'a':1}]}",
            @"{'inbounds':[{'b':1}]}",
            @"{'inbounds':[{'b':1},{'a':1}]}")]
        [DataRow(@"{'a':1,'b':1}", @"{'b':2}", @"{'a':1,'b':2}")]
        [DataRow(@"{'a':1}", @"{'b':1}", @"{'a':1,'b':1}")]
        [DataRow(@"{}", @"{}", @"{}")]
        [DataRow(
            @"{'inbounds':[{'a':1}],'outbounds':null}",
            @"{'inbounds':null,'outbounds':[{'b':1}]}",
            @"{'inbounds':[{'a':1}],'outbounds':[{'b':1}]}")]
        public void CombineConfigTest(string left, string right, string expect)
        {
            // outbounds inbounds
            var body = JObject.Parse(left);
            var mixin = JObject.Parse(right);

            Misc.Utils.CombineConfigWithRoutingInFront(ref body, mixin);

            var e = JObject.Parse(expect);
            var dbg = body.ToString();
            var equal = JObject.DeepEquals(e, body);

            Assert.AreEqual(true, equal);

            // test whether mixin changed
            var orgMixin = JObject.Parse(right);
            var same = JObject.DeepEquals(orgMixin, mixin);
            Assert.AreEqual(true, same);
        }

        [DataTestMethod]

        // deepequals regardless dictionary's keys order 
        [DataRow(@"{a:'123',b:null,c:{b:1}}",
            @"{a:null,c:{a:1,c:1}}",
            @"{a:null,b:null,c:{a:1,b:1,c:1}}")]

        [DataRow(@"{a:'123',b:null,c:{a:2,b:1}}",
            @"{a:null,b:'123',c:{a:1,c:1}}",
            @"{a:null,b:'123',c:{a:1,b:1,c:1}}")]

        [DataRow(@"{}", @"{}", @"{}")]
        [DataRow(@"{a:'123',b:null}", @"{a:null,b:'123'}", @"{a:null,b:'123'}")]
        [DataRow(@"{a:[1,2],b:{}}", @"{a:[3],b:{a:[1,2,3]}}", @"{a:[3,2],b:{a:[1,2,3]}}")]
        public void MergeJson(string bodyStr, string mixinStr, string expect)
        {
            var body = JObject.Parse(bodyStr);
            Misc.Utils.MergeJson(body, JObject.Parse(mixinStr));

            var e = JObject.Parse(expect);
            Assert.AreEqual(true, JObject.DeepEquals(body, e));
        }

        [TestMethod]
        public void ImportItemList2JObject()
        {
            Models.Datas.ImportItem GenItem(bool includeSpeedTest, bool includeActivate, string url, string alias)
            {
                return new Models.Datas.ImportItem
                {
                    isUseOnActivate = includeActivate,
                    isUseOnSpeedTest = includeSpeedTest,
                    isUseOnPackage = false,
                    url = url,
                    alias = alias,
                };
            }
            var items = new List<List<Models.Datas.ImportItem>>();
            var expects = new List<string>();

            items.Add(new List<Models.Datas.ImportItem> {
                GenItem(true,true,"a.com","a"),
                GenItem(false,true,"b.com","b"),
                GenItem(true,false,"c.com",""),
            });

            expects.Add(@"{'v2rayg':{'import':{'a.com':'a','c.com':''}}}");

            items.Add(new List<Models.Datas.ImportItem> { });
            expects.Add(@"{'v2rayg':{'import':{}}}");

            for (var i = 0; i < items.Count; i++)
            {
                var expect = JObject.Parse(expects[i]);
                var json = Misc.Utils.ImportItemList2JObject(items[i], true, false, false);
                var result = JObject.DeepEquals(expect, json);
                Assert.AreEqual(true, result);
            }
        }

        [TestMethod]
        public void ParseImportTest()
        {
            var data = new Dictionary<string, string>();

            void kv(string name, string key, string val)
            {
                var json = JObject.Parse(@"{}");
                if (data.ContainsKey(name))
                {
                    json = JObject.Parse(data[name]);
                }
                json[key] = val;
                data[name] = json.ToString(Newtonsoft.Json.Formatting.None);
            }

            void import(string name, string url)
            {
                var json = JObject.Parse(@"{}");
                if (data.ContainsKey(name))
                {
                    json = JObject.Parse(data[name]);
                }
                var imp = Misc.Utils.GetKey(json, "v2rayg.import");
                if (imp == null || !(imp is JObject))
                {
                    json["v2rayg"] = JObject.Parse(@"{'import':{}}");

                }
                json["v2rayg"]["import"][url] = "";
                data[name] = json.ToString(Newtonsoft.Json.Formatting.None);
            }

            List<string> fetcher(List<string> keys)
            {
                var result = new List<string>();

                foreach (var key in keys)
                {
                    try
                    {
                        // Debug.WriteLine(key);
                        result.Add(data[key]);
                    }
                    catch
                    {
                        throw new System.Net.WebException();
                    }
                }

                return result;
            }

            bool eq(JObject left, JObject right)
            {
                var jleft = left.DeepClone() as JObject;
                var jright = right.DeepClone() as JObject;
                jleft["v2rayg"] = null;
                jright["v2rayg"] = null;
                return JObject.DeepEquals(jleft, jright);
            }

            JObject parse(string key, int depth = 3)
            {
                var config = JObject.Parse(data[key]);
                return Misc.Utils.ParseImportRecursively(fetcher, config, depth);
            }

            void check(string expect, string value)
            {
                Assert.AreEqual(true, eq(JObject.Parse(expect), parse(value)));
            }

            data["base"] = "{'v2rayg':{}}";
            kv("a", "a", "1");
            kv("b", "b", "1");
            kv("baser", "r", "1");
            import("baser", "baser");
            import("mixAB", "a");
            import("mixAB", "b");
            import("mixC", "mixAB");
            kv("mixC", "a", "2");
            kv("mixC", "c", "1");
            import("mixCAb", "mixC");
            import("mixCAb", "mixAB");
            kv("mixCAb", "c", "2");
            import("mixABC", "a");
            import("mixABC", "b");
            import("mixABC", "mixC");
            import("final", "mixAB");
            import("final", "mixC");
            import("final", "mixCAb");
            import("final", "baser");
            kv("final", "msg", "omg");

            check(@"{'a':'2','b':'1','c':'2','r':'1','msg':'omg'}", "final");
            check(@"{'a':'2','b':'1','c':'1'}", "mixABC");
            check(@"{'a':'1','b':'1','c':'2'}", "mixCAb");
            check(@"{'a':'2','c':'1','b':'1'}", "mixC");
            check(@"{'a':'1','b':'1'}", "mixAB");
            check(data["base"], "base");
            check(data["baser"], "baser");
        }
    }
}
