// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.SuggestionSupport;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SuggestionService
{
    [ExportWorkspaceService(typeof(IDocumentSupportsSuggestionService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioDocumentSupportsCodeFixService : IDocumentSupportsSuggestionService
    {
        public bool SupportsCodeFixes(Document document)
        {
            return GetContainedDocument(document) == null;
        }

        public bool SupportsRefactorings(Document document)
        {
            return GetContainedDocument(document) == null;
        }

        public bool SupportsRename(Document document)
        {
            var containedDocument = GetContainedDocument(document);
            return containedDocument == null || containedDocument.SupportsRename;
        }

        public bool SupportsGoToNextPreviousMethod(Document document)
        {
            return GetContainedDocument(document) == null;
        }

        private static ContainedDocument GetContainedDocument(Document document)
        {
            var visualStudioWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
            if (visualStudioWorkspace == null)
            {
                return null;
            }

            return visualStudioWorkspace.GetHostDocument(document.Id) as ContainedDocument;
        }
    }
}
