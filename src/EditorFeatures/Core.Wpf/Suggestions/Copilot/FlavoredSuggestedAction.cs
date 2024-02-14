// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.CodeActions;
using System.Linq;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionWithNestedFlavors
    {
        /// <summary>
        /// Suggested action for an additional custom hyperlink in the preview pane.
        /// Note: this is only used
        /// as a 'flavor' inside CodeRefactoringSuggestedAction and CodeFixSuggestedAction.
        /// </summary>
        protected sealed class FlavoredSuggestedAction : SuggestedAction
        {
            private FlavoredSuggestedAction(
                IThreadingContext threadingContext,
                SuggestedActionsSourceProvider sourceProvider,
                Workspace workspace,
                Solution originalSolution,
                ITextBuffer subjectBuffer,
                object provider,
                CodeAction originalCodeAction)
                : base(threadingContext, sourceProvider, workspace, originalSolution, subjectBuffer, provider, originalCodeAction)
            {
            }

            public static SuggestedAction Create(SuggestedActionWithNestedFlavors suggestedAction, CodeAction codeAction)
            {
                return new FlavoredSuggestedAction(
                    suggestedAction.ThreadingContext,
                    suggestedAction.SourceProvider,
                    suggestedAction.Workspace,
                    suggestedAction.OriginalSolution,
                    suggestedAction.SubjectBuffer,
                    suggestedAction.Provider,
                    codeAction);
            }
        }
    }
}
