// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
