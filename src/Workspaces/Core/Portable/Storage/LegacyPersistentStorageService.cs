// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// Implements public <see cref="IPersistentStorageService"/>.
    /// Internally, it is preferred to work directly with <see cref="IChecksummedPersistentStorageService"/>,
    /// which can be retrieved by <see cref="PersistentStorageExtensions.GetPersistentStorageService(HostWorkspaceServices, StorageDatabase)"/>.
    /// </summary>
    internal sealed class LegacyPersistentStorageService : IPersistentStorageService
    {
        [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService)), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new LegacyPersistentStorageService(workspaceServices);
        }

        private readonly HostWorkspaceServices _workspaceServices;

        public LegacyPersistentStorageService(HostWorkspaceServices workspaceServices)
            => _workspaceServices = workspaceServices;

        public IPersistentStorage GetStorage(Solution solution)
            => GetStorageAsync(solution, CancellationToken.None).AsTask().GetAwaiter().GetResult();

        public async ValueTask<IPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken)
            => await _workspaceServices.GetPersistentStorageService(solution.Options).
                GetStorageAsync(SolutionKey.ToSolutionKey(solution), checkBranchId: true, cancellationToken).ConfigureAwait(false);
    }
}
