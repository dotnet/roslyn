// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
