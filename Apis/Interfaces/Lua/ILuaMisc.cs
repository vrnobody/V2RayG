﻿using Newtonsoft.Json.Linq;
using NLua;
using System.Collections.Generic;

namespace Apis.Interfaces.Lua
{
    public interface ILuaMisc
    {

        #region vgc.Forms
        /// <summary>
        /// 在UI线程中执行一个函数
        /// </summary>
        /// <param name="func"></param>
        void Invoke(LuaFunction func);

        /// <summary>
        /// 调出Json编辑器窗口
        /// </summary>
        /// <param name="config">预置内容</param>
        void ShowFormJsonEditor(string config);

        /// <summary>
        /// 调出设定及二维码窗口
        /// </summary>
        /// <param name="coreServ">一个服务器</param>
        void ShowFormServerSettings(ICoreServCtrl coreServ);

        /// <summary>
        /// 调出选项窗口
        /// </summary>
        void ShowFormOption();

        /// <summary>
        /// 调出Luna脚本管理器窗口
        /// </summary>
        void ShowFormLunaMgr();

        /// <summary>
        /// 调出Luna脚本编辑器窗口
        /// </summary>
        void ShowFormLunaEditor();

        /// <summary>
        /// 调出主窗口
        /// </summary>
        void ShowFormMain();

        /// <summary>
        /// 调出日志窗口
        /// </summary>
        void ShowFormLog();

        #endregion

        #region vgc
        // timeout = long.MaxValue
        /// <summary>
        /// 获取测速超时的准确数值
        /// </summary>
        /// <returns>超时的数值</returns>
        long GetTimeoutValue();

        /// <summary>
        /// 扫描屏幕上的二维码
        /// </summary>
        /// <returns>二维码解码后的内容</returns>
        string ScanQrcode();

        /// <summary>
        /// 获取全部订阅设置
        /// </summary>
        /// <returns>订阅设置Json字符串</returns>
        string GetSubscriptionConfig();

        /// <summary>
        /// 替换当前订阅设置
        /// </summary>
        /// <param name="cfgStr">订阅设置Json字符串</param>
        void SetSubscriptionConfig(string cfgStr);

        // share among all scripts
        /// <summary>
        /// 从本地存储读取一个键为key的字符串
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns></returns>
        string ReadLocalStorage(string key);

        // share among all scripts
        /// <summary>
        /// 向本地存储写入一字符串，以key为键名
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">字符串内容</param>
        void WriteLocalStorage(string key, string value);

        // remove a key from local storage
        /// <summary>
        /// 删除一个键名为key的本地存储字符串
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns></returns>
        bool RemoveLocalStorage(string key);

        // get all keys of local storage
        /// <summary>
        /// 查询本地存储中所有键名
        /// </summary>
        /// <returns>所有键名</returns>
        List<string> LocalStorageKeys();
        #endregion

        #region utils
        /// <summary>
        /// 获取当前并行测速队列服务器总数。一个服务器只记一次。
        /// </summary>
        /// <returns>测速队列长度</returns>
        int GetSpeedtestQueueLength();

        /// <summary>
        /// 获取本软件所处目录
        /// </summary>
        /// <returns>当前目录</returns>
        string GetAppDir();

        /// <summary>
        /// 获取预定义函数源码
        /// </summary>
        /// <returns>预定义源码</returns>
        string PredefinedFunctions();

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="contents">若干内容</param>
        void Print(params object[] contents);

        /// <summary>
        /// 等待一段时间
        /// </summary>
        /// <param name="milSec">毫秒</param>
        void Sleep(int milSec);

        /// <summary>
        /// 查找替换字串中的内容
        /// </summary>
        /// <param name="text">整个字串</param>
        /// <param name="oldStr">要查找的字串</param>
        /// <param name="newStr">替换为字串</param>
        /// <returns>替换后字串</returns>
        string Replace(string text, string oldStr, string newStr);

        /// <summary>
        /// 生成指定长度的随机十六进制字符串
        /// </summary>
        /// <param name="len">长度</param>
        /// <returns></returns>
        string RandomHex(int len);

        /// <summary>
        /// 生成一个随机UUID
        /// </summary>
        /// <returns></returns>
        string NewGuid();

        #endregion

        #region UI thing
        /// <summary>
        /// 调出浏览目录窗口
        /// </summary>
        /// <returns>选中的目录路径</returns>
        string BrowseFolder();

        /// <summary>
        /// 调出浏览文件窗口
        /// </summary>
        /// <returns>选中的文件路径</returns>
        string BrowseFile();

        /// <summary>
        /// 调出浏览指定后缀文件窗口
        /// </summary>
        /// <param name="extends">后缀</param>
        /// <returns>选中的文件路径</returns>
        string BrowseFile(string extends);

        // 2MiB char max
        /// <summary>
        /// 调出输入字符串窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <returns>用户输入的内容</returns>
        string Input(string title);

        // 25 lines max
        /// <summary>
        /// 调出输入字符串窗口，指定初始行数
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="lines">初始行数</param>
        /// <returns></returns>
        string Input(string title, int lines);

        /// <summary>
        /// 调出输入字符串窗口，指定初始行数及初始内容
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="content">初始内容</param>
        /// <param name="lines">初始行数</param>
        /// <returns></returns>
        string Input(string title, string content, int lines);

