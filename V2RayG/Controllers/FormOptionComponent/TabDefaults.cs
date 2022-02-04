using System;
using System.Windows.Forms;

namespace V2RayG.Controllers.OptionComponent
{
    class TabDefaults : OptionComponentController
    {
        Services.Settings setting;

        ComboBox cboxDefImportMode = null,
            cboxDefSpeedtestUrl = null,
            cboxDefSpeedtestExpectedSize = null;

        CheckBox chkSetSpeedtestIsUse = null,
            chkImportSsShareLink = null,
            chkImportTrojanShareLink = null,
            chkImportInjectGlobalImport = null;

        TextBox tboxDefImportAddr = null,
            tboxSetSpeedtestCycles = null,
            tboxSetSpeedtestTimeout = null;

        RichTextBox exRTBoxDefCustomInbounds = null;

        public TabDefaults(
            ComboBox cboxDefImportMode,
            TextBox tboxDefImportAddr,

            CheckBox chkImportSsShareLink,
            CheckBox chkImportTrojanShareLink,
            CheckBox chkImportInjectGlobalImport,

            CheckBox chkSetSpeedtestIsUse,
            ComboBox cboxDefSpeedtestUrl,
            TextBox tboxSetSpeedtestCycles,
            ComboBox cboxDefSpeedtestExpectedSize,
            TextBox tboxSetSpeedtestTimeout,

            RichTextBox exRTBoxDefCustomInbounds)
        {
            this.setting = Services.Settings.Instance;

            this.exRTBoxDefCustomInbounds = exRTBoxDefCustomInbounds;

            // Do not put these lines of code into InitElement.
            this.cboxDefImportMode = cboxDefImportMode;
            this.tboxDefImportAddr = tboxDefImportAddr;
            this.chkImportSsShareLink = chkImportSsShareLink;
            this.chkImportTrojanShareLink = chkImportTrojanShareLink;
            this.chkImportInjectGlobalImport = chkImportInjectGlobalImport;
            this.chkSetSpeedtestIsUse = chkSetSpeedtestIsUse;
            this.cboxDefSpeedtestUrl = cboxDefSpeedtestUrl;
            this.tboxSetSpeedtestCycles = tboxSetSpeedtestCycles;
            this.cboxDefSpeedtestExpectedSize = cboxDefSpeedtestExpectedSize;
            this.tboxSetSpeedtestTimeout = tboxSetSpeedtestTimeout;

            InitElement();
        }

        private void InitElement()
        {
            exRTBoxDefCustomInbounds.Text = setting.CustomDefInbounds;

            // mode
            chkImportInjectGlobalImport.Checked = setting.CustomDefImportGlobalImport;
            chkImportSsShareLink.Checked = setting.CustomDefImportSsShareLink;
            chkImportTrojanShareLink.Checked = setting.CustomDefImportTrojanShareLink;

            cboxDefImportMode.SelectedIndex = setting.CustomDefImportMode;
            tboxDefImportAddr.TextChanged += OnTboxImportAddrTextChanged;
            tboxDefImportAddr.Text = string.Format(
                @"{0}:{1}",
                setting.CustomDefImportIp,
                setting.CustomDefImportPort);

            // speedtest
            chkSetSpeedtestIsUse.Checked = setting.isUseCustomSpeedtestSettings;
            tboxSetSpeedtestCycles.Text = setting.CustomSpeedtestCycles.ToString();
            cboxDefSpeedtestUrl.Text = setting.CustomSpeedtestUrl;
            cboxDefSpeedtestExpectedSize.Text = setting.CustomSpeedtestExpectedSizeInKib.ToString();
            tboxSetSpeedtestTimeout.Text = setting.CustomSpeedtestTimeout.ToString();
        }

        #region public method
        public override bool SaveOptions()
        {
            if (!IsOptionsChanged())
            {
                return false;
            }

            setting.CustomDefInbounds = exRTBoxDefCustomInbounds.Text;

            // mode
            if (Apis.Misc.Utils.TryParseAddress(tboxDefImportAddr.Text, out string ip, out int port))
            {
                setting.CustomDefImportIp = ip;
                setting.CustomDefImportPort = port;
            }
            setting.CustomDefImportMode = cboxDefImportMode.SelectedIndex;

            setting.CustomDefImportSsShareLink = chkImportSsShareLink.Checked;
            setting.CustomDefImportTrojanShareLink = chkImportTrojanShareLink.Checked;
            setting.CustomDefImportGlobalImport = chkImportInjectGlobalImport.Checked;

            // speedtest
            setting.isUseCustomSpeedtestSettings = chkSetSpeedtestIsUse.Checked;
            setting.CustomSpeedtestUrl = cboxDefSpeedtestUrl.Text;
            setting.CustomSpeedtestCycles = Apis.Misc.Utils.Str2Int(tboxSetSpeedtestCycles.Text);
            setting.CustomSpeedtestExpectedSizeInKib = Apis.Misc.Utils.Str2Int(cboxDefSpeedtestExpectedSize.Text);
            setting.CustomSpeedtestTimeout = Apis.Misc.Utils.Str2Int(tboxSetSpeedtestTimeout.Text);

            setting.SaveUserSettingsNow();
            return true;
        }

        public override bool IsOptionsChanged()
        {
            var success = Apis.Misc.Utils.TryParseAddress(tboxDefImportAddr.Text, out string ip, out int port);
            if (!success
                || setting.CustomDefInbounds != exRTBoxDefCustomInbounds.Text
                || setting.CustomDefImportGlobalImport != chkImportInjectGlobalImport.Checked
                || setting.CustomDefImportSsShareLink != chkImportSsShareLink.Checked
                || setting.CustomDefImportTrojanShareLink != chkImportTrojanShareLink.Checked
                || setting.CustomDefImportIp != ip
                || setting.CustomDefImportPort != port
                || setting.CustomDefImportMode != cboxDefImportMode.SelectedIndex

                || setting.isUseCustomSpeedtestSettings != chkSetSpeedtestIsUse.Checked
                || setting.CustomSpeedtestUrl != cboxDefSpeedtestUrl.Text
                || setting.CustomSpeedtestExpectedSizeInKib != Apis.Misc.Utils.Str2Int(cboxDefSpeedtestExpectedSize.Text)
                || setting.CustomSpeedtestCycles != Apis.Misc.Utils.Str2Int(tboxSetSpeedtestCycles.Text)
                || setting.CustomSpeedtestTimeout != Apis.Misc.Utils.Str2Int(tboxSetSpeedtestTimeout.Text))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region private method
        void OnTboxImportAddrTextChanged(object sender, EventArgs e) =>
            Apis.Misc.UI.MarkInvalidAddressWithColorRed(tboxDefImportAddr);
        #endregion
    }
}
