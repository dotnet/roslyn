// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial struct DynamicFlagsCustomTypeInfo
    {
        internal static DynamicFlagsCustomTypeInfo Create(params bool[] dynamicFlags)
        {
            if (dynamicFlags == null || dynamicFlags.Length == 0)
            {
                return default(DynamicFlagsCustomTypeInfo);
            }

            var builder = ArrayBuilder<bool>.GetInstance(dynamicFlags.Length);
            builder.AddRange(dynamicFlags);
            var result = new DynamicFlagsCustomTypeInfo(builder, startIndex: 0);
            builder.Free();
            return result;
        }
    }
}
