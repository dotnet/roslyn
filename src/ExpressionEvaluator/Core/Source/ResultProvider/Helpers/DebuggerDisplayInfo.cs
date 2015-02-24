// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct DebuggerDisplayInfo
    {
        public readonly DkmClrType TargetType;
        public readonly DkmClrDebuggerDisplayAttribute Attribute;

        public DebuggerDisplayInfo(DkmClrType targetType, DkmClrDebuggerDisplayAttribute attribute)
        {
            TargetType = targetType;
            Attribute = attribute;
        }
    }
}
