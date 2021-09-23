// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Caravela.Compiler.UnitTests
{
    public class DiagnosticsTests : CommandLineTestBase
    {
        /// <summary>
        /// Tests that warnings that stem from generated code are not reported to the user.
        /// </summary>
        [Fact]
        public void TransformedCodeDoesNotGenerateWarning()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  int _f;  }");

            var args = new[] { "/t:library", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { int _f; }") }
                .ToImmutableArray();
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            // Check that warnings are only reported when located in source code.
            Assert.Contains("warning CS0169: The field 'C._f' is never used", output);
            Assert.DoesNotContain("warning CS0169: The field 'D._f' is never used", output);
        }

        [Fact]
        public void NoAnalyzerDiagnosticOnGeneratedCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var args = new[] { "/t:library", src.Path };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") }
                .ToImmutableArray();
            var analyzers = new DiagnosticAnalyzer[] { new WarningForEachClassAnalyzer(outWriter) }.ToImmutableArray();
            var csc = CreateCSharpCompiler(null, dir.Path, args, analyzers: analyzers, transformers: transformers);

            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(1, exitCode);

            // Check that the analyzer ran.
            Assert.Contains("Analyzer initialized.", output);
            Assert.Contains("Analyzing syntax tree.", output);

            // Check that the analyzer did not see the transformed code and that reported warnings come through.
            Assert.Contains("Analyzing 'C'.", output);
            Assert.Contains("Analyzing 'D'.", output);
            Assert.Contains("warning MY001: Found a class 'C'.", output);
            Assert.Contains("error MY002: Found a class 'C'.", output);
            Assert.DoesNotContain("warning MY001: Found a class 'D'.", output);
            Assert.DoesNotContain("error MY002: Found a class 'D'.", output);
        }

        [Fact]
        public void TransformersCanSuppressWarnings()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  int _f;  }");

            var args = new[] { "/t:library", src.Path };

            var analyzers = new DiagnosticAnalyzer[] { new WarningForEachClassAnalyzer() }.ToImmutableArray();
            var transformers =
                new ISourceTransformer[]
                {
                    new AppendTransformer("class D { int _f; }"), new SuppressAnythingTransformer()
                }.ToImmutableArray();
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers, analyzers: analyzers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            // Check that the analyzer did not see the transformed code.
            Assert.DoesNotContain("warning MY001: Found a class 'C'.", output);
            Assert.DoesNotContain("warning MY001: Found a class 'D'.", output);
            Assert.DoesNotContain("warning CS0169: The field 'C._f' is never used", output);
            Assert.DoesNotContain("warning CS0169: The field 'D._f' is never used", output);
        }

        [Fact]
        public void SourceCodeCanReferenceGeneratedCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  D _d;  }");

            var args = new[] { "/t:library", src.Path };

            var transformers = new ISourceTransformer[] { new AppendTransformer("class D { }") }
                .ToImmutableArray();
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);
        }


        private class WarningForEachClassAnalyzer : DiagnosticAnalyzer
        {
            private readonly StringWriter? _outWriter;

            private readonly DiagnosticDescriptor _warning = new("MY001", "", "Found a class '{0}'.",
                "test", DiagnosticSeverity.Warning, true);

            private readonly DiagnosticDescriptor _error = new("MY002", "", "Found a class '{0}'.",
                "test", DiagnosticSeverity.Error, true);

            public WarningForEachClassAnalyzer(StringWriter? outWriter = null)
            {
                _outWriter = outWriter;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(_warning, _error);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
                _outWriter?.WriteLine("Analyzer initialized.");
            }

            private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                _outWriter?.WriteLine("Analyzing syntax tree.");

                foreach (var c in context.Tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    _outWriter?.WriteLine($"Analyzing '{c.Identifier.Text}'.");
                    context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(_warning, c.Location, c.Identifier.Text));
                    context.ReportDiagnostic(Microsoft.CodeAnalysis.Diagnostic.Create(_error, c.Location, c.Identifier.Text));
                }
            }
        }

        private class AppendTransformer : ISourceTransformer
        {
            private readonly CompilationUnitSyntax _newCode;

            public AppendTransformer(string newCode)
            {
                _newCode = (CompilationUnitSyntax)SyntaxFactory.ParseSyntaxTree(newCode).GetRoot()!;
            }

            public void Execute(TransformerContext context)
            {
                var syntaxTree = context.Compilation.SyntaxTrees.Single();
                var oldRoot = (CompilationUnitSyntax)syntaxTree.GetRoot();
                var newRoot = oldRoot.AddMembers(_newCode.Members.ToArray());
                var modifiedSyntaxTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
                context.Compilation = context.Compilation.ReplaceSyntaxTree(syntaxTree, modifiedSyntaxTree);
            }
        }

        private  class SuppressAnythingTransformer : ISourceTransformer
        {
            public void Execute(TransformerContext context)
            {
                context.RegisterDiagnosticFilter(request => request.Suppress());
            }
        }
    }
}
