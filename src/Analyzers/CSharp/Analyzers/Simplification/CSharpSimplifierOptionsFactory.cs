// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal static class CSharpSimplifierOptionsFactory
{
    internal static CSharpSimplifierOptions GetCSharpSimplifierOptions(this AnalyzerOptions options, SyntaxTree syntaxTree)
    {
        var configOptions = options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        var ideOptions = options.GetIdeOptions();

        return CSharpSimplifierOptions.Create(configOptions, (CSharpSimplifierOptions?)ideOptions.SimplifierOptions);
    }
}
