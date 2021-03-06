using Newtonsoft.Json.Linq;

namespace Apis.Models.Datas
{
    public class CoreServSettings
    {
        public string serverName, serverDescription, inboundAddress, mark, remark;
        public int inboundMode;
        public double index;
        public bool isAutorun, isUntrack, isGlobalImport;

        public CoreServSettings()
        {
            var em = string.Empty;

            serverName = em;
            serverDescription = em;
            inboundAddress = em;
            mark = em;
            remark = em;
            index = 0;
            inboundMode = 0;
            isAutorun = false;
            isUntrack = false;
            isGlobalImport = false;
        }

        public CoreServSettings(Apis.Interfaces.ICoreServCtrl coreServ) :
            this()
        {
            var cs = coreServ.GetCoreStates();

            index = cs.GetIndex();
            mark = cs.GetMark();
            remark = cs.GetRemark();
            isAutorun = cs.IsAutoRun();
            isUntrack = cs.IsUntrack();
            isGlobalImport = cs.IsInjectGlobalImport();
            inboundMode = cs.GetInboundType();
            inboundAddress = cs.GetInboundAddr();

            try
            {
                var ccfg = coreServ.GetConfiger();
                var cfg = ccfg.GetConfig();
                var json = JObject.Parse(cfg);
                var GetStr = Misc.Utils.GetStringByKeyHelper(json);
                serverName = GetStr("v2rayg.alias");
                serverDescription = GetStr("v2rayg.description");
            }
            catch { }
        }

        public override bool Equals(object target)
        {
            if (target == null || !(target is CoreServSettings))
            {
                return false;
            }

            var t = target as CoreServSettings;
            if (t.serverName != serverName
                || (int)t.index != (int)index
                || t.serverDescription != serverDescription
                || t.inboundAddress != inboundAddress
                || t.mark != mark
                || t.remark != remark
                || t.inboundMode != inboundMode
                || t.isAutorun != isAutorun
                || t.isUntrack != isUntrack
                || t.isGlobalImport != isGlobalImport)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
