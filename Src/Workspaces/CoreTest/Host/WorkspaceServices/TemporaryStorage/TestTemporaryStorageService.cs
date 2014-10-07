// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.TemporaryStorage
{
    [ExportWorkspaceService(typeof(ITemporaryStorageService), "Test"), Shared]
    internal class TestTemporaryStorageService : ITemporaryStorageService
    {
        public ITemporaryStorage CreateTemporaryStorage(CancellationToken cancellationToken)
        {
            return new TestTemporaryStorage();
        }
    }
}
