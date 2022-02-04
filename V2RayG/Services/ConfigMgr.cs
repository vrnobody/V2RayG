using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using V2RayG.Resources.Resx;

namespace V2RayG.Services
{
    sealed public class ConfigMgr :
         BaseClasses.SingletonService<ConfigMgr>,
        Apis.Interfaces.Services.IConfigMgrService
    {
        Settings setting;
        Cache cache;

        static long TIMEOUT = Apis.Models.Consts.Core.SpeedtestTimeout;

        ConfigMgr() { }

        #region public methods

        public long RunCustomSpeedTest(string rawConfig, string testUrl, int testTimeout) =>
            QueuedSpeedTesting(rawConfig, "Custom speed-test", testUrl, testTimeout, false, false, false, null).Item1;

        public long RunSpeedTest(string rawConfig)
        {
            var url = GetDefaultSpeedtestUrl();
            return QueuedSpeedTesting(rawConfig, "Default speed-test", "", GetDefaultTimeout(), false, false, false, null).Item1;
        }

        public Tuple<long, long> RunDefaultSpeedTest(
            string rawConfig,
            string title,
            EventHandler<Apis.Models.Datas.StrEvent> logDeliever)
        {
            var url = GetDefaultSpeedtestUrl();
            return QueuedSpeedTesting(rawConfig, title, url, GetDefaultTimeout(), true, true, false, logDeliever);
        }

        public string InjectImportTpls(
            string config,
            bool isIncludeSpeedTest,
            bool isIncludeActivate)
        {
            JObject import = Misc.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                isIncludeSpeedTest,
                isIncludeActivate,
                false);

            Misc.Utils.MergeJson(import, JObject.Parse(config));
            return import.ToString();
        }

        public JObject DecodeConfig(
            string rawConfig,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl)
        {
            var coreConfig = rawConfig;
            JObject decodedConfig = null;

            try
            {
                string injectedConfig = coreConfig;
                if (isInjectActivateTpl || isInjectSpeedTestTpl)
                {
                    injectedConfig = InjectImportTpls(
                        rawConfig,
                        isInjectSpeedTestTpl,
                        isInjectActivateTpl);
                }

                decodedConfig = ParseImport(injectedConfig);

                // v2ray-core v5 disable temporary
                // MergeCustomTlsSettings(ref decodedConfig);

                cache.core[coreConfig] = decodedConfig.ToString(Formatting.None);
            }
            catch { }

            if (decodedConfig == null)
            {
                setting.SendLog(I18N.DecodeImportFail);
                if (isUseCache)
                {
                    try
                    {
                        decodedConfig = JObject.Parse(cache.core[coreConfig]);
                    }
                    catch (KeyNotFoundException) { }
                    setting.SendLog(I18N.UsingDecodeCache);
                }
            }

            return decodedConfig;
        }

        public bool ModifyInboundWithCustomSetting(
            ref JObject config,
            int inbType,
            string ip,
            int port)
        {
            if (inbType == (int)Models.Datas.Enums.ProxyTypes.Config)
            {
                return true;
            }

            if (inbType == (int)Models.Datas.Enums.ProxyTypes.Custom)
            {
                try
                {
                    var inbs = JArray.Parse(setting.CustomDefInbounds);
                    config["inbounds"] = inbs;
                    return true;
                }
                catch
                {
                    setting.SendLog(I18N.ParseCustomInboundsSettingFail);
                }
                return false;
            }

            if (inbType != (int)Models.Datas.Enums.ProxyTypes.HTTP
                && inbType != (int)Models.Datas.Enums.ProxyTypes.SOCKS)
            {
                return false;
            }

            var protocol = Misc.Utils.InboundTypeNumberToName(inbType);
            try
            {
                JObject o = CreateInboundSetting(ip, port, protocol);
                ReplaceInboundSetting(ref config, o);
#if DEBUG
                var debug = config.ToString(Formatting.Indented);
#endif
                return true;
            }
            catch
            {
                setting.SendLog(I18N.CoreCantSetLocalAddr);
            }
            return false;
        }

