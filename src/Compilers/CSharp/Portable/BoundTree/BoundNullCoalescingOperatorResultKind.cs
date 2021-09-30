// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents the operand type used for the result of a null-coalescing
    /// operator. Used when determining nullability.
    /// </summary>
    internal enum BoundNullCoalescingOperatorResultKind
    {
        /// <summary>
        /// No valid type for operator.
        /// </summary>
        NoCommonType,

        /// <summary>
        /// Type of left operand is used.
        /// </summary>
        LeftType,

        /// <summary>
        /// Nullable underlying type of left operand is used.
        /// </summary>
        LeftUnwrappedType,

        /// <summary>
        /// Type of right operand is used.
        /// </summary>
        RightType,

        /// <summary>
        /// Type of right operand is used and nullable left operand is converted
        /// to underlying type before converting to right operand type.
        /// </summary>
        LeftUnwrappedRightType,

        /// <summary>
        /// Type of right operand is dynamic and is used.
        /// </summary>
        RightDynamicType,
    }
}
