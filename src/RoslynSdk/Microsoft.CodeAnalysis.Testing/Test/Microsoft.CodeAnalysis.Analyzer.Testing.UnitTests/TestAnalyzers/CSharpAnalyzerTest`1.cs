// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.TestAnalyzers
{
    internal class CSharpAnalyzerTest<TAnalyzer> : AnalyzerTest<DefaultVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public sealed override string Language => LanguageNames.CSharp;

        protected sealed override string DefaultFileExt => "cs";

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(LanguageVersion.Default);

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            yield return new TAnalyzer();
        }
    }
}