        /*
         * exceptions  
         * test<FormatException> base64 decode fail
         * test<System.Net.WebException> url not exist
         * test<Newtonsoft.Json.JsonReaderException> json decode fail
         */
        public JObject ParseImport(string configString)
        {
            var maxDepth = Apis.Models.Consts.Import.ParseImportDepth;

            var result = Misc.Utils.ParseImportRecursively(
                GetHtmlContentFromCache,
                JObject.Parse(configString),
                maxDepth);

            try
            {
                Misc.Utils.RemoveKeyFromJObject(result, "v2rayg.import");
            }
            catch (KeyNotFoundException)
            {
                // do nothing;
            }

            return result;
        }

        /// <summary>
        /// update running servers list
        /// </summary>
        /// <param name="includeCurServer"></param>
        public Models.Datas.ServerTracker GenCurTrackerSetting(
            IEnumerable<Controllers.CoreServerCtrl> servers,
            string curServerConfig,
            bool isStart)
        {
            var trackerSetting = setting.GetServerTrackerSetting();
            var tracked = trackerSetting.serverList;

            var running = servers
                .Where(s => s.GetCoreCtrl().IsCoreRunning()
                    && !s.GetCoreStates().IsUntrack())
                .Select(s => s.GetConfiger().GetConfig())
                .ToList();

            tracked.RemoveAll(c => !running.Any(r => r == c));  // remove stopped
            running.RemoveAll(r => tracked.Any(t => t == r));
            tracked.AddRange(running);
            tracked.Remove(curServerConfig);

            if (isStart)
            {
                trackerSetting.curServer = curServerConfig;
            }
            else
            {
                trackerSetting.curServer = null;
            }

            trackerSetting.serverList = tracked;
            return trackerSetting;
        }

        public List<Controllers.CoreServerCtrl> GenServersBootList(
            IEnumerable<Controllers.CoreServerCtrl> serverList)
        {
            var trackerSetting = setting.GetServerTrackerSetting();
            if (!trackerSetting.isTrackerOn)
            {
                return serverList.Where(s => s.GetCoreStates().IsAutoRun()).ToList();
            }

            setting.isServerTrackerOn = true;
            var trackList = trackerSetting.serverList;

            var bootList = serverList
                .Where(s => s.GetCoreStates().IsAutoRun()
                || trackList.Contains(s.GetConfiger().GetConfig()))
                .ToList();

            if (string.IsNullOrEmpty(trackerSetting.curServer))
            {
                return bootList;
            }

            bootList.RemoveAll(s => s.GetConfiger().GetConfig() == trackerSetting.curServer);
            var lastServer = serverList.FirstOrDefault(
                    s => s.GetConfiger().GetConfig() == trackerSetting.curServer);
            if (lastServer != null && !lastServer.GetCoreStates().IsUntrack())
            {
                bootList.Insert(0, lastServer);
            }
            return bootList;
        }

        public JObject GenV5ServersPackageConfig(
            List<Apis.Interfaces.ICoreServCtrl> servList,
            string packageName,
            Apis.Models.Datas.Enums.PackageTypes packageType)
        {
            JObject package;
            switch (packageType)
            {
                case Apis.Models.Datas.Enums.PackageTypes.Chain:
                    package = GenV5ChainConfig(servList, packageName);
                    break;
                case Apis.Models.Datas.Enums.PackageTypes.Balancer:
                default:
                    package = GenV5BalancerConfig(servList, packageName);
                    break;
            }

            try
            {
                var finalConfig = GetGlobalImportConfigForPacking();
                Misc.Utils.CombineConfigWithRoutingInTheEnd(ref finalConfig, package);
                return finalConfig;
            }
            catch
            {
                setting.SendLog(I18N.InjectPackagingImportsFail);
                return package;
            }
        }

        public void Run(
            Settings setting,
            Cache cache)
        {
            this.setting = setting;
            this.cache = cache;
        }

        #endregion

