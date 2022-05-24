﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using V2RayG.Resources.Resx;
using Apis.Interfaces;

namespace V2RayG.Services
{

    public class Servers :
        BaseClasses.SingletonService<Servers>,
        Apis.Interfaces.Services.IServersService
    {
        Settings setting = null;
        Cache cache = null;
        ConfigMgr configMgr;
        Notifier notifier;

        ServersComponents.QueryHandler queryHandler;
        ServersComponents.IndexHandler indexHandler;

        public event EventHandler
            OnCoreStart, // ICoreServCtrl sender
            OnCoreClosing, // ICoreServCtrl sender
            OnCoreStop,

            OnServerPropertyChange,
            OnServerCountChange,

            // special events
            OnRequireFlyPanelReload;

        List<Controllers.CoreServerCtrl> coreServList =
            new List<Controllers.CoreServerCtrl>();

        ConcurrentDictionary<string, bool> markList = new ConcurrentDictionary<string, bool>();
        ConcurrentDictionary<string, bool> configCache = new ConcurrentDictionary<string, bool>();

        Apis.Libs.Tasks.LazyGuy lazyServerSettingsRecorder;
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        Apis.Libs.Tasks.Bar speedTestingBar = new Apis.Libs.Tasks.Bar();

        Servers()
        {
            lazyServerSettingsRecorder = new Apis.Libs.Tasks.LazyGuy(
                SaveServersSettingsWorker,
                Apis.Models.Consts.Intervals.LazySaveServerListIntreval,
                2 * 1000)
            {
                Name = "Servers.SaveSettings()",
            };
        }

        public void Run(
           Settings setting,
           Cache cache,
           ConfigMgr configMgr,
           Notifier notifier)
        {
            this.notifier = notifier;
            this.configMgr = configMgr;
            this.cache = cache;
            this.setting = setting;
            InitServerCtrlList();
            UpdateMarkList();

            queryHandler = new ServersComponents.QueryHandler(
                locker,
                coreServList);

            indexHandler = new ServersComponents.IndexHandler(
                locker,
                coreServList);
        }

        #region sort
        public void ResetIndex() => indexHandler.ResetIndex();

        public void ResetIndexQuiet() => indexHandler.ResetIndexQuiet();

        public void ReverseSelectedByIndex()
        {
            SortSelectedServers((list) => indexHandler.ReverseCoreservCtrlListByIndex(ref list));
        }

        public void SortSelectedBySpeedTest()
        {
            SortSelectedServers((list) => indexHandler.SortCoreServCtrlListBySpeedTestResult(ref list));
        }
        public void SortSelectedByDownloadTotal()
        {
            SortSelectedServers((list) => indexHandler.SortCoreServerCtrlListByDownloadTotal(ref list));
        }
        public void SortSelectedByUploadTotal()
        {
            SortSelectedServers((list) => indexHandler.SortCoreServerCtrlListByUploadTotal(ref list));
        }


        public void SortSelectedByLastModifiedDate()
        {
            SortSelectedServers((list) => indexHandler.SortCoreServerCtrlListByLastModifyDate(ref list));
        }

        public void SortSelectedBySummary()
        {
            SortSelectedServers((list) => indexHandler.SortCoreServCtrlListBySummary(ref list));
        }

        #endregion

        #region querys
        public List<ICoreServCtrl> GetSelectedServers(bool descending = false) =>
            queryHandler.GetSelectedServers(descending);

        public List<ICoreServCtrl> GetRunningServers() =>
            queryHandler.GetRunningServers();

        public List<ICoreServCtrl> GetAllServersOrderByIndex() =>
            queryHandler.GetAllServersOrderByIndex();

        public List<ICoreServCtrl> GetTrackableServerList() =>
            queryHandler.GetTrackableServerList();

        #endregion

        #region event relay

        void InvokeEventOnServerCountChange(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnServerCountChange, sender, EventArgs.Empty);

        void InvokeEventHandlerIgnoreError(EventHandler handler, object sender, EventArgs args)
        {
            try
            {
                handler?.Invoke(sender, args);
            }
            catch { }
        }

        // must transfer sender!
        void InvokeEventOnCoreStartIgnoreError(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnCoreStart, sender, EventArgs.Empty);

        // must transfer sender!
        void InvokeEventOnCoreClosingIgnoreError(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnCoreClosing, sender, EventArgs.Empty);

        // must transfer sender!
        void InvokeEventOnCoreStopIgnoreError(object sender, EventArgs args) =>
            InvokeEventHandlerIgnoreError(OnCoreStop, sender, EventArgs.Empty);

        void InvokeEventOnServerPropertyChange(object sender, EventArgs arg)
        {
            lazyServerSettingsRecorder.Deadline();
            InvokeEventHandlerIgnoreError(OnServerPropertyChange, null, EventArgs.Empty);
        }

        void OnTrackCoreStartHandler(object sender, EventArgs args) =>
            TrackCoreRunningStateHandler(sender, true);

        void OnTrackCoreStopHandler(object sender, EventArgs args) =>
            TrackCoreRunningStateHandler(sender, false);

        void BindEventsTo(Controllers.CoreServerCtrl server)
        {
            server.OnCoreClosing += InvokeEventOnCoreClosingIgnoreError;
            server.OnCoreStart += OnTrackCoreStartHandler;
            server.OnCoreStop += OnTrackCoreStopHandler;
            server.OnPropertyChanged += InvokeEventOnServerPropertyChange;
        }

        void ReleaseEventsFrom(Controllers.CoreServerCtrl server)
        {
            server.OnCoreClosing -= InvokeEventOnCoreClosingIgnoreError;
            server.OnCoreStart -= OnTrackCoreStartHandler;
            server.OnCoreStop -= OnTrackCoreStopHandler;

            server.OnPropertyChanged -= InvokeEventOnServerPropertyChange;
        }
        #endregion

        #region server tracking

        void ServerTrackingUpdateWorker(
            Controllers.CoreServerCtrl coreServCtrl,
            bool isStart)
        {
            var config = coreServCtrl?.GetConfiger()?.GetConfig();

            var curTrackerSetting =
                configMgr.GenCurTrackerSetting(
                    coreServList.AsReadOnly(),
                    config ?? string.Empty,
                    isStart);

            setting.SaveServerTrackerSetting(curTrackerSetting);
            return;
        }

        Libs.Sys.CancelableTimeout lazyServerTrackingTimer = null;
        void DoServerTrackingLater(Action onTimeout)
        {
            lazyServerTrackingTimer?.Release();
            lazyServerTrackingTimer = null;
            lazyServerTrackingTimer = new Libs.Sys.CancelableTimeout(onTimeout, 2000);
            lazyServerTrackingTimer.Start();
        }

        void TrackCoreRunningStateHandler(object sender, bool isCoreStart)
        {
            // for plugins
            if (isCoreStart)
            {
                InvokeEventOnCoreStartIgnoreError(sender, EventArgs.Empty);
            }
            else
            {
                InvokeEventOnCoreStopIgnoreError(sender, EventArgs.Empty);
            }

            if (!setting.isServerTrackerOn)
            {
                return;
            }

            var server = sender as Controllers.CoreServerCtrl;
            if (server.GetCoreStates().IsUntrack())
            {
                return;
            }

            DoServerTrackingLater(
                () => ServerTrackingUpdateWorker(
                    server, isCoreStart));
        }
        #endregion

        #region public method

        public void RequireFormMainReload() =>
            InvokeEventHandlerIgnoreError(OnRequireFlyPanelReload, this, EventArgs.Empty);

        /// <summary>
        /// return -1 when fail
        /// </summary>
        /// <returns></returns>
        public int GetAvailableHttpProxyPort()
        {
            List<ICoreServCtrl> list = GetRunningServers();

            foreach (var serv in list)
            {
                if (serv.GetConfiger().IsSuitableToBeUsedAsSysProxy(
                    true, out bool isSocks, out int port))
                {
                    return port;
                }
            }
            return -1;
        }

        public void OnAutoTrackingOptionChanged() =>
            ServerTrackingUpdateWorker(null, false);

        public int CountSelectedServers()
        {
            locker.EnterReadLock();
            try
            {
                return coreServList.Count(s => s.GetCoreStates().IsSelected());
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public int Count() => coreServList.Count;

        public void SetAllServerIsSelected(bool isSelected)
        {
            List<Controllers.CoreServerCtrl> cache;

            locker.EnterReadLock();
            try
            {
                cache = coreServList.ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }

            foreach (var c in cache)
            {
                try
                {
                    c.GetCoreStates().SetIsSelected(isSelected);
                }
                catch { }
            }
        }

        public string[] GetMarkList() =>
            markList.Keys.OrderBy(x => x).ToArray();

        public void AddNewMark(string newMark)
        {
            if (string.IsNullOrEmpty(newMark) || markList.ContainsKey(newMark))
            {
                return;
            }
            markList.TryAdd(newMark, true);
        }

        public void UpdateMarkList()
        {
            locker.EnterReadLock();
            try
            {
                markList.Clear();
                foreach (var core in coreServList)
                {
                    var mark = core.GetCoreStates().GetMark();
                    AddNewMark(mark);
                }
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public void RestartServersWithImportMark()
        {
            var list = new List<Controllers.CoreServerCtrl>();

            locker.EnterReadLock();
            try
            {
                list = coreServList
                    .Where(s => s.GetCoreStates().IsInjectGlobalImport() && s.GetCoreCtrl().IsCoreRunning())
                    .OrderBy(s => s.GetCoreStates().GetIndex())
                    .ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }

            RestartServersThen(list);
        }

        public bool IsEmpty()
        {

            locker.EnterReadLock();
            try
            {
                return !(this.coreServList.Any());
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public bool IsSelecteAnyServer()
        {
            locker.EnterReadLock();
            try
            {
                return coreServList.Any(s => s.GetCoreStates().IsSelected());
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public string PackSelectedServersV5(
            string orgUid, string pkgName,
             string interval, string url,
            Apis.Models.Datas.Enums.BalancerStrategies strategy,
            Apis.Models.Datas.Enums.PackageTypes packageType)
        {
            var servList = queryHandler.GetSelectedServers();
            return PackServersIntoV5PackageWorker(
                servList, orgUid, pkgName, interval, url, strategy, packageType);
        }

        /// <summary>
        /// packageName is Null or empty ? "PackageV4" : packageName
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="servList"></param>
        public string PackServersV5Ui(
            List<Apis.Interfaces.ICoreServCtrl> servList,
            string orgUid,
            string packageName,
            string interval,
            string url,
            Apis.Models.Datas.Enums.BalancerStrategies strategy,
            Apis.Models.Datas.Enums.PackageTypes packageType)
        {
            if (servList == null || servList.Count < 1)
            {
                Apis.Misc.UI.MsgBoxAsync(I18N.ListIsEmpty);
                return "";
            }

            var uid = PackServersIntoV5PackageWorker(
                servList, orgUid, packageName, interval, url, strategy, packageType);
            Misc.UI.ShowMessageBoxDoneAsync();
            return uid;
        }

        public bool RunSpeedTestOnSelectedServers()
        {
            var evDone = new AutoResetEvent(false);
            var success = BatchSpeedTestWorkerThen(GetSelectedServer(), () => evDone.Set());
            notifier.BlockingWaitOne(evDone);
            return success;
        }

        public void RunSpeedTestOnSelectedServersBg()
        {
            var success = BatchSpeedTestWorkerThen(
                GetSelectedServer(),
                () => MessageBox.Show(I18N.SpeedTestFinished));
            if (!success)
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
            }
        }

        public void RestartServersThen(
            IEnumerable<Apis.Interfaces.ICoreServCtrl> servers,
            Action done = null)
        {
            var list = new List<Apis.Interfaces.ICoreServCtrl>();
            locker.EnterReadLock();
            try
            {
                list = servers.ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }
            void worker(int index, Action next)
            {
                list[index].GetCoreCtrl().RestartCoreThen(next);
            }

            Misc.Utils.ChainActionHelperAsync(list.Count, worker, done);
        }

        public void WakeupServersInBootList()
        {
            List<Controllers.CoreServerCtrl> bootList = configMgr.GenServersBootList(coreServList);

            void worker(int index, Action next)
            {
                bootList[index].GetCoreCtrl().RestartCoreThen(next);
            }

            Misc.Utils.ChainActionHelperAsync(bootList.Count, worker);
        }

        public void RestartSelectedServersThen(Action done = null)
        {
            var list = coreServList;

            void worker(int index, Action next)
            {
                if (list[index].GetCoreStates().IsSelected())
                {
                    list[index].GetCoreCtrl().RestartCoreThen(next);
                }
                else
                {
                    next();
                }
            }

            Misc.Utils.ChainActionHelperAsync(list.Count, worker, done);
        }

        public void StopSelectedServersThen(Action lambda = null)
        {
            var list = coreServList;

            void worker(int index, Action next)
            {
                if (list[index].GetCoreStates().IsSelected())
                {
                    list[index].GetCoreCtrl().StopCoreThen(next);
                }
                else
                {
                    next();
                }
            }

            Misc.Utils.ChainActionHelperAsync(list.Count, worker, lambda);
        }

        public void RestartOneServerByUid(string uid)
        {
            StopAllServers();
            var core = coreServList.FirstOrDefault(c => c.GetCoreStates().GetUid() == uid);
            if (core != null)
            {
                core.GetCoreCtrl().RestartCore();
            }
        }

        public void StopAllServers()
        {
            List<Controllers.CoreServerCtrl> list;

            locker.EnterReadLock();
            try
            {
                list = coreServList.Where(c => c.GetCoreCtrl().IsCoreRunning()).ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }

            foreach (var serv in list)
            {
                serv.GetCoreCtrl().StopCore();
            }
        }

        public void StopAllServersThen(Action lambda = null)
        {
            List<Controllers.CoreServerCtrl> list;

            locker.EnterReadLock();
            try
            {
                list = coreServList.Where(c => c.GetCoreCtrl().IsCoreRunning()).ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }

            void worker(int index, Action next)
            {
                list[index].GetCoreCtrl().StopCoreThen(next);
            }

            Misc.Utils.ChainActionHelperAsync(list.Count, worker, lambda);
        }

        public void DeleteSelectedServersThen(Action done = null)
        {
            if (!speedTestingBar.Install())
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            List<Controllers.CoreServerCtrl> coreServs;
            locker.EnterWriteLock();
            try
            {
                coreServs = coreServList.Where(cs => cs.GetCoreStates().IsSelected()).ToList();
                foreach (var cs in coreServs)
                {
                    var cfg = cs.GetConfiger().GetConfig();
                    configCache.TryRemove(cfg, out _);
                    coreServList.Remove(cs);
                }
            }
            finally
            {
                locker.ExitWriteLock();
            }

            void worker(int index, Action next)
            {
                var cs = coreServs[index];
                DisposeCoreServThen(cs, next);
            }

            void finish()
            {
                lazyServerSettingsRecorder.Deadline();
                UpdateMarkList();
                ResetIndexQuiet();
                RequireFormMainReload();
                InvokeEventOnServerCountChange(this, EventArgs.Empty);
                speedTestingBar.Remove();

                done?.Invoke();
            }

            Misc.Utils.ChainActionHelperAsync(coreServs.Count, worker, finish);
        }

        public void DeleteAllServersThen(Action done = null)
        {
            if (!speedTestingBar.Install())
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            void finish()
            {
                lazyServerSettingsRecorder.Deadline();
                UpdateMarkList();
                RequireFormMainReload();
                InvokeEventOnServerCountChange(this, EventArgs.Empty);
                speedTestingBar.Remove();
                done?.Invoke();
            }

            List<Controllers.CoreServerCtrl> servs;
            locker.EnterWriteLock();
            try
            {
                servs = coreServList.ToList();
                configCache.Clear();
                coreServList.Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }

            void worker(int index, Action next)
            {
                var cs = servs[index];
                DisposeCoreServThen(cs, next);
            }

            Misc.Utils.ChainActionHelperAsync(servs.Count, worker, finish);
        }

        public void UpdateAllServersSummary()
        {
            var list = coreServList.ToList(); // clone
            foreach (var core in list)
            {
                try
                {
                    core.GetConfiger().UpdateSummary();
                    if (core.GetCoreStates().GetLastModifiedUtcTicks() == 0)
                    {
                        var utcTicks = DateTime.UtcNow.Ticks;
                        core.GetCoreStates().SetLastModifiedUtcTicks(utcTicks);
                    }
                }
                catch { }
            }

            RequireFormMainReload();
            setting.LazyGC();
            InvokeEventOnServerPropertyChange(this, EventArgs.Empty);
        }

        public void DeleteServerByConfig(string config)
        {
            if (!speedTestingBar.Install())
            {
                MessageBox.Show(I18N.LastTestNoFinishYet);
                return;
            }

            Controllers.CoreServerCtrl coreServ;
            locker.EnterWriteLock();
            try
            {
                coreServ = coreServList.FirstOrDefault(cs => cs.GetConfiger().GetConfig() == config);
                if (coreServ != null)
                {
                    configCache.TryRemove(config, out _);
                    coreServList.Remove(coreServ);
                }
            }
            finally
            {
                locker.ExitWriteLock();
            }

            if (coreServ == null)
            {
                MessageBox.Show(I18N.CantFindOrgServDelFail);
                speedTestingBar.Remove();
                return;
            }

            DisposeCoreServThen(coreServ, () =>
            {
                InvokeEventOnServerCountChange(this, EventArgs.Empty);
                lazyServerSettingsRecorder.Deadline();
                UpdateMarkList();
                ResetIndexQuiet();
                RequireFormMainReload();
                speedTestingBar.Remove();
            });
        }

        public bool IsServerExist(string config)
        {
            locker.EnterReadLock();
            try
            {
                return IsServerExistWorker(config);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public bool AddServer(string config, string mark, bool quiet = false)
        {
            // first check
            if (IsServerExist(config))
            {
                return false;
            }

            var coreInfo = new Apis.Models.Datas.CoreInfo
            {
                isInjectImport = setting.CustomDefImportGlobalImport,
                customInbType = setting.CustomDefImportMode,
                inbIp = setting.CustomDefImportIp,
                inbPort = setting.CustomDefImportPort,
                config = config,
                customMark = mark,
            };

            var newServer = new Controllers.CoreServerCtrl(coreInfo);
            newServer.Run(cache, setting, configMgr, this);

            bool duplicated = true;
            locker.EnterWriteLock();
            try
            {
                // double check
                if (!IsServerExistWorker(config))
                {
                    configCache.TryAdd(config, true);
                    coreServList.Add(newServer);
                    var idx = coreServList.Count();
                    newServer.GetCoreStates().SetIndexQuiet(idx);
                    AddNewMark(mark);
                    duplicated = false;
                }
            }
            finally
            {
                locker.ExitWriteLock();
            }

            if (duplicated)
            {
                newServer.Dispose();
                return false;
            }

            BindEventsTo(newServer);
            newServer.GetConfiger().UpdateSummary();

            if (!quiet)
            {
                // UpdateSummaryThen will invoke OnServerPropertyChange.
                InvokeEventOnServerCountChange(this, EventArgs.Empty);
                RequireFormMainReload();
            }
            setting.LazyGC();
            lazyServerSettingsRecorder.Deadline();
            return true;
        }

        public bool ReplaceServerConfig(string orgConfig, string newConfig)
        {
            Controllers.CoreServerCtrl coreCtrl;

            locker.EnterReadLock();
            try
            {
                coreCtrl = coreServList.FirstOrDefault(cs => cs.GetConfiger().GetConfig() == orgConfig);
            }
            finally
            {
                locker.ExitReadLock();
            }

            if (coreCtrl == null)
            {
                return false;
            }

            configCache.TryRemove(orgConfig, out _);
            configCache.TryAdd(newConfig, true);
            coreCtrl.GetConfiger().SetConfig(newConfig);
            coreCtrl.GetCoreStates().SetLastModifiedUtcTicks(DateTime.UtcNow.Ticks);
            return true;
        }

        public string ReplaceOrAddNewServer(string orgUid, string newConfig) =>
            ReplaceOrAddNewServer(orgUid, newConfig, @"");

        public string ReplaceOrAddNewServer(string orgUid, string newConfig, string mark)
        {
            string orgConfig = null;

            locker.EnterReadLock();
            try
            {
                var orgServ = coreServList.FirstOrDefault(s => s.GetCoreStates().GetUid() == orgUid);
                if (orgServ != null)
                {
                    orgConfig = orgServ.GetConfiger().GetConfig();
                }
            }
            finally
            {
                locker.ExitReadLock();
            }

            if (orgConfig != null)
            {
                ReplaceServerConfig(orgConfig, newConfig);
                return orgUid;
            }

            AddServer(newConfig, mark);
            locker.EnterReadLock();
            try
            {
                var newServ = coreServList.FirstOrDefault(s => s.GetConfiger().GetConfig() == newConfig);
                if (newServ != null)
                {
                    return newServ.GetCoreStates().GetUid();
                }
            }
            finally
            {
                locker.ExitReadLock();
            }

            return string.Empty;
        }
        #endregion

        #region private methods
        bool IsServerExistWorker(string config)
        {
            return configCache.ContainsKey(config);
        }

        void SaveServersSettingsWorker()
        {
            List<Apis.Models.Datas.CoreInfo> coreInfoList;
            locker.EnterReadLock();
            try
            {
                coreInfoList = coreServList
                   .Select(s => s.GetCoreStates().GetAllRawCoreInfo())
                   .ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }
            setting.SaveServerList(coreInfoList);
        }

        void SortSelectedServers(Action<List<ICoreServCtrl>> sorter)
        {
            lock (locker)
            {
                var selectedServers = queryHandler.GetSelectedServers().ToList();
                sorter?.Invoke(selectedServers);
            }
            RequireFormMainReload();
            InvokeEventOnServerPropertyChange(this, EventArgs.Empty);
        }

        private List<ICoreServCtrl> GetSelectedServer()
        {
            return queryHandler.GetSelectedServers(false);
        }

        void InjectBalacerStrategy(
            ref JObject config,
            string interval,
            string url,
            Apis.Models.Datas.Enums.BalancerStrategies strategy)
        {
            switch (strategy)
            {
                case Apis.Models.Datas.Enums.BalancerStrategies.LeastLoad:
                    config["routing"]["balancingRule"][0]["strategy"]["type"] = "leastload";
                    break;
                case Apis.Models.Datas.Enums.BalancerStrategies.LeastPing:
                    config["routing"]["balancingRule"][0]["strategy"]["type"] = "leastping";
                    // 不知道v2ray-core v5的service怎么配置
                    break;
                default:
                    break;
            }
        }

        string PackServersIntoV5PackageWorker(
           List<ICoreServCtrl> servList,
           string orgUid,
           string packageName,
           string interval,
           string url,
           Apis.Models.Datas.Enums.BalancerStrategies strategy,
           Apis.Models.Datas.Enums.PackageTypes packageType)
        {
            if (servList == null || servList.Count < 1)
            {
                return "";
            }

            JObject package = configMgr.GenV5ServersPackageConfig(
                servList, packageName, packageType);
            string mark;

            switch (packageType)
            {
                case Apis.Models.Datas.Enums.PackageTypes.Balancer:
                    InjectBalacerStrategy(ref package, interval, url, strategy);
                    mark = @"PackageV5";
                    break;
                case Apis.Models.Datas.Enums.PackageTypes.Chain:
                default:
                    mark = @"ChainV5";
                    break;
            }

            var newConfig = package.ToString(Formatting.None);
            string newUid = ReplaceOrAddNewServer(orgUid, newConfig, mark);

            UpdateMarkList();
            setting.SendLog(I18N.PackageDone);
            return newUid;
        }

        bool BatchSpeedTestWorkerThen(IEnumerable<ICoreServCtrl> servList, Action next)
        {
            if (!speedTestingBar.Install())
            {
                return false;
            }

            setting.isSpeedtestCancelled = false;

            var randList = Apis.Misc.Utils.Shuffle(servList);

            Apis.Misc.Utils.RunInBackground(() =>
            {
                Misc.Utils.ExecuteInParallel(
                    randList,
                    serv => serv.GetCoreCtrl().RunSpeedTest());
                speedTestingBar.Remove();
                setting.SendLog(I18N.SpeedTestFinished);
                next?.Invoke();
            });
            return true;
        }

        void InitServerCtrlList()
        {
            locker.EnterWriteLock();
            try
            {
                var coreInfoList = setting.LoadCoreInfoList();
                foreach (var coreInfo in coreInfoList)
                {
                    var server = new Controllers.CoreServerCtrl(coreInfo);
                    coreServList.Add(server);
                }
            }
            finally
            {
                locker.ExitWriteLock();
            }

            foreach (var server in coreServList)
            {
                server.Run(cache, setting, configMgr, this);
                var cfg = server.GetConfiger().GetConfig();
                configCache.TryAdd(cfg, true);
                BindEventsTo(server);
            }
        }

        void DisposeCoreServThen(Controllers.CoreServerCtrl coreServ, Action next = null)
        {
            var copy = coreServ;

            Apis.Misc.Utils.RunInBackground(() =>
            {
                ReleaseEventsFrom(copy);
                copy.Dispose();
                next?.Invoke();
            });
        }

        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            Apis.Libs.Sys.FileLogger.Info("Servers.Cleanup() begin");

            setting.isServerTrackerOn = false;
            if (setting.GetShutdownReason() == Apis.Models.Datas.Enums.ShutdownReasons.Abort)
            {
                Apis.Libs.Sys.FileLogger.Info("Servers.Cleanup() abort");
                return;
            }


            Apis.Libs.Sys.FileLogger.Info("Servers.Cleanup() stop tracking");
            lazyServerTrackingTimer?.Timeout();
            lazyServerTrackingTimer?.Release();

            Apis.Libs.Sys.FileLogger.Info("Servers.Cleanup() save data");
            lazyServerSettingsRecorder?.Dispose();
            SaveServersSettingsWorker();
        }

        #endregion

        #region debug
#if DEBUG
        public void DbgFastRestartTest(int round)
        {
            var list = coreServList.ToList();
            var rnd = new Random();

            var count = list.Count;
            Apis.Misc.Utils.RunInBackground(() =>
            {
                var taskList = new List<Task>();
                for (int i = 0; i < round; i++)
                {
                    var index = rnd.Next(0, count);
                    var isStopCore = rnd.Next(0, 2) == 0;
                    var server = list[index];

                    var task = new Task(() =>
                    {
                        AutoResetEvent sayGoodbye = new AutoResetEvent(false);
                        if (isStopCore)
                        {
                            server.GetCoreCtrl().StopCoreThen(() => sayGoodbye.Set());
                        }
                        else
                        {
                            server.GetCoreCtrl().RestartCoreThen(() => sayGoodbye.Set());
                        }
                        notifier.BlockingWaitOne(sayGoodbye);
                    }, TaskCreationOptions.LongRunning);

                    taskList.Add(task);
                    task.Start();
                }

                Task.WaitAll(taskList.ToArray());
                MessageBox.Show(I18N.Done);
            });
        }
#endif
        #endregion
    }
}
