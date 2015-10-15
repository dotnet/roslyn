// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// a delegate cache for a continuation style chaining
    /// </summary>
    internal interface IOperationHolder<TResult>
    {
        Func<int, SyntaxToken, SyntaxToken, NextOperation<TResult>, TResult> NextOperation { get; }
        Func<int, SyntaxToken, SyntaxToken, IOperationHolder<TResult>, TResult> Continuation { get; }
    }
}
