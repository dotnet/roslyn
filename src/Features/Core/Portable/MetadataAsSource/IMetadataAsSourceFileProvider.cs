// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal interface IMetadataAsSourceFileProvider
{
    /// <summary>
    /// Generates a file from metadata. Will be called under a lock to prevent concurrent access.
    /// </summary>
    Task<(MetadataAsSourceFile File, MetadataAsSourceFileMetadata FileMetadata)?> GetGeneratedFileAsync(
        MetadataAsSourceWorkspace metadataWorkspace,
        Workspace sourceWorkspace,
        Project sourceProject,
        ISymbol symbol,
        bool signaturesOnly,
        MetadataAsSourceOptions options,
        TelemetryMessage? telemetryMessage,
        IMetadataDocumentPersister persister,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called to clean up any state. Will be called under a lock to prevent concurrent access.
    /// </summary>
    void CleanupGeneratedFiles(MetadataAsSourceWorkspace workspace);
}
