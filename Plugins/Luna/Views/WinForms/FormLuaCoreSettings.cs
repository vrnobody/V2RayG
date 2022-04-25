﻿using Luna.Resources.Langs;
using System.Windows.Forms;

namespace Luna.Views.WinForms
{
    internal partial class FormLuaCoreSettings : Form
    {
        #region Sigleton
        static FormLuaCoreSettings _instant;
        static readonly object formInstanLocker = new object();
        public static void ShowForm(Controllers.LuaCoreCtrl luaCoreCtrl)
        {
            FormLuaCoreSettings f = null;

            if (_instant == null || _instant.IsDisposed)
            {
                Apis.Misc.UI.Invoke(() =>
                {
                    f = new FormLuaCoreSettings();
                });
            }

            lock (formInstanLocker)
            {
                if (_instant == null || _instant.IsDisposed)
                {
                    _instant = f;
                    f.FormClosed += (s, a) => _instant = null;
                    f = null;
                }
            }

            Apis.Misc.UI.Invoke(() =>
            {
                f?.Close();
                var inst = _instant;
                if (inst != null)
                {
                    inst.InitControls(luaCoreCtrl);
                    inst.Show();
                    inst.Activate();
                }
            });
        }
        #endregion

        Controllers.LuaCoreCtrl luaCoreCtrl = null;

        FormLuaCoreSettings()
        {
            InitializeComponent();
            Apis.Misc.UI.AutoSetFormIcon(this);
        }

        #region public methods

        #endregion

        #region private methods
        void InitControls(Controllers.LuaCoreCtrl luaCoreCtrl)
        {
            this.luaCoreCtrl = luaCoreCtrl;
            UpdateUi();
        }

        void UpdateUi()
        {
            Apis.Misc.UI.Invoke(() =>
            {
                var ctrl = this.luaCoreCtrl;
                if (ctrl == null)
                {
                    Apis.Misc.UI.MsgBox(I18N.ScriptNotFound);
                    return;
                }

                tboxName.Text = ctrl.name;
                chkAutorun.Checked = ctrl.isAutoRun;
                chkHidden.Checked = ctrl.isHidden;
                chkClrSupports.Checked = ctrl.isLoadClr;
            });
        }
        #endregion

        #region UI events
        private void btnSave_Click(object sender, System.EventArgs e)
        {
            var ctrl = luaCoreCtrl;
            if (ctrl == null)
            {
                Apis.Misc.UI.MsgBox(I18N.ScriptNotFound);
                return;
            }
            ctrl.name = tboxName.Text;
            ctrl.isAutoRun = chkAutorun.Checked;
            ctrl.isHidden = chkHidden.Checked;
            ctrl.isLoadClr = chkClrSupports.Checked;
            Close();
        }
        #endregion

    }
}
