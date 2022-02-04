using System.Collections.Generic;
using V2RayG.Resources.Resx;

namespace V2RayG.Models.Datas
{
    class Table
    {
        public static readonly Dictionary<Models.Datas.Enums.Cultures, string> Cultures = new Dictionary<Enums.Cultures, string>
        {
            { Enums.Cultures.auto,"auto" },
            { Enums.Cultures.enUS,"en" },
            { Enums.Cultures.zhCN,"cn" },
        };

        public static readonly string[] EnviromentVariablesName = new string[] {
            "V2RAY_BUF_READV",
            "V2RAY_LOCATION_ASSET",
            "V2RAY_LOCATION_CONFDIR",
            "V2RAY_LOCATION_CONFIG",
            "V2RAY_RAY_BUFFER_SIZE",
        };

        public static readonly string[] customInbTypeNames = new string[] {
            "config",
            "http",
            "socks",
            "custom",
        };
    }
}
