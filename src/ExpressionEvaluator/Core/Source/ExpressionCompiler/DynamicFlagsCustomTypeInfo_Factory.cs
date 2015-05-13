// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial struct DynamicFlagsCustomTypeInfo
    {
        public static DynamicFlagsCustomTypeInfo Create(CustomTypeInfo customTypeInfo)
        {
            return new DynamicFlagsCustomTypeInfo(customTypeInfo.PayloadTypeId == PayloadTypeId ? customTypeInfo.Payload : null);
        }

        public static DynamicFlagsCustomTypeInfo Create(ImmutableArray<bool> dynamicFlags)
        {
            Debug.Assert(!dynamicFlags.IsDefaultOrEmpty);

            var builder = ArrayBuilder<bool>.GetInstance(dynamicFlags.Length);
            builder.AddRange(dynamicFlags);
            var result = new DynamicFlagsCustomTypeInfo(builder, startIndex: 0);
            builder.Free();
            return result;
        }
    }
}
