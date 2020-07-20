// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class FixAllSuggestedAction
    {
        private sealed partial class FixAllCodeAction : FixSomeCodeAction
        {
            public FixAllCodeAction(FixAllState fixAllState)
                : base(fixAllState, showPreviewChangesDialog: true)
            {
            }

            public override string Title
                => this.FixAllState.Scope switch
                {
                    FixAllScope.Document => FeaturesResources.Document,
                    FixAllScope.Project => FeaturesResources.Project,
                    FixAllScope.Solution => FeaturesResources.Solution,
                    _ => throw new NotSupportedException(),
                };

            internal override string Message => FeaturesResources.Computing_fix_all_occurrences_code_fix;
        }
    }
}
