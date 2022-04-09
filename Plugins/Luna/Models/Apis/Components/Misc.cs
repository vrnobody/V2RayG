﻿using Luna.Services;
using Newtonsoft.Json.Linq;
using NLua;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using Apis.Interfaces;
using GlobalApis = global::Apis;

namespace Luna.Models.Apis.Components
{
    internal sealed class Misc :
        GlobalApis.BaseClasses.ComponentOf<LuaApis>,
        GlobalApis.Interfaces.Lua.ILuaMisc
    {
        Services.Settings settings;
        private readonly FormMgrSvc formMgr;
        GlobalApis.Interfaces.Services.IUtilsService vgcUtils;
        GlobalApis.Interfaces.Services.IShareLinkMgrService vgcSlinkMgr;
        GlobalApis.Interfaces.Services.INotifierService vgcNotifier;
        GlobalApis.Interfaces.Services.IServersService vgcServer;
        GlobalApis.Interfaces.Services.ISettingsService vgcSettings;

        public Misc(
            GlobalApis.Interfaces.Services.IApiService api,
            Services.Settings settings,
            Services.FormMgrSvc formMgr)
        {
            this.settings = settings;
            this.formMgr = formMgr;
            vgcNotifier = api.GetNotifierService();
            vgcUtils = api.GetUtilsService();
            vgcSlinkMgr = api.GetShareLinkMgrService();
            vgcServer = api.GetServersService();
            vgcSettings = api.GetSettingService();
        }


        #region ILuaMisc.WinForms
        public void Invoke(LuaFunction func) => GlobalApis.Misc.UI.Invoke(() => func.Call());

        public void ShowFormJsonEditor(string config) => vgcNotifier.ShowFormJsonEditor(config);

        public void ShowFormServerSettings(ICoreServCtrl coreServ) => vgcNotifier.ShowFormServerSettings(coreServ);

        public void ShowFormOption() => vgcNotifier.ShowFormOption();

        public void ShowFormLunaMgr() => formMgr.ShowFormMain();

        public void ShowFormLunaEditor() => formMgr.ShowOrCreateFirstEditor();

        public void ShowFormMain() => vgcNotifier.ShowFormMain();

        public void ShowFormLog() => vgcNotifier.ShowFormLog();

        #endregion

        #region ILuaMisc.ImportLinks     
        public int ImportLinks(string links, string mark) =>
            vgcSlinkMgr.ImportLinksWithOutV2cfgLinksSync(links, mark);

        #endregion

        #region ILuaMisc.Forms

        public string ShowData(string title, NLua.LuaTable columns, NLua.LuaTable rows, int defColumn)
        {
            var dt = LuaTableToDataTable(columns, rows);
            return ShowDataGridDialog(title, dt, defColumn);
        }

        public string ShowData(string title, NLua.LuaTable columns, NLua.LuaTable rows) =>
            ShowData(title, columns, rows, -1);

        public string BrowseFolder() => GlobalApis.Misc.UI.ShowSelectFolderDialog();

        public string BrowseFile() =>
            GlobalApis.Misc.UI.ShowSelectFileDialog(
                GlobalApis.Models.Consts.Files.AllExt);

        public string BrowseFile(string filter) =>
                GlobalApis.Misc.UI.ShowSelectFileDialog(filter);

        public string Input(string title) => Input(title, 3);

        public string Input(string title, int lines) => Input(title, string.Empty, lines);

        public string Input(string title, string content, int lines)
        {
            Func<AutoResetEvent, Views.WinForms.FormInput> creater =
                 (done) => new Views.WinForms.FormInput(done, title, content, lines);
            return GetResult<Views.WinForms.FormInput, string>(creater);
        }

        public List<int> Choices(string title, NLua.LuaTable choices) =>
            Choices(title, choices, false);

        public List<int> Choices(string title, NLua.LuaTable choices, bool isShowKey)
        {
            var list = global::Luna.Misc.Utils.LuaTableToList(choices, isShowKey);
            return GetResultFromChoicesDialog(title, list.ToArray());
        }

        public List<int> Choices(string title, params string[] choices) =>
            GetResultFromChoicesDialog(title, choices);

        public int Choice(string title, NLua.LuaTable choices) =>
            Choice(title, choices, false, -1);

        public int Choice(string title, NLua.LuaTable choices, bool isShowKey) =>
            Choice(title, choices, isShowKey, -1);

        public int Choice(string title, NLua.LuaTable choices, bool isShowKey, int selected)
        {
            var list = global::Luna.Misc.Utils.LuaTableToList(choices, isShowKey);
            return GetResultFromChoiceDialog(title, list.ToArray(), selected);
        }

        public int Choice(string title, params string[] choices) =>
            GetResultFromChoiceDialog(title, choices, -1);

        public bool Confirm(string content) =>
            GlobalApis.Misc.UI.Confirm(content);

        public void Alert(string content) =>
            MessageBox.Show(content, GlobalApis.Misc.Utils.GetAppName());

        #endregion

        #region other ILuaMisc stuff
        public int GetSpeedtestQueueLength() => vgcSettings.GetSpeedtestQueueLength();

        public string Replace(string text, string oldStr, string newStr) =>
            text?.Replace(oldStr, newStr);

        public string RandomHex(int len) => GlobalApis.Misc.Utils.RandomHex(len);

        public string NewGuid() => Guid.NewGuid().ToString();

        public string GetSubscriptionConfig() => vgcSettings.GetSubscriptionConfig();

        public void SetSubscriptionConfig(string cfgStr) => vgcSettings.SetSubscriptionConfig(cfgStr);

        public long GetTimeoutValue() => GlobalApis.Models.Consts.Core.SpeedtestTimeout;

        public void RefreshFormMain() => vgcServer.RequireFormMainReload();

        public void WriteLocalStorage(string key, string value) =>
          settings.SetLuaShareMemory(key, value);

        public string ReadLocalStorage(string key) =>
            settings.GetLuaShareMemory(key);

        public bool RemoveLocalStorage(string key) =>
            settings.RemoveShareMemory(key);

        public List<string> LocalStorageKeys() =>
            settings.ShareMemoryKeys();

        public string Config2VeeLink(string config)
        {
            try
            {
                return vgcSlinkMgr.EncodeConfigToShareLink(
                config, GlobalApis.Models.Datas.Enums.LinkTypes.v);
            }
            catch { }
            return null;
        }

        public string Config2VmessLink(string config)
        {
            try
            {
                return vgcSlinkMgr.EncodeConfigToShareLink(
                config, GlobalApis.Models.Datas.Enums.LinkTypes.vmess);
            }
            catch { }
            return null;
        }

        public string Config2V2cfg(string config)
        {
            try
            {
                return vgcSlinkMgr.EncodeConfigToShareLink(
                config, GlobalApis.Models.Datas.Enums.LinkTypes.v2cfg);
            }
            catch { }
            return null;
        }

        public string ShareLink2ConfigString(string shareLink)
        {
            try
            {
                return vgcSlinkMgr.DecodeShareLinkToConfig(shareLink) ?? @"";
            }
            catch { }
            return null;
        }

        public string AddVeePrefix(string b64Str) =>
           vgcUtils.AddLinkPrefix(b64Str, GlobalApis.Models.Datas.Enums.LinkTypes.v);

        public string AddVmessPrefix(string b64Str) =>
           vgcUtils.AddLinkPrefix(b64Str, GlobalApis.Models.Datas.Enums.LinkTypes.vmess);

        public string AddV2cfgPrefix(string b64Str) =>
           vgcUtils.AddLinkPrefix(b64Str, GlobalApis.Models.Datas.Enums.LinkTypes.v2cfg);

        /// <summary>
        /// null: failed
        /// </summary>
        public string Base64Encode(string text) =>
            vgcUtils.Base64Encode(text);

        /// <summary>
        /// null: failed
        /// </summary>
        public string Base64Decode(string b64Str) =>
            vgcUtils.Base64Decode(b64Str);

        public string GetLinkBody(string link) =>
            vgcUtils.GetLinkBody(link);

        public string PredefinedFunctions() =>
            Resources.Files.Datas.LuaPredefinedFunctions;

        public string ScanQrcode() =>
            vgcUtils.ScanQrcode();

        public string GetAppDir() => GlobalApis.Misc.Utils.GetAppDir();

        public void Sleep(int milSec) => GlobalApis.Misc.Utils.Sleep(milSec);

        public void Print(params object[] contents)
        {
            var text = "";
            foreach (var content in contents)
            {
                if (content == null)
                {
                    text += @"nil";
                }
                else
                {
                    text += content.ToString();
                }
            }
            GetParent().SendLog(text);
        }
        #endregion

        #region private methods
        List<Type> GetTypesFromRows(NLua.LuaTable rows)
        {
            if (rows == null)
            {
                return null;
            }

            var ts = new List<Type>();
            foreach (var row in rows.Values)
            {
                foreach (var cell in (row as NLua.LuaTable).Values)
                {
                    ts.Add(cell.GetType());
                }
                return ts;
            }
            return null;
        }

        DataTable LuaTableToDataTable(NLua.LuaTable columns, NLua.LuaTable rows)
        {
            var d = new DataTable();

            if (columns == null)
            {
                return d;
            }

            var ts = GetTypesFromRows(rows);

            var idx = 0;
            foreach (var column in columns.Values)
            {
                var name = column.ToString();
                if (ts == null)
                {
                    d.Columns.Add(name);
                }
                else
                {
                    d.Columns.Add(name, ts[idx++]);
                }
            }

            if (rows == null)
            {
                return d;
            }
            var rowsKey = rows.Keys;
            foreach (var rowkey in rowsKey)
            {
                var row = rows[rowkey] as NLua.LuaTable;
                var items = new List<object>();
                foreach (var item in row.Values)
                {
                    items.Add(item);
                }

                d.Rows.Add(items.ToArray());
            }
            return d;
        }

        string ShowDataGridDialog(string title, DataTable dataSource, int defColumn)
        {
            Func<AutoResetEvent, Views.WinForms.FormDataGrid> creater =
                  (done) => new Views.WinForms.FormDataGrid(done, title, dataSource, defColumn);
            return GetResult<Views.WinForms.FormDataGrid, string>(creater);
        }

        private static List<int> GetResultFromChoicesDialog(string title, string[] choices)
        {
            Func<AutoResetEvent, Views.WinForms.FormChoices> creater =
                (done) => new Views.WinForms.FormChoices(done, title, choices);
            return GetResult<Views.WinForms.FormChoices, List<int>>(creater);
        }

        static TResult GetResult<TForm, TResult>(Func<AutoResetEvent, TForm> creater)
             where TForm : Form, GlobalApis.Interfaces.Lua.IWinFormControl<TResult>
        {
            AutoResetEvent done = new AutoResetEvent(false);
            TResult r = default;
            TForm form = null;
            GlobalApis.Misc.UI.Invoke(() =>
            {
                form = creater.Invoke(done);
                form.Show();
                form.Activate();
            });
            done.WaitOne();
            GlobalApis.Misc.UI.Invoke(() =>
            {
                r = form.GetResult();
                form.Dispose();
            });
            return r;
        }

        private static int GetResultFromChoiceDialog(string title, string[] choices, int selected)
        {
            Func<AutoResetEvent, Views.WinForms.FormChoice> creater =
                (done) => new Views.WinForms.FormChoice(done, title, choices, selected);

            return GetResult<Views.WinForms.FormChoice, int>(creater);
        }

        #endregion
    }
}
