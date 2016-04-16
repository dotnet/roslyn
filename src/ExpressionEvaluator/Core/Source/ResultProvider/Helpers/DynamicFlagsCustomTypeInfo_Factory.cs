// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal partial struct DynamicFlagsCustomTypeInfo
    {
        public static DynamicFlagsCustomTypeInfo Create(DkmClrCustomTypeInfo typeInfo)
        {
            return new DynamicFlagsCustomTypeInfo(typeInfo != null && typeInfo.PayloadTypeId == PayloadTypeId ? typeInfo.Payload : null);
        }

        public static DynamicFlagsCustomTypeInfo Create(ArrayBuilder<bool> dynamicFlags)
        {
            Debug.Assert(dynamicFlags != null);
            Debug.Assert(dynamicFlags.Count > 0);
            return new DynamicFlagsCustomTypeInfo(dynamicFlags, startIndex: 0);
        }
    }
}
