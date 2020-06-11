// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    [NonDefaultable]
    internal readonly struct NextAlignTokensOperationAction
    {
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly int _index;
        private readonly SyntaxNode _node;
        private readonly List<AlignTokensOperation> _list;

        public NextAlignTokensOperationAction(
            ImmutableArray<AbstractFormattingRule> formattingRules,
            int index,
            SyntaxNode node,
            List<AlignTokensOperation> list)
        {
            _formattingRules = formattingRules;
            _index = index;
            _node = node;
            _list = list;
        }

        private NextAlignTokensOperationAction NextAction
            => new NextAlignTokensOperationAction(_formattingRules, _index + 1, _node, _list);

        public void Invoke()
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (_index >= _formattingRules.Length)
            {
                return;
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                _formattingRules[_index].AddAlignTokensOperations(_list, _node, NextAction);
                return;
            }
        }
    }
}
