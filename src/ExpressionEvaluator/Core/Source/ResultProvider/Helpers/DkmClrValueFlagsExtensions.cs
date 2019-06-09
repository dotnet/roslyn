// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DkmClrValueFlagsExtensions
    {
        internal static bool IsError(this DkmClrValue value)
        {
            return (value.ValueFlags & DkmClrValueFlags.Error) != 0;
        }
    }
}
