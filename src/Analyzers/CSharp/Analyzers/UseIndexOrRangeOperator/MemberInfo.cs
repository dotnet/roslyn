// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    internal readonly struct MemberInfo(
        IPropertySymbol lengthLikeProperty,
        IMethodSymbol? overloadedMethodOpt)
    {
        /// <summary>
        /// The <c>Length</c>/<c>Count</c> property on the type.  Must be public, non-static, no-parameter,
        /// <see cref="int"/>-returning.
        /// </summary>
        public readonly IPropertySymbol LengthLikeProperty = lengthLikeProperty;

        /// <summary>
        /// Optional paired overload that takes a <see cref="T:System.Range"/>/<see cref="T:System.Index"/> parameter instead.
        /// </summary>
        [SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "Required to avoid ambiguous reference warnings.")]
        public readonly IMethodSymbol? OverloadedMethodOpt = overloadedMethodOpt;
    }
}
