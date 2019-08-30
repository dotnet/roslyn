// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents light bulb menu item for code refactorings.
    /// </summary>
    internal sealed class CodeRefactoringSuggestedAction : SuggestedActionWithNestedFlavors
    {
        public CodeRefactoringSuggestedAction(
            IThreadingContext threadingContext,
            SuggestedActionsSourceProvider sourceProvider,
            Workspace workspace,
            ITextBuffer subjectBuffer,
            CodeRefactoringProvider provider,
            CodeAction codeAction)
            : base(threadingContext, sourceProvider, workspace, subjectBuffer, provider, codeAction)
        {
        }
    }
}
