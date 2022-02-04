using Luna.Models.Data;
using Luna.Resources.Langs;
using System.Windows.Forms;

namespace Luna.Views.WinForms
{
    internal partial class FormEditor : Form
    {
        Controllers.FormEditorCtrl.ButtonCtrl editorCtrl;
        Controllers.FormEditorCtrl.AutoCompleteCtrl acmCtrl;
        Controllers.FormEditorCtrl.MenuCtrl menuCtrl;

        Services.LuaServer luaServer;
        Services.Settings settings;
        Services.FormMgrSvc formMgr;
        private readonly LuaCoreSetting initialCoreSettings;
        Apis.Interfaces.Services.IApiService api;

        ScintillaNET.Scintilla editor;
        string title = "";

        public static FormEditor CreateForm(
            Apis.Interfaces.Services.IApiService api,
            Services.Settings settings,
            Services.LuaServer luaServer,
            Services.FormMgrSvc formMgr,

            Models.Data.LuaCoreSetting initialCoreSettings)
        {
            FormEditor r = null;
            Apis.Misc.UI.Invoke(() =>
            {
                r = new FormEditor(
                    api, settings, luaServer, formMgr,

                    initialCoreSettings);
            });
            return r;
        }

        FormEditor(
            Apis.Interfaces.Services.IApiService api,
            Services.Settings settings,
            Services.LuaServer luaServer,
            Services.FormMgrSvc formMgr,

            LuaCoreSetting initialCoreSettings)
        {
            this.api = api;
            this.formMgr = formMgr;
            this.initialCoreSettings = initialCoreSettings;

            this.settings = settings;
            this.luaServer = luaServer;

            InitializeComponent();
            Apis.Misc.UI.AutoSetFormIcon(this);
            title = string.Format(I18N.LunaScrEditor, Properties.Resources.Version);

            editor = Misc.UI.CreateLuaEditor(pnlScriptEditor);

            this.Text = title;
        }


        private void FormEditor_Load(object sender, System.EventArgs e)
        {

            InitSplitPanel();

            lbStatusBarMsg.Text = "";

            editorCtrl = new Controllers.FormEditorCtrl.ButtonCtrl(
                this,
                editor,
                cboxScriptName,
                btnNewScript,
                btnSaveScript,

                btnRunScript,
                btnStopScript,
                btnKillScript,
                btnClearOutput,

                btnShowFormSearch,
                btnGotoLine,
                tboxQuickSearch,

                rtBoxOutput);

            acmCtrl = new Controllers.FormEditorCtrl.AutoCompleteCtrl(
                editor,
                cboxVarList,
                cboxFunctionList,
                enableCodeAnalyzeToolStripMenuItem,
                toolStripStatusCodeAnalyze);

            acmCtrl.Run(settings);

            editorCtrl.Run(api, settings, formMgr, luaServer);

            menuCtrl = new Controllers.FormEditorCtrl.MenuCtrl(
                this,
                editorCtrl,

                newWindowToolStripMenuItem,
                showScriptManagerToolStripMenuItem,
                loadFileToolStripMenuItem,
                saveAsToolStripMenuItem,
                exitToolStripMenuItem,
                loadCLRLibraryToolStripMenuItem,
                toolStripStatusClrLib,
                cboxScriptName);

            menuCtrl.Run(formMgr, initialCoreSettings);

            this.FormClosing += FormClosingHandler;
            this.FormClosed += (s, a) =>
            {
                this.KeyDown -= KeyDownHandler;
                acmCtrl.Cleanup();
                editorCtrl.Cleanup();
                menuCtrl.Cleanup();
            };

            this.KeyDown += KeyDownHandler;
        }

        #region private methods
        void KeyDownHandler(object sender, KeyEventArgs a)
        {
            Apis.Misc.Utils.RunInBackground(
                () => Apis.Misc.UI.Invoke(() =>
                {
                    editorCtrl?.KeyBoardShortcutHandler(a);
                    acmCtrl?.KeyBoardShortcutHandler(a);
                }));
        }

        void InitSplitPanel()
        {
            splitContainerTabEditor.SplitterDistance = this.Width * 6 / 10;
            SetOutputPanelCollapseState(true);

        }
        private void FormClosingHandler(object sender, FormClosingEventArgs e)
        {
            if (settings.IsClosing())
            {
                return;
            }

            if (editorCtrl.IsChanged())
            {
                e.Cancel = !Apis.Misc.UI.Confirm(I18N.DiscardUnsavedChanges);
            }
        }
        #endregion

        #region public methods
        public void SetTitleTail(string text)
        {
            var t = $"{title} {text}";
            if (t == this.Text)
            {
                return;
            }

            var len = t.Length;
            var max = 100;
            if (len > max)
            {
                var head = 60;
                var tail = max - head;
                t = t.Substring(0, head) + " ... " + t.Substring(len - tail, tail);
            }

            Apis.Misc.UI.Invoke(() =>
            {
                this.Text = t;

                // tooltip would not work in form title
                // this.toolTip1.SetToolTip(this, text ?? "");
            });
        }

        public void SetOutputPanelCollapseState(bool isCollapsed)
        {
            splitContainerTabEditor.Panel2Collapsed = isCollapsed;
            outputPanelToolStripMenuItem.Checked = !isCollapsed;
        }
        #endregion


        #region UI event handlers
        private void outputPanelToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            var isCollapsed = !splitContainerTabEditor.Panel2Collapsed;
            SetOutputPanelCollapseState(isCollapsed);
        }

        #endregion
    }
}
