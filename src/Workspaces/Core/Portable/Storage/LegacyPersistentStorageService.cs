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
    /// Obsolete.  Roslyn no longer supports a mechanism to perform arbitrary persistence of data.  If such functionality
    /// is needed, consumers are responsible for providing it themselves with whatever semantics are needed.
    /// </summary>
    [Obsolete("Roslyn no longer exports a mechanism to perform persistence.", error: true)]
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
                => new LegacyPersistentStorageService();
        }

        public LegacyPersistentStorageService()
        {
        }

        public IPersistentStorage GetStorage(Solution solution)
            => NoOpPersistentStorage.GetOrThrow(throwOnFailure: false);
    }
}
