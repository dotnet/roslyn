// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SuggestionService
{
    [ExportWorkspaceService(typeof(ITextBufferSupportsFeatureService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioTextBufferSupportsFeatureService : ITextBufferSupportsFeatureService
    {
        public bool SupportsCodeFixes(ITextBuffer textBuffer)
        {
            return GetContainedDocument(textBuffer) == null;
        }

        public bool SupportsRefactorings(ITextBuffer textBuffer)
        {
            return GetContainedDocument(textBuffer) == null;
        }

        public bool SupportsRename(ITextBuffer textBuffer)
        {
            var sourceTextContainer = textBuffer.AsTextContainer();
            if (Workspace.TryGetWorkspace(sourceTextContainer, out var workspace))
            {
                return workspace.GetRelatedDocumentIds(sourceTextContainer)
                    .Select(id => ContainedDocument.TryGetContainedDocument(id))
                    .All(cd => cd == null || cd.SupportsRename);
            }

            return false;
        }

        public bool SupportsNavigationToAnyPosition(ITextBuffer textBuffer)
        {
            return GetContainedDocument(textBuffer) == null;
        }

        private static ContainedDocument GetContainedDocument(ITextBuffer textBuffer)
        {
            var sourceTextContainer = textBuffer.AsTextContainer();
            if (Workspace.TryGetWorkspace(sourceTextContainer, out var workspace)
                && workspace is VisualStudioWorkspaceImpl vsWorkspace)
            {
                var id = vsWorkspace.GetDocumentIdInCurrentContext(sourceTextContainer);
                if (id != null)
                {
                    return ContainedDocument.TryGetContainedDocument(id);
                }
            }

            return null;
        }
    }
}
