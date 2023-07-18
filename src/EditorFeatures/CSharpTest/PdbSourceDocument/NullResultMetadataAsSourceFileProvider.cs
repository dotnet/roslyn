// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument
{
    /// <summary>
    /// IMetadataAsSourceFileService has to always return a result, but for our testing
    /// we remove the decompilation provider that would normally ensure that. This provider
    /// takes it place to ensure we always return a known null result, so we can also verify
    /// against it in tests.
    /// </summary>
    [ExportMetadataAsSourceFileProvider("Dummy"), Shared]
    [ExtensionOrder(After = PdbSourceDocumentMetadataAsSourceFileProvider.ProviderName)]
    internal class NullResultMetadataAsSourceFileProvider : IMetadataAsSourceFileProvider
    {
        // Represents a null result
        public static MetadataAsSourceFile NullResult = new("", null, null, null);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NullResultMetadataAsSourceFileProvider()
        {
        }

        public void CleanupGeneratedFiles(MetadataAsSourceWorkspace workspace)
        {
        }

        public Task<MetadataAsSourceFile?> GetGeneratedFileAsync(MetadataAsSourceWorkspace metadataWorkspace, Workspace sourceWorkspace, Project sourceProject, ISymbol symbol, bool signaturesOnly, MetadataAsSourceOptions options, string tempPath, TelemetryMessage? telemetry, CancellationToken cancellationToken)
        {
            return Task.FromResult<MetadataAsSourceFile?>(NullResult);
        }

        public Project? MapDocument(Document document)
        {
            return null;
        }

        public bool TryAddDocumentToWorkspace(MetadataAsSourceWorkspace workspace, string filePath, Text.SourceTextContainer sourceTextContainer)
        {
            return true;
        }

        public bool TryRemoveDocumentFromWorkspace(MetadataAsSourceWorkspace workspace, string filePath)
        {
            return true;
        }

        public bool ShouldCollapseOnOpen(MetadataAsSourceWorkspace workspace, string filePath, BlockStructureOptions options)
        {
            return true;
        }
    }
}
