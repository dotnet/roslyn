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

        /// <summary>
        /// When runtime async is enabled for this await expression, this represents either:
        /// <list type="bullet">
        /// <item>
        /// A call to <c>System.Runtime.CompilerServices.AsyncHelpers.Await</c>, if this is a
        /// supported task type. In such cases, <see cref="GetAwaiterMethod" />,
        /// <see cref="IsCompletedProperty" />, and <see cref="GetResultMethod" /> will be
        /// <see langword="null" />.
        /// </item>
        /// <item>
        /// A call to <c>System.Runtime.CompilerServices.AsyncHelpers.AwaitAwaiter|UnsafeAwaitAwaiter</c>.
        /// In these cases, the other properties may be non-<see langword="null" /> if the
        /// the rest of the await expression is successfully bound.
        /// </item>
        /// </list>
        /// </summary>
        public IMethodSymbol? RuntimeAwaitMethod { get; }

        internal AwaitExpressionInfo(IMethodSymbol? getAwaiter, IPropertySymbol? isCompleted, IMethodSymbol? getResult, IMethodSymbol? runtimeAwaitMethod, bool isDynamic)
        {
            GetAwaiterMethod = getAwaiter;
            IsCompletedProperty = isCompleted;
            GetResultMethod = getResult;
            RuntimeAwaitMethod = runtimeAwaitMethod;
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
                && object.Equals(this.RuntimeAwaitMethod, other.RuntimeAwaitMethod)
                && IsDynamic == other.IsDynamic;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetAwaiterMethod, Hash.Combine(IsCompletedProperty, Hash.Combine(GetResultMethod, Hash.Combine(RuntimeAwaitMethod, IsDynamic.GetHashCode()))));
        }
    }
}
