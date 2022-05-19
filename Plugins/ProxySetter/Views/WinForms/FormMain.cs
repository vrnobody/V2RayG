﻿using ProxySetter.Resources.Langs;
using System;
using System.Windows.Forms;

namespace ProxySetter.Views.WinForms
{
    partial class FormMain : Form
    {
        Services.PsSettings setting;
        Services.PacServer pacServer;
        Services.ServerTracker servTracker;

        Controllers.FormVGCPluginCtrl formVGCPluginCtrl;
        Timer updateSysProxyInfoTimer = null;

        public static FormMain CreateForm(
            Services.PsSettings setting,
            Services.PacServer pacServer,
            Services.ServerTracker servTracker)
        {
            FormMain r = null;
            Apis.Misc.UI.Invoke(() =>
            {
                r = new FormMain(setting, pacServer, servTracker);
            });
            return r;
        }

        FormMain(
            Services.PsSettings setting,
            Services.PacServer pacServer,
            Services.ServerTracker servTracker)
        {
            this.setting = setting;
            this.pacServer = pacServer;
            this.servTracker = servTracker;

            this.FormClosing += (s, a) =>
            {
                var confirm = true;
                if (!setting.IsClosing() && !this.formVGCPluginCtrl.IsOptionsSaved())
                {
                    confirm = Apis.Misc.UI.Confirm(I18N.ConfirmCloseWinWithoutSave);
                }

                if (confirm)
                {
                    ReleaseUpdateTimer();
                    formVGCPluginCtrl.Cleanup();
                }
                else
                {
                    a.Cancel = true;
                }
            };

            InitializeComponent();
        }

        private void FormPluginMain_Shown(object sender, System.EventArgs e)
        {
            Apis.Misc.UI.AutoSetFormIcon(this);

            this.Text = string.Format(
                "{0} v{1}",
                Properties.Resources.Name,
                Properties.Resources.Version);

            formVGCPluginCtrl = CreateFormCtrl();

            UpdateSysProxyInfo(null, EventArgs.Empty);
            StartUpdateTimer();
        }

        #region private method
        void UpdateSysProxyInfo(object sender, EventArgs args)
        {
            var proxySetting = Libs.Sys.ProxySetter.GetProxySetting();
            string proxyUrl = "Direct";

            switch (proxySetting.proxyMode)
            {
                case (int)Libs.Sys.WinInet.ProxyModes.PAC:
                    proxyUrl = proxySetting.pacUrl;
                    break;
                case (int)Libs.Sys.WinInet.ProxyModes.Proxy:
                    proxyUrl = "http://" + proxySetting.proxyAddr;
                    break;
            }

            if (lbBasicProxyLink.Text != proxyUrl)
            {
                lbBasicProxyLink.Text = proxyUrl;
            }
        }

        void ReleaseUpdateTimer()
        {
            if (updateSysProxyInfoTimer != null)
            {
                updateSysProxyInfoTimer.Stop();
                updateSysProxyInfoTimer.Tick -= UpdateSysProxyInfo;
                updateSysProxyInfoTimer.Dispose();
            }
        }

        void StartUpdateTimer()
        {
            updateSysProxyInfoTimer = new Timer
            {
                Interval = 2000,
            };
            updateSysProxyInfoTimer.Tick += UpdateSysProxyInfo;
            updateSysProxyInfoTimer.Start();
        }

        Controllers.FormVGCPluginCtrl CreateFormCtrl()
        {
            var ctrl = new Controllers.FormVGCPluginCtrl();

            ctrl.Plug(new Controllers.VGCPluginComponents.TabUsage(
                linkLabelUsageTxthinkingPac,
                tboxUsageReadMe));

            ctrl.Plug(new Controllers.VGCPluginComponents.TabStatus(
                pacServer,

                lbBasicCurPacServerStatus,
                lbBasicProxyLink,
                btnBasicStartPacServer,
                btnBasicStopPacServer,
                btnBasicViewInNotepad,
                btnBasicDebugPacServer,
                btnBaiscCopyProxyLink));

            ctrl.Plug(new Controllers.VGCPluginComponents.TabBasicSetting(
                setting,
                servTracker,

                cboxBasicPacProtocol,
                cboxBasicSysProxyMode,
                tboxBasicGlobalPort,
                tboxBaiscPacPort,
                cboxBasicPacMode,
                tboxBasicCustomPacPath,
                chkBasicAutoUpdateSysProxy,
                chkBasicPacAlwaysOn,
                chkBasicUseCustomPac,
                btnBasicBrowseCustomPac,

                // hotkey
                chkBasicUseHotkey,
                chkBasicUseAlt,
                chkBasicUseShift,
                tboxBasicHotkey));

            ctrl.Plug(new Controllers.VGCPluginComponents.TabPacCustomList(
                setting,
                rtboxPacWhiteList,
                rtboxPacBlackList,

                btnSetSortWhitelist,
                btnSetSortBlacklist));

            return ctrl;
        }
        #endregion

        #region UI event handler
        private void btnSave_Click(object sender, System.EventArgs e)
        {
            var changed = formVGCPluginCtrl.SaveAllOptions();
            if (changed)
            {
                servTracker.Restart();
            }
            MessageBox.Show(I18N.Done);
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        private void chkBasicUseCustomPac_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = chkBasicUseCustomPac.Checked;
            chkBasicAutoUpdateSysProxy.Enabled = !isChecked;
            tboxBasicGlobalPort.Enabled = !isChecked;
            cboxBasicPacMode.Enabled = !isChecked;
            cboxBasicPacProtocol.Enabled = !isChecked;
            btnBasicDebugPacServer.Enabled = !isChecked;
            rtboxPacWhiteList.Enabled = !isChecked;
            rtboxPacBlackList.Enabled = !isChecked;
            btnBasicViewInNotepad.Enabled = !isChecked;
        }
        #endregion
    }
}
