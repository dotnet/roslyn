// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public class AnalyzerVerifier<TAnalyzer> : CSharpAnalyzerVerifier<TAnalyzer, MSTestVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
    }
}
