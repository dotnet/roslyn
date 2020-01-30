// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class SuppressMessageAttributeCompilerTests : SuppressMessageAttributeTests
    {
        protected override Task VerifyAsync(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] diagnostics, string rootNamespace = null)
        {
            Assert.True(analyzers != null && analyzers.Length > 0, "Must specify at least one diagnostic analyzer to test suppression");
            var compilation = CreateCompilation(source, language, rootNamespace);
            compilation.VerifyAnalyzerDiagnostics(analyzers, expected: diagnostics);
            return Task.FromResult(false);
        }

        protected override bool ConsiderArgumentsForComparingDiagnostics => true;

        private static Compilation CreateCompilation(string source, string language, string rootNamespace)
        {
            string fileName = language == LanguageNames.CSharp ? "Test.cs" : "Test.vb";
            string projectName = "TestProject";

            var syntaxTree = language == LanguageNames.CSharp ?
                CSharpSyntaxTree.ParseText(source, path: fileName) :
                VisualBasicSyntaxTree.ParseText(source, path: fileName);

            if (language == LanguageNames.CSharp)
            {
                return CSharpCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef });
            }
            else
            {
                return VisualBasicCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: new[] { TestBase.MscorlibRef },
                    options: new VisualBasicCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        rootNamespace: rootNamespace));
            }
        }

        [Fact]
        public async Task AnalyzerExceptionDiagnosticsWithDifferentContext()
        {
            var diagnostic = Diagnostic("AD0001", null)
                .WithArguments(
                    "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionForEachNamedTypeAnalyzer",
                    "System.Exception",
                    "ThrowExceptionAnalyzer exception")
                .WithLocation(1, 1);

            // expect 3 different diagnostics with 3 different contexts.
            await VerifyCSharpAsync(@"
public class C
{
}
public class C1
{
}
public class C2
{
}
",
                new[] { new ThrowExceptionForEachNamedTypeAnalyzer() },
                diagnostics: new[] { diagnostic, diagnostic, diagnostic });
        }

        [Fact]
        public async Task AnalyzerExceptionFromSupportedDiagnosticsCall()
        {
            var diagnostic = Diagnostic("AD0001", null)
                .WithArguments(
                    "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionFromSupportedDiagnostics",
                    "System.Exception",
                    "SupportedDiagnostics exception")
                .WithLocation(1, 1);

            await VerifyCSharpAsync("public class C { }",
                new[] { new ThrowExceptionFromSupportedDiagnostics() },
                diagnostics: new[] { diagnostic });
        }
    }
}
