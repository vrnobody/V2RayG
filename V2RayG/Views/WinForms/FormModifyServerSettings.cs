using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using V2RayG.Resources.Resx;
using Apis.Interfaces;

namespace V2RayG.Views.WinForms
{
    public partial class FormModifyServerSettings : Form
    {
        #region Sigleton
        static FormModifyServerSettings _instant;
        static readonly object formInstanLocker = new object();
        public static void ShowForm(ICoreServCtrl coreServ)
        {
            FormModifyServerSettings form = null;

            if (_instant == null || _instant.IsDisposed)
            {
                Apis.Misc.UI.Invoke(() =>
                {
                    form = new FormModifyServerSettings();
                });
            }

            lock (formInstanLocker)
            {
                if (_instant == null || _instant.IsDisposed)
                {
                    _instant = form;
                    form.FormClosed += (s, a) => _instant = null;
                    form = null;
                }
            }

            Apis.Misc.UI.Invoke(() =>
            {
                form?.Close();
                var inst = _instant;
                if (inst != null)
                {
                    inst.InitControls(coreServ);
                    inst.Show();
                    inst.Activate();
                }
            });

        }
        #endregion

        private ICoreServCtrl coreServ;
        Apis.Models.Datas.CoreServSettings orgCoreServSettings;
        Services.Servers servers;

        public FormModifyServerSettings()
        {
            InitializeComponent();
            Apis.Misc.UI.AutoSetFormIcon(this);
            servers = Services.Servers.Instance;
        }

        private void FormModifyServerSettings_Load(object sender, System.EventArgs e)
        {
            cboxZoomMode.SelectedIndex = 0;
        }

        void InitControls(ICoreServCtrl coreServ)
        {
            this.coreServ = coreServ;
            orgCoreServSettings = new Apis.Models.Datas.CoreServSettings(coreServ);
            var marks = servers.GetMarkList();
            lbServerTitle.Text = coreServ.GetCoreStates().GetTitle();
            cboxMark.Items.Clear();
            cboxMark.Items.AddRange(marks);
            Misc.UI.ResetComboBoxDropdownMenuWidth(cboxMark);
            UpdateControls(orgCoreServSettings);
            AutoSelectShareLinkType();
            UpdateShareLink();
        }

        #region private methods
        void AutoSelectShareLinkType()
        {
            var slinkMgr = Services.ShareLinkMgr.Instance;
            var config = coreServ.GetConfiger().GetConfig();
            var ts = new List<Apis.Models.Datas.Enums.LinkTypes> {
                Apis.Models.Datas.Enums.LinkTypes.ss,
                Apis.Models.Datas.Enums.LinkTypes.vmess,
                Apis.Models.Datas.Enums.LinkTypes.vless,
            };

            for (int i = 0; i < ts.Count; i++)
            {
                if (!string.IsNullOrEmpty(slinkMgr.EncodeConfigToShareLink(config, ts[i])))
                {
                    cboxShareLinkType.SelectedIndex = ts.Count - i - 1;
                    return;
                }
            }
            cboxShareLinkType.SelectedIndex = 0;
        }

        Apis.Models.Datas.CoreServSettings GetterSettings()
        {
            var result = new Apis.Models.Datas.CoreServSettings();
            result.index = Apis.Misc.Utils.Str2Int(tboxServIndex.Text);
            result.serverName = tboxServerName.Text;
            result.serverDescription = tboxDescription.Text;
            result.inboundMode = cboxInboundMode.SelectedIndex;
            result.inboundAddress = cboxInboundAddress.Text;
            result.mark = cboxMark.Text;
            result.remark = tboxRemark.Text;
            result.isAutorun = chkAutoRun.Checked;
            result.isGlobalImport = chkGlobalImport.Checked;
            result.isUntrack = chkUntrack.Checked;
            return result;
        }

