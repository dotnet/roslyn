// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Persistence
{
    [ExportWorkspaceService(typeof(IPersistentStorageService), "Test"), Shared]
    public class TestPersistenceService : IPersistentStorageService2
    {
        [ImportingConstructor]
        public TestPersistenceService()
        {
        }

        public IPersistentStorage GetStorage(Solution solution)
            => NoOpPersistentStorage.Instance;

        public IPersistentStorage GetStorage(Solution solution, bool checkBranchId)
            => NoOpPersistentStorage.Instance;
    }
}