        /// <summary>
        /// 调出数据展示窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="columns">列标题</param>
        /// <param name="rows">内容行</param>
        /// <returns></returns>
        string ShowData(string title, NLua.LuaTable columns, NLua.LuaTable rows);

        /// <summary>
        /// 调出数据展示窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="columns">列标题</param>
        /// <param name="rows">内容行</param>
        /// <param name="defColumn">初始选定列号</param>
        /// <returns></returns>
        string ShowData(string title, NLua.LuaTable columns, NLua.LuaTable rows, int defColumn);

        // 18 choices max
        /// <summary>
        /// 调出多选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <returns>选中行号集合</returns>
        List<int> Choices(string title, params string[] choices);

        /// <summary>
        /// 调出多选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <returns>选中行号集合</returns>
        List<int> Choices(string title, NLua.LuaTable choices);

        /// <summary>
        /// 调出多选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <param name="isShowKey">是否显示行号</param>
        /// <returns>选中行号集合</returns>
        List<int> Choices(string title, NLua.LuaTable choices, bool isShowKey);

        /// <summary>
        /// 调整单选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <returns>选中行号</returns>
        int Choice(string title, params string[] choices);

        /// <summary>
        /// 调整单选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <returns>选中行号</returns>
        int Choice(string title, NLua.LuaTable choices);

        /// <summary>
        /// 调整单选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <param name="isShowKey">是否显示行号</param>
        /// <returns>选中行号</returns>
        int Choice(string title, NLua.LuaTable choices, bool isShowKey);

        /// <summary>
        /// 调整单选窗口
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="choices">各选项</param>
        /// <param name="isShowKey">是否显示行号</param>
        /// <param name="selected">默认选中行号</param>
        /// <returns>选中行号</returns>
        int Choice(string title, NLua.LuaTable choices, bool isShowKey, int selected);

        /// <summary>
        /// 调出确认窗口
        /// </summary>
        /// <param name="content">显示内容</param>
        /// <returns></returns>
        bool Confirm(string content);

        /// <summary>
        /// 调出信息框
        /// </summary>
        /// <param name="content">显示内容</param>
        void Alert(string content);

        // sort server panel by index
        /// <summary>
        /// 刷新主窗口（修改服务器序号后重新排序）
        /// </summary>
        void RefreshFormMain();

        #endregion

        #region encode decode
        // GetLinkBody("vmess://abcdefg") == "abcdefg"
        /// <summary>
        /// vmess://abcdefg => abcdefg
        /// </summary>
        /// <param name="link">任意链接</param>
        /// <returns>链接内容</returns>
        string GetLinkBody(string link);

        // v2cfg://(b64Str)
        /// <summary>
        /// abcdefg => v2cfg://abcdefg
        /// </summary>
        /// <param name="b64Str">任意内容</param>
        /// <returns>v2cfg://（任意内容）</returns>
        string AddV2cfgPrefix(string b64Str);

        // v://(b64Str)
        /// <summary>
        /// abcdefg => v://abcdefg
        /// </summary>
        /// <param name="b64Str">任意内容</param>
        /// <returns>v://（任意内容）</returns>
        string AddVeePrefix(string b64Str);

        // vmess://(b64Str)
        /// <summary>
        /// abcdefg => vmess://abcdefg
        /// </summary>
        /// <param name="b64Str">任意内容</param>
        /// <returns>vmess://（任意内容）</returns>
        string AddVmessPrefix(string b64Str);

        /// <summary>
        /// 对字符串进行Base64编码
        /// </summary>
        /// <param name="text">任意字符串</param>
        /// <returns>Base64字符串</returns>
        string Base64Encode(string text);

        /// <summary>
        /// 将Base64字符串解码
        /// </summary>
        /// <param name="b64Str">Base64字符串</param>
        /// <returns>解码后的字符串（解码失败返回null）</returns>
        string Base64Decode(string b64Str);

        /// <summary>
        /// 将config编码为v2cfg链接
        /// </summary>
        /// <param name="config">服务器完整config.json</param>
        /// <returns>v2cfg://...</returns>
        string Config2V2cfg(string config);

        /// <summary>
        /// 将config编码为vee链接
        /// </summary>
        /// <param name="config">服务器完整config.json</param>
        /// <returns>v://...</returns>
        string Config2VeeLink(string config);

        /// <summary>
        /// 将config编码为vmess链接
        /// </summary>
        /// <param name="config">服务器完整config.json</param>
        /// <returns>vmess://...</returns>
        string Config2VmessLink(string config);

        /// <summary>
        /// 将各种分享链接解码为config.json
        /// </summary>
        /// <param name="shareLink">各种分享链接</param>
        /// <returns>服务器config.json</returns>
        string ShareLink2ConfigString(string shareLink);

        // links = "vmess://... ss://...  (...)"
        /// <summary>
        /// 从links字符串导入分享链接，并添加mark标记
        /// </summary>
        /// <param name="links">各种分享链接</param>
        /// <param name="mark">标记</param>
        /// <returns>成功导入的链接数</returns>
        int ImportLinks(string links, string mark);
        #endregion
    }
}
