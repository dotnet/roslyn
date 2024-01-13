// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct TypeAndCustomInfo
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
