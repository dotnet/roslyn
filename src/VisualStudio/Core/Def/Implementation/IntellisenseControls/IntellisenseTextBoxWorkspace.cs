// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
{
    internal sealed class IntellisenseTextBoxWorkspace : Workspace
    {
        private BackgroundCompiler _backgroundCompiler;

        public IntellisenseTextBoxWorkspace(Solution solution, Project project, string documentText)
            : base(project.Solution.Workspace.Services.HostServices, nameof(IntellisenseTextBoxWorkspace))
        {
            // The solution we are handed is still parented by the original workspace. We want to
            // inherit it's "no partial solutions" flag so that way this workspace will also act
            // deterministically if we're in unit tests
            this.TestHookPartialSolutionsDisabled = solution.Workspace.TestHookPartialSolutionsDisabled;

            // Create a new document to hold the temporary code
            ChangeSignatureDocumentId = DocumentId.CreateNewId(project.Id);
            this.SetCurrentSolution(solution.AddDocument(ChangeSignatureDocumentId, Guid.NewGuid().ToString(), documentText));

            _backgroundCompiler = new BackgroundCompiler(this);
        }

        public Document? ChangeSignatureDocument => this.CurrentSolution.GetDocument(this.ChangeSignatureDocumentId);

        public DocumentId ChangeSignatureDocumentId { get; }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            this.OnDocumentOpened(documentId, textContainer);
        }
    }
}
