// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

#if NET8_0_OR_GREATER

/// <summary>
/// Reflection APIs are inadequate to introspect inline arrays as they box the value type, making
/// it impossible to take a ref to the first element field and read elements out of it. 
/// This definition is intended to be used when testing inline arrays.
/// </summary>
/// <remarks>
/// To support testing new <typeparamref name="T"/> types, 
/// add an appropriate cast in <see cref="DkmClrValue.GetArrayElement(int[], DkmInspectionContext)"/>
/// </remarks>
[InlineArray(Length)]
[DebuggerDisplay("{ToString(),nq}")]
internal struct SampleInlineArray<T>
{
    public const int Length = 4;
    public T _elem0;

    public override string ToString()
    {
        return string.Join(",", MemoryMarshal.CreateSpan(ref _elem0, Length).ToArray());
    }
}

#endif
