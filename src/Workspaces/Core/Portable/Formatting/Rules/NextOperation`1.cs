// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Represents a next operation to run in a continuation style chaining.
    /// </summary>
    internal struct NextOperation<TResult>
    {
        private int index;
        private SyntaxToken token1;
        private SyntaxToken token2;
        private IOperationHolder<TResult> operationCache;

        public NextOperation(int index, SyntaxToken token1, SyntaxToken token2, IOperationHolder<TResult> operationCache)
        {
            this.index = index;
            this.token1 = token1;
            this.token2 = token2;
            this.operationCache = operationCache;
        }

        public TResult Invoke()
        {
            return operationCache.Continuation(index, token1, token2, operationCache);
        }
    }
}
