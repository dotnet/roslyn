// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Additional information for rewriting an async-iterator.
    /// </summary>
    internal sealed class AsyncIteratorInfo
    {
        // This `ManualResetValueTaskSourceCore<bool>` struct implements the `IValueTaskSource` logic
        internal FieldSymbol PromiseOfValueOrEndField { get; }

        // This `CancellationTokenSource` field helps combine two cancellation tokens
        internal FieldSymbol CombinedTokensField { get; }

        // Stores the current/yielded value
        internal FieldSymbol CurrentField { get; }

        // Whether the state machine is in dispose mode
        internal FieldSymbol DisposeModeField { get; }

        // Method to fulfill the promise with a result: `void ManualResetValueTaskSourceCore<T>.SetResult(T result)`
        internal MethodSymbol SetResultMethod { get; }

        // Method to fulfill the promise with an exception: `void ManualResetValueTaskSourceCore<T>.SetException(Exception error)`
        internal MethodSymbol SetExceptionMethod { get; }

        public AsyncIteratorInfo(FieldSymbol promiseOfValueOrEndField, FieldSymbol combinedTokensField, FieldSymbol currentField, FieldSymbol disposeModeField,
            MethodSymbol setResultMethod, MethodSymbol setExceptionMethod)
        {
            PromiseOfValueOrEndField = promiseOfValueOrEndField;
            CombinedTokensField = combinedTokensField;
            CurrentField = currentField;
            DisposeModeField = disposeModeField;
            SetResultMethod = setResultMethod;
            SetExceptionMethod = setExceptionMethod;
        }
    }
}
