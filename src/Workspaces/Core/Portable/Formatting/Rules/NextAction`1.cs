// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Represents a next operation to run in a continuation style chaining.
    /// </summary>
    internal struct NextAction<TArgument>
    {
        private readonly int _index;
        private readonly SyntaxNode _node;
        private readonly SyntaxToken _lastToken;
        private readonly IActionHolder<TArgument> _actionCache;

        public NextAction(int index, SyntaxNode node, SyntaxToken lastToken, IActionHolder<TArgument> actionCache)
        {
            _index = index;
            _node = node;
            _lastToken = lastToken;
            _actionCache = actionCache;
        }

        public void Invoke(List<TArgument> arguments)
        {
            _actionCache.Continuation(_index, arguments, _node, _lastToken, _actionCache);
        }
    }
}
