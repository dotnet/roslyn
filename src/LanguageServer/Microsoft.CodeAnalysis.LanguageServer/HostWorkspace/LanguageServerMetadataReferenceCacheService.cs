// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(IMetadataReferenceCacheService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LanguageServerMetadataReferenceCacheService()
    : IMetadataReferenceCacheService
{
    private readonly SharedMetadataReferenceCache _referenceCache = new();

    public PortableExecutableReference GetReference(
        string resolvedPath,
        MetadataReferenceProperties properties,
        Func<string, MetadataReferenceProperties, PortableExecutableReference> createReference)
        => _referenceCache.GetReference(resolvedPath, properties, createReference);
}
