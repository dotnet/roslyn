// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal enum DiagnosticKind
{
    All = 0,
    CompilerSyntax = 1,
    CompilerSemantic = 2,
    AnalyzerSyntax = 3,
    AnalyzerSemantic = 4,
}
