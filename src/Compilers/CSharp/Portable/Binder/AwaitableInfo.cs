// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Internal structure containing all semantic information about an await expression.
    /// </summary>
    internal struct AwaitableInfo : IEquatable<AwaitableInfo>
    {
        public MethodSymbol GetAwaiter { get; }

        public PropertySymbol IsCompleted { get; }

        public MethodSymbol GetResult { get; }

        public TypeSymbol Type { get; }

        public bool IsDynamic => GetResult == null;

        internal AwaitableInfo(MethodSymbol getAwaiterMethod,
            PropertySymbol isCompletedProperty,
            MethodSymbol getResultMethod,
            TypeSymbol type)
        {
            this.GetAwaiter = getAwaiterMethod;
            this.IsCompleted = isCompletedProperty;
            this.GetResult = getResultMethod;
            this.Type = type;
        }

        public override bool Equals(object obj)
        {
            return obj is AwaitableInfo && Equals((AwaitableInfo)obj);
        }

        public bool Equals(AwaitableInfo other)
        {
            return object.Equals(this.GetAwaiter, other.GetAwaiter)
                && object.Equals(this.IsCompleted, other.IsCompleted)
                && object.Equals(this.GetResult, other.GetResult)
                && object.Equals(this.Type, other.Type);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetAwaiter, Hash.Combine(IsCompleted, Hash.Combine(GetResult, Type.GetHashCode())));
        }

        public static bool operator ==(AwaitableInfo left, AwaitableInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AwaitableInfo left, AwaitableInfo right)
        {
            return !left.Equals(right);
        }
    }
}
