// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Structure containing all semantic information about an await expression.
    /// </summary>
    public struct AwaitExpressionInfo : IEquatable<AwaitExpressionInfo>
    {
        public IMethodSymbol GetAwaiterMethod { get; }

        public IPropertySymbol IsCompletedProperty { get; }

        public IMethodSymbol GetResultMethod { get; }

        public bool IsDynamic => GetResultMethod is null;

        internal AwaitExpressionInfo(IMethodSymbol getAwaiter, IPropertySymbol isCompleted, IMethodSymbol getResult)
        {
            GetAwaiterMethod = getAwaiter;
            IsCompletedProperty = isCompleted;
            GetResultMethod = getResult;
        }

        public override bool Equals(object obj)
        {
            return obj is AwaitExpressionInfo otherAwait && Equals(otherAwait);
        }

        public bool Equals(AwaitExpressionInfo other)
        {
            return object.Equals(this.GetAwaiterMethod, other.GetAwaiterMethod)
                && object.Equals(this.IsCompletedProperty, other.IsCompletedProperty)
                && object.Equals(this.GetResultMethod, other.GetResultMethod);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetAwaiterMethod, Hash.Combine(IsCompletedProperty, Hash.Combine(GetResultMethod, 0)));
        }
    }
}
