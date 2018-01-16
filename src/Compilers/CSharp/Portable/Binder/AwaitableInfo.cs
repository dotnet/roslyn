// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Internal structure containing all semantic information about an await expression.
    /// </summary>
    internal sealed class AwaitableInfo
    {
        public readonly MethodSymbol getAwaiter;
        public readonly PropertySymbol isCompleted;
        public readonly MethodSymbol getResult;

        public bool IsDynamic => getResult is null;

        internal AwaitableInfo(MethodSymbol getAwaiterMethod, PropertySymbol isCompletedProperty, MethodSymbol getResultMethod)
        {
            this.getAwaiter = getAwaiterMethod;
            this.isCompleted = isCompletedProperty;
            this.getResult = getResultMethod;
        }

        internal AwaitableInfo Update(MethodSymbol newGetAwaiter, PropertySymbol newIsCompleted, MethodSymbol newGetResult)
        {
            if (ReferenceEquals(getAwaiter, newGetAwaiter) && ReferenceEquals(isCompleted, newIsCompleted) && ReferenceEquals(getResult, newGetResult))
            {
                return this;
            }

            return new AwaitableInfo(newGetAwaiter, newIsCompleted, newGetResult);
        }
    }
}
