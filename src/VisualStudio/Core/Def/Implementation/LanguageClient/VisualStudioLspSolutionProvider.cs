// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [Export(typeof(ILspSolutionProvider)), Shared]
    internal class VisualStudioLspSolutionProvider : ILspSolutionProvider
    {
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly MiscellaneousFilesWorkspace _miscellaneousFilesWorkspace;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioLspSolutionProvider(
            VisualStudioWorkspace visualStudioWorkspace,
            MiscellaneousFilesWorkspace miscellaneousFilesWorkspace,
            IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _visualStudioWorkspace = visualStudioWorkspace;
            _miscellaneousFilesWorkspace = miscellaneousFilesWorkspace;
            _metadataAsSourceFileService = metadataAsSourceFileService;
        }

        public Solution GetCurrentSolutionForMainWorkspace()
        {
            return _visualStudioWorkspace.CurrentSolution;
        }

        public ImmutableArray<Document> GetDocuments(Uri documentUri)
        {
            // First check the VS workspace for matching documents.
            var documents = _visualStudioWorkspace.CurrentSolution.GetDocuments(documentUri);
            if (!documents.IsEmpty)
            {
                return documents;
            }

            // If there's none in the VS workspace, then check the misc files workspace.
            documents = _miscellaneousFilesWorkspace.CurrentSolution.GetDocuments(documentUri);
            if (!documents.IsEmpty)
            {
                return documents;
            }

            // If there's none in the misc files workspace, then check the metadata files workspace.
            var metadataWorkspace = _metadataAsSourceFileService.TryGetWorkspace();
            if (metadataWorkspace != null)
            {
                documents = metadataWorkspace.CurrentSolution.GetDocuments(documentUri);
            }

            return documents;
        }
    }
}
