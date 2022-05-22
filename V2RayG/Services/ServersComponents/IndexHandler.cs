using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace V2RayG.Services.ServersComponents
{
    internal sealed class IndexHandler
    {
        ReaderWriterLockSlim locker;
        List<Controllers.CoreServerCtrl> coreServList;

        public IndexHandler(
            ReaderWriterLockSlim locker,
            List<Controllers.CoreServerCtrl> coreServList)
        {
            this.locker = locker;
            this.coreServList = coreServList;
        }

        #region properties

        #endregion

        #region public methods
        public void SortCoreServCtrlListBySpeedTestResult(
            ref List<Apis.Interfaces.ICoreServCtrl> coreList)
        {
            SortServerItemList(ref coreList, SpeedTestComparer);
        }

        public void ReverseCoreservCtrlListByIndex(
            ref List<Apis.Interfaces.ICoreServCtrl> coreList)
        {
            SortServerItemList(ref coreList, ReverseIndexComparer);
        }

        public void SortCoreServerCtrlListByDownloadTotal(
            ref List<Apis.Interfaces.ICoreServCtrl> coreList)
        {
            SortServerItemList(ref coreList, DownloadTotalDecComparer);
        }

        public void SortCoreServerCtrlListByUploadTotal(
            ref List<Apis.Interfaces.ICoreServCtrl> coreList)
        {
            SortServerItemList(ref coreList, UploadTotalDecComparer);
        }

        public void SortCoreServerCtrlListByLastModifyDate(
            ref List<Apis.Interfaces.ICoreServCtrl> coreList)
        {
            SortServerItemList(ref coreList, UtcTicksDecComparer);
        }

        public void SortCoreServCtrlListBySummary(
            ref List<Apis.Interfaces.ICoreServCtrl> coreList)
        {
            SortServerItemList(ref coreList, SummaryComparer);
        }


        public void ResetIndex()
        {
            var pkgs = new List<Tuple<double, Apis.Interfaces.CoreCtrlComponents.ICoreStates>>();

            locker.EnterReadLock();
            try
            {
                List<Apis.Interfaces.CoreCtrlComponents.ICoreStates> coreStates = coreServList
                   .OrderBy(c => c.GetCoreStates().GetIndex())
                   .Select(c => c.GetCoreStates())
                   .ToList();
                double idx = 0;
                foreach (var coreState in coreStates)
                {
                    var pkg = new Tuple<double, Apis.Interfaces.CoreCtrlComponents.ICoreStates>(++idx, coreState);
                    pkgs.Add(pkg);
                }
            }
            finally
            {
                locker.ExitReadLock();
            }

            Apis.Misc.Utils.RunInBackground(() =>
            {
                foreach (var pkg in pkgs)
                {
                    var coreState = pkg.Item2;
                    var idx = pkg.Item1;
                    coreState.SetIndex(idx);
                }
            });
        }

        public void ResetIndexQuiet()
        {
            List<Controllers.CoreServerCtrl> sortedServers = new List<Controllers.CoreServerCtrl>();
            locker.EnterReadLock();
            try
            {
                sortedServers = coreServList
                    .OrderBy(c => c.GetCoreStates().GetIndex())
                    .ToList();
            }
            finally
            {
                locker.ExitReadLock();
            }

            for (int i = 0; i < sortedServers.Count(); i++)
            {
                var index = i + 1.0; // closure
                sortedServers[i]
                    .GetCoreStates()
                    .SetIndexQuiet(index);
            }
        }
        #endregion

        #region private methods
        int ReverseIndexComparer(
           Apis.Interfaces.ICoreServCtrl a,
           Apis.Interfaces.ICoreServCtrl b)
        {
            var idxA = a.GetCoreStates().GetIndex();
            var idxB = b.GetCoreStates().GetIndex();
            return idxB.CompareTo(idxA);
        }

        int SpeedTestComparer(
            Apis.Interfaces.ICoreServCtrl a,
            Apis.Interfaces.ICoreServCtrl b)
        {
            var spa = a.GetCoreStates().GetSpeedTestResult();
            var spb = b.GetCoreStates().GetSpeedTestResult();
            return spa.CompareTo(spb);
        }

        int DownloadTotalDecComparer(
           Apis.Interfaces.ICoreServCtrl a,
           Apis.Interfaces.ICoreServCtrl b)
        {
            var ticksA = a.GetCoreStates().GetDownlinkTotalInBytes();
            var ticksB = b.GetCoreStates().GetDownlinkTotalInBytes();
            return ticksB.CompareTo(ticksA);
        }

        int UploadTotalDecComparer(
           Apis.Interfaces.ICoreServCtrl a,
           Apis.Interfaces.ICoreServCtrl b)
        {
            var ticksA = a.GetCoreStates().GetUplinkTotalInBytes();
            var ticksB = b.GetCoreStates().GetUplinkTotalInBytes();
            return ticksB.CompareTo(ticksA);
        }

        int UtcTicksDecComparer(
           Apis.Interfaces.ICoreServCtrl a,
           Apis.Interfaces.ICoreServCtrl b)
        {
            var ticksA = a.GetCoreStates().GetLastModifiedUtcTicks();
            var ticksB = b.GetCoreStates().GetLastModifiedUtcTicks();
            return ticksB.CompareTo(ticksA);
        }

        int SummaryComparer(
            Apis.Interfaces.ICoreServCtrl a,
            Apis.Interfaces.ICoreServCtrl b)
        {
            var sma = a.GetCoreStates().GetSummary();
            var smb = b.GetCoreStates().GetSummary();

            var rsma = Apis.Misc.Utils.ReverseSummary(sma);
            var rsmb = Apis.Misc.Utils.ReverseSummary(smb);

            return rsma.CompareTo(rsmb);
        }

        void SortServerItemList(
             ref List<Apis.Interfaces.ICoreServCtrl> selectedServers,
             Comparison<Apis.Interfaces.ICoreServCtrl> comparer)
        {
            if (selectedServers == null || selectedServers.Count() < 2)
            {
                return;
            }
            locker.EnterWriteLock();
            try
            {
                selectedServers.Sort(comparer);
                var minIndex = selectedServers.Select(s => s.GetCoreStates().GetIndex()).Min();
                var delta = 1.0 / 2 / selectedServers.Count;
                for (int i = 0; i < selectedServers.Count; i++)
                {
                    selectedServers[i].GetCoreStates()
                        .SetIndexQuiet(minIndex + delta * (i + 1));
                }
            }
            finally
            {
                locker.ExitWriteLock();
            }
            ResetIndexQuiet();
        }
        #endregion

        #region protected methods

        #endregion
    }
}