        #region private methods
        JObject GenV5ChainConfig(
        List<Apis.Interfaces.ICoreServCtrl> servList,
        string packageName)
        {
            var package = cache.tpl.LoadPackage("chainV5Tpl");
            var outbounds = package["outbounds"] as JArray;
            var description = new List<string>();

            JObject prev = null;
            for (var i = 0; i < servList.Count; i++)
            {
                var s = servList[i];
                var parts = Misc.Utils.ExtractOutboundsFromConfig(
                    s.GetConfiger().GetFinalConfig());
                var c = 0;
                foreach (JObject p in parts)
                {
                    var tag = $"node{i}s{c++}";
                    p["tag"] = tag;
                    if (prev != null)
                    {
                        prev["proxySettings"] = JObject.Parse(@"{tag: '',transportLayer: true}");
                        prev["proxySettings"]["tag"] = tag;
                        outbounds.Add(prev);
                    }
                    prev = p;
                }
                var name = s.GetCoreStates().GetName();
                if (c == 0)
                {
                    setting.SendLog(I18N.PackageFail + ": " + name);
                }
                else
                {
                    description.Add($"{i}.[{name}]");
                    setting.SendLog(I18N.PackageSuccess + ": " + name);
                }
            }
            outbounds.Add(prev);

            package["v2rayg"]["alias"] = string.IsNullOrEmpty(packageName) ? "ChainV4" : packageName;
            package["v2rayg"]["description"] =
                $"[Total: {description.Count()}] " +
                string.Join(" ", description);

            return package;
        }

        private JObject GenV5BalancerConfig(List<Apis.Interfaces.ICoreServCtrl> servList, string packageName)
        {
            var package = cache.tpl.LoadPackage("pkgV5Tpl");
            var outbounds = package["outbounds"] as JArray;
            var description = new List<string>();

            for (var i = 0; i < servList.Count; i++)
            {
                var s = servList[i];
                var parts = Misc.Utils.ExtractOutboundsFromConfig(
                    s.GetConfiger().GetFinalConfig());
                var c = 0;
                foreach (JObject p in parts)
                {
                    p["tag"] = $"node{i}s{c++}";
                    outbounds.Add(p);
                }
                var name = s.GetCoreStates().GetName();
                if (c == 0)
                {
                    setting.SendLog(I18N.PackageFail + ": " + name);
                }
                else
                {
                    description.Add($"{i}.[{name}]");
                    setting.SendLog(I18N.PackageSuccess + ": " + name);
                }
            }

            package["v2rayg"]["alias"] = string.IsNullOrEmpty(packageName) ? "PackageV4" : packageName;
            package["v2rayg"]["description"] =
                $"[Total: {description.Count()}] " +
                string.Join(" ", description);
            return package;
        }

        void MergeCustomTlsSettings(ref JObject config)
        {
            var outB = Misc.Utils.GetKey(config, "outbound") ??
                Misc.Utils.GetKey(config, "outbounds.0");

            if (outB == null)
            {
                return;
            }

            JObject streamSettings = Misc.Utils.GetKey(outB, "streamSettings") as JObject;
            if (streamSettings == null)
            {
                return;
            }

            if (setting.isSupportSelfSignedCert)
            {
                var selfSigned = JObject.Parse(@"{tlsSettings: {allowInsecure: true}}");
                Misc.Utils.MergeJson(streamSettings, selfSigned);
            }

            if (setting.isEnableUtlsFingerprint)
            {
                var uTlsFingerprint = JObject.Parse(@"{tlsSettings: {}}");
                uTlsFingerprint["tlsSettings"]["fingerprint"] = setting.uTlsFingerprint;
                Misc.Utils.MergeJson(streamSettings, uTlsFingerprint);
            }
        }

        int GetDefaultTimeout()
        {
            var customTimeout = setting.CustomSpeedtestTimeout;
            if (customTimeout > 0)
            {
                return customTimeout;
            }
            return Apis.Models.Consts.Intervals.DefaultSpeedTestTimeout;
        }

        string GetDefaultSpeedtestUrl() =>
          setting.isUseCustomSpeedtestSettings ?
          setting.CustomSpeedtestUrl :
          Apis.Models.Consts.Webs.GoogleDotCom;

        JObject GetGlobalImportConfigForPacking()
        {
            var imports = Misc.Utils.ImportItemList2JObject(
                setting.GetGlobalImportItems(),
                false, false, true);
            return ParseImport(imports.ToString());
        }

        Tuple<long, long> QueuedSpeedTesting(
            string rawConfig,
            string title,
            string testUrl,
            int testTimeout,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl,
            EventHandler<Apis.Models.Datas.StrEvent> logDeliever)
        {
            Interlocked.Increment(ref setting.SpeedtestCounter);

            // setting.SpeedTestPool may change while testing
            var pool = setting.SpeedTestPool;
            pool.Wait();

            var result = new Tuple<long, long>(Apis.Models.Consts.Core.SpeedtestAbort, 0);
            if (!setting.isSpeedtestCancelled)
            {
                var port = Apis.Misc.Utils.GetFreeTcpPort();
                var cfg = CreateSpeedTestConfig(rawConfig, port, isUseCache, isInjectSpeedTestTpl, isInjectActivateTpl);
                result = DoSpeedTesting(title, testUrl, testTimeout, port, cfg, logDeliever);
            }

            pool.Release();
            Interlocked.Decrement(ref setting.SpeedtestCounter);
            return result;
        }

