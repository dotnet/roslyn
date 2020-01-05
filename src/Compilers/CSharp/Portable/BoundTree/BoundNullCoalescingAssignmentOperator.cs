// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundNullCoalescingAssignmentOperator
    {
        internal bool IsNullableValueTypeAssignment
        {
            get
            {
                var leftType = LeftOperand.Type;
                if (leftType?.IsNullableType() != true)
                {
                    return false;
                }

                var nullableUnderlying = leftType.GetNullableUnderlyingType();
                return nullableUnderlying.Equals(RightOperand.Type);
            }
        }
    }
}
