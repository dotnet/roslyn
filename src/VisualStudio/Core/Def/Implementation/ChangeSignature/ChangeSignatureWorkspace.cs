// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editor.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class ChangeSignatureWorkspace : Workspace
    {
        public ChangeSignatureWorkspace(Solution solution, Project project)
            : base(project.Solution.Workspace.Services.HostServices, nameof(ChangeSignatureWorkspace))
        {
            // The solution we are handed is still parented by the original workspace. We want to
            // inherit it's "no partial solutions" flag so that way this workspace will also act
            // deterministically if we're in unit tests
            this.TestHookPartialSolutionsDisabled = solution.Workspace.TestHookPartialSolutionsDisabled;

            // Create a new document to hold the temporary code
            ChangeSignatureDocumentId = DocumentId.CreateNewId(project.Id);
            this.SetCurrentSolution(solution.AddDocument(ChangeSignatureDocumentId, Guid.NewGuid().ToString(), GetDocumentText()));

            Options = Options.WithChangedOption(EditorCompletionOptions.UseSuggestionMode, true);
        }

        // TODO do we need to keep this?
        private static string GetDocumentText()
        {
            return $@"
{{
}}
";
        }

        public Document ChangeSignatureDocument => this.CurrentSolution.GetDocument(this.ChangeSignatureDocumentId);
        public DocumentId ChangeSignatureDocumentId { get; }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            this.OnDocumentOpened(documentId, textContainer);
        }
    }
}
