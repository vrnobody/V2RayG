using System.Collections.Generic;
using GlobalApis = global::Apis;

namespace Luna.Models.Apis.Components
{
    internal sealed class Server :
        GlobalApis.BaseClasses.ComponentOf<LuaApis>,
        GlobalApis.Interfaces.Lua.ILuaServer
    {
        GlobalApis.Interfaces.Services.IServersService vgcServers;
        GlobalApis.Interfaces.Services.IConfigMgrService vgcConfigMgr;

        public Server(
             GlobalApis.Interfaces.Services.IApiService api)
        {
            vgcServers = api.GetServersService();
            vgcConfigMgr = api.GetConfigMgrService();
        }

        #region balancer
        public int BalancerStrategyRandom { get; } = (int)GlobalApis.Models.Datas.Enums.BalancerStrategies.Random;

        public int BalancerStrategyLeastPing { get; } = (int)GlobalApis.Models.Datas.Enums.BalancerStrategies.LeastPing;

        public int BalancerStrategyLeastLoad { get; } = (int)GlobalApis.Models.Datas.Enums.BalancerStrategies.LeastLoad;
        #endregion

        public int Count() =>
            vgcServers.Count();

        public void UpdateAllSummary() =>
            vgcServers.UpdateAllServersSummary();

        public void ResetIndexes() =>
            vgcServers.ResetIndexQuiet();

        // expose for ILuaServer
        public long RunSpeedTest(string rawConfig) =>
            vgcConfigMgr.RunSpeedTest(rawConfig);

        public long RunCustomSpeedTest(string rawConfig, string testUrl, int testTimeout) =>
            vgcConfigMgr.RunCustomSpeedTest(rawConfig, testUrl, testTimeout);

        public List<GlobalApis.Interfaces.ICoreServCtrl> GetAllServers() =>
            vgcServers.GetAllServersOrderByIndex();

        public void ReverseSelectedByIndex() =>
            vgcServers.ReverseSelectedByIndex();

        public void SortSelectedServersByLastModifiedDate() =>
            vgcServers.SortSelectedByLastModifiedDate();

        public void SortSelectedServersBySummary() =>
            vgcServers.SortSelectedBySummary();

        public void SortSelectedServersBySpeedTest() =>
            vgcServers.SortSelectedBySpeedTest();

        public void StopAllServers() =>
            vgcServers.StopAllServers();

        public bool RunSpeedTestOnSelectedServers() =>
            vgcServers.RunSpeedTestOnSelectedServers();

        public string PackSelectedServers(string orgUid, string pkgName) =>
            PackSelectedServers(
                orgUid, pkgName,
                (int)GlobalApis.Models.Datas.Enums.BalancerStrategies.Random);

        public string PackSelectedServers(string orgUid, string pkgName, int strategy)
        {
            return PackSelectedServers(
                orgUid,
                pkgName,
                strategy,
                string.Empty,
                string.Empty);
        }

        public string PackSelectedServers(
            string orgUid, string pkgName, int strategy,
            string interval, string url)
        {
            var st = (GlobalApis.Models.Datas.Enums.BalancerStrategies)strategy;
            return vgcServers.PackSelectedServersV5(
                orgUid,
                pkgName,
                interval,
                url,
                st,
                GlobalApis.Models.Datas.Enums.PackageTypes.Balancer);
        }

        public string ChainSelectedServers(string orgUid, string pkgName) =>
            vgcServers.PackSelectedServersV5(
                orgUid,
                pkgName,
                string.Empty,
                string.Empty,
                GlobalApis.Models.Datas.Enums.BalancerStrategies.Random,
                GlobalApis.Models.Datas.Enums.PackageTypes.Chain);
    }
}
