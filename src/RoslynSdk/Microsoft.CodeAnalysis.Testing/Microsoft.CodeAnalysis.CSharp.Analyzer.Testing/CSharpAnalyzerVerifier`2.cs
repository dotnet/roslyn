// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpAnalyzerVerifier<TAnalyzer, TVerifier> : AnalyzerVerifier<TAnalyzer, CSharpAnalyzerTest<TAnalyzer, TVerifier>, TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
    }
}
