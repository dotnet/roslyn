// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    [NonDefaultable]
    internal readonly struct NextGetAdjustNewLinesOperation
    {
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly int _index;
        private readonly SyntaxToken _previousToken;
        private readonly SyntaxToken _currentToken;

        public NextGetAdjustNewLinesOperation(
            ImmutableArray<AbstractFormattingRule> formattingRules,
            int index,
            SyntaxToken previousToken,
            SyntaxToken currentToken)
        {
            _formattingRules = formattingRules;
            _index = index;
            _previousToken = previousToken;
            _currentToken = currentToken;
        }

        private NextGetAdjustNewLinesOperation NextOperation
            => new NextGetAdjustNewLinesOperation(_formattingRules, _index + 1, _previousToken, _currentToken);

        public AdjustNewLinesOperation? Invoke()
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (_index >= _formattingRules.Length)
            {
                return null;
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                return _formattingRules[_index].GetAdjustNewLinesOperation(_previousToken, _currentToken, NextOperation);
            }
        }
    }
}
