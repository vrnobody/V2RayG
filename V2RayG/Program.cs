using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using V2RayG.Resources.Resx;


namespace V2RayG
{
    static class Program
    {
        #region single instance
        // https://stackoverflow.com/questions/19147/what-is-the-correct-way-to-create-a-single-instance-application

        static Mutex mutex = new Mutex(
            true,
#if DEBUG
            "{d979a84c-12cb-433c-bd54-bbf7fc77af37}"
#else
            "{16556140-e029-4135-8ace-2823081720e9}"
#endif
            );
        #endregion

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.Name = Apis.Models.Consts.Libs.UiThreadName;

            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                var app = new Services.Launcher();
                if (app.Warmup())
                {
                    app.Run();
                    Application.Run(app.context);
                    app.Dispose();
                }
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show(I18N.ExitOtherVGCFirst);
            }
        }

        #region DPI awareness
        [DllImport("user32.dll")]
        public extern static IntPtr SetProcessDPIAware();
        #endregion
    }
}
