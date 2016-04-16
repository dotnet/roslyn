// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SyntaxTreeIdentifierInfo : AbstractSyntaxTreeInfo
    {
        public static bool TryGetIdentifierLocations(Document document, VersionStamp version, string identifier, List<int> positions, CancellationToken cancellationToken)
        {
            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();
            using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
            {
                var esentStorage = storage as ISyntaxTreeInfoPersistentStorage;
                if (esentStorage == null)
                {
                    // basically, we don't support it. return true so that we don't try to precalculate it
                    return false;
                }

                return esentStorage.ReadIdentifierPositions(document, version, identifier, positions, cancellationToken);
            }
        }

        public static async Task<bool> IdentifierSetPrecalculatedAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();

            using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
            {
                var esentStorage = storage as ISyntaxTreeInfoPersistentStorage;
                if (esentStorage == null)
                {
                    // basically, we don't support it. return true so that we don't try to precalculate it
                    return true;
                }

                var persistedVersion = esentStorage.GetIdentifierSetVersion(document);
                return document.CanReusePersistedSyntaxTreeVersion(syntaxVersion, persistedVersion);
            }
        }

        public static async Task SaveIdentifierSetAsync(Document document, CancellationToken cancellationToken)
        {
            Contract.Requires(document.IsFromPrimaryBranch());

            var persistentStorageService = document.Project.Solution.Workspace.Services.GetService<IPersistentStorageService>();

            using (var storage = persistentStorageService.GetStorage(document.Project.Solution))
            {
                var esentStorage = storage as ISyntaxTreeInfoPersistentStorage;
                if (esentStorage == null)
                {
                    return;
                }

                var version = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                esentStorage.WriteIdentifierLocations(document, version, root, cancellationToken);
            }
        }
    }
}
