// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                get
                {
                    switch (this.FixAllState.Scope)
                    {
                        case FixAllScope.Document:
                            return FeaturesResources.Document;
                        case FixAllScope.Project:
                            return FeaturesResources.Project;
                        case FixAllScope.Solution:
                            return FeaturesResources.Solution;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            internal override string Message => FeaturesResources.Computing_fix_all_occurrences_code_fix;
        }
    }
}
