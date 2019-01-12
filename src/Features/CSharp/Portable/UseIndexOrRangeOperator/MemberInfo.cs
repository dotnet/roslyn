// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    internal readonly struct MemberInfo
    {
        /// <summary>
        /// The Length/Count property on the type.  Must be public, non-static, no-parameter,
        /// int32 returning.
        /// </summary>
        public readonly IPropertySymbol LengthLikeProperty;

        /// <summary>
        /// Optional paired overload that takes a Range/Index parameter instead.
        /// </summary>
        public readonly IMethodSymbol OverloadedMethodOpt;

        public MemberInfo(
            IPropertySymbol lengthLikeProperty,
            IMethodSymbol overloadedMethodOpt)
        {
            LengthLikeProperty = lengthLikeProperty;
            OverloadedMethodOpt = overloadedMethodOpt;
        }
    }
}
