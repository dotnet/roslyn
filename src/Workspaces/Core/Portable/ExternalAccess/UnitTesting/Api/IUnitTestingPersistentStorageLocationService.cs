// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
