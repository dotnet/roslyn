// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Represents a next operation to run in a continuation style chaining.
    /// </summary>
    internal struct NextAction<TArgument>
    {
        private readonly int index;
        private readonly SyntaxNode node;
        private readonly IActionHolder<TArgument> actionCache;

        public NextAction(int index, SyntaxNode node, IActionHolder<TArgument> actionCache)
        {
            this.index = index;
            this.node = node;
            this.actionCache = actionCache;
        }

        public void Invoke(List<TArgument> arguments)
        {
            this.actionCache.Continuation(this.index, arguments, this.node, this.actionCache);
        }
    }
}
