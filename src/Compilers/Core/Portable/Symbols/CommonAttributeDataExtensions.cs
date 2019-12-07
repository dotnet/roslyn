// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
