// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal class UnifiedCodeRefactoringSuggestedAction : UnifiedSuggestedAction
    {
        public CodeRefactoringProvider CodeRefactoringProvider { get; }

        public UnifiedCodeRefactoringSuggestedAction(
            Workspace workspace,
            CodeRefactoringProvider codeRefactoringProvider,
            CodeAction codeAction)
            : base(workspace, codeAction)
        {
            CodeRefactoringProvider = codeRefactoringProvider;
        }
    }
}
