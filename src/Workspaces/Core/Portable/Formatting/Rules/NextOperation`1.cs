// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// Represents a next operation to run in a continuation style chaining.
    /// </summary>
    internal struct NextOperation<TResult>
    {
        private readonly int _index;
        private SyntaxToken _token1;
        private SyntaxToken _token2;
        private readonly IOperationHolder<TResult> _operationCache;

        public NextOperation(int index, SyntaxToken token1, SyntaxToken token2, IOperationHolder<TResult> operationCache)
        {
            _index = index;
            _token1 = token1;
            _token2 = token2;
            _operationCache = operationCache;
        }

        public TResult Invoke()
        {
            return _operationCache.Continuation(_index, _token1, _token2, _operationCache);
        }
    }
}
