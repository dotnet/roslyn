// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal readonly struct ActionCache<TArgument>
    {
        public Action<int, List<TArgument>, SyntaxNode, NextAction<TArgument>> NextOperation { get; }
        public Action<int, List<TArgument>, SyntaxNode, ActionCache<TArgument>> Continuation { get; }

        public ActionCache(
            Action<int, List<TArgument>, SyntaxNode, NextAction<TArgument>> nextOperation,
            Action<int, List<TArgument>, SyntaxNode, ActionCache<TArgument>> continuation)
        {
            this.NextOperation = nextOperation;
            this.Continuation = continuation;
        }
    }
}
