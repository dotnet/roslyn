// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    internal readonly struct NextGetAdjustSpacesOperation
    {
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;
        private readonly int _index;
        private readonly SyntaxToken _previousToken;
        private readonly SyntaxToken _currentToken;
        private readonly OptionSet _optionSet;

        public NextGetAdjustSpacesOperation(
            ImmutableArray<AbstractFormattingRule> formattingRules,
            int index,
            SyntaxToken previousToken,
            SyntaxToken currentToken,
            OptionSet optionSet)
        {
            _formattingRules = formattingRules;
            _index = index;
            _previousToken = previousToken;
            _currentToken = currentToken;
            _optionSet = optionSet;
        }

        private NextGetAdjustSpacesOperation NextOperation
            => new NextGetAdjustSpacesOperation(_formattingRules, _index + 1, _previousToken, _currentToken, _optionSet);

        public AdjustSpacesOperation Invoke()
        {
            // If we have no remaining handlers to execute, then we'll execute our last handler
            if (_index >= _formattingRules.Length)
            {
                return null;
            }
            else
            {
                // Call the handler at the index, passing a continuation that will come back to here with index + 1
                return _formattingRules[_index].GetAdjustSpacesOperation(_previousToken, _currentToken, _optionSet, NextOperation);
            }
        }
    }
}
