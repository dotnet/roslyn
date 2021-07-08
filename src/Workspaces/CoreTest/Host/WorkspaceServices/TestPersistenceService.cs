// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Persistence
{
    [ExportWorkspaceService(typeof(IPersistentStorageService), "Test"), Shared]
    public class TestPersistenceService : IPersistentStorageService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestPersistenceService()
        {
        }

        public IPersistentStorage GetStorage(Solution solution)
            => NoOpPersistentStorage.GetOrThrow(solution.Options);

        public ValueTask<IPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.GetOrThrow(solution.Options));
    }
}
