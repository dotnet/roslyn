using System;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface IUnitTestingPersistentStorageLocationService
    {
        event EventHandler<UnitTestingPersistentStorageLocationChangingEventArgsWrapper> StorageLocationChanging;
        bool IsSupported(Workspace workspace);
        string TryGetStorageLocation(SolutionId solutionId);
    }
}
