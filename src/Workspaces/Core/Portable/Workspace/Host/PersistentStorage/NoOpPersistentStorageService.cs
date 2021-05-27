// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Host
{
    internal class NoOpPersistentStorageService : IChecksummedPersistentStorageService
    {
#if DOTNET_BUILD_FROM_SOURCE
        public static readonly IPersistentStorageService Instance = new NoOpPersistentStorageService();
#else
        private static readonly IPersistentStorageService Instance = new NoOpPersistentStorageService();
#endif

        private NoOpPersistentStorageService()
        {
        }

        public static IPersistentStorageService GetOrThrow(OptionSet optionSet)
            => optionSet.GetOption(StorageOptions.DatabaseMustSucceed)
                ? throw new InvalidOperationException("Database was not supported")
                : Instance;

        public IPersistentStorage GetStorage(Solution solution)
            => NoOpPersistentStorage.GetOrThrow(solution.Options);

        public ValueTask<IPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.GetOrThrow(solution.Options));

        ValueTask<IChecksummedPersistentStorage> IChecksummedPersistentStorageService.GetStorageAsync(Solution solution, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.GetOrThrow(solution.Options));

        ValueTask<IChecksummedPersistentStorage> IChecksummedPersistentStorageService.GetStorageAsync(Solution solution, bool checkBranchId, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.GetOrThrow(solution.Options));

        ValueTask<IChecksummedPersistentStorage> IChecksummedPersistentStorageService.GetStorageAsync(Workspace workspace, SolutionKey solutionKey, bool checkBranchId, CancellationToken cancellationToken)
            => new(NoOpPersistentStorage.GetOrThrow(workspace.Options));
    }
}
