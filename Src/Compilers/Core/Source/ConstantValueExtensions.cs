using Microsoft.CodeAnalysis.Text;
// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

namespace Microsoft.CodeAnalysis
{
    internal static class ConstantValueExtensions
    {
        internal static bool IsPrimitiveZeroOrNull(this ConstantValue value)
        {
            return (object)value != null && (value.IsNull || value.IsPrimitiveZero());
        }

        internal static bool IsPrimitiveZero(this ConstantValue value)
        {
            return (object)value != null && value.IsZero;
        }

        internal static bool IsPrimitiveOne(this ConstantValue value)
        {
            return (object)value != null && value.IsOne;
        }
    }
}