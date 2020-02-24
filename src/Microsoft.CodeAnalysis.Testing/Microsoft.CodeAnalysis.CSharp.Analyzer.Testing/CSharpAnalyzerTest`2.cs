// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpAnalyzerTest<TAnalyzer, TVerifier> : AnalyzerTest<TVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
        private static readonly LanguageVersion DefaultLanguageVersion =
            Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

        protected override string DefaultFileExt => "cs";

        public override string Language => LanguageNames.CSharp;

        protected override CompilationOptions CreateCompilationOptions()
            => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

        protected override ParseOptions CreateParseOptions()
            => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            => new[] { new TAnalyzer() };
    }
}
