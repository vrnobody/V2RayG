using System.Diagnostics;

namespace Apis.Tests.IComponentOfTests
{
    public class CompLv1 :
        Apis.BaseClasses.ComponentOf<Container>
    {
        public CompLv1() { }

        public string Name() => "Component lv1";

        protected override void CleanupAfterChildrenDisposed()
        {
            Debug.WriteLine("Comp lv1 disposed.");
        }
    }
}
