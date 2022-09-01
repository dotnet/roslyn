// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class FixAllCodeFixSuggestedAction
    {
        private sealed partial class FixAllCodeAction : AbstractFixAllCodeFixCodeAction
        {
            public FixAllCodeAction(IFixAllState fixAllState)
                : base(fixAllState, showPreviewChangesDialog: true)
            {
            }
        }
    }
}
