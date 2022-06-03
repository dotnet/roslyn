// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DebuggerIntelliSense
{
    internal sealed class DebuggerIntelliSenseWorkspace : Workspace
    {
        public DebuggerIntelliSenseWorkspace(Solution solution)
            : base(solution.Workspace.Services.HostServices, WorkspaceKind.Debugger)
        {
            // The solution we are handed is still parented by the original workspace. We want to
            // inherit it's "no partial solutions" flag so that way this workspace will also act
            // deterministically if we're in unit tests
            TestHookPartialSolutionsDisabled = solution.Workspace.TestHookPartialSolutionsDisabled;

            SetCurrentSolution(solution);
        }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
            => OnDocumentOpened(documentId, textContainer);
    }
}
