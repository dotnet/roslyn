// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct TypeAndCustomInfo
    {
        public readonly DkmClrType ClrType;
        public readonly DkmClrCustomTypeInfo Info;

        public TypeAndCustomInfo(DkmClrType type, DkmClrCustomTypeInfo info = null)
        {
            Debug.Assert(type != null); // Can only be null in the default instance.
            ClrType = type;
            Info = info;
        }

        public Type Type
        {
            get { return (ClrType == null) ? null : ClrType.GetLmrType(); }
        }
    }
}
