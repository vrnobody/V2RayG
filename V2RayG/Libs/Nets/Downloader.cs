using System;
using System.IO;
using System.Net;
using V2RayG.Resources.Resx;

namespace V2RayG.Libs.Nets
{
    internal sealed class Downloader
    {
        public event EventHandler OnDownloadCompleted, OnDownloadCancelled, OnDownloadFail;
        public event EventHandler<Apis.Models.Datas.IntEvent> OnProgress;

        string _packageName;
        string _version = @"v5.0.3";
        string _source = Apis.Models.Consts.Core.GetSourceUrlByIndex(0);

        public int proxyPort { get; set; } = -1;
        WebClient webClient;

        Services.Settings setting;

        public Downloader(Services.Settings setting)
        {
            this.setting = setting;
            SetArchitecture(false);
            webClient = null;
        }

        #region public method
        public void SetSource(int index)
        {
            _source = Apis.Models.Consts.Core.GetSourceUrlByIndex(index);
        }

        public void SetArchitecture(bool win64 = false)
        {
            var arch = win64 ? "64" : "32";
            _packageName = $"v2ray-windows-{arch}.zip";
        }

        public void SetVersion(string version)
        {
            _version = version;
        }

        public string GetPackageName()
        {
            return _packageName;
        }

        public void DownloadV2RayCore()
        {
            // debug
            /*
            {
                setting.SendLog("Debug: assume download completed");
                DownloadCompleted(false);
                return;
            }
            */

            Download();
        }

        public bool UnzipPackage()
        {
            var path = GetLocalFolderPath();
            var filename = GetLocalFilename();
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(filename))
            {
                setting.SendLog(I18N.LocateTargetFolderFail);
                return false;
            }

            Apis.Misc.Utils.Sleep(1000);
            try
            {
                RemoveOldExe(path);
                Misc.Utils.ZipFileDecompress(filename, path);
                RemoveConfigJson(path);
            }
            catch (Exception ex)
            {
                setting.SendLog(I18N.DecompressFileFail + Environment.NewLine + ex.ToString());
                return false;
            }
            return true;
        }

        public void Cleanup()
        {
            Cancel();
            webClient?.Dispose();
        }

        public void Cancel()
        {
            webClient?.CancelAsync();
        }
        #endregion

        #region private method
        void RemoveConfigJson(string path)
        {
            var f = Path.Combine(path, "config.json");
            try
            {
                if (File.Exists(f))
                {
                    File.Delete(f);
                }
            }
            catch { }
        }

        void RemoveOldExe(string path)
        {
            string exe = Apis.Models.Consts.Core.V2RayCoreExeFileName;

            string prefix = "bak";

            var newFn = Path.Combine(path, $"{prefix}.{exe}");
            if (File.Exists(newFn))
            {
                try
                {
                    File.Delete(newFn);
                }
                catch
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd.HHmmss.ffff");
                    newFn = Path.Combine(path, $"{prefix}.{now}.{exe}");
                }
            }

            var orgFn = Path.Combine(path, exe);
            if (File.Exists(orgFn))
            {
                File.Move(orgFn, newFn);
            }
        }

        void SendProgress(int percentage)
        {
            try
            {
                OnProgress?.Invoke(this,
                    new Apis.Models.Datas.IntEvent(Math.Max(1, percentage)));
            }
            catch { }
        }

        void NotifyDownloadResults(bool status)
        {
            try
            {
                if (status)
                {
                    OnDownloadCompleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnDownloadFail?.Invoke(this, EventArgs.Empty);
                }
            }
            catch { }
        }

        void UpdateCore()
        {
            var servers = Services.Servers.Instance;

            // var pluginServ = Services.PluginsServer.Instance;
            // pluginServ.StopAllPlugins();

            Apis.Misc.Utils.Sleep(1000);

            var activeServerList = servers.GetRunningServers();
            servers.StopAllServersThen(() =>
            {
                var status = UnzipPackage();
                NotifyDownloadResults(status);

                // pluginServ.RestartAllPlugins();

                if (activeServerList.Count > 0)
                {
                    servers.RestartServersThen(activeServerList);
                }
            });
        }

        void DownloadCompleted(bool cancelled)
        {
            webClient?.Dispose();
            webClient = null;

            if (cancelled)
            {
                try
                {
                    OnDownloadCancelled?.Invoke(this, EventArgs.Empty);
                }
                catch { }
                return;
            }

            setting.SendLog(string.Format("{0}", I18N.DownloadCompleted));
            UpdateCore();
        }

        string GetLocalFolderPath()
        {
            var path = setting.isPortable ?
                Apis.Misc.Utils.GetCoreFolderFullPath() :
                Misc.Utils.GetSysAppDataFolder();

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    Apis.Misc.UI.MsgBoxAsync(I18N.CreateFolderFail);
                    return null;
                }
            }
            return path;
        }

        string GetLocalFilename()
        {
            var path = GetLocalFolderPath();
            return string.IsNullOrEmpty(path) ? null : Path.Combine(path, _packageName);
        }

        string GenReleaseUrl()
        {
            // tail =  "/releases/download/{0}/{1}";
            string tpl = _source + @"/download/{0}/{1}";
            return string.Format(tpl, _version, _packageName);
        }

        void Download()
        {
            string url = GenReleaseUrl();

            var filename = GetLocalFilename();
            if (string.IsNullOrEmpty(filename))
            {
                return;
            }

            webClient = new WebClient();
            webClient.Headers.Add(Apis.Models.Consts.Webs.UserAgent);

            if (proxyPort > 0)
            {
                webClient.Proxy = new WebProxy(
                    Apis.Models.Consts.Webs.LoopBackIP, proxyPort);
            }

            webClient.DownloadProgressChanged += (s, a) =>
            {
                SendProgress(a.ProgressPercentage);
            };

            webClient.DownloadFileCompleted += (s, a) =>
            {
                DownloadCompleted(a.Cancelled);
            };

            setting.SendLog(string.Format("{0}:{1}", I18N.Download, url));
            webClient.DownloadFileAsync(new Uri(url), filename);
        }

        #endregion
    }
}
