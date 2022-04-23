﻿using Luna.Resources.Langs;
using NLua;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Luna.Controllers
{
    internal class LuaCoreCtrl
    {
        public EventHandler OnStateChange;

        Services.Settings settings;
        Models.Data.LuaCoreSetting coreSetting;
        Models.Apis.LuaApis luaApis;
        Models.Apis.LuaSignal luaSignal;
        Models.Apis.LuaSys luaSys = null;

        Thread luaCoreThread;
        private readonly bool enableTracebackFeature;

        public LuaCoreCtrl(bool enableTracebackFeature)
        {
            this.enableTracebackFeature = enableTracebackFeature;
        }

        public void Run(
            Services.Settings settings,
            Models.Data.LuaCoreSetting luaCoreState,
            Models.Apis.LuaApis luaApis)
        {
            this.settings = settings;
            this.coreSetting = luaCoreState;
            this.luaApis = luaApis;
            this.luaSignal = new Models.Apis.LuaSignal(settings);
        }

        #region properties 
        public string name
        {
            get => coreSetting.name;
            set
            {
                if (coreSetting.name == value)
                {
                    return;
                }
                coreSetting.name = value;
                Save();
                InvokeOnStateChangeIgnoreError();
            }
        }

        public double index
        {
            get => coreSetting.index;
            set
            {
                if (coreSetting.index == value)
                {
                    return;
                }
                coreSetting.index = value;
                Save();
            }
        }

        public bool isLoadClr
        {
            get => coreSetting.isLoadClr;
            set
            {
                if (value == coreSetting.isLoadClr)
                {
                    return;
                }
                coreSetting.isLoadClr = value;
                Save();
                InvokeOnStateChangeIgnoreError();
            }
        }

        public bool isHidden
        {
            get => coreSetting.isHidden;
            set
            {
                if (coreSetting.isHidden == value)
                {
                    return;
                }
                coreSetting.isHidden = value;
                Save();
                InvokeOnStateChangeIgnoreError();
            }
        }

        public bool isAutoRun
        {
            get => coreSetting.isAutorun;
            set
            {
                if (coreSetting.isAutorun == value)
                {
                    return;
                }

                coreSetting.isAutorun = value;
                Save();
                InvokeOnStateChangeIgnoreError();
            }
        }

        bool _isRunning = false;
        public bool isRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value)
                {
                    return;
                }

                _isRunning = value;
                if (_isRunning == false)
                {
                    SendLog($"{coreSetting.name} {I18N.Stopped}");
                }
                InvokeOnStateChangeIgnoreError();
            }
        }

        void InvokeOnStateChangeIgnoreError()
        {
            try
            {
                OnStateChange?.Invoke(null, null);
            }
            catch { }
        }
        #endregion

        #region public methods
        public Models.Data.LuaCoreSetting GetCoreSettings() =>
            coreSetting;

        public void SetScriptName(string name)
        {
            coreSetting.name = name;
        }

        public void ReplaceScript(string script)
        {
            coreSetting.script = script;
            Save();
        }

        public void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            SendLog($"{I18N.SendStopSignalTo} {coreSetting.name}");
            luaSignal.SetStopSignal(true);
            luaSys?.OnSignalStop();
        }

        public void Abort() => KillCore(2000);

        public void AbortNow() => KillCore(1);

        public void Start()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;

            SendLog($"{I18N.Start} {coreSetting.name}");

            luaCoreThread = new Thread(RunLuaScript)
            {
                IsBackground = true,
                Name = "LuaStateContainer",
            };

            luaCoreThread.Start();
        }

        public void Cleanup()
        {
            AbortNow();
        }
        #endregion

        #region private methods
        void KillCore(int timeout)
        {
            if (!isRunning)
            {
                return;
            }

            Stop();

            if (!luaCoreThread.Join(timeout))
            {
                SendLog($"{I18N.Terminate} {coreSetting.name}");
                try
                {
                    luaCoreThread.Abort();
                }
                catch { }
            }

            luaSys?.Dispose();
            luaSys = null;

            isRunning = false;
        }

        List<Type> assemblies = null;
        List<Type> GetAllAssemblies()
        {
            if (assemblies == null)
            {
                assemblies = Apis.Misc.Utils.GetAllAssembliesType();
            }
            return assemblies;
        }

        void SendLog(string content) => luaApis.SendLog(content);

        void RunLuaScript()
        {
            luaSys?.Dispose();
            luaSys = new Models.Apis.LuaSys(luaApis, GetAllAssemblies);

            luaSignal.ResetAllSignals();

            using (Lua core = CreateLuaCore(luaSys))
            {
                try
                {
                    core.DoString(coreSetting.script);
                }
                catch (Exception e)
                {
                    SendLog($"[{coreSetting.name}] {e}");
                    if (core.UseTraceback)
                    {
                        SendLog(core.GetDebugTraceback());
                    }
                }
            }

            luaSys?.Dispose();
            luaSys = null;

            isRunning = false;
        }

        Lua CreateLuaCore(Models.Apis.LuaSys luaSys)
        {
            var lua = new Lua()
            {
                UseTraceback = enableTracebackFeature,
            };

            if (isLoadClr)
            {
                lua.LoadCLRPackage();
            }

            lua.State.Encoding = Encoding.UTF8;

            // bug: lua can access all public functions
            var misc = luaApis.GetChild<Apis.Interfaces.Lua.ILuaMisc>();

            lua["Signal"] = luaSignal;
            lua["Sys"] = luaSys;

            lua["Misc"] = misc;
            lua["Server"] = luaApis.GetChild<Apis.Interfaces.Lua.ILuaServer>();
            lua["Web"] = luaApis.GetChild<Apis.Interfaces.Lua.ILuaWeb>();

            lua.DoString(misc.PredefinedFunctions());
            return lua;
        }

        void Save() => settings.SaveUserSettingsLater();

        #endregion
    }
}
