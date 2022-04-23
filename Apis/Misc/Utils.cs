﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScintillaNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Apis.Resources.Langs;

namespace Apis.Misc
{
    public static class Utils
    {
        #region editor
        public static void BindEditorDragDropEvent(Scintilla editor)
        {
            editor.AllowDrop = true;

            editor.DragEnter += (s, a) =>
            {
                a.Effect = DragDropEffects.Move;
            };

            editor.DragDrop += (s, a) =>
            {
                var data = a.Data;
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    var filenames = a.Data.GetData(DataFormats.FileDrop) as string[];
                    HandleEditorFileDropEvent(editor, filenames);
                }
            };
        }

        public static void HandleEditorFileDropEvent(Scintilla editor, string[] filenames)
        {
            if (filenames == null)
            {
                return;
            }

            foreach (var filename in filenames)
            {
                if (!File.Exists(filename))
                {
                    continue;
                }

                string content;
                string scriptName;

                try
                {
                    content = File.ReadAllText(filename);
                    scriptName = Path.GetFileName(filename);
                }
                catch
                {
                    continue;
                }

                var name = AutoEllipsis(scriptName, 40);
                if (string.IsNullOrWhiteSpace(content))
                {
                    var err = string.Format(I18N.FileIsEmpty, name);
                    UI.MsgBox(err);
                    continue;
                }

                var msg = string.Format(I18N.ConfirmLoadFileContent, name);
                if (string.IsNullOrEmpty(editor.Text) || Apis.Misc.UI.Confirm(msg))
                {
                    editor.Text = content;
                }
            }
        }

        public static string GetWordFromCurPos(Scintilla editor)
        {
            var line = editor.Lines[editor.CurrentLine];
            var text = line.Text;

            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var start = editor.CurrentPosition - line.Position - 1;
            start = Clamp(start, 0, text.Length);
            var end = start;
            for (; start >= 0; start--)
            {
                var c = text[start];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
            }

            for (; end < text.Length; end++)
            {
                var c = text[end];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
            }

            var len = end - start - 1;
            if (len < 1)
            {
                return "";
            }

            return text.Substring(start + 1, len);
        }
        #endregion


        #region system
        public static string GetCurCallStack()
        {
            var s = new List<string>();

            StackTrace stack = new StackTrace();
            foreach (var frame in stack.GetFrames())
            {
                var method = frame.GetMethod();
                var mn = Misc.Utils.GetFriendlyMethodDeclareInfo(method as MethodInfo);
                s.Add($" -> {mn}");
            }

            return string.Join("\n", s);
        }

        public static bool TryParseKeyMesssage(
            string keyName, bool hasAlt, bool hasCtrl, bool hasShift,
             out uint modifier,
             out uint keyCode)
        {
            keyCode = 0;
            modifier = 0;

            if (!(hasCtrl || hasShift || hasAlt)
               || !Enum.TryParse(keyName, out Keys key))
            {
                return false;
            }

            keyCode = (uint)key;

            uint ctrl = hasCtrl ? (uint)Models.Datas.Enums.ModifierKeys.Control : 0;
            uint alt = hasAlt ? (uint)Models.Datas.Enums.ModifierKeys.Alt : 0;
            uint shift = hasShift ? (uint)Models.Datas.Enums.ModifierKeys.Shift : 0;

            modifier = ctrl | alt | shift;


            return true;
        }

        #endregion

        #region List
        static Random rngForShuffle = new Random();

        public static List<T> Shuffle<T>(IEnumerable<T> source)
        {
            var list = source.ToList();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rngForShuffle.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }
        #endregion

        #region files
        public static string GetImageResolution(string filename)
        {
            try
            {
                var img = Image.FromFile(filename);
                return $"{img.Width}x{img.Height}";
            }
            catch { }
            return null;
        }

