// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(IMetadataProviderService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerMetadataProviderService(ServerConfiguration serverConfiguration)
    : AbstractMetadataProviderService
{
    private readonly SharedMetadataCache? _metadataCache = serverConfiguration.UseSharedMetadataCache
        ? new SharedMetadataCache()
        : null;

    public override MetadataProviderResult GetMetadata(string resolvedPath, MetadataImageKind kind)
        => _metadataCache is null
            ? base.GetMetadata(resolvedPath, kind)
            : _metadataCache.GetMetadata(resolvedPath, kind, GetMetadataFromBase);

    private MetadataProviderResult GetMetadataFromBase(string resolvedPath, MetadataImageKind kind)
        => base.GetMetadata(resolvedPath, kind);
}
