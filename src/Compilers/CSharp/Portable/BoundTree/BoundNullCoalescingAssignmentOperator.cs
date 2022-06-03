// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