        public static string PickRandomLine(string filename)
        {
            string url = string.Empty;

            if (!File.Exists(filename))
            {
                return url;
            }

            using (var file = File.OpenText(filename))
            {
                int numberSeen = 0;
                var rng = new Random();
                var lines = File.ReadLines(filename);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrEmpty(line) && rng.Next(++numberSeen) == 0)
                    {
                        url = line;
                    }
                }
            }

            return url.Replace("\r", "").Replace("\n", "");
        }
        #endregion

        #region string
        public static string Md5Base64(string text)
        {
            var bytes = Md5Hash(text);
            var b64 = Convert.ToBase64String(bytes);
            return b64;
        }

        public static byte[] Md5Hash(string text)
        {
            MD5 md5Hasher = MD5.Create();
            var hash = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(text));
            return hash;
        }

        public static string GetAppName() => Properties.Resources.AppName;

        public static string AutoEllipsis(string text, int lenInAscii)
        {
            var ellipsis = Models.Consts.AutoEllipsis.ellipsis;
            var defFont = Models.Consts.AutoEllipsis.defFont;

            if (string.IsNullOrEmpty(text) || lenInAscii <= 0)
            {
                return string.Empty;
            }

            var width = TextRenderer.MeasureText(text, defFont).Width;
            var baseline = TextRenderer.MeasureText(new string('a', lenInAscii), defFont).Width;

            if (width <= baseline)
            {
                return text;
            }

            int end = Math.Min(text.Length, lenInAscii);
            int pos = BinarySearchForEllipsisPos(text, 0, end, baseline);
            return text.Substring(0, pos) + ellipsis;
        }

        static int BinarySearchForEllipsisPos(string text, int start, int end, int baseline)
        {
            int mid = (start + end) / 2;
            while (mid != start && mid != end)
            {
                var s = text.Substring(0, mid) + Models.Consts.AutoEllipsis.ellipsis;
                var w = TextRenderer.MeasureText(s, Models.Consts.AutoEllipsis.defFont).Width;
                if (w == baseline)
                {
                    return mid;
                }

                if (w < baseline)
                {
                    start = mid;
                }
                else
                {
                    end = mid;
                }
                mid = (start + end) / 2;
            }
            return mid;
        }


        public static bool TryPatchGitHubUrl(string url, out string patched)
        {
            patched = string.Empty;

            try
            {
                var groups = Regex.Match(url, Models.Consts.Patterns.GitHuhFileUrl).Groups;
                if (groups != null && groups.Count == 3)
                {
                    var repo = groups[1];
                    var tail = groups[2];
                    patched = $"https://raw.githubusercontent.com{repo}{tail}";
                    return true;
                }
            }
            catch (ArgumentException) { }
            catch (RegexMatchTimeoutException) { }

            try
            {
                var groups = Regex.Match(url, Models.Consts.Patterns.GitHuhFileUrl).Groups;
                if (groups != null && groups.Count == 3)
                {
                    var repo = groups[1];
                    var tail = groups[2];
                    patched = $"https://raw.githubusercontent.com{repo}{tail}";
                    return true;
                }
            }
            catch (ArgumentException) { }
            catch (RegexMatchTimeoutException) { }

            return false;
        }

        public static bool TryExtractAliasFromSubscriptionUrl(
            string url, out string alias)
        {
            alias = string.Empty;
            try
            {
                var groups = Regex.Match(url, Models.Consts.Patterns.ExtractAliasFromSubscriptUrl).Groups;
                if (groups != null && groups.Count == 2)
                {
                    alias = groups[1].Value;
                    return !string.IsNullOrEmpty(alias);
                }
            }
            catch (ArgumentException) { }
            catch (RegexMatchTimeoutException) { }

            return false;
        }

        public static string ReverseSummary(string summary)
        {

            if (string.IsNullOrEmpty(summary))
            {
                return "";
            }

            const char separator = '@';
            if (summary.IndexOf(separator) < 0)
            {
                return summary;
            }

            var rs = summary.Split(separator).Reverse();
            return string.Join(separator.ToString(), rs);
        }

        public static List<string> SortPacList(IEnumerable<string> pacList)
        {
            var tmpList = new List<string>();
            var result = new List<string>();
            foreach (var item in pacList)
            {
                if (!string.IsNullOrWhiteSpace(item)
                    && !string.IsNullOrEmpty(item)
                    && !item.StartsWith(@"//"))
                {
                    tmpList.Add(item);
                    continue;
                }

                tmpList.Sort(StringComparer.Ordinal);
                result.AddRange(tmpList);
                result.Add(item);
                tmpList.Clear();
            }
            tmpList.Sort(StringComparer.Ordinal);
            result.AddRange(tmpList);
            return result;
        }

        #endregion

        #region net


        static HttpClient CreateHttpClient(int port)
        {
            HttpClient hc;
            if (port > 0 && port < 65536)
            {
                var httpClientHandler = new HttpClientHandler
                {
                    Proxy = new WebProxy(Models.Consts.Webs.LoopBackIP, port),
                };
                hc = new HttpClient(handler: httpClientHandler, disposeHandler: true);
            }
            else
            {
                hc = new HttpClient();
            }
            hc.DefaultRequestHeaders.Add(Models.Consts.Webs.UserAgentKey, Models.Consts.Webs.ChromeUserAgent);
            return hc;
        }


        static async Task<long> TimedDownloadWorker(
            string url, int port,
            Func<long, bool> onProgress,
            CancellationToken token)
        {
            long timeout = Models.Consts.Core.SpeedtestTimeout;

            Stopwatch sw = new Stopwatch();

            try
            {
                using (HttpClient hc = CreateHttpClient(port))
                {
                    var opt = HttpCompletionOption.ResponseHeadersRead;
                    sw.Start();

                    using (var response = await hc.GetAsync(url, opt, token).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            return timeout;
                        }

                        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            byte[] buffer = new byte[4 * 1024];
                            long read;
                            do
                            {
                                read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                                if (!onProgress.Invoke(read))
                                {
                                    break;
                                }
                            } while (read > 0);
                        }
                    }
                    sw.Stop();
                }
                return sw.ElapsedMilliseconds;
            }
            catch
            {
                // break point for debugging
            }

            return timeout;
        }

        /// <summary>
        /// return (milSec, recvBytesLen)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="port"></param>
        /// <param name="expectedSizeInKiB"></param>
        /// <param name="timeout"></param>
        /// <returns>(milSec, recvBytesLen)</returns>
        public static Tuple<long, long> TimedDownloadTest(
            string url, int port, int expectedSizeInKiB, int timeout)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("URL must not null!");
            }

            long to = Models.Consts.Core.SpeedtestTimeout;

            long latency = to;
            long totalRead = 0;
            long expectedBytes = expectedSizeInKiB * 1024;
            Func<long, bool> onProgress = (read) =>
            {
                totalRead += read;
                if (totalRead > expectedBytes && totalRead > 0)
                {
                    return false;
                }
                return true;
            };

            var maxTimeout = timeout > 0 ? timeout : Models.Consts.Intervals.DefaultSpeedTestTimeout;
            var cts = new CancellationTokenSource(maxTimeout);

            var done = new AutoResetEvent(false);
            var t = new Task(async () =>
            {
                latency = await TimedDownloadWorker(url, port, onProgress, cts.Token);
                done.Set();
            }, TaskCreationOptions.LongRunning);
            t.ConfigureAwait(false);
            t.Start();
            done.WaitOne((int)(maxTimeout * 1.5));

            if (totalRead > 0 && totalRead > expectedBytes)
            {
                return new Tuple<long, long>(latency, totalRead);
            }

            return new Tuple<long, long>(to, totalRead);
        }

        public static bool IsValidPort(string port)
        {
            return IsValidPort(Str2Int(port));
        }

        public static bool IsValidPort(int port)
        {
            return port > 0 && port < 65536;
        }

        public static bool TryParseAddress(string address, out string ip, out int port)
        {
            ip = Models.Consts.Webs.LoopBackIP;
            port = 1080;

            int index = address.LastIndexOf(':');
            if (index < 0)
            {
                return false;
            }

            var ipStr = address.Substring(0, index);
            var portInt = Str2Int(address.Substring(index + 1));
            if (string.IsNullOrEmpty(ipStr) || portInt < 1 || portInt > 65535)
            {
                return false;
            }

            ip = ipStr;
            port = portInt;
            return true;
        }

        static readonly IPEndPoint _defaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, port: 0);
        public static int GetFreeTcpPort()
        {
            // https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
            var port = -1;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                lock (_defaultLoopbackEndpoint)
                {
                    try
                    {
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                        // socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        socket.Bind(_defaultLoopbackEndpoint);
                        port = ((IPEndPoint)socket.LocalEndPoint).Port;
                    }
                    catch { }
                }
            }
            return port;
        }
        #endregion

        #region Task
        public static void Sleep(TimeSpan timespan)
        {
            try
            {
                Thread.Sleep(timespan);
            }
            catch { }
        }
        public static void Sleep(int milSec)
        {
            try
            {
                // Task.Delay(milSec).Wait();
                Thread.Sleep(milSec);
            }
            catch { }
        }

        public static void SetProcessEnvs(Process proc, Dictionary<string, string> envs)
        {
            if (envs == null || envs.Count <= 0)
            {
                return;
            }

            var procEnv = proc.StartInfo.EnvironmentVariables;
            foreach (var env in envs)
            {
                procEnv[env.Key] = env.Value;
            }
        }

        static readonly AutoResetEvent sendCtrlCLocker = new AutoResetEvent(true);
        public static bool SendStopSignal(Process proc)
        {
            // https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c

            const int CTRL_C_EVENT = 0;

            var success = false;
            if (!sendCtrlCLocker.WaitOne(Models.Consts.Core.SendCtrlCTimeout))
            {
                return false;
            }
            try
            {
                if (Libs.Sys.ConsoleCtrls.AttachConsole((uint)proc.Id))
                {
                    Libs.Sys.ConsoleCtrls.SetConsoleCtrlHandler(null, true);
                    try
                    {
                        if (Libs.Sys.ConsoleCtrls.GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0)
                            && proc.WaitForExit(Models.Consts.Core.SendCtrlCTimeout))
                        {
                            success = true;
                        }
                    }
                    catch { }
                    Libs.Sys.ConsoleCtrls.FreeConsole();
                    Libs.Sys.ConsoleCtrls.SetConsoleCtrlHandler(null, false);
                }
            }
            catch { }
            sendCtrlCLocker.Set();

            return success;
        }

        public static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited)
                {
                    proc.Kill();
                    proc.WaitForExit(1000);
                }
            }
            catch
            {
                // Process already exited.
            }
        }

        public static Task RunInBackground(Action worker, bool configAwait = false)
        {
            Action job = () =>
            {
                try
                {
                    var missionId = Utils.RandomHex(8);
                    if (UI.IsInUiThread())
                    {
                        Libs.Sys.FileLogger.Warn($"Task [{missionId}] running in UI thread");
#if DEBUG
                        Libs.Sys.FileLogger.DumpCallStack("Caller stack:");
#endif
                    }
                    worker?.Invoke();
                    if (UI.IsInUiThread())
                    {
                        Libs.Sys.FileLogger.Warn($"task [{missionId}] finished");
                    }
                }
                catch (Exception e)
                {
                    Libs.Sys.FileLogger.Error($"Background task error:\n{e}");
                    throw;
                }
            };

            try
            {
                var t = new Task(job, TaskCreationOptions.LongRunning);
                if (!configAwait)
                {
                    t.ConfigureAwait(false);
                }
                t.Start();
                return t;
            }
            catch (Exception e)
            {
                Libs.Sys.FileLogger.Error($"Create background task error:\n{e}");
            }
            return Task.FromResult(false);
        }
        #endregion

        #region Json
        public static Func<string, string, string> GetStringByPrefixAndKeyHelper(JObject json)
        {
            var o = json;
            return (prefix, key) =>
            {
                return GetValue<string>(o, $"{prefix}.{key}");
            };
        }

        public static Func<string, string> GetStringByKeyHelper(JObject json)
        {
            var o = json;
            return (key) =>
            {
                return GetValue<string>(o, $"{key}");
            };
        }

        public static T GetValue<T>(JToken json, string prefix, string key)
        {
            return GetValue<T>(json, $"{prefix}.{key}");
        }

        /// <summary>
        /// return null if not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T GetValue<T>(JToken json, string path)
        {
            var key = GetKey(json, path);

            var def = default(T) == null && typeof(T) == typeof(string) ?
                (T)(object)string.Empty :
                default;

            try
            {
                switch (key?.Type)
                {
                    case JTokenType.Object:
                    case JTokenType.Array:
                    case null:
                        return def;
                    default:
                        return key.Value<T>();
                }
            }
            catch { }
            return def;
        }

        /// <summary>
        /// return null if path is null or path not exists.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static JToken GetKey(JToken json, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }


            var curPos = json;
            var keys = path.Split('.');

            int depth;
            for (depth = 0; depth < keys.Length; depth++)
            {
                if (curPos == null || !curPos.HasValues)
                {
                    break;
                }

                try
                {
                    if (int.TryParse(keys[depth], out int n))
                    {
                        curPos = curPos[n];
                    }
                    else
                    {
                        curPos = curPos[keys[depth]];
                    }
                }
                catch
                {
                    return null;
                }
            }

            return depth < keys.Length ? null : curPos;

        }

        public static bool WriteAllTextNow(string path, string contents)
        {
            // https://stackoverflow.com/questions/25366534/file-writealltext-not-flushing-data-to-disk
            try
            {
                // get the bytes
                var data = Encoding.UTF8.GetBytes(contents);

                // write the data to a temp file
                using (var tempFile = File.Create(path, 4096, FileOptions.WriteThrough))
                {
                    tempFile.Write(data, 0, data.Length);
                }
                return true;
            }
            catch (Exception e)
            {
                Libs.Sys.FileLogger.Error($"WriteAllTextNow() exception: {e.ToString()}");
            }
            return false;
        }

        /// <summary>
        /// return parsed T object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static T LoadAndParseJsonFile<T>(string filename)
           where T : class
        {
            if (File.Exists(filename))
            {
                try
                {
                    var content = File.ReadAllText(filename);
                    var result = JsonConvert.DeserializeObject<T>(content);
                    return result;
                }
                catch { }
            }
            return null;
        }


        /// <summary>
        /// a<b: -, a=b: 0, a>b: +
        /// </summary>     
        public static int JsonKeyComparer(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return a.CompareTo(b);
            }

            var listA = a.Split('.').ToList();
            var listB = b.Split('.').ToList();
            var lenA = listA.Count;
            var lenB = listB.Count;

            var maxLen = Math.Min(lenA, lenB);
            var result = 0;
            for (int i = 0; i < maxLen && result == 0; i++)
            {
                var itemA = listA[i];
                var itemB = listB[i];
                if (int.TryParse(itemA, out int numA)
                    && int.TryParse(itemB, out int numB))
                {
                    result = numA.CompareTo(numB);
                }
                else
                {
                    result = itemA.CompareTo(itemB);
                }

            }

            return result == 0 ? lenA - lenB : result;
        }

        public static Dictionary<string, string> GetterJsonSections(
            JToken jtoken)
        {
            var rootKey = Models.Consts.Config.ConfigSectionDefRootKey;
            var defDepth = Models.Consts.Config.ConfigSectionDefDepth;
            var setting = Models.Consts.Config.ConfigSectionDefSetting;

            var ds = new Dictionary<string, string>();

            GetterJsonDataStructRecursively(
                ref ds, jtoken, rootKey, defDepth, setting);

            ds.Remove(rootKey);

            int index = rootKey.Length + 1;
            return ds
                .Select(kv => new KeyValuePair<string, string>(kv.Key.Substring(index), kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        static bool IsValidJobjectKey(string key)
        {
            if (string.IsNullOrEmpty(key)
                || int.TryParse(key, out int blackhole))
            {
                return false;
            }

            return true;
        }

        static void GetterJsonDataStructRecursively(
            ref Dictionary<string, string> sections,
            JToken jtoken,
            string root,
            int depth,
            Dictionary<string, int> setting)
        {
            if (depth < 0)
            {
                return;
            }

            if (setting.ContainsKey(root))
            {
                depth = setting[root];
            }

            switch (jtoken)
            {
                case JObject jobject:
                    sections[root] = Models.Consts.Config.JsonObject;
                    foreach (var prop in jobject.Properties())
                    {
                        var key = prop.Name;
                        if (!IsValidJobjectKey(key))
                        {
                            continue;
                        }
                        var subRoot = $"{root}.{key}";
                        GetterJsonDataStructRecursively(
                           ref sections, jobject[key], subRoot, depth - 1, setting);
                    }
                    break;

                case JArray jarry:
                    sections[root] = Models.Consts.Config.JsonArray;
                    for (int i = 0; i < jarry.Count(); i++)
                    {
                        var key = i;
                        var subRoot = $"{root}.{key}";
                        GetterJsonDataStructRecursively(
                            ref sections, jarry[key], subRoot, depth - 1, setting);
                    }
                    break;
                default:
                    break;
            }
        }

        public static string TrimConfig(string config)
        {
            try
            {
                var cfg = JObject.Parse(config);
                return cfg?.ToString(Formatting.None);
            }
            catch { }
            return null;
        }

        public static bool TryParseJObject(
           string jsonString, out JObject json)
        {
            json = null;
            try
            {
                json = JObject.Parse(jsonString);
                return true;
            }
            catch { }
            return false;
        }

        public static void SavePluginSetting<T>(
            string pluginName,
            T userSettings,
            Interfaces.Services.ISettingsService vgcSetting)
            where T : class
        {
            var content = Utils.SerializeObject(userSettings);
            vgcSetting.SavePluginsSetting(pluginName, content);
        }

        public static T LoadPluginSetting<T>(
            string pluginName,
            Interfaces.Services.ISettingsService vgcSetting)
            where T : class, new()
        {
            var empty = new T();
            var userSettingString =
                vgcSetting.GetPluginsSetting(pluginName);

            if (string.IsNullOrEmpty(userSettingString))
            {
                return empty;
            }

            try
            {
                var result = Apis.Misc.Utils
                    .DeserializeObject<T>(userSettingString);
                return result ?? empty;
            }
            catch { }
            return empty;
        }

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string content) where T : class
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            try
            {
                var result = JsonConvert.DeserializeObject<T>(content);
                if (result != null)
                {
                    return result;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <param name="serializeObject"></param>
        /// <returns></returns>
        public static string SerializeObject(object serializeObject)
        {
            if (serializeObject == null)
            {
                return null;
            }
            return JsonConvert.SerializeObject(serializeObject);
        }

        /// <summary>
        /// return null if fail
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <returns></returns>
        public static T Clone<T>(T a) where T : class
        {
            if (a == null)
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(
                    JsonConvert.SerializeObject(a));
            }
            catch { }
            return null;
        }
        #endregion

        #region string processor

        /// <summary>
        /// Return empty list if some thing goes wrong.
        /// </summary>
        public static List<string> ExtractBase64Strings(string text, int minLen)
        {
            var b64s = new List<string>();
            try
            {
                var matches = Regex.Matches(text, Models.Consts.Patterns.Base64NonStandard);
                foreach (Match match in matches)
                {
                    var v = match.Value;
                    if (string.IsNullOrEmpty(v) || v.Length < minLen)
                    {
                        continue;
                    }
                    b64s.Add(v);
                }
            }
            catch { }
            return b64s;
        }

        public static string GetFragment(
            Scintilla editor,
            string searchPattern)
        {
            // https://github.com/Ahmad45123/AutoCompleteMenu-ScintillaNET

            var selectedText = editor.SelectedText;
            if (selectedText.Length > 0)
            {
                return selectedText;
            }

            string text = editor.Text;
            var regex = new Regex(searchPattern);

            var startPos = editor.CurrentPosition;

            //go forward
            int i = startPos;
            while (i >= 0 && i < text.Length)
            {
                if (!regex.IsMatch(text[i].ToString()))
                    break;
                i++;
            }

            var endPos = i;

            //go backward
            i = startPos;
            while (i > 0 && (i - 1) < text.Length)
            {
                if (!regex.IsMatch(text[i - 1].ToString()))
                    break;
                i--;
            }
            startPos = i;

            return GetSubString(startPos, endPos, text);
        }

        static string GetSubString(int start, int end, string text)
        {
            // https://github.com/Ahmad45123/AutoCompleteMenu-ScintillaNET

            if (string.IsNullOrEmpty(text))
                return "";
            if (start >= text.Length)
                return "";
            if (end > text.Length)
                return "";

            return text.Substring(start, end - start);
        }

        public static bool PartialMatchCi(string source, string partial) =>
            PartialMatch(source.ToLower(), partial.ToLower());

        public static bool PartialMatch(string source, string partial) =>
            MeasureSimilarity(source, partial) > 0;

        public static long MeasureSimilarityCi(string source, string partial) =>
            MeasureSimilarity(source.ToLower(), partial.ToLower());

        /// <summary>
        /// -1: not match
        ///  1: equal
        /// >1: the smaller the value, the more similar
        /// </summary>
        public static long MeasureSimilarity(string source, string partial)
        {
            if (string.IsNullOrEmpty(partial))
            {
                return 1;
            }

            if (string.IsNullOrEmpty(source))
            {
                return -1;
            }

            long marks = 1;

            var s = source;
            var p = partial;

            int idxS = 0, idxP = 0;
            int lenS = s.Length, lenP = p.Length;
            while (idxS < lenS && idxP < lenP)
            {
                if (s[idxS] == p[idxP])
                {
                    idxP++;
                }
                else
                {
                    marks += lenP - idxP;
                }
                idxS++;
            }

            if (idxP != lenP)
            {
                return -1;
            }

            return marks;
        }

        public static string GetLinkPrefix(string shareLink)
        {
            var index = shareLink.IndexOf(@"://");
            if (index == -1)
            {
                return null;
            }

            var prefix = shareLink.Substring(0, index);
            return prefix.ToLower();
        }

        public static Models.Datas.Enums.LinkTypes DetectLinkType(
            string shareLink)
        {
            var unknow = Models.Datas.Enums.LinkTypes.unknow;
            var prefix = GetLinkPrefix(shareLink);
            if (!string.IsNullOrEmpty(prefix)
                && Enum.TryParse(prefix, out Models.Datas.Enums.LinkTypes linkType))
            {
                return linkType;
            }
            return unknow;
        }

        /// <summary>
        /// regex = @"(?&lt;groupName>pattern)"
        /// <para>return string.Empty if sth. goes wrong</para>
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="pattern"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string ExtractStringWithPattern(string groupName, string pattern, string content)
        {
            var ptnStr = string.Format(@"(?<{0}>{1})", groupName, pattern);
            Regex rgx = new Regex(ptnStr);
            Match match = rgx.Match(content ?? string.Empty);
            if (match.Success)
            {
                return match.Groups[groupName].Value;
            }
            return string.Empty;
        }

        #endregion

        #region numbers
        public static bool TryParseEnum<TEnum>(int value, out TEnum outEnum)
            where TEnum : struct
        {
            outEnum = (TEnum)(object)value;
            if (Enum.IsDefined(typeof(TEnum), value))
            {

                return true;
            }
            return false;
        }

        public static int Str2Int(string value)
        {
            if (float.TryParse(value, out float f))
            {
                return (int)Math.Round(f);
            };
            return 0;
        }

        public static bool AreEqual(double left, double right)
        {
            return Math.Abs(left - right) < Models.Consts.Config.FloatPointNumberTolerance;
        }

        public static long SpeedtestMean(long left, long right, double weight) =>
            (long)SpeedtestMean((double)left, (double)right, weight);

        public static double SpeedtestMean(double left, double right, double weight)
        {
            if (weight <= 0 || weight >= 1)
            {
                throw new ArgumentOutOfRangeException("weight should between 0 to 1");
            }

            if (left <= 0 || right <= 0)
            {
                return Math.Max(left, right);
            }

            /*
             * 预期：
             * 由于最后一次测速服务器速度已经稳定，很有价值。
             * 而首次测速通常是没有缓存的，对分析服务器的速度也很重要，
             * 中间测速结果的重要程度则随测速次数递增。
             * 
             * 假设：
             * 连续做三次速度测试，权重为0.6，
             * 将三次速度测试迭代进这个求平均函数中将得到：
             * first * 0.6 * 0.6 + second * 0.6 * 0.4 + third * 0.4;
             * 即 first * 0.36 + second * 0.24 + third * 0.4;
             * 可见首次和末次测速结果占比较重，中间那次占比较低，符合预期。
             * 
             * 测试3至6次结果如下，均符合预期
             * 3    36% 24% 40%         
             * 4    22% 14% 24% 40%     
             * 5    13%  9% 14% 24% 40% 
             * 6     8%  5%  9% 14% 24% 40%
             * 
             * p.s.我不会告诉你，其实是因为我懒得写个列表存中间结果才这么搞的。
             */

            return left * weight + right * (1 - weight);
        }

        public static int GetLenInBitsOfInt(int value)
        {
            var k = 0;
            while (value > 0)
            {
                value = value >> 1;
                k++;
            }
            return value < 0 ? -1 : k;
        }

        #endregion

        #region Misc
        public static string GetLuaModuleName(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            var appDir = GetAppDir();
            if (fullPath.StartsWith(appDir))
            {
                fullPath = fullPath.Substring(appDir.Length);
            }

            var mn = fullPath.Replace("\\", ".").Replace("/", ".");
            while (mn != null && mn.StartsWith("."))
            {
                mn = mn.Substring(1);
            }

            if (mn != null && mn.ToLower().EndsWith(".lua"))
            {
                mn = mn.Substring(0, mn.Length - ".lua".Length);
            }

            return mn;
        }

        public static bool IsImportResultSuccess(string[] result) =>
           result[3] == Apis.Models.Consts.Import.MarkImportSuccess;

        public static void TrimDownConcurrentQueue<T>(
            ConcurrentQueue<T> queue,
            int maxLines,
            int minLines)
        {
            var count = queue.Count();
            if (maxLines < 1 || count < maxLines)
            {
                return;
            }

            var loop = Clamp(count - minLines, 0, count);
            var blackHole = default(T);
            for (int i = 0; i < loop; i++)
            {
                queue.TryDequeue(out blackHole);
            }
        }

        public static bool IsHttpLink(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return false;
            }
            if (link.ToLower().StartsWith("http"))
            {
                return true;
            }
            return false;
        }

        public static string RelativePath2FullPath(string path)
        {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            {
                return path;
            }

            var appDir = GetAppDir();
            return Path.Combine(appDir, path);
        }

        public static string CopyFromClipboard()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch { }
            return string.Empty;
        }

        public static bool CopyToClipboard(string content)
        {
            try
            {
                Clipboard.SetText(content);
                return true;
            }
            catch { }
            return false;
        }

        // Assembly location may change while app running.
        // So we should cache it when app starts.
        static string appDirCache = GenAppDir();
        static string GenAppDir()
        {
            // z:\vgc\libs\vgcapi.dll
            var vgcApiDllFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var parent = new DirectoryInfo(vgcApiDllFile).Parent;
            if (parent.Name == "libs")
            {
                parent = parent.Parent;
            }
            return parent.FullName;
        }

        public static string GetCoreFolderFullPath() =>
            Path.Combine(GetAppDir(), Models.Consts.Files.CoreFolderName);

        public static string GetAppDir() => appDirCache;

        /// <summary>
        /// min to max - 1
        /// </summary>
        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(Math.Min(value, max - 1), min);
        }

        static readonly Random randHexSource = new Random();
        public static string RandomHex(int length)
        {
            //  https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
            if (length <= 0)
            {
                return string.Empty;
            }

            const string chars = "0123456789abcdef";
            int charLen = chars.Length;

            int rndIndex;
            StringBuilder sb = new StringBuilder("");
            lock (randHexSource)
            {
                for (int i = 0; i < length; i++)
                {
                    rndIndex = randHexSource.Next(charLen);
                    sb.Append(chars[rndIndex]);
                }
            }
            return sb.ToString();
        }
        #endregion

        #region reflection

        static public string GetPublicFieldsInfoOfType(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Select(field =>
                {
                    var pf = field.IsStatic ? "Static " : "";
                    var tn = GetFriendlyTypeName(field.FieldType);
                    return $"{pf}{tn} {field.Name}";
                })
                .OrderBy(fn => fn);

            return string.Join("\n", fields);
        }

        static public string GetPublicMethodsInfoOfType(Type type)
        {
            List<string> staticMems = new List<string>();
            List<string> dynamicMems = new List<string>();
            List<string> allMems = new List<string>();

            var methods = type.GetMethods()
                .Where(m => m.IsPublic)
                .ToList();

            foreach (var method in methods)
            {
                var fn = GetFriendlyMethodDeclareInfo(method);
                if (method.IsStatic)
                {
                    staticMems.Add(fn);
                }
                else
                {
                    dynamicMems.Add(fn);
                }
            }

            staticMems.Sort();
            dynamicMems.Sort();
            allMems.AddRange(staticMems);
            allMems.AddRange(dynamicMems);

            return string.Join("\n", allMems);
        }

        static public List<Type> GetAllAssembliesType()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(t => t.GetTypes())
                .Where(t => t.IsClass)
                .ToList();
        }

        /// <summary>
        /// e.g. static void Sum&lt;int>(int a, int b)
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        static public string GetFriendlyMethodDeclareInfo(MethodInfo method)
        {
            var pms = method.GetParameters()
                .Select(arg =>
                {
                    var tn = GetFriendlyTypeName(arg.ParameterType);
                    var name = arg.Name;
                    return $"{tn} {name}";
                });

            var head = method.IsStatic ? @"Static " : @"";
            var rtt = GetFriendlyTypeName(method.ReturnType);
            var fn = GetFriendlyMethodName(method);
            var args = string.Join(@", ", pms);
            return $"{head}{rtt} {fn}({args})";
        }

        static public string GetFriendlyMethodName(MethodInfo method)
        {
            var name = method.Name;
            if (!method.IsGenericMethod)
            {
                return name;
            }

            var args = method
                .GetGenericArguments()
                .Select(arg => GetFriendlyTypeName(arg));

            return $"{name}<{string.Join(@", ", args)}>";
        }

        public static string GetFriendlyTypeName(Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyTypeName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName;
        }

        static public List<Tuple<string, string>> GetPublicPropsInfoOfType(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Select(field =>
                {
                    var tn = GetFriendlyTypeName(field.PropertyType);
                    return new Tuple<string, string>(tn, field.Name);
                })
                .ToList();

        static public List<Tuple<string, string>> GetPublicEventsInfoOfType(Type type) =>
            type.GetEvents(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Select(field =>
                {
                    var tn = GetFriendlyTypeName(field.EventHandlerType);
                    return new Tuple<string, string>(tn, field.Name);
                })
                .ToList();


        /// <summary>
        /// [0: ReturnType 1: MethodName 2: ParamsStr 3: ParamsWithType]
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<Tuple<string, string, string, string>> GetPublicMethodNameAndParam(Type type)
        {
            var fullNames = new List<Tuple<string, string, string, string>>();
            var methods = type.GetMethods();
            foreach (var method in methods)
            {
                if (!method.IsPublic)
                {
                    continue;
                }
                var name = method.Name;
                if (name.StartsWith("add_On") || name.StartsWith("remove_On"))
                {
                    continue;
                }
                var paramStrs = GenParamStr(method);
                var returnType = GetFriendlyTypeName(method.ReturnType);
                fullNames.Add(
                    new Tuple<string, string, string, string>(
                        returnType, name, paramStrs.Item1, paramStrs.Item2));
            }
            return fullNames;
        }

        static Tuple<string, string> GenParamStr(System.Reflection.MethodInfo methodInfo)
        {
            var fullParamList = new List<string>();
            var paramList = new List<string>();

            foreach (var paramInfo in methodInfo.GetParameters())
            {

                fullParamList.Add(
                    paramInfo.ParameterType.Name +
                    " " +
                    paramInfo.Name);

                paramList.Add(paramInfo.Name);
            }

            return new Tuple<string, string>(
                string.Join(@", ", paramList),
                string.Join(@", ", fullParamList));
        }

        public static List<string> GetPublicMethodNames(Type type)
        {
            var exceptList = new List<string>
            {
                "add_OnPropertyChanged",
                "remove_OnPropertyChanged",
            };

            var methodsName = new List<string>();
            var methods = type.GetMethods();
            foreach (var method in type.GetMethods())
            {
                var name = method.Name;
                if (method.IsPublic && !exceptList.Contains(name))
                {
                    methodsName.Add(name);
                }
            }
            return methodsName;
        }
        #endregion
    }
}
