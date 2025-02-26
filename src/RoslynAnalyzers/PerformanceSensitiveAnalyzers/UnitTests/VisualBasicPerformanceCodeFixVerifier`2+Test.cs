// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests
{
    public static partial class VisualBasicPerformanceCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        internal sealed class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
        }
    }
}
