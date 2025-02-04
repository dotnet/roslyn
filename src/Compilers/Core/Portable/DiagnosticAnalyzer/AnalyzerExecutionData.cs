// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal readonly record struct AnalyzerExecutionData(
        DiagnosticAnalyzer Analyzer,
        ISymbol DeclaredSymbol,
        SemanticModel SemanticModel,
        TextSpan? FilterSpan,
        bool IsGeneratedCode);
}
