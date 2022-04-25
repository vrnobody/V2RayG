﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace V2RayG.Views.WinForms
{
    public partial class FormBatchModifyServerSetting : Form
    {
        #region Sigleton
        static FormBatchModifyServerSetting _instant;
        public static FormBatchModifyServerSetting GetForm()
        {
            Apis.Misc.UI.Invoke(() =>
            {
                if (_instant == null || _instant.IsDisposed)
                {
                    _instant = new FormBatchModifyServerSetting();
                    _instant.FormClosed += (s, a) => _instant = null;
                    _instant.Show();
                }
                else
                {
                    _instant.Activate();
                }
            });

            return _instant;
        }
        #endregion

        Services.Servers servers;

        FormBatchModifyServerSetting()
        {
            servers = Services.Servers.Instance;

            InitializeComponent();

            Apis.Misc.UI.AutoSetFormIcon(this);
        }

        private void FormBatchModifyServerInfo_Shown(object sender, EventArgs e)
        {
            this.cboxMark.Items.Clear();
            cboxMark.Items.AddRange(servers.GetMarkList());

            var firstCtrl = servers.GetSelectedServers(false).FirstOrDefault();
            if (firstCtrl == null)
            {
                return;
            }

            var first = firstCtrl.GetCoreStates().GetAllRawCoreInfo();


            this.cboxInMode.SelectedIndex = first.customInbType;
            this.tboxInIP.Text = first.inbIp;
            this.tboxInPort.Text = first.inbPort.ToString();
            this.cboxMark.Text = first.customMark;
            this.tboxRemark.Text = first.customRemark;
            this.cboxAutorun.SelectedIndex = first.isAutoRun ? 0 : 1;
            this.cboxImport.SelectedIndex = first.isInjectImport ? 0 : 1;
        }

        #region UI event
        private void chkShareOverLAN_CheckedChanged(object sender, EventArgs e)
        {
            var isChecked = chkShareOverLAN.Checked;
            if (isChecked)
            {
                tboxInIP.Text = "0.0.0.0";
            }
            else
            {
                tboxInIP.Text = Apis.Models.Consts.Webs.LoopBackIP;
            }
            tboxInIP.Enabled = !isChecked;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            var list = servers.GetSelectedServers(false);

            var newMode = chkInMode.Checked ? cboxInMode.SelectedIndex : -1;
            var newIP = chkInIP.Checked ? tboxInIP.Text : null;
            var newPort = chkInPort.Checked ? Apis.Misc.Utils.Str2Int(tboxInPort.Text) : -1;
            var newMark = chkMark.Checked ? cboxMark.Text : null;
            var newAutorun = chkAutorun.Checked ? cboxAutorun.SelectedIndex : -1;
            var newImport = chkImport.Checked ? cboxImport.SelectedIndex : -1;
            var isPortAutoIncrease = chkIncrement.Checked;
            var newRemark = chkRemark.Checked ? tboxRemark.Text : null;


            ModifyServersSetting(
                list,
                newMode, newIP, newPort, isPortAutoIncrease,
                newMark, newRemark, newAutorun, newImport);
        }

        #endregion

        #region private method
        void ModifyServersSetting(
            List<Apis.Interfaces.ICoreServCtrl> list,
            int newMode, string newIP, int newPort, bool isPortAutoIncrease,
            string newMark, string newRemark, int newAutorun, int newImport)
        {
            Action<int, Action> worker = (index, next) =>
            {
                var portNumber = isPortAutoIncrease ? newPort + index : newPort;

                var server = list[index];
                if (!server.GetCoreCtrl().IsCoreRunning())
                {
                    ModifyServerSetting(
                        ref server,
                        newMode, newIP, portNumber,
                        newMark, newRemark, newAutorun, newImport);
                    server.InvokeEventOnPropertyChange();
                    next();
                    return;
                }

                server.GetCoreCtrl().StopCoreThen(() =>
                {
                    ModifyServerSetting(
                        ref server,
                        newMode, newIP, portNumber,
                        newMark, newRemark, newAutorun, newImport);
                    server.GetCoreCtrl().RestartCoreThen();
                    next();
                });
            };

            var that = this;
            Action done = () =>
            {
                servers.UpdateMarkList();
                Apis.Misc.UI.Invoke(() => that.Close());
            };

            Misc.Utils.ChainActionHelperAsync(list.Count, worker, done);

        }

        void ModifyServerSetting(
            ref Apis.Interfaces.ICoreServCtrl serverCtrl,
            int newMode, string newIP, int newPort,
            string newMark, string newRemark, int newAutorun, int newImport)
        {
            var server = serverCtrl.GetCoreStates().GetAllRawCoreInfo();

            if (newAutorun >= 0)
            {
                server.isAutoRun = newAutorun == 0;
            }

            if (newImport >= 0)
            {
                server.isInjectImport = newImport == 0;
            }

            if (newMode >= 0)
            {
                server.customInbType = newMode;
            }

            if (newIP != null)
            {
                server.inbIp = newIP;
            }
            if (newPort >= 0)
            {
                server.inbPort = newPort;
            }

            if (newMark != null)
            {
                server.customMark = newMark;
            }

            if (!string.IsNullOrEmpty(newRemark))
            {
                server.customRemark = newRemark;
            }
        }

        #endregion
    }
}
