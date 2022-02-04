using Pacman.Resources.Langs;
using System.Collections.Generic;
using System.Linq;

namespace Pacman.Services
{
    public class Settings
    {
        Apis.Interfaces.Services.ISettingsService vgcSetting;
        Apis.Interfaces.Services.IServersService vgcServers;
        readonly string pluginName = Properties.Resources.Name;
        Models.Data.UserSettings userSettings;

        public Settings() { }

        public void Run(Apis.Interfaces.Services.IApiService vgcApi)
        {
            vgcServers = vgcApi.GetServersService();
            vgcSetting = vgcApi.GetSettingService();
            userSettings = LoadUserSettings();
        }

        #region properties

        #endregion

        #region public methods
        public string Chain(
            List<Apis.Interfaces.ICoreServCtrl> servList, string orgServerUid, string packageName)
        {
            return vgcServers.PackServersV5Ui(
                servList,
                orgServerUid,
                packageName,
                string.Empty,
                string.Empty,
                Apis.Models.Datas.Enums.BalancerStrategies.Random,
                Apis.Models.Datas.Enums.PackageTypes.Chain);
        }

        public string Pack(
            List<Apis.Interfaces.ICoreServCtrl> servList,
            string orgServerUid,
            string packageName,
            string interval,
            string url,
            Apis.Models.Datas.Enums.BalancerStrategies strategy)
        {
            return vgcServers.PackServersV5Ui(
                servList,
                orgServerUid,
                packageName,
                interval,
                url,
                strategy, Apis.Models.Datas.Enums.PackageTypes.Balancer);
        }

        public List<Apis.Interfaces.ICoreServCtrl> GetAllServersList() =>
            vgcServers.GetAllServersOrderByIndex();

        public List<Models.Data.Package> GetPackageList()
        {
            return userSettings.packages;
        }

        public Models.Data.Package GetPackageByIndex(int index)
        {
            var max = userSettings.packages.Count;
            if (max <= 0)
            {
                return new Models.Data.Package();
            }

            index = Apis.Misc.Utils.Clamp(index, 0, max);
            return userSettings.packages[index];
        }

        public void RemovePackageByName(string name)
        {
            var num = userSettings.packages.RemoveAll(p => p.name == name);
            SaveUserSettings();
            if (num <= 0)
            {
                Libs.UI.MsgBox(I18N.Fail);
            }
        }

        public bool SavePackage(Models.Data.Package package)
        {
            if (package == null)
            {
                return false;
            }

            var p = userSettings.packages.FirstOrDefault(s => s.name == package.name);
            if (p == null)
            {
                userSettings.packages.Add(package);
            }
            else
            {
                if (!string.IsNullOrEmpty(package.uid))
                {
                    p.uid = package.uid;
                }
                p.strategy = package.strategy;
                p.interval = package.interval;
                p.url = package.url;
                p.beans = package.beans;
            }

            SaveUserSettings();
            return true;
        }

        public void SaveUserSettings()
        {
            try
            {
                var content = Apis.Misc.Utils.SerializeObject(userSettings);
                vgcSetting.SavePluginsSetting(pluginName, content);
            }
            catch { }
        }

        public void Cleanup()
        {

        }
        #endregion

        #region private methods
        Models.Data.UserSettings LoadUserSettings()
        {
            var empty = new Models.Data.UserSettings();
            var userSettingString = vgcSetting.GetPluginsSetting(pluginName);
            if (string.IsNullOrEmpty(userSettingString))
            {
                return empty;
            }

            try
            {
                var result = Apis.Misc.Utils
                    .DeserializeObject<Models.Data.UserSettings>(
                        userSettingString);
                return result ?? empty;
            }
            catch { }

            return empty;
        }
        #endregion
    }
}
