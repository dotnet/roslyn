// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

[NonDefaultable]
internal readonly struct NextAnchorIndentationOperationAction(
    ImmutableArray<AbstractFormattingRule> formattingRules,
    int index,
    SyntaxNode node,
    List<AnchorIndentationOperation> list)
{
    private NextAnchorIndentationOperationAction NextAction
        => new(formattingRules, index + 1, node, list);

    public void Invoke()
    {
        // If we have no remaining handlers to execute, then we'll execute our last handler
        if (index >= formattingRules.Length)
        {
            return;
        }
        else
        {
            // Call the handler at the index, passing a continuation that will come back to here with index + 1
            formattingRules[index].AddAnchorIndentationOperations(list, node, NextAction);
            return;
        }
    }
}
