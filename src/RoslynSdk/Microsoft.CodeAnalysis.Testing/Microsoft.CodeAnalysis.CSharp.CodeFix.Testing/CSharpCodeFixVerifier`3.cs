// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpCodeFixVerifier<TAnalyzer, TCodeFix, TVerifier> : CodeFixVerifier<TAnalyzer, TCodeFix, CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier>, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
    }
}
