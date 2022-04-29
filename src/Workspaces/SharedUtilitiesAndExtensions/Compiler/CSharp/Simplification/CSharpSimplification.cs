// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal sealed class CSharpSimplification : AbstractSimplification
{
    public static readonly CSharpSimplification Instance = new();

    public override SimplifierOptions DefaultOptions
        => CSharpSimplifierOptions.Default;

    public override SimplifierOptions GetSimplifierOptions(AnalyzerConfigOptions options, SimplifierOptions? fallbackOptions)
        => CSharpSimplifierOptions.Create(options, (CSharpSimplifierOptions?)fallbackOptions);
}
