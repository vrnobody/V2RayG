﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using V2RayG.Resources.Resx;

namespace V2RayG.Services
{
    public sealed class ShareLinkMgr :
        BaseClasses.SingletonService<ShareLinkMgr>,
        Apis.Interfaces.Services.IShareLinkMgrService
    {
        Settings setting;
        Servers servers;

        ShareLinkComponents.Codecs codecs;

        public ShareLinkMgr()
        {
            codecs = new ShareLinkComponents.Codecs();
        }

        #region properties

        #endregion

        #region IShareLinkMgrService methods

        /// <summary>
        /// return null if fail!
        /// </summary>
        public string DecodeShareLinkToConfig(string shareLink)
        {
            var linkType = Apis.Misc.Utils.DetectLinkType(shareLink);
            switch (linkType)
            {
                case Apis.Models.Datas.Enums.LinkTypes.vmess:
                    return codecs.Decode<ShareLinkComponents.VmessDecoder>(shareLink);
                case Apis.Models.Datas.Enums.LinkTypes.v2cfg:
                    return codecs.Decode<ShareLinkComponents.V2cfgDecoder>(shareLink);
                case Apis.Models.Datas.Enums.LinkTypes.vless:
                    return codecs.Decode<ShareLinkComponents.VlessDecoder>(shareLink);
                case Apis.Models.Datas.Enums.LinkTypes.trojan:
                    return codecs.Decode<ShareLinkComponents.TrojanDecoder>(shareLink);
                default:
                    return null;
            }
        }

        /// <summary>
        /// return null if fail!
        /// </summary>
        public string EncodeConfigToShareLink(
            string config,
            Apis.Models.Datas.Enums.LinkTypes linkType)
        {
            switch (linkType)
            {
                case Apis.Models.Datas.Enums.LinkTypes.ss:
                    return codecs.Encode<ShareLinkComponents.SsDecoder>(config);
                case Apis.Models.Datas.Enums.LinkTypes.vmess:
                    return codecs.Encode<ShareLinkComponents.VmessDecoder>(config);
                case Apis.Models.Datas.Enums.LinkTypes.v2cfg:
                    return codecs.Encode<ShareLinkComponents.V2cfgDecoder>(config);
                case Apis.Models.Datas.Enums.LinkTypes.vless:
                    return codecs.Encode<ShareLinkComponents.VlessDecoder>(config);
                default:
                    return null;
            }
        }
        #endregion

        #region public methods
        public T GetCodec<T>()
            where T : Apis.BaseClasses.ComponentOf<ShareLinkComponents.Codecs>
        {
            return codecs.GetChild<T>();
        }

        public int UpdateSubscriptions(int proxyPort)
        {
            var enabledSubs = setting.GetSubscriptionItems()
                .Where(s => s.isUse)
                .ToList();

            var links = Misc.Utils.FetchLinksFromSubcriptions(enabledSubs, proxyPort);
            var decoders = GenDecoderList(false);
            var results = ImportLinksBatchModeSync(links, decoders);
            return CountImportSuccessResult(results);
        }

        public int ImportLinksWithOutV2cfgLinksSync(string links, string mark)
        {
            if (string.IsNullOrEmpty(links))
            {
                return 0;
            }

            var pair = new string[] { links, mark ?? "" };
            var linkList = new List<string[]> { pair };
            var decoders = GenDecoderList(false);
            var results = ImportLinksBatchModeSync(linkList, decoders);

            // servers.UpdateAllServersSummary();

            return CountImportSuccessResult(results);
        }

        public void ImportLinkWithOutV2cfgLinksBatchModeSync(
            IEnumerable<string[]> linkList)
        {
            var decoders = GenDecoderList(false);
            var results = ImportLinksBatchModeSync(linkList, decoders);
            ShowImportResults(results);
        }

        public void ImportLinkWithOutV2cfgLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            var decoders = GenDecoderList(false);
            ImportLinksBatchModeAsync(linkList, decoders, true);
        }

        public void ImportLinkWithV2cfgLinks(string text)
        {
            var pair = new string[] { text, "" };
            var linkList = new List<string[]> { pair };
            var decoders = GenDecoderList(true);

            ImportLinksBatchModeAsync(linkList, decoders, true);
        }

        public void Run(
           Settings setting,
           Servers servers,
           Cache cache)
        {
            this.setting = setting;
            this.servers = servers;

            codecs.Run(cache, setting);
        }

        #endregion

        #region private methods
        int CountImportSuccessResult(IEnumerable<string[]> result)
        {
            return result.Where(r => Apis.Misc.Utils.IsImportResultSuccess(r)).Count();
        }

