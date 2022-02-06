namespace Apis.Models.Datas
{
    public class Enums
    {
        public enum PackageTypes
        {
            Chain,
            Balancer,
        }

        public enum BalancerStrategies
        {
            Random = 0,
            LeastPing = 1,
            LeastLoad = 2,
        }

        public enum ModifierKeys
        {
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }

        public enum ShutdownReasons
        {
            Undefined,  // default
            CloseByUser,  // close by user
            Poweroff, // system shut down
            Abort, // attacked by aliens :>
        }

        public enum LinkTypes
        {
            vmess = 0,
            v2cfg = 1,
            ss = 2,
            http = 3,
            https = 4,
            v = 5,
            trojan = 6,
            vless = 7,
            unknow = 256, // for enum parse
        }

        /// <summary>
        /// Inbound types
        /// </summary>
        public enum ProxyTypes
        {
            Config = 0,
            HTTP = 1,
            SOCKS = 2,
            Custom = 3,
        }

        public enum FormLocations
        {
            TopLeft,
            BottomLeft,
            TopRight,
            BottomRight,
        }

        public enum SaveFileErrorCode
        {
            Fail,
            Cancel,
            Success,
        }
    }
}
