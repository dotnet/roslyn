// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Internal structure containing all semantic information about an await expression.
    /// </summary>
    internal class AwaitableInfo
    {
        public MethodSymbol GetAwaiter { get; }

        public PropertySymbol IsCompleted { get; }

        public MethodSymbol GetResult { get; }

        public bool IsDynamic => GetResult is null;

        internal AwaitableInfo(MethodSymbol getAwaiterMethod, PropertySymbol isCompletedProperty, MethodSymbol getResultMethod)
        {
            this.GetAwaiter = getAwaiterMethod;
            this.IsCompleted = isCompletedProperty;
            this.GetResult = getResultMethod;
        }
    }
}
