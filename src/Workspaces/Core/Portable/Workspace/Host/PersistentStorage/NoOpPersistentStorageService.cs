// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.Host
{
    internal class NoOpPersistentStorageService : IChecksummedPersistentStorageService
    {
        public static readonly IPersistentStorageService Instance = new NoOpPersistentStorageService();

        private NoOpPersistentStorageService()
        {
        }

        public IPersistentStorage GetStorage(Solution solution)
            => NoOpPersistentStorage.Instance;

        public ValueTask<IPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.Instance);

        ValueTask<IChecksummedPersistentStorage> IChecksummedPersistentStorageService.GetStorageAsync(Solution solution, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.Instance);

        ValueTask<IChecksummedPersistentStorage> IChecksummedPersistentStorageService.GetStorageAsync(Solution solution, bool checkBranchId, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.Instance);

        ValueTask<IChecksummedPersistentStorage> IChecksummedPersistentStorageService.GetStorageAsync(Workspace workspace, SolutionKey solutionKey, bool checkBranchId, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.Instance);
    }
}
