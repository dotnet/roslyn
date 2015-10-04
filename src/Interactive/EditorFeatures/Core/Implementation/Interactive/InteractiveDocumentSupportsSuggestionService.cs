// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.SuggestionSupport;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    [ExportWorkspaceService(typeof(IDocumentSupportsSuggestionService), WorkspaceKind.Interactive), Shared]
    internal sealed class InteractiveDocumentSupportsCodeFixService : IDocumentSupportsSuggestionService
    {
        public bool SupportsCodeFixes(Document document)
        {
            // TODO (acasey): confirm with IDE team
            var project = document.Project;
            var projectIds = project.Solution.ProjectIds;
            return project.DocumentIds[0] == document.Id && projectIds[projectIds.Count - 1] == project.Id;
        }

        public bool SupportsRefactorings(Document document)
        {
            return false;
        }

        public bool SupportsRename(Document document)
        {
            return false;
        }
    }
}
