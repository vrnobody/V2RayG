﻿using System.Collections.Generic;
using System.Linq;

namespace Luna.Services
{
    internal class Settings :
        Apis.BaseClasses.Disposable

    {
        Apis.Interfaces.Services.ISettingsService vgcSetting;

        readonly string pluginName = Properties.Resources.Name;
        Models.Data.UserSettings userSettings;
        Libs.LuaSnippet.SnippetsCache snpCache;

        Apis.Libs.Tasks.LazyGuy lazyBookKeeper;

        public Settings()
        {
            lazyBookKeeper = new Apis.Libs.Tasks.LazyGuy(
                    SaveUserSettingsNow,
                    Apis.Models.Consts.Intervals.LazySaveLunaSettingsInterval,
                    3000);
        }

        #region properties
        bool _isDisposing = false;
        public void SetIsDisposing(bool value) => _isDisposing = value;

        public bool isScreenLocked => vgcSetting.IsScreenLocked();


        #endregion

        #region internal methods

        public Libs.LuaSnippet.BestMatchSnippets CreateBestMatchSnippet(ScintillaNET.Scintilla editor)
            => snpCache?.CreateBestMatchSnippets(editor);

        #endregion

        #region public methods

        public void SendLog(string contnet)
        {
            var name = Properties.Resources.Name;
            vgcSetting.SendLog(string.Format("[{0}] {1}", name, contnet));
        }

        public bool IsClosing() => _isDisposing || vgcSetting.IsClosing();

        public void Run(
            Apis.Interfaces.Services.ISettingsService vgcSetting)
        {
            this.vgcSetting = vgcSetting;

            this.snpCache = new Libs.LuaSnippet.SnippetsCache();

            userSettings = Apis.Misc.Utils
                .LoadPluginSetting<Models.Data.UserSettings>(
                    pluginName, vgcSetting);

            userSettings.NormalizeData();
        }

        public string GetLuaShareMemory(string key)
        {
            if (string.IsNullOrEmpty(key) || !userSettings.luaShareMemory.ContainsKey(key))
            {
                return @"";
            }
            return userSettings.luaShareMemory[key];
        }

        readonly object shareMemoryLocker = new object();
        public bool RemoveShareMemory(string key)
        {
            if (string.IsNullOrEmpty(key)
                || !userSettings.luaShareMemory.ContainsKey(key))
            {
                return false;
            }

            bool success;
            lock (shareMemoryLocker)
            {
                success = userSettings.luaShareMemory.Remove(key);
            }
            SaveUserSettingsLater();
            return success;
        }

        public List<string> ShareMemoryKeys()
        {
            lock (shareMemoryLocker)
            {
                return userSettings.luaShareMemory.Keys.ToList();
            }
        }

        public void SetLuaShareMemory(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            lock (shareMemoryLocker)
            {
                userSettings.luaShareMemory[key] = value;
            }
            SaveUserSettingsLater();
        }

        public List<Models.Data.LuaCoreSetting> GetLuaCoreSettings() =>
            userSettings.luaServers;

        public void SaveUserSettingsLater() =>
            lazyBookKeeper?.Deadline();
        #endregion

        #region protected methods
        protected override void Cleanup()
        {
            lazyBookKeeper?.Dispose();
            snpCache?.Dispose();
            SaveUserSettingsNow();
        }
        #endregion

        #region private methods
        void SaveUserSettingsNow() =>
            Apis.Misc.Utils.SavePluginSetting(
                pluginName, userSettings, vgcSetting);

        #endregion
    }
}
