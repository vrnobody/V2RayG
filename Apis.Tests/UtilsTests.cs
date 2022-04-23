﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Apis.Misc.Utils;

namespace Apis.Tests
{
    [TestClass]
    public class UtilsTests
    {
        [DataTestMethod]
        [DataRow("lua", "lua")]
        [DataRow("lua\\hello", "lua.hello")]
        [DataRow("lua/hello.lua", "lua.hello")]
        [DataRow("", "")]
        public void GetLuaModuleNameTest(string relativePath, string expect)
        {
            var root = GetAppDir();
            var fullPath = Path.Combine(root, relativePath);
            var result = GetLuaModuleName(fullPath);
            Assert.AreEqual(expect, result);
        }


        [DataTestMethod]
        [DataRow("D1", true, true, false, true, 3u, 49u)]
        public void TryParseKeyMessageTests(
            string keyName, bool hasAlt, bool hasCtrl, bool hasShift,
            bool ok, uint modifier, uint keyCode)
        {
            var result = TryParseKeyMesssage(keyName, hasAlt, hasCtrl, hasShift,
                out var m, out var c);
            Assert.AreEqual(ok, result);
            Assert.AreEqual(modifier, m);
            Assert.AreEqual(keyCode, c);
        }

        [DataTestMethod]
        [DataRow("中iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii试", 34, true, "中iiiiiiiiiiiiiiiiiiiiiiiiiiiiii")]
        [DataRow("中aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa文", 34, true, "中aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [DataRow("中iiiii试", 6, true, "中ii")]
        [DataRow("中文aa测试", 8, true, "中文aa")]
        [DataRow("中文aa测试", 7, true, "中文a")]
        [DataRow("中文aa测试", 6, true, "中文")]
        [DataRow("中aaa文测试", 6, true, "中aa")]
        [DataRow("中aaa文测试", 5, true, "中a")]
        [DataRow("中文测试", 4, true, "中")]
        [DataRow("中文测试", 3, true, "")]
        [DataRow("a中文测试", 9, false, "a中文测试")]
        [DataRow("a中文测试", 7, true, "a中文")]
        [DataRow("a中文测试", 6, true, "a中")]
        [DataRow("a中文测试", 5, true, "a中")]
        [DataRow("a中文测试", 4, true, "a")]
        [DataRow("a中文测试", 3, true, "a")]
        [DataRow("a中文测试", 2, true, "")]
        [DataRow("a中文测试", 1, true, "")]
        [DataRow("a中文测试", 0, false, "")]
        [DataRow("a中文测试", -1, false, "")]
        [DataRow("aaaaaaaaa", 5, true, "aaa")]
        [DataRow("", 100, false, "")]
        public void AutoEllipsisTest(string org, int len, bool isEllipsised, string expect)
        {
            var defFont = Apis.Models.Consts.AutoEllipsis.defFont;
            var orgLen = org.Length;
            var result = AutoEllipsis(org, len);

            if (orgLen <= 0 || len <= 0)
            {
                Assert.AreEqual(string.Empty, result);
                return;
            }

            var orgWidth = TextRenderer.MeasureText(org, defFont).Width;
            var resultWidth = TextRenderer.MeasureText(result, defFont).Width;
            var expectedWidth = TextRenderer.MeasureText(new string('a', len), defFont).Width;

            if (orgWidth <= expectedWidth)
            {
                Assert.AreEqual(org, result);
                return;
            }

            var ellipsis = Apis.Models.Consts.AutoEllipsis.ellipsis;
            Assert.AreEqual(ellipsis.Last(), result.Last());

            var d = TextRenderer.MeasureText(ellipsis, defFont).Width;

            if (resultWidth <= d)
            {
                return;
            }

            Assert.IsTrue(resultWidth <= expectedWidth);
            Assert.IsTrue(resultWidth >= expectedWidth - d);

            /* 以下代码只对特定字体有效
            string ellipsis = Apis.Models.Consts.AutoEllipsis.ellipsis;
            var cut = AutoEllipsis(org, len);
            var exp = expect + (isEllipsised ? ellipsis : "");
            Assert.AreEqual(exp, cut);
            */
        }

        [DataTestMethod]
        [DataRow(
            @"https://github.com/user/reponame/blob/master/README.md",
            true,
            @"https://raw.githubusercontent.com/user/reponame/master/README.md")]
        public void TryPatchGitHubUrlTest(string url, bool expectedResult, string expectedPatched)
        {
            var result = TryPatchGitHubUrl(url, out var patched);
            Assert.AreEqual(expectedResult, result);
            Assert.AreEqual(expectedPatched, patched);
        }

        [DataTestMethod]
        [DataRow(@"abc", @"http://baidu.com/abc/a/b/c/d.html")]
        [DataRow(@"a1中b2文", @"http://baidu.com/a1中b2文/a/b/c/d.html")]
        [DataRow(@"index.html", @"http://baidu.com/index.html")]
        [DataRow(@"a1中b2文?", @"fttp::ssh:://%20#2@&/a1中b2文?/a/b/c/d.html?#a=1&value=10")]

        public void TryExtractAliasFromSubsUrlNormalTest(string expected, string url)
        {
            if (TryExtractAliasFromSubscriptionUrl(url, out var alias))
            {
                Assert.AreEqual(expected, alias);
            }
            else
            {
                Assert.Fail();
            }

        }

        [DataTestMethod]
        [DataRow(@"http://baidu.com")]
        [DataRow(@"http://baidu.com//")]
        [DataRow(@"http://baidu.com/")]
        [DataRow(@"abcd1234")]
        public void TryExtractAliasFromSubsUrlFailTest(string url)
        {
            if (TryExtractAliasFromSubscriptionUrl(url, out var _))
            {
                Assert.Fail();
            }
        }

        [DataTestMethod]
        [DataRow("vmess.mkcp.tls@whatsurproblem.com", "whatsurproblem.com@vmess.mkcp.tls")]
        [DataRow("vmess.ws.tls@1.2.3.4", "1.2.3.4@vmess.ws.tls")]
        [DataRow("a@b@c", "c@b@a")]
        [DataRow("a@b", "b@a")]
        [DataRow("", "")]
        [DataRow(null, "")]
        [DataRow("a", "a")]
        public void ReverseSummaryTest(string summary, string expect)
        {
            var result = ReverseSummary(summary);
            Assert.AreEqual(expect, result);
        }


        [DataTestMethod]
        [DataRow(
            @"c,,a,//---,中文,3,,2,1,//abc中文,中文,a,1,,中文,a,1",
            @"c,,a,//---,3,中文,,1,2,//abc中文,1,a,中文,,1,a,中文")]
        [DataRow(@"a,b,中文,3,2,1", @"1,2,3,a,b,中文")]
        [DataRow(@"3,2,1", @"1,2,3")]
        public void SortPacListTest(string source, string expStr)
        {
            var testList = source.Split(new char[] { ',' }, StringSplitOptions.None);
            var expectList = expStr.Split(',');
            var result = SortPacList(testList);

            Assert.IsTrue(expectList.SequenceEqual(result));

        }

        [DataTestMethod]
        [DataRow("a::b:1233333", false, "127.0.0.1", 1080)]
        [DataRow("a::b:123", true, "a::b", 123)]
        [DataRow("ab123", false, "127.0.0.1", 1080)]
        [DataRow("ab123:", false, "127.0.0.1", 1080)]
        [DataRow(":123", false, "127.0.0.1", 1080)]
        [DataRow(":", false, "127.0.0.1", 1080)]
        public void TryParseIPAddrTest(string address, bool expResult, string expIp, int expPort)
        {
            var result = TryParseAddress(address, out string ip, out int port);
            Assert.AreEqual(expResult, result);
            Assert.AreEqual(expIp, ip);
            Assert.AreEqual(expPort, port);

        }

        [TestMethod]
        public void AreEqualTest()
        {
            var minVal = Apis.Models.Consts.Config.FloatPointNumberTolerance;
            var a = 0.1;
            var b1 = a + minVal * 2;
            var b2 = a - minVal * 2;
            var c1 = a + minVal / 2;
            var c2 = a - minVal / 2;

            Assert.IsFalse(AreEqual(a, b1));
            Assert.IsFalse(AreEqual(a, b2));
            Assert.IsTrue(AreEqual(a, c1));
            Assert.IsTrue(AreEqual(a, c2));
        }


        [DataTestMethod]
        [DataRow(1, 2, 0.6, (long)(0.6 * 1 + 0.4 * 2))]
        [DataRow(-1, 2, 0.6, 2)]
        [DataRow(1, -2, 0.6, 1)]
        [DataRow(-1, -2, 0.6, -1)]
        public void IntegerSpeedtestMeanTest(
            long first,
            long second,
            double weight,
            long expect)
        {
            var result = SpeedtestMean(first, second, weight);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(0.1, 0.2, 0.3, 0.1 * 0.3 + 0.2 * 0.7)]
        [DataRow(-0.1, 0.2, 0.3, 0.2)]
        [DataRow(0.1, -0.2, 0.3, 0.1)]
        [DataRow(-0.1, -0.2, 0.3, -0.1)]
        public void DoubleSpeedtestMeanTest(
            double first,
            double second,
            double weight,
            double expect)
        {
            var result = SpeedtestMean(first, second, weight);
            Assert.IsTrue(AreEqual(expect, result));
        }

        [DataTestMethod]
        [DataRow(@"o,o.14,o.11,o.1,o.3,o.4", @"o,o.1,o.3,o.4,o.11,o.14")]
        [DataRow(@"b3.2,b3.1.3,a1", @"a1,b3.1.3,b3.2")]
        [DataRow(@"b3,b10,a1", @"a1,b10,b3")]
        [DataRow(@"b,a,1,,", @",,1,a,b")]
        [DataRow(@"c.10.a,a,c.3.b,c.3.a", @"a,c.3.a,c.3.b,c.10.a")]
        public void JsonKeyComparerTest(string rawKeys, string rawExpects)
        {
            var keyList = rawKeys.Split(',').ToList();
            var expect = rawExpects.Split(',');

            keyList.Sort((a, b) => JsonKeyComparer(a, b));

            for (int i = 0; i < keyList.Count; i++)
            {
                Assert.AreEqual(expect[i], keyList[i]);
            }
        }

        [DataTestMethod]
        [DataRow(@"abeec", @"abc", 3)]
        [DataRow(@"abeecee", @"abc", 3)]
        [DataRow(@"eabec", @"abc", 5)]
        [DataRow(@"aeebc", @"abc", 5)]
        [DataRow(@"eeabc", @"abc", 7)]
        [DataRow(@"", @"", 1)]
        [DataRow(@"abc", @"", 1)]
        [DataRow(@"abc", @"abc", 1)]
        public void MeasureSimilarityTest(
            string source, string partial, long expect)
        {
            var result = MeasureSimilarity(source, partial);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(
            @"{1:[[],[]],'':{},b:123,c:{}}",
            @"c:{}")]
        [DataRow(
            @"{a:[{},{}],b:{}}",
            @"a:[],b:{},a.0:{},a.1:{}")]
        [DataRow(
            @"{a:[[[],[],[]],[[]]],b:{}}",
            @"a:[],b:{},a.0:[],a.1:[]")]
        [DataRow(
            @"{a:[[[],[],[]],[[]]],b:'abc',c:{a:[],b:{d:1}}}",
            @"a:[],a.0:[],a.1:[],c:{},c.a:[],c.b:{}")]
        public void GetterJsonDataStructWorkerTest(string jsonString, string expect)
        {
            var expDict = expect
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split(
                    new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(v => v[0], v => v[1]);

            var jobject = JObject.Parse(jsonString);
            var sections = GetterJsonSections(jobject);

            foreach (var kv in expDict)
            {
                if (kv.Value != sections[kv.Key])
                {
                    Assert.Fail();
                }
            }

            foreach (var kv in sections)
            {
                if (kv.Value != expDict[kv.Key])
                {
                    Assert.Fail();
                }
            }
        }

        [DataTestMethod]
        [DataRow("EvABk文,tv字vvc", "字文", false)]
        [DataRow("EvABk文,tv字vvc", "ab字", true)]
        [DataRow("ab vvvc", "bc", true)]
        [DataRow("abc", "ac", true)]
        [DataRow("", "a", false)]
        [DataRow("", "", true)]
        public void PartialMatchTest(string source, string partial, bool expect)
        {
            var result = PartialMatchCi(source, partial);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(@"http://abc.com", @"http")]
        [DataRow(@"https://abc.com", @"https")]
        [DataRow(@"VMess://abc.com", @"vmess")]
        public void GetLinkPrefixTest(string link, string expect)
        {
            var prefix = GetLinkPrefix(link);
            Assert.AreEqual(expect, prefix);
        }

        [DataTestMethod]
        [DataRow(@"http://abc.com",
            Apis.Models.Datas.Enums.LinkTypes.http)]
        [DataRow(@"V://abc.com",
            Apis.Models.Datas.Enums.LinkTypes.v)]
        [DataRow(@"vmess://abc.com",
            Apis.Models.Datas.Enums.LinkTypes.vmess)]
        [DataRow(@"v2cfg://abc.com",
            Apis.Models.Datas.Enums.LinkTypes.v2cfg)]
        [DataRow(@"linkTypeNotExist://abc.com",
            Apis.Models.Datas.Enums.LinkTypes.unknow)]
        [DataRow(@"abc.com",
            Apis.Models.Datas.Enums.LinkTypes.unknow)]
        [DataRow(@"ss://abc.com",
            Apis.Models.Datas.Enums.LinkTypes.ss)]
        public void DetectLinkTypeTest(string link, Apis.Models.Datas.Enums.LinkTypes expect)
        {
            var linkType = DetectLinkType(link);
            Assert.AreEqual(expect, linkType);
        }


        [DataTestMethod]
        [DataRow(-4, -1)]
        [DataRow(-65535, -1)]
        [DataRow(-65535, -1)]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(4, 3)]
        [DataRow(8, 4)]
        [DataRow(16, 5)]
        [DataRow(65535, 16)]
        [DataRow(65536, 17)]
        public void GetLenInBitsOfIntTest(int value, int expect)
        {
            var len = GetLenInBitsOfInt(value);
            Assert.AreEqual(expect, len);
        }

        /* this test takes about 3min in github action
        [TestMethod]
        public void GetFreePortMultipleThreadsTest()
        {
            List<int> ports = new List<int>();
            object portsWriteLocker = new object();
            void checkPort(int p)
            {
                lock (portsWriteLocker)
                {
                    if (ports.Contains(p))
                    {
                        Assert.Fail();
                    }
                    ports.Add(p);
                }
            }

            void worker()
            {
                var freePort = GetFreeTcpPort();
                checkPort(freePort);
                IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port: freePort);
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(ep);
                    Task.Delay(10).Wait();
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(RunInBackground(worker));
            }

            Task.WaitAll(tasks.ToArray());
        }
        */

        [TestMethod]
        public void GetFreePortSingleThreadTest()
        {
            List<int> ports = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                int port = GetFreeTcpPort();
                Assert.AreEqual(true, port > 0);
                Assert.AreEqual(false, ports.Contains(port));
                ports.Add(port);
            }
        }

#if DEBUG
        [TestMethod]
        public void LazyGuyChainedTaskTest()
        {
            var str = "";

            Action<Action> secTask = (done) =>
             {
                 Task.Delay(200).Wait();
                 str += "2";
                 done();
             };

            Action<Action> firstTask = (done) =>
            {
                Task.Delay(200).Wait();
                str += "1";
                secTask(done);
            };

            var alex = new Apis.Libs.Tasks.LazyGuy(firstTask, 1000, 300);

            str = "";
            alex.Deadline();
            alex.Throttle();
            alex.Deadline();
            alex.Throttle();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual("12", str);

            str = "";
            alex.Postpone();
            alex.Deadline();
            alex.Postpone();
            alex.Throttle();
            Task.Delay(500).Wait();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual("12", str);

            str = "";
            alex.Postpone();
            Task.Delay(500).Wait();
            alex.Postpone();
            Task.Delay(500).Wait();
            alex.Postpone();
            Task.Delay(500).Wait();
            alex.Postpone();
            Task.Delay(500).Wait();
            alex.Postpone();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual("12", str);

            str = "";
            alex.Deadline();
            alex.Deadline();
            alex.Deadline();
            Assert.AreEqual("", str);
            Task.Delay(5000).Wait();
            Assert.AreEqual("12", str);

            str = "";
            alex.Deadline();
            alex.Deadline();
            alex.Deadline();
            alex.ForgetIt();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual("", str);
            alex.PickItUp();

            str = "";
            alex.Throttle();
            alex.Throttle();
            alex.Throttle();
            alex.ForgetIt();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual("", str);
            alex.PickItUp();

            str = "";
            alex.Throttle();
            Task.Delay(100).Wait();
            alex.Throttle();
            alex.Throttle();
            Assert.AreEqual("", str);
            Task.Delay(5000).Wait();
            Assert.AreEqual("1212", str);

        }
#endif

#if DEBUG
        [TestMethod]
        public void LazyGuySingleTaskTest()
        {
            var str = "";

            void task()
            {
                Task.Delay(200).Wait();
                str += ".";
            }

            var adam = new Apis.Libs.Tasks.LazyGuy(task, 1000, 300);

            str = "";
            adam.Postpone();
            Task.Delay(500).Wait();
            Console.WriteLine("500 x 1");
            adam.Postpone();
            Task.Delay(500).Wait();
            Console.WriteLine("500 x 2");
            adam.Postpone();
            Task.Delay(500).Wait();
            Console.WriteLine("500 x 3");
            adam.Postpone();
            Task.Delay(500).Wait();
            Console.WriteLine("500 x 4");
            adam.Postpone();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual(".", str);

            str = "";
            adam.Deadline();
            Task.Delay(10).Wait();
            adam.Deadline();
            Task.Delay(10).Wait();
            adam.Deadline();
            Task.Delay(10).Wait();
            adam.Deadline();
            Task.Delay(10).Wait();
            adam.Deadline();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual(".", str);

            str = "";
            adam.Deadline();
            Task.Delay(3000).Wait();
            Assert.AreEqual(".", str);

            str = "";
            adam.Deadline();
            adam.ForgetIt();
            Task.Delay(3000).Wait();
            Assert.AreEqual("", str);
            adam.PickItUp();

            str = "";
            adam.Throttle();
            adam.ForgetIt();
            Task.Delay(1000).Wait();
            Assert.AreEqual("", str);
            adam.PickItUp();

            str = "";
            adam.Throttle();
            Task.Delay(50).Wait(); // wait for task spin up
            adam.Throttle();
            adam.Throttle();
            adam.Throttle();
            adam.Throttle();
            Assert.AreEqual("", str);
            Task.Delay(3000).Wait();
            Assert.AreEqual("..", str);

            str = "";
            adam.Throttle();
            Task.Delay(1000).Wait();
            Assert.AreEqual(".", str);
        }

#endif

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("11,22,abc")]
        public void CloneTest(string orgStr)
        {
            var org = orgStr?.Split(',').ToList();
            var clone = Clone<List<string>>(org);
            var sClone = SerializeObject(clone);
            var sOrg = SerializeObject(org);
            Assert.AreEqual(sOrg, sClone);
        }

        [DataTestMethod]
        [DataRow("0", 0)]
        [DataRow("-1", -1)]
        [DataRow("str-1.234", 0)]
        [DataRow("-1.234str", 0)]
        [DataRow("-1.234", -1)]
        [DataRow("1.432", 1)]
        [DataRow("1.678", 2)]
        [DataRow("-1.678", -2)]
        public void Str2Int(string value, int expect)
        {
            Assert.AreEqual(expect, Apis.Misc.Utils.Str2Int(value));
        }
    }
}
