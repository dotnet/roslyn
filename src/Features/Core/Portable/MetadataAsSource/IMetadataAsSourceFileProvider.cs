// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal interface IMetadataAsSourceFileProvider
    {
        /// <summary>
        /// Generates a file from metadata. Will be called under a lock to prevent concurrent access.
        /// </summary>
        Task<MetadataAsSourceFile?> GetGeneratedFileAsync(Workspace workspace, Project project, ISymbol symbol, bool signaturesOnly, MetadataAsSourceOptions options, string tempPath, CancellationToken cancellationToken);

        /// <summary>
        /// Called when the file returned from <see cref="GetGeneratedFileAsync(Workspace, Project, ISymbol, bool, MetadataAsSourceOptions, string, CancellationToken)"/>
        /// needs to be added to the workspace, to be opened. Will be called under a lock to prevent concurrent access.
        /// </summary>
        bool TryAddDocumentToWorkspace(Workspace workspace, string filePath, SourceTextContainer sourceTextContainer);

        /// <summary>
        /// Called when the file is being closed, and so needs to be removed from the workspace.
        /// Will be called under a lock to prevent concurrent access.
        /// </summary>
        bool TryRemoveDocumentFromWorkspace(Workspace workspace, string filePath);

        /// <summary>
        /// Called to clean up any state. Will be called under a lock to prevent concurrent access.
        /// </summary>
        void CleanupGeneratedFiles(Workspace? workspace);

        /// <summary>
        /// Maps from a document to its project for the purposes of symbol mapping via <see cref="ISymbolMappingService"/>
        /// </summary>
        Project? MapDocument(Document document);
    }
}
