// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Structure containing all semantic information about an await expression.
    /// </summary>
    public struct AwaitExpressionInfo : IEquatable<AwaitExpressionInfo>
    {
        private readonly AwaitableInfo _awaitableInfo;

        public IMethodSymbol GetAwaiterMethod => _awaitableInfo?.GetAwaiter;

        public IPropertySymbol IsCompletedProperty => _awaitableInfo?.IsCompleted;

        public IMethodSymbol GetResultMethod => _awaitableInfo?.GetResult;

        public bool IsDynamic => _awaitableInfo?.IsDynamic == true;

        internal AwaitExpressionInfo(AwaitableInfo awaitableInfo)
        {
            Debug.Assert(awaitableInfo != null);
            _awaitableInfo = awaitableInfo;
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
            if (_awaitableInfo is null)
            {
                return 0;
            }
            return Hash.Combine(GetAwaiterMethod, Hash.Combine(IsCompletedProperty, GetResultMethod.GetHashCode()));
        }
    }
}
