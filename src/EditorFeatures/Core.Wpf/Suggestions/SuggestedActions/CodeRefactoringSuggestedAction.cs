﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.UnifiedSuggestions.UnifiedSuggestedActions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents light bulb menu item for code refactorings.
    /// </summary>
    internal sealed class CodeRefactoringSuggestedAction : SuggestedActionWithNestedFlavors, ICodeRefactoringSuggestedAction
    {
        public CodeRefactoringProvider CodeRefactoringProvider { get; }

        public CodeRefactoringSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            CodeRefactoringProvider provider,
            CodeAction codeAction)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer, provider, codeAction)
        {
            CodeRefactoringProvider = provider;
        }
    }
}
