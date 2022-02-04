using System.Diagnostics;

namespace Apis.Tests.IComponentOfTests
{
    public class CompLv2 :
        Apis.BaseClasses.ComponentOf<CompLv1>
    {

        public CompLv2() { }

        public string Name() => "Comp lv2";

        protected override void CleanupAfterChildrenDisposed()
        {
            Debug.WriteLine("Comp lv2 disposed.");
        }
    }
}
