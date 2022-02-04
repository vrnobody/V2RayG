using System;

namespace Apis.Interfaces.ComponentOf
{
    // has container
    internal interface IChildComponent : IDisposable
    {
        void SetParent(object parent);
    }
}
