﻿using System;
using System.Windows.Forms;
using V2RayG.Resources.Resx;

namespace V2RayG.Views.WinForms
{
    public partial class FormSingleServerLog : Form
    {
        public static FormSingleServerLog CreateLogForm(string title, Apis.Libs.Sys.QueueLogger logger)
        {
            FormSingleServerLog logForm = null;
            Apis.Misc.UI.Invoke(() =>
            {
                logForm = new FormSingleServerLog(title, logger);
                logForm.Show();
            });
            return logForm;
        }

        long updateTimestamp = -1;
        Apis.Libs.Tasks.Routine logUpdater;
        Apis.Libs.Sys.QueueLogger qLogger;

        bool isPaused = false;

        FormSingleServerLog(
            string title,
            Apis.Libs.Sys.QueueLogger logger)
        {
            this.qLogger = logger;
            logUpdater = new Apis.Libs.Tasks.Routine(
                RefreshUi,
                Apis.Models.Consts.Intervals.SiFormLogRefreshInterval);

            InitializeComponent();
            Apis.Misc.UI.AutoSetFormIcon(this);
            this.Text = I18N.Log + " - " + title;
        }

        private void RefreshUi()
        {
            var timestamp = qLogger.GetTimestamp();
            if (updateTimestamp == timestamp)
            {
                return;
            }

            updateTimestamp = timestamp;
            var logs = qLogger.GetLogAsString(true);
            Apis.Misc.UI.UpdateRichTextBox(rtBoxLogger, logs);
        }

        private void FormSingleServerLog_Load(object sender, EventArgs e)
        {
            logUpdater.Run();
        }

        private void FormSingleServerLog_FormClosed(object sender, FormClosedEventArgs e)
        {
            logUpdater.Dispose();

            // Potential memory leaks
            // qLogger.Dispose();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Apis.Misc.UI.Confirm(I18N.ConfirmClearLog))
            {
                qLogger.Reset();
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isPaused)
            {
                return;
            }
            isPaused = true;
            pauseToolStripMenuItem.Checked = isPaused;
            logUpdater.Pause();
        }

        private void resumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
            pauseToolStripMenuItem.Checked = isPaused;
            logUpdater.Run();
        }
    }
}
