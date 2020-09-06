// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public IPersistentStorage GetStorage(Solution solution, bool checkBranchId)
            => NoOpPersistentStorage.Instance;

        public IPersistentStorage GetStorage(Workspace workspace, SolutionKey solutionKey, bool checkBranchId)
            => NoOpPersistentStorage.Instance;

        IChecksummedPersistentStorage IChecksummedPersistentStorageService.GetStorage(Solution solution)
            => NoOpPersistentStorage.Instance;

        IChecksummedPersistentStorage IChecksummedPersistentStorageService.GetStorage(Solution solution, bool checkBranchId)
            => NoOpPersistentStorage.Instance;

        IChecksummedPersistentStorage IChecksummedPersistentStorageService.GetStorage(Workspace workspace, SolutionKey solutionKey, bool checkBranchId)
            => NoOpPersistentStorage.Instance;
    }
}
