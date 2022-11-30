// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics;

[Flags]
internal enum DiagnosticKinds
{
    CompilerSyntax = 1 << 0,
    AnalyzerSyntax = 1 << 1,
    CompilerSemantic = 1 << 2,
    AnalyzerSemantic = 1 << 3,

    AllCompiler = CompilerSyntax | CompilerSemantic,
    AllAnalyzer = AnalyzerSyntax | AnalyzerSemantic,
    AllSyntax = CompilerSyntax | AnalyzerSyntax,
    AllSemantic = CompilerSemantic | AnalyzerSemantic,
    All = AllSyntax | AllSemantic
}
