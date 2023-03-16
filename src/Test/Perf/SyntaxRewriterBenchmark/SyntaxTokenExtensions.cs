// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Baseline;
using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using SyntaxToken0 = Baseline::Microsoft.CodeAnalysis.SyntaxToken;

namespace Roslyn.SyntaxRewriterBenchmark;

internal static class SyntaxTokenExtensions
{
    public static SyntaxToken0 ToBaseline(this in SyntaxToken latestToken)
    {
        return Unsafe.As<SyntaxToken, SyntaxToken0>(ref Unsafe.AsRef(latestToken));
    }

    public static SyntaxToken0 ToBaseline(this in SyntaxToken0 baselineToken) => baselineToken;

    public static SyntaxToken FromBaseline(this in SyntaxToken0 baselineToken)
    {
        return Unsafe.As<SyntaxToken0, SyntaxToken>(ref Unsafe.AsRef(baselineToken));
    }

    public static SyntaxToken ToLatest(this in SyntaxToken0 baselineToken) => FromBaseline(baselineToken);

    public static SyntaxToken ToLatest(this in SyntaxToken latestToken) => latestToken;
}
