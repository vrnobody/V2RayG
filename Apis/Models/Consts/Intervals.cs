﻿namespace Apis.Models.Consts
{
    static public class Intervals
    {
        public const int GetStartCoreTokenInterval = 391;

        // Service.Setting 
        public const int LazyGcDelay = 5 * 60 * 1000; // 10 minutes
        public const int LazySaveUserSettingsDelay = 2 * 60 * 1000;
        public const int LazySaveServerListIntreval = 60 * 1000 + 13;
        public const int LazySaveStatisticsDatadelay = 5 * 60 * 1000;

        public const int LazySaveLunaSettingsInterval = 60 * 1000 + 17;

        public const int DefaultSpeedTestTimeout = 20 * 1000;
        public const int DefaultFetchTimeout = 30 * 1000;


        public const int NotifierMenuUpdateIntreval = 1000;

        public const int SiFormLogRefreshInterval = 500;
        public const int LuaPluginLogRefreshInterval = 500;

        public const int FormQrcodeMenuUpdateDelay = 1000;
    }
}
