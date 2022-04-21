﻿namespace Apis.Models.Consts
{
    static public class Intervals
    {
        public const int GetStartCoreTokenInterval = 391;

        // Service.Setting 
        public const int LazyGcDelay = 5 * 60 * 1000; // 10 minutes
        public const int LazySaveUserSettingsDelay = 5 * 60 * 1000;
        public const int LazySaveServerListIntreval = 3 * 60 * 1000;
        public const int LazySaveStatisticsDatadelay = 5 * 60 * 1000;

        public const int LazySaveLunaSettingsInterval = 3 * 60 * 1000;

        public const int DefaultSpeedTestTimeout = 20 * 1000;
        public const int DefaultFetchTimeout = 30 * 1000;


        public const int NotifierMenuUpdateIntreval = 1000;

        public const int SiFormLogRefreshInterval = 500;
        public const int LuaPluginLogRefreshInterval = 500;

        public const int FormQrcodeMenuUpdateDelay = 1000;
    }
}
