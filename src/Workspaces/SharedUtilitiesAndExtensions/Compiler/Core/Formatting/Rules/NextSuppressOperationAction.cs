﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    [NonDefaultable]
    internal readonly struct NextSuppressOperationAction
    {
        private readonly OperationFactory _operationFactory;
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly int _index;
        private readonly SyntaxNode _node;
        private readonly SegmentedList<SuppressOperation> _list;

        public NextSuppressOperationAction(
            OperationFactory operationFactory,
            ImmutableArray<AbstractFormattingRule> formattingRules,
            int index,
            SyntaxNode node,
            SegmentedList<SuppressOperation> list)
        {
            _operationFactory = operationFactory;
            _formattingRules = formattingRules;
            _index = index;
            _node = node;
            _list = list;
        }

        private NextSuppressOperationAction NextAction
            => new(_operationFactory, _formattingRules, _index + 1, _node, _list);

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
                _formattingRules[_index].AddSuppressOperations(_operationFactory, _list, _node, NextAction);
                return;
            }
        }
    }
}
