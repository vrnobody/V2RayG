namespace V2RayG.Models.Datas
{
    public class ImportSharelinkOptions
    {

        public bool IsImportTrojanShareLink { get; set; }

        public bool IsImportSsShareLink { get; set; }
        public bool IsInjectGlobalImport { get; set; }

        public int Mode { get; set; } // Models.Datas.Enum.ProxyTypes
        public string Ip { get; set; }
        public int Port { get; set; }

        public ImportSharelinkOptions()
        {
            IsImportTrojanShareLink = false;
            IsImportSsShareLink = false;
            IsInjectGlobalImport = false;

            Mode = (int)Apis.Models.Datas.Enums.ProxyTypes.HTTP;
            Ip = Apis.Models.Consts.Webs.LoopBackIP;
            Port = Apis.Models.Consts.Webs.DefaultProxyPort;
        }

        public bool Equals(ImportSharelinkOptions target)
        {
            if (target == null
                || IsImportTrojanShareLink != target.IsImportTrojanShareLink
                || IsImportSsShareLink != target.IsImportSsShareLink
                || IsInjectGlobalImport != target.IsInjectGlobalImport
                || Mode != target.Mode
                || !Ip.Equals(target.Ip)
                || Port != target.Port)
            {
                return false;
            }
            return true;
        }
    }
}
