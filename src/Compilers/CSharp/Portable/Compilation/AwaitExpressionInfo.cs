// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Structure containing all semantic information about an await expression.
    /// </summary>
    public readonly struct AwaitExpressionInfo : IEquatable<AwaitExpressionInfo>
    {
        public IMethodSymbol? GetAwaiterMethod { get; }

        public IPropertySymbol? IsCompletedProperty { get; }

        public IMethodSymbol? GetResultMethod { get; }

        public bool IsDynamic { get; }

        internal AwaitExpressionInfo(IMethodSymbol getAwaiter, IPropertySymbol isCompleted, IMethodSymbol getResult, bool isDynamic)
        {
            GetAwaiterMethod = getAwaiter;
            IsCompletedProperty = isCompleted;
            GetResultMethod = getResult;
            IsDynamic = isDynamic;
        }

        public override bool Equals(object? obj)
        {
            return obj is AwaitExpressionInfo otherAwait && Equals(otherAwait);
        }

        public bool Equals(AwaitExpressionInfo other)
        {
            return object.Equals(this.GetAwaiterMethod, other.GetAwaiterMethod)
                && object.Equals(this.IsCompletedProperty, other.IsCompletedProperty)
                && object.Equals(this.GetResultMethod, other.GetResultMethod)
                && IsDynamic == other.IsDynamic;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetAwaiterMethod, Hash.Combine(IsCompletedProperty, Hash.Combine(GetResultMethod, IsDynamic.GetHashCode())));
        }
    }
}