        void UpdateControls(Apis.Models.Datas.CoreServSettings coreServSettings)
        {
            var s = coreServSettings;
            tboxServIndex.Text = s.index.ToString();
            tboxServerName.Text = s.serverName;
            tboxDescription.Text = s.serverDescription;
            cboxInboundMode.SelectedIndex = s.inboundMode;
            cboxInboundAddress.Text = s.inboundAddress;
            cboxMark.Text = s.mark;
            tboxRemark.Text = s.remark;
            chkAutoRun.Checked = s.isAutorun;
            chkGlobalImport.Checked = s.isGlobalImport;
            chkUntrack.Checked = s.isUntrack;
        }

        void UpdateShareLink()
        {
            var slinkMgr = Services.ShareLinkMgr.Instance;
            var config = coreServ.GetConfiger().GetConfig();
            var ty = Apis.Models.Datas.Enums.LinkTypes.ss;
            switch (cboxShareLinkType.Text.ToLower())
            {
                case "vmess":
                    ty = Apis.Models.Datas.Enums.LinkTypes.vmess;
                    break;
                case "vless":
                    ty = Apis.Models.Datas.Enums.LinkTypes.vless;
                    break;
                default:
                    break;
            }
            var link = slinkMgr.EncodeConfigToShareLink(config, ty);
            tboxShareLink.Text = link;
        }

        void SetQRCodeImage(Image img)
        {
            var oldImage = pboxQrcode.Image;

            pboxQrcode.Image = img;

            if (oldImage != img)
            {
                oldImage?.Dispose();
            }
        }

        #endregion

        #region UI events
        private void cboxInboundAddress_TextChanged(object sender, System.EventArgs e)
        {
            Apis.Misc.UI.MarkInvalidAddressWithColorRed(cboxInboundAddress);
        }

        private void cboxInboundMode_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var idx = cboxInboundMode.SelectedIndex;
            cboxInboundAddress.Enabled = idx == 1 || idx == 2;
        }

        private void tboxShareLink_TextChanged(object sender, System.EventArgs e)
        {
            var text = tboxShareLink.Text;
            if (string.IsNullOrEmpty(text))
            {
                SetQRCodeImage(null);
                return;
            }

            Tuple<Bitmap, Libs.QRCode.QRCode.WriteErrors> r =
               Libs.QRCode.QRCode.GenQRCode(text, 320);

            switch (r.Item2)
            {
                case Libs.QRCode.QRCode.WriteErrors.Success:
                    SetQRCodeImage(r.Item1);
                    break;
                case Libs.QRCode.QRCode.WriteErrors.DataEmpty:
                    SetQRCodeImage(null);
                    MessageBox.Show(I18N.EmptyLink);
                    break;
                case Libs.QRCode.QRCode.WriteErrors.DataTooBig:
                    SetQRCodeImage(null);
                    MessageBox.Show(I18N.DataTooBig);
                    break;
            }

        }

        private void cboxShareLinkType_SelectedValueChanged(object sender, System.EventArgs e)
        {
            UpdateShareLink();
        }

        private void cboxZoomMode_SelectedValueChanged(object sender, System.EventArgs e)
        {
            pboxQrcode.SizeMode = cboxZoomMode.Text.ToLower() == "none" ?
                PictureBoxSizeMode.CenterImage :
                PictureBoxSizeMode.Zoom;
        }

        private void btnCopyShareLink_Click(object sender, EventArgs e)
        {
            Misc.Utils.CopyToClipboardAndPrompt(tboxShareLink.Text);
        }

        private void btnSaveQrcode_Click(object sender, EventArgs e)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = Apis.Models.Consts.Files.PngExt,
                FilterIndex = 1,
                RestoreDirectory = true,
            };

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    pboxQrcode.Image.Save(myStream, System.Drawing.Imaging.ImageFormat.Png);
                    myStream.Close();
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var curSettings = GetterSettings();
            if (!curSettings.Equals(orgCoreServSettings))
            {
                coreServ.UpdateCoreSettings(curSettings);
                servers.UpdateMarkList();
            }
            Close();
        }

        #endregion
    }
}
