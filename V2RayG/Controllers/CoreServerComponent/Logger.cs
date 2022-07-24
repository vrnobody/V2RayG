namespace V2RayG.Controllers.CoreServerComponent
{
    sealed public class Logger :
        Apis.BaseClasses.ComponentOf<CoreServerCtrl>,
        Apis.Interfaces.CoreCtrlComponents.ILogger
    {
        Apis.Libs.Sys.QueueLogger qLogger = new Apis.Libs.Sys.QueueLogger();
        Services.Settings setting;

        public Logger(Services.Settings setting)
        {
            this.setting = setting;
        }

        #region public methods
        public void Log(string message)
        {
            qLogger.Log(message);
            try
            {
                setting.SendLog($"[{coreInfo.GetIndex()}.{coreInfo.GetShortName()}] {message}");
            }
            catch { }
        }

        CoreStates coreInfo;
        public override void Prepare()
        {
            coreInfo = GetParent().GetChild<CoreStates>();
        }

        Views.WinForms.FormSingleServerLog logForm = null;
        readonly object formLogLocker = new object();
        public void ShowFormLog()
        {
            Views.WinForms.FormSingleServerLog form = null;

            if (logForm == null)
            {
                var title = coreInfo.GetTitle();
                Apis.Misc.UI.Invoke(() =>
                {
                    form = Views.WinForms.FormSingleServerLog.CreateLogForm(title, qLogger);
                });
            }

            lock (formLogLocker)
            {
                if (logForm == null)
                {
                    logForm = form;
                    form.FormClosed += (s, a) => logForm = null;
                    form = null;
                }
            }

            Apis.Misc.UI.Invoke(() =>
            {
                form?.Close();
                logForm?.Activate();
            });
        }
        #endregion

        #region private methods

        #endregion

        #region protected methods
        protected override void CleanupAfterChildrenDisposed()
        {
            Apis.Misc.UI.CloseFormIgnoreError(logForm);
            qLogger.Dispose();
        }
        #endregion
    }
}
