// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class ActionCache<TArgument> : IActionHolder<TArgument>
    {
        public Action<int, List<TArgument>, SyntaxNode, NextAction<TArgument>> NextOperation { get; private set; }
        public Action<int, List<TArgument>, SyntaxNode, IActionHolder<TArgument>> Continuation { get; private set; }

        public ActionCache(
            Action<int, List<TArgument>, SyntaxNode, NextAction<TArgument>> nextOperation,
            Action<int, List<TArgument>, SyntaxNode, IActionHolder<TArgument>> continuation)
        {
            this.NextOperation = nextOperation;
            this.Continuation = continuation;
        }
    }
}
