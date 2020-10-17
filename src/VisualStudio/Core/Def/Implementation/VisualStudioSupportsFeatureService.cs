﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public VisualStudioTextBufferSupportsFeatureService()
            {
            }

            public bool SupportsCodeFixes(ITextBuffer textBuffer)
                => SupportsCodeFixesWorker(GetContainedDocumentId(textBuffer));

            public bool SupportsRefactorings(ITextBuffer textBuffer)
                => SupportsRefactoringsWorker(GetContainedDocumentId(textBuffer));

            public bool SupportsRename(ITextBuffer textBuffer)
            {
                var sourceTextContainer = textBuffer.AsTextContainer();
                if (Workspace.TryGetWorkspace(sourceTextContainer, out var workspace))
                {
                    var documentId = workspace.GetDocumentIdInCurrentContext(sourceTextContainer);
                    return SupportsRenameWorker(workspace.CurrentSolution.GetRelatedDocumentIds(documentId));
                }

                return false;
            }

            public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer)
                => SupportsNavigationToAnyPositionWorker(GetContainedDocumentId(textBuffer));

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
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public VisualStudioDocumentSupportsFeatureService()
            {
            }

            public bool SupportsCodeFixes(Document document)
                => SupportsCodeFixesWorker(document.Id);

            public bool SupportsRefactorings(Document document)
                => SupportsRefactoringsWorker(document.Id);

            public bool SupportsRename(Document document)
                => SupportsRenameWorker(document.Project.Solution.GetRelatedDocumentIds(document.Id));

            public bool SupportsNavigationToAnyPosition(Document document)
                => SupportsNavigationToAnyPositionWorker(document.Id);
        }

        private static bool SupportsCodeFixesWorker(DocumentId id)
            => ContainedDocument.TryGetContainedDocument(id) == null;

        private static bool SupportsRefactoringsWorker(DocumentId id)
            => ContainedDocument.TryGetContainedDocument(id) == null;

        private static bool SupportsRenameWorker(ImmutableArray<DocumentId> ids)
        {
            return ids.Select(id => ContainedDocument.TryGetContainedDocument(id))
                    .All(cd => cd == null || cd.SupportsRename);
        }

        private static bool SupportsNavigationToAnyPositionWorker(DocumentId id)
            => ContainedDocument.TryGetContainedDocument(id) == null;
    }
}
