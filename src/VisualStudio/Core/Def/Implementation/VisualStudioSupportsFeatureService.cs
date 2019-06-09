// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SuggestionService
{
    internal sealed class VisualStudioSupportsFeatureService
    {
        [ExportWorkspaceService(typeof(ITextBufferSupportsFeatureService), ServiceLayer.Host), Shared]
        private class VisualStudioTextBufferSupportsFeatureService : ITextBufferSupportsFeatureService
        {
            [ImportingConstructor]
            public VisualStudioTextBufferSupportsFeatureService()
            {
            }

            public bool SupportsCodeFixes(ITextBuffer textBuffer)
            {
                return SupportsCodeFixesWorker(GetContainedDocumentId(textBuffer));
            }

            public bool SupportsRefactorings(ITextBuffer textBuffer)
            {
                return SupportsRefactoringsWorker(GetContainedDocumentId(textBuffer));
            }

            public bool SupportsRename(ITextBuffer textBuffer)
            {
                var sourceTextContainer = textBuffer.AsTextContainer();
                if (Workspace.TryGetWorkspace(sourceTextContainer, out var workspace))
                {
                    return SupportsRenameWorker(workspace.GetRelatedDocumentIds(sourceTextContainer).ToImmutableArray());
                }

                return false;
            }

            public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer)
            {
                return SupportsNavigationToAnyPositionWorker(GetContainedDocumentId(textBuffer));
            }

            private static DocumentId GetContainedDocumentId(ITextBuffer textBuffer)
            {
                var sourceTextContainer = textBuffer.AsTextContainer();
                if (Workspace.TryGetWorkspace(sourceTextContainer, out var workspace)
                    && workspace is VisualStudioWorkspaceImpl vsWorkspace)
                {
                    return vsWorkspace.GetDocumentIdInCurrentContext(sourceTextContainer);
                }

                return null;
            }
        }

        [ExportWorkspaceService(typeof(IDocumentSupportsFeatureService), ServiceLayer.Host), Shared]
        private class VisualStudioDocumentSupportsFeatureService : IDocumentSupportsFeatureService
        {
            [ImportingConstructor]
            public VisualStudioDocumentSupportsFeatureService()
            {
            }

            public bool SupportsCodeFixes(Document document)
            {
                return SupportsCodeFixesWorker(document.Id);
            }

            public bool SupportsRefactorings(Document document)
            {
                return SupportsRefactoringsWorker(document.Id);
            }

            public bool SupportsRename(Document document)
            {
                return SupportsRenameWorker(document.Project.Solution.GetRelatedDocumentIds(document.Id));
            }

            public bool SupportsNavigationToAnyPosition(Document document)
            {
                return SupportsNavigationToAnyPositionWorker(document.Id);
            }
        }

        private static bool SupportsCodeFixesWorker(DocumentId id)
        {
            return ContainedDocument.TryGetContainedDocument(id) == null;
        }

        private static bool SupportsRefactoringsWorker(DocumentId id)
        {
            return ContainedDocument.TryGetContainedDocument(id) == null;
        }

        private static bool SupportsRenameWorker(ImmutableArray<DocumentId> ids)
        {
            return ids.Select(id => ContainedDocument.TryGetContainedDocument(id))
                    .All(cd => cd == null || cd.SupportsRename);
        }

        private static bool SupportsNavigationToAnyPositionWorker(DocumentId id)
        {
            return ContainedDocument.TryGetContainedDocument(id) == null;
        }
    }
}
