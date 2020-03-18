// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    internal static class CommonAttributeDataExtensions
    {
        public static bool TryGetGuidAttributeValue(this AttributeData attrData, out string? guidString)
        {
            if (attrData.CommonConstructorArguments.Length == 1)
            {
                object? value = attrData.CommonConstructorArguments[0].ValueInternal;

                if (value == null || value is string)
                {
                    guidString = (string?)value;
                    return true;
                }
            }

            guidString = null;
            return false;
        }
    }
}
