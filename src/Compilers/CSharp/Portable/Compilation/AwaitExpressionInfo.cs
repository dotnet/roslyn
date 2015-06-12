// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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

        public bool IsDynamic { get; }

        internal AwaitExpressionInfo(IMethodSymbol getAwaiterMethod,
                                     IPropertySymbol isCompletedProperty,
                                     IMethodSymbol getResultMethod,
                                     bool isDynamic)
        {
            this.GetAwaiterMethod = getAwaiterMethod;
            this.IsCompletedProperty = isCompletedProperty;
            this.GetResultMethod = getResultMethod;
            this.IsDynamic = isDynamic;
        }

        public override bool Equals(object obj)
        {
            return obj is AwaitExpressionInfo && Equals((AwaitExpressionInfo)obj);
        }

        public bool Equals(AwaitExpressionInfo other)
        {
            return object.Equals(this.GetAwaiterMethod, other.GetAwaiterMethod)
                && object.Equals(this.IsCompletedProperty, other.IsCompletedProperty)
                && object.Equals(this.GetResultMethod, other.GetResultMethod)
                && this.IsDynamic == other.IsDynamic;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetAwaiterMethod, Hash.Combine(IsCompletedProperty, Hash.Combine(GetResultMethod, IsDynamic.GetHashCode())));
        }
    }
}
