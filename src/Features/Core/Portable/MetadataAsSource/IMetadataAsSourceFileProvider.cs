// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal interface IMetadataAsSourceFileProvider
    {
        /// <summary>
        /// Generates a file from metadata. Will be called under a lock to prevent concurrent access.
        /// </summary>
        Task<MetadataAsSourceFile?> GetGeneratedFileAsync(
            MetadataAsSourceWorkspace metadataWorkspace, Workspace sourceWorkspace, Project sourceProject, ISymbol symbol, bool signaturesOnly, MetadataAsSourceOptions options, string tempPath, TelemetryMessage? telemetryMessage, CancellationToken cancellationToken);

        /// <summary>
        /// Called to clean up any state. Will be called under a lock to prevent concurrent access.
        /// </summary>
        void CleanupGeneratedFiles(MetadataAsSourceWorkspace workspace);

        /// <summary>
        /// Called when the file returned from <see cref="GetGeneratedFileAsync"/> needs to be added to the workspace,
        /// to be opened.  Will be called on the main thread of the workspace host.
        /// </summary>
        bool TryAddDocumentToWorkspace(MetadataAsSourceWorkspace workspace, string filePath, SourceTextContainer sourceTextContainer);

        /// <summary>
        /// Called when the file is being closed, and so needs to be removed from the workspace.  Will be called on the
        /// main thread of the workspace host.
        /// </summary>
        bool TryRemoveDocumentFromWorkspace(MetadataAsSourceWorkspace workspace, string filePath);

        /// <summary>
        /// Called to determine if the file should be collapsed by default when opened for the first time.  Will be
        /// called on the main thread of the workspace host.
        /// </summary>
        bool ShouldCollapseOnOpen(MetadataAsSourceWorkspace workspace, string filePath, BlockStructureOptions blockStructureOptions);

        /// <summary>
        /// Maps from a document to its project for the purposes of symbol mapping via <see cref="ISymbolMappingService"/>
        /// </summary>
        Project? MapDocument(Document document);
    }
}
