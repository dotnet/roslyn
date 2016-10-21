// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal sealed class SuppressionCodeAction : CodeAction.CodeActionWithNestedActions
    {
        public SuppressionCodeAction(Diagnostic diagnostic, ImmutableArray<CodeAction> nestedActions)
            : base(string.Format(FeaturesResources.Suppress_0, diagnostic.Id),
                   nestedActions, isInlinable: false)
        {
        }

        // Put suppressions at the end of everything.
        internal override CodeActionPriority Priority => CodeActionPriority.None;
    }
}