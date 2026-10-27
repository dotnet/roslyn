// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP1_1 && !NET46

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.TestAnalyzers
{
    internal class CSharpSuppressorTest<TAnalyzer, TSuppressor> : CSharpAnalyzerTest<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TSuppressor : DiagnosticSuppressor, new()
    {
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => base.GetDiagnosticAnalyzers().Concat(new[] { new TSuppressor() });
    }
}

#endif
