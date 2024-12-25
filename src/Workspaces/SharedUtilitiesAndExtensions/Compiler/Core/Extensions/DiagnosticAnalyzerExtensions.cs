// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class CompilerDiagnosticAnalyzerNames
{
    internal const string CSharpCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer";
    internal const string VisualBasicCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer";
}

internal static partial class DiagnosticAnalyzerExtensions
{
    public static bool IsCompilerAnalyzer(this DiagnosticAnalyzer analyzer)
    {
        // TODO: find better way.
        var typeString = analyzer.GetType().ToString();
        if (typeString == CompilerDiagnosticAnalyzerNames.CSharpCompilerAnalyzerTypeName)
        {
            return true;
        }

        if (typeString == CompilerDiagnosticAnalyzerNames.VisualBasicCompilerAnalyzerTypeName)
        {
            return true;
        }

        return false;
    }
}
