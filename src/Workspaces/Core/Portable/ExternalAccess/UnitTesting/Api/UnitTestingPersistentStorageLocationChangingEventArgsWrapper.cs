using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingPersistentStorageLocationChangingEventArgsWrapper
    {
        internal UnitTestingPersistentStorageLocationChangingEventArgsWrapper(
            PersistentStorageLocationChangingEventArgs underlyingObject)
            => UnderlyingObject = underlyingObject;

        internal PersistentStorageLocationChangingEventArgs UnderlyingObject { get; }
    }
}
