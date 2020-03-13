// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal readonly struct NextSuppressOperationAction
    {
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly int _index;
        private readonly SyntaxNode _node;
        private readonly AnalyzerConfigOptions _options;
        private readonly List<SuppressOperation> _list;

        public NextSuppressOperationAction(
            ImmutableArray<AbstractFormattingRule> formattingRules,
            int index,
            SyntaxNode node,
            AnalyzerConfigOptions options,
            List<SuppressOperation> list)
        {
            _formattingRules = formattingRules;
            _index = index;
            _node = node;
            _options = options;
            _list = list;
        }

        private NextSuppressOperationAction NextAction
            => new NextSuppressOperationAction(_formattingRules, _index + 1, _node, _options, _list);

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
                _formattingRules[_index].AddSuppressOperations(_list, _node, _options, NextAction);
                return;
            }
        }
    }
}
