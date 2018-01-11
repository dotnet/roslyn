// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Public structure containing all semantic information about an await expression.
    /// </summary>
    public struct AwaitExpressionInfo : IEquatable<AwaitExpressionInfo>
    {
        private readonly AwaitableInfo _awaitableInfo;

        public IMethodSymbol GetAwaiterMethod => _awaitableInfo.GetAwaiter;

        public IPropertySymbol IsCompletedProperty => _awaitableInfo.IsCompleted;

        public IMethodSymbol GetResultMethod => _awaitableInfo.GetResult; 

        public bool IsDynamic => GetResultMethod == null;

        internal AwaitExpressionInfo(AwaitableInfo awaitableInfo)
        {
            _awaitableInfo = awaitableInfo;
        }

        public override bool Equals(object obj)
        {
            return obj is AwaitExpressionInfo && Equals((AwaitExpressionInfo)obj);
        }

        public bool Equals(AwaitExpressionInfo other)
        {
            return _awaitableInfo.Equals(other);
        }

        public override int GetHashCode()
        {
            return _awaitableInfo.GetHashCode();
        }
    }
}
