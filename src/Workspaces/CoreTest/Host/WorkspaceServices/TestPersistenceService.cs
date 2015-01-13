// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Persistence
{
    [ExportWorkspaceService(typeof(IPersistentStorageService), "Test"), Shared]
    public class TestPersistenceService : IPersistentStorageService
    {
        private readonly IPersistentStorage storage = new NoOpPersistentStorage();

        IPersistentStorage IPersistentStorageService.GetStorage(Solution solution)
        {
            return storage;
        }
    }
}
