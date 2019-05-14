// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal abstract class AbstractTopLevelConfigurationOrSuppressionCodeAction : CodeAction.CodeActionWithNestedActions
    {
        protected AbstractTopLevelConfigurationOrSuppressionCodeAction(ImmutableArray<CodeAction> nestedActions, string title)
            : base(title, nestedActions, isInlinable: false)
        {
        }

        // Put configurations/suppressions at the end of everything.
        internal override CodeActionPriority Priority => CodeActionPriority.None;
    }
}
