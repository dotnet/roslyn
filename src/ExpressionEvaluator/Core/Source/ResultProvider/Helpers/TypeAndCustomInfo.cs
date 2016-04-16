// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct TypeAndCustomInfo
    {
        public readonly Type Type;
        public readonly DkmClrCustomTypeInfo Info;

        public TypeAndCustomInfo(Type type, DkmClrCustomTypeInfo info)
        {
            Debug.Assert(type != null); // Can only be null in the default instance.
            Type = type;
            Info = info;
        }

        public TypeAndCustomInfo(Type type)
            : this(type, null)
        {
        }

        public TypeAndCustomInfo(DkmClrType type)
            : this(type.GetLmrType(), null)
        {
        }
    }
}