        bool WaitUntilCoreReady(Libs.V2Ray.Core core)
        {
            const int jiff = 300;
            int cycle = 30 * 1000 / jiff;
            int i;
            for (i = 0; i < cycle && !core.isReady && core.isRunning; i++)
            {
                Apis.Misc.Utils.Sleep(jiff);
            }

            if (!core.isRunning)
            {
                return false;
            }

            if (i < cycle)
            {
                return true;
            }
            return false;
        }

        Tuple<long, long> DoSpeedTesting(
            string title,
            string testUrl,
            int testTimeout,
            int port,
            string config,
            EventHandler<Apis.Models.Datas.StrEvent> logDeliever)
        {
            void log(string content) => logDeliever?.Invoke(this, new Apis.Models.Datas.StrEvent(content));

            log($"{I18N.SpeedtestPortNum}{port}");
            if (string.IsNullOrEmpty(config))
            {
                log(I18N.DecodeImportFail);
                return new Tuple<long, long>(TIMEOUT, 0);
            }

            var speedTester = new Libs.V2Ray.Core(setting) { title = title };
            if (logDeliever != null)
            {
                speedTester.OnLog += logDeliever;
            }

            long latency = TIMEOUT;
            long len = 0;
            try
            {
                speedTester.RestartCoreIgnoreError(config);
                if (WaitUntilCoreReady(speedTester))
                {
                    var expectedSizeInKib = setting.isUseCustomSpeedtestSettings ? setting.CustomSpeedtestExpectedSizeInKib : -1;
                    var r = Apis.Misc.Utils.TimedDownloadTest(testUrl, port, expectedSizeInKib, testTimeout);
                    latency = r.Item1;
                    len = r.Item2;
                }
                speedTester.StopCore();
            }
            catch { }
            if (logDeliever != null)
            {
                speedTester.OnLog -= logDeliever;
            }
            return new Tuple<long, long>(latency, len);
        }

        List<string> GetHtmlContentFromCache(IEnumerable<string> urls)
        {
            if (urls == null || urls.Count() <= 0)
            {
                return new List<string>();
            }
            return Misc.Utils.ExecuteInParallel(urls, (url) => cache.html[url]);
        }

        JObject CreateInboundSetting(
           string ip,
           int port,
           string protocol)
        {
            var o = JObject.Parse(@"{}");
            o["tag"] = "agentin";
            o["protocol"] = protocol;
            o["listen"] = ip;
            o["port"] = port;
            o["settings"] = JObject.Parse(@"{}");
            return o;
        }

        string CreateSpeedTestConfig(
            string rawConfig,
            int port,
            bool isUseCache,
            bool isInjectSpeedTestTpl,
            bool isInjectActivateTpl)
        {
            var empty = string.Empty;
            if (port <= 0)
            {
                return empty;
            }

            var config = DecodeConfig(
                rawConfig, isUseCache, isInjectSpeedTestTpl, isInjectActivateTpl);

            if (config == null)
            {
                return empty;
            }

            // default log level is warnig
            config["log"] = JObject.Parse(@"{}");

            if (!ModifyInboundWithCustomSetting(
                ref config,
                (int)Models.Datas.Enums.ProxyTypes.HTTP,
                Apis.Models.Consts.Webs.LoopBackIP,
                port))
            {
                return empty;
            }

            // debug
            var configString = config.ToString(Formatting.None);

            return configString;
        }

        void ReplaceInboundSetting(ref JObject config, JObject o)
        {
            // Bug. Stream setting will mess things up.
            // Misc.Utils.MergeJson(ref config, o);

            var hasInbounds = Misc.Utils.GetKey(config, "inbounds.0") != null;

            if (!hasInbounds)
            {
                config["inbounds"] = JArray.Parse(@"[{}]");
            }
            config["inbounds"][0] = o;
        }


        #endregion
    }
}
