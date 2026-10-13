// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpCodeFixTest<TAnalyzer, TCodeFix, TVerifier> : CodeFixTest<TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
        where TVerifier : IVerifier, new()
    {
        private static readonly LanguageVersion DefaultLanguageVersion =
            Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            => new[] { new TCodeFix() };

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { new TAnalyzer() };

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        public override Type SyntaxKindType => typeof(SyntaxKind);

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);
    }
}
