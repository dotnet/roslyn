// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal readonly struct ActionCache<TArgument>
    {
        public delegate void NextOperationAction(int index, List<TArgument> list, SyntaxNode node, in NextAction<TArgument> next);
        public delegate void ContinuationAction(int index, List<TArgument> list, SyntaxNode node, in ActionCache<TArgument> actionCache);

        public NextOperationAction NextOperation { get; }
        public ContinuationAction Continuation { get; }

        public ActionCache(
            NextOperationAction nextOperation,
            ContinuationAction continuation)
        {
            this.NextOperation = nextOperation;
            this.Continuation = continuation;
        }
    }
}