        List<Apis.Interfaces.IShareLinkDecoder> GenDecoderList(
           bool isIncludeV2cfgDecoder)
        {
            var decoders = new List<Apis.Interfaces.IShareLinkDecoder>
            {
                codecs.GetChild<ShareLinkComponents.VlessDecoder>(),
                codecs.GetChild<ShareLinkComponents.VmessDecoder>(),
            };

            if (setting.CustomDefImportTrojanShareLink)
            {
                decoders.Add(codecs.GetChild<ShareLinkComponents.TrojanDecoder>());
            }

            if (setting.CustomDefImportSsShareLink)
            {
                decoders.Add(codecs.GetChild<ShareLinkComponents.SsDecoder>());
            }

            if (isIncludeV2cfgDecoder)
            {
                decoders.Add(codecs.GetChild<ShareLinkComponents.V2cfgDecoder>());
            }

            return decoders;
        }


        void ImportLinksBatchModeAsync(
            IEnumerable<string[]> linkList,
            IEnumerable<Apis.Interfaces.IShareLinkDecoder> decoders,
            bool isShowImportResults)
        {
            Apis.Misc.Utils.RunInBackground(() =>
            {
                var results = ImportLinksBatchModeSync(linkList, decoders);
                if (isShowImportResults)
                {
                    ShowImportResults(results);
                }

            });
        }

        /// <summary>
        /// <para>linkList=List(string[]{0: text, 1: mark}>)</para>
        /// <para>decoders = List(IShareLinkDecoder)</para>
        /// </summary>
        IEnumerable<string[]> ImportLinksBatchModeSync(
            IEnumerable<string[]> linkList,
            IEnumerable<Apis.Interfaces.IShareLinkDecoder> decoders)
        {
            var jobs = new List<Tuple<string, string, Apis.Interfaces.IShareLinkDecoder>>();

            foreach (var link in linkList)
            {
                foreach (var decoder in decoders)
                {
                    jobs.Add(new Tuple<string, string, Apis.Interfaces.IShareLinkDecoder>(
                        link[0], link[1], decoder));
                }
            }

            List<string[]> worker(Tuple<string, string, Apis.Interfaces.IShareLinkDecoder> job)
            {
                return ImportShareLinks(job.Item1, job.Item2, job.Item3);
            }

            var results = Misc.Utils.ExecuteInParallel(jobs, worker);
            return results.SelectMany(r => r);
        }

        void ShowImportResults(IEnumerable<string[]> results)
        {
            var c = results.Count();

            if (c <= 0)
            {
                MessageBox.Show(I18N.NoLinkFound);
                return;
            }

            if (IsImportAnyServer(results))
            {
                servers.RequireFormMainReload();
            }

            Apis.Misc.Utils.RunInBackground(() =>
            {
                Views.WinForms.FormImportLinksResult.ShowResult(results);
            });
        }

        private List<string[]> ImportShareLinks(
            string text,
            string mark,
            Apis.Interfaces.IShareLinkDecoder decoder)
        {
            var links = decoder.ExtractLinksFromText(text);

            // Do not use ExecuteInParallel here!
            // Because server's order may changes!

            var results = new List<string[]>();
            foreach (var link in links)
            {
                var decodedConfig = codecs.Decode(link, decoder);
                var msg = AddLinkToServerList(mark, decodedConfig);
                var result = GenImportResult(link, msg.Item1, msg.Item2, mark);
                results.Add(result);
            }

            return results;
        }

        bool IsImportAnyServer(IEnumerable<string[]> importResults)
        {
            return importResults.Any(r => Apis.Misc.Utils.IsImportResultSuccess(r));
        }

        private Tuple<bool, string> AddLinkToServerList(
            string mark,
            string decodedConfig)
        {
            if (string.IsNullOrEmpty(decodedConfig))
            {
                return new Tuple<bool, string>(false, I18N.DecodeFail);
            }
            var isSuccess = servers.AddServer(decodedConfig, mark, true);
            var reason = isSuccess ? I18N.Success : I18N.DuplicateServer;
            return new Tuple<bool, string>(isSuccess, reason);
        }

        string[] GenImportResult(
            string link,
            bool success,
            string reason,
            string mark)
        {
            var importSuccessMark = success ?
                Apis.Models.Consts.Import.MarkImportSuccess :
                Apis.Models.Consts.Import.MarkImportFail;


            return new string[]
            {
                string.Empty,  // reserved for index
                link,
                mark,
                importSuccessMark,  // be aware of IsImportResultSuccess()
                reason,
            };
        }
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            Apis.Libs.Sys.FileLogger.Info("ShareLinkMgr.Cleanup() begin");
            codecs?.Dispose();
            Apis.Libs.Sys.FileLogger.Info("ShareLinkMgr.Cleanup() done");
        }
        #endregion
    }
}
