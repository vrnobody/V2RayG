using System.Diagnostics;

namespace Apis.Tests.IComponentOfTests
{
    public class Container :
        Apis.BaseClasses.ComponentOf<Container>
    {
        public Container() { }

        public string Name() => "Container";

        protected override void CleanupAfterChildrenDisposed()
        {
            Debug.WriteLine("Container disposed.");
        }

    }
}
