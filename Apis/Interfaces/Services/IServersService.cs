using System;
using System.Collections.Generic;

namespace Apis.Interfaces.Services
{
    public interface IServersService
    {
        event EventHandler OnCoreStart, OnCoreClosing, OnCoreStop;

        int Count();

        int GetAvailableHttpProxyPort();
        string ReplaceOrAddNewServer(string orgUid, string newConfig);

        string ReplaceOrAddNewServer(string orgUid, string newConfig, string mark);

        void RequireFormMainReload();

        void ResetIndexQuiet();

        void RestartOneServerByUid(string uid);

        bool RunSpeedTestOnSelectedServers();

        void ReverseSelectedByIndex();

        void SortSelectedByLastModifiedDate();

        void SortSelectedBySpeedTest();

        void SortSelectedBySummary();

        void StopAllServers();

        void StopAllServersThen(Action lambda = null);

        void UpdateAllServersSummary();

        string PackSelectedServersV5(
              string orgUid, string pkgName,
              string interval, string url,
              Apis.Models.Datas.Enums.BalancerStrategies strategy,
              Apis.Models.Datas.Enums.PackageTypes packageType);

        string PackServersV5Ui(
            List<Apis.Interfaces.ICoreServCtrl> servList,
            string orgUid,
            string packageName,
            string interval,
            string url,
            Apis.Models.Datas.Enums.BalancerStrategies strategy,
            Apis.Models.Datas.Enums.PackageTypes packageType);

        List<ICoreServCtrl> GetTrackableServerList();
        List<ICoreServCtrl> GetAllServersOrderByIndex();
        List<ICoreServCtrl> GetSelectedServers(bool descending = false);
    }
}
