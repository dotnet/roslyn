// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(IMetadataCacheService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerMetadataCacheService(ServerConfiguration serverConfiguration) : IMetadataCacheService
{
    private readonly SharedMetadataCache? _metadataCache = serverConfiguration.UseSharedMetadataCache
        ? new SharedMetadataCache()
        : null;

    public bool TryGetMetadata(
        string resolvedPath,
        MetadataImageKind kind,
        [NotNullWhen(true)] out Metadata? metadata)
    {
        if (_metadataCache is null)
        {
            metadata = null;
            return false;
        }

        metadata = _metadataCache.GetMetadata(resolvedPath, kind);
        return true;
    }
}
