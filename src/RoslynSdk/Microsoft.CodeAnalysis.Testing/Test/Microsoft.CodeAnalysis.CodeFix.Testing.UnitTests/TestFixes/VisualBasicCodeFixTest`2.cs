// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Testing.TestFixes
{
    internal class VisualBasicCodeFixTest<TAnalyzer, TCodeFix> : CodeFixTest<DefaultVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public override string Language => LanguageNames.VisualBasic;

        public override Type SyntaxKindType => typeof(SyntaxKind);

        protected override string DefaultFileExt => "vb";

        protected override CompilationOptions CreateCompilationOptions()
            => new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        protected override ParseOptions CreateParseOptions()
            => new VisualBasicParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);

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
