// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.TestFixes
{
    internal class CSharpCodeFixTest<TAnalyzer, TCodeFix> : CodeFixTest<DefaultVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public sealed override string Language => LanguageNames.CSharp;

        public sealed override Type SyntaxKindType => typeof(SyntaxKind);

        protected sealed override string DefaultFileExt => "cs";

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            yield return new TAnalyzer();
        }

        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
        {
            yield return new TCodeFix();
        }
    }
}
