// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.UnitTests;
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

        private static readonly Lazy<ImmutableArray<MetadataReference>> s_references = new Lazy<ImmutableArray<MetadataReference>>(() =>
        {
            const string unconditionalSuppressMessageDef = @"
namespace System.Diagnostics.CodeAnalysis
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple=true, Inherited=false)]
    public sealed class UnconditionalSuppressMessageAttribute : System.Attribute
    {
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }
        public string Category { get; }
        public string CheckId { get; }
        public string Scope { get; set; }
        public string Target { get; set; }
        public string MessageId { get; set; }
        public string Justification { get; set; }
    }
}";
            var compRef = CSharpCompilation.Create("unconditionalsuppress",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: new[] { CSharpTestSource.Parse(unconditionalSuppressMessageDef) },
                references: new[] { TestBase.MscorlibRef }).EmitToImageReference();

            return ImmutableArray.Create(TestBase.MscorlibRef, compRef, TestBase.ValueTupleRef);

        }, System.Threading.LazyThreadSafetyMode.PublicationOnly);

        private static Compilation CreateCompilation(string source, string language, string rootNamespace)
        {
            string fileName = language == LanguageNames.CSharp ? "Test.cs" : "Test.vb";
            string projectName = "TestProject";
            var references = s_references.Value;

            var syntaxTree = language == LanguageNames.CSharp ?
                CSharpTestSource.Parse(source, path: fileName) :
                BasicTestSource.Parse(source, path: fileName);

            if (language == LanguageNames.CSharp)
            {
                return CSharpCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree, },
                    references: references);
            }
            else
            {
                return VisualBasicCompilation.Create(
                    projectName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new VisualBasicCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        rootNamespace: rootNamespace));
            }
        }

        [Fact]
        public async Task AnalyzerExceptionDiagnosticsWithDifferentContext()
        {
            var exception = new Exception();

            var baseDiagnostic = Diagnostic("AD0001", null).WithLocation(1, 1);
            var diagnosticC = baseDiagnostic
                .WithArguments(
                    "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionForEachNamedTypeAnalyzer",
                    "System.Exception",
                    exception.Message,
                    (IFormattable)$@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: TestProject
ISymbol: C (NamedType)")}

{new LazyToString(() => exception.ToString())}
-----

{string.Format(CodeAnalysisResources.DisableAnalyzerDiagnosticsMessage, "ThrowException")}");
            var diagnosticC1 = baseDiagnostic
                .WithArguments(
                    "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionForEachNamedTypeAnalyzer",
                    "System.Exception",
                    exception.Message,
                    (IFormattable)$@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: TestProject
ISymbol: C1 (NamedType)")}

{new LazyToString(() => exception.ToString())}
-----

{string.Format(CodeAnalysisResources.DisableAnalyzerDiagnosticsMessage, "ThrowException")}");
            var diagnosticC2 = baseDiagnostic
                .WithArguments(
                    "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionForEachNamedTypeAnalyzer",
                    "System.Exception",
                    exception.Message,
                    (IFormattable)$@"{string.Format(CodeAnalysisResources.ExceptionContext, $@"Compilation: TestProject
ISymbol: C2 (NamedType)")}

{new LazyToString(() => exception.ToString())}
-----

{string.Format(CodeAnalysisResources.DisableAnalyzerDiagnosticsMessage, "ThrowException")}");

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
                new[] { new ThrowExceptionForEachNamedTypeAnalyzer(ExceptionDispatchInfo.Capture(exception)) },
                diagnostics: new[] { diagnosticC, diagnosticC1, diagnosticC2 });
        }

        [Fact]
        public async Task AnalyzerExceptionFromSupportedDiagnosticsCall()
        {
            var exception = new Exception();
            const string AnalyzerName = "Microsoft.CodeAnalysis.UnitTests.Diagnostics.SuppressMessageAttributeTests+ThrowExceptionFromSupportedDiagnostics";

            var diagnostic = Diagnostic("AD0001", null)
                .WithArguments(
                    AnalyzerName,
                    "System.Exception",
                    exception.Message,
                    (IFormattable)$@"{new LazyToString(() =>
                        exception.ToString().Substring(0, exception.ToString().IndexOf("---")) + "-----" + Environment.NewLine + Environment.NewLine +
                        string.Format(CodeAnalysisResources.CompilerAnalyzerThrows, AnalyzerName, exception.GetType().ToString(), exception.Message, exception.ToString() + Environment.NewLine + "-----"))}")
                .WithLocation(1, 1);

            await VerifyCSharpAsync("public class C { }",
                new[] { new ThrowExceptionFromSupportedDiagnostics(exception) },
                diagnostics: new[] { diagnostic });
        }
    }
}
