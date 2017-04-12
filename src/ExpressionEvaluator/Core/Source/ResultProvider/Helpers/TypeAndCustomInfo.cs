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
            get
            {
                var t = ClrType?.GetLmrType();

                //TODO: Sometimes we get byref types here when dealing with ref-returning members.
                //      That probably should not happen.
                //      For now we will just unwrap unexpected byrefs
                if (t?.IsByRef == true)
                {
                    t = t.GetElementType();
                    Debug.Assert(!t.IsByRef, "double byref type?");
                }

                return t;
            }
        }
    }
}
