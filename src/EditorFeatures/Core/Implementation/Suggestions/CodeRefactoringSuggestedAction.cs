// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// Represents light bulb menu item for code refactorings.
    /// </summary>
    internal class CodeRefactoringSuggestedAction : SuggestedActionWithFlavors
    {
        public CodeRefactoringSuggestedAction(
            Workspace workspace,
            ITextView textView,
            ITextBuffer subjectBuffer,
            ICodeActionEditHandlerService editHandler,
            IWaitIndicator waitIndicator,
            CodeAction codeAction,
            CodeRefactoringProvider provider)
            : base(workspace, textView, subjectBuffer, editHandler, waitIndicator, codeAction, provider)
        {
        }
    }
}
