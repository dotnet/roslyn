// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents the operand type used for the result of a null-coalescing
    /// operator. Used when determining nullability.
    /// </summary>
    internal enum BoundNullCoalescingOperatorResultKind
    {
        NoCommonType,
        LeftType,
        LeftUnwrappedType,
        RightType,
        LeftUnwrappedRightType,
        RightDynamicType,
    }
}
