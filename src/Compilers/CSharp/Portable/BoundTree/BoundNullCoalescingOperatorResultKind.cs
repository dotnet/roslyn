// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
