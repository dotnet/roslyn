// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    internal static class CommonAttributeDataExtensions
    {
        public static bool TryGetGuidAttributeValue(this AttributeData attrData, out string? guidString)
        {
            if (attrData.CommonConstructorArguments.Length == 1)
            {
                return attrData.CommonConstructorArguments[0].TryGetGuidAttributeValue(out guidString);
            }

            guidString = null;
            return false;
        }

        public static bool TryGetGuidAttributeValue(this TypedConstant typedConstant, out string? guidString)
        {
            object? value = typedConstant.ValueInternal;

            if (value == null || value is string)
            {
                guidString = (string?)value;
                return true;
            }

            guidString = null;
            return false;
        }
    }
}
