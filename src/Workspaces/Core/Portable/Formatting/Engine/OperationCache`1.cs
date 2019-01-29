// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// a delegate cache for a continuation style chaining
    /// </summary>
    internal readonly struct OperationCache<TResult>
    {
        public delegate TResult NextOperationFunc(int index, SyntaxToken token1, SyntaxToken token2, ref NextOperation<TResult> next);
        public delegate TResult ContinuationFunc(int index, SyntaxToken token1, SyntaxToken token2, in OperationCache<TResult> operationCache);

        public OperationCache(
            NextOperationFunc nextOperation,
            ContinuationFunc continuation)
        {
            this.NextOperation = nextOperation;
            this.Continuation = continuation;
        }

        public NextOperationFunc NextOperation { get; }
        public ContinuationFunc Continuation { get; }
    }
}
