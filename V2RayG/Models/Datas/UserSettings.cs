﻿using System.Collections.Generic;

namespace V2RayG.Models.Datas
{
    class UserSettings
    {
        #region public properties
        public string CustomInbounds { get; set; }
        public string DebugLogFilePath { get; set; }
        public bool isEnableDebugFile { get; set; }
        public int QuickSwitchServerLatency { get; set; }
        public bool isAutoPatchSubsInfo { get; set; }

        // FormOption->Defaults->Mode
        public ImportSharelinkOptions ImportOptions = null;

        // FormOption->Defaults->Speedtest
        public SpeedTestOptions SpeedtestOptions = null;

        // FormDownloadCore
        public bool isDownloadWin32V2RayCore { get; set; }
        public string v2rayCoreDownloadSource { get; set; }
        public List<string> V2RayCoreDownloadVersionList = null;

        public bool isSupportSelfSignedCert { get; set; }
        public string uTlsFingerprint { get; set; }

        public bool isEnableUtlsFingerprint { get; set; }

        public int ServerPanelPageSize { get; set; }
        public bool isEnableStat { get; set; } = false;
        public bool CfgShowToolPanel { get; set; }
        public bool isPortable { get; set; }
        public bool isCheckUpdateWhenAppStart { get; set; }

        public bool isCheckV2RayCoreUpdateWhenAppStart { get; set; }
        public bool isUpdateUseProxy { get; set; }

        // v2ray-core v4.23.1 multiple config file supports
        public string MultiConfItems { get; set; }

        public string ImportUrls { get; set; }
        public string DecodeCache { get; set; }
        public string SubscribeUrls { get; set; }

        public string PluginInfoItems { get; set; }
        public string PluginsSetting { get; set; }
        public string CompressedPluginsSetting { get; set; }
        public string CompressedUnicodePluginsSetting { get; set; }

        public string Culture { get; set; }
        public string CoreInfoList { get; set; }
        public string CompressedCoreInfoList { get; set; }
        public string CompressedUnicodeCoreInfoList { get; set; }

        public string ServerTracker { get; set; }
        public string WinFormPosList { get; set; }

        public int MaxConcurrentV2RayCoreNum { get; set; }
        #endregion


        public UserSettings()
        {
            Normalized();

            isDownloadWin32V2RayCore = true;
            v2rayCoreDownloadSource = Apis.Models.Consts.Core.GetSourceUrlByIndex(0);

            CustomInbounds = @"[]";

            DebugLogFilePath = @"";
            isEnableDebugFile = false;

            QuickSwitchServerLatency = 0;

            isSupportSelfSignedCert = false;
            uTlsFingerprint = @"";
            isEnableUtlsFingerprint = false;

            isAutoPatchSubsInfo = false;

            ServerPanelPageSize = 8;

            MaxConcurrentV2RayCoreNum = 20;

            isCheckUpdateWhenAppStart = false;

            isCheckV2RayCoreUpdateWhenAppStart = false;

            isUpdateUseProxy = true;

            CfgShowToolPanel = true;
            isPortable = true;

            MultiConfItems = string.Empty;
            ImportUrls = string.Empty;
            DecodeCache = string.Empty;
            SubscribeUrls = @"[]";

            // PluginInfoItems = "[{\"filename\":\"ProxySetter\",\"isUse\":true}]";
            PluginInfoItems = string.Empty;
            PluginsSetting = string.Empty;
            CompressedPluginsSetting = string.Empty;
            CompressedUnicodePluginsSetting = string.Empty;

            Culture = string.Empty;
            CoreInfoList = string.Empty;
            CompressedCoreInfoList = string.Empty;
            CompressedUnicodeCoreInfoList = string.Empty;

            ServerTracker = string.Empty;
            WinFormPosList = string.Empty;

        }

        #region public methods
        public void Normalized()
        {
            V2RayCoreDownloadVersionList = V2RayCoreDownloadVersionList ?? new List<string>();
            ImportOptions = ImportOptions ?? new ImportSharelinkOptions();
            SpeedtestOptions = SpeedtestOptions ?? new SpeedTestOptions();
        }
        #endregion
    }
}
