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

namespace Metalama.Compiler.UnitTests
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
            var analyzers = new DiagnosticAnalyzer[]
            {
                new ReportDiagnosticForEachClassAnalyzer("MY001", DiagnosticSeverity.Warning, outWriter),
                new ReportDiagnosticForEachClassAnalyzer("MY002", DiagnosticSeverity.Error, outWriter)
            }.ToImmutableArray();
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
            Assert.DoesNotContain("warning MY001: Found a class 'D'.", output);

            // Errors should also be suppressed because they don't have the CS prefix.
            Assert.Contains("error MY002: Found a class 'C'.", output);
            Assert.DoesNotContain("error MY002: Found a class 'D'.", output);
        }

        [Fact]
        public void TransformersCanSuppressWarnings()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  int _f;  }");

            var args = new[] { "/t:library", src.Path };

            var analyzers =
                new DiagnosticAnalyzer[]
                {
                    new ReportDiagnosticForEachClassAnalyzer("MY001", DiagnosticSeverity.Warning)
                }.ToImmutableArray();
            var transformers =
                new ISourceTransformer[]
                {
                    new AppendTransformer("class D { int _f; }"), new SuppressTransformer("MY001"),
                    new SuppressTransformer("CS0169")
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

        [Fact]
        public void SourceOnlyAnalyzerSeesSourceCode()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  }");

            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText($@"
is_global = true
build_property.MetalamaSourceOnlyAnalyzers = all");

            var args = new[] { "/t:library", $"/analyzerconfig:{analyzerConfig.Path}", src.Path };

            var transformers = ImmutableArray.Create<ISourceTransformer>(new AppendTransformer("class D { }"));
            var analyzers =
                ImmutableArray.Create<DiagnosticAnalyzer>(new ReportWarningIfTwoCompilationUnitMembersAnalyzer());

            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers, analyzers: analyzers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Contains("warning MY002", output);
            Assert.DoesNotContain("warning MY001", output);

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void DiagnosticOnMovedNodeIsFound()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {  void M() { M(); }  }");

            var args = new[] { "/t:library", src.Path };

            var transformers = ImmutableArray.Create<ISourceTransformer>(new ChangeTreeParentAndReportTransformer());
            var csc = CreateCSharpCompiler(null, dir.Path, args, transformers: transformers);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            Assert.Equal(0, exitCode);

            Assert.Contains("MY001", output);
        }


        private class ReportDiagnosticForEachClassAnalyzer : DiagnosticAnalyzer
        {
            private readonly StringWriter? _outWriter;

            private readonly DiagnosticDescriptor _diagnostic;


            public ReportDiagnosticForEachClassAnalyzer(string id, DiagnosticSeverity severity,
                StringWriter? outWriter = null)
            {
                _outWriter = outWriter;
                _diagnostic = new DiagnosticDescriptor(id, "", "Found a class '{0}'.", "test", severity, true);
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(_diagnostic);

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
                    context.ReportDiagnostic(
                        Microsoft.CodeAnalysis.Diagnostic.Create(_diagnostic, c.Location, c.Identifier.Text));
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
                context.ReplaceSyntaxTree(syntaxTree, modifiedSyntaxTree);
            }
        }

        private class SuppressTransformer : ISourceTransformer
        {
            private readonly string _diagnosticId;

            public SuppressTransformer(string diagnosticId)
            {
                _diagnosticId = diagnosticId;
            }

            public void Execute(TransformerContext context)
            {
                context.RegisterDiagnosticFilter(
                    new SuppressionDescriptor("Suppress." + _diagnosticId, _diagnosticId, ""),
                    request => request.Suppress());
            }
        }

        private class ReportWarningIfTwoCompilationUnitMembersAnalyzer : DiagnosticAnalyzer
        {
            private static DiagnosticDescriptor _badWarning = new("MY001", "Test",
                "More than one member in compilation unit: {0}", "Test", DiagnosticSeverity.Warning, true);

            private static DiagnosticDescriptor _goodWarning = new("MY002", "Test",
                "Processing {0}", "Test", DiagnosticSeverity.Warning, true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
                ImmutableArray.Create(_badWarning, _goodWarning);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
            }

            private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
            {
                var compilationUnit = (CompilationUnitSyntax)context.Tree.GetRoot();

                // Write a message for each member to make sure that the analyzer runs.
                foreach (var member in compilationUnit.Members)
                {
                    context.ReportDiagnostic(
                        Microsoft.CodeAnalysis.Diagnostic.Create(_goodWarning, member.Location, member.ToString()));
                }

                // Write a message if there are two members to make sure we see the source code.
                if (compilationUnit.Members.Count > 1)
                {
                    foreach (var member in compilationUnit.Members)
                    {
                        context.ReportDiagnostic(
                            Microsoft.CodeAnalysis.Diagnostic.Create(_badWarning, member.Location, member.ToString()));
                    }
                }
            }
        }

        private class ChangeTreeParentAndReportTransformer : CSharpSyntaxRewriter, ISourceTransformer
        {
            private static readonly DiagnosticDescriptor _warning = new("MY001", "Test", "Warning on '{0}'", "Test", DiagnosticSeverity.Warning, true);
            private TransformerContext? _context;

            public void Execute(TransformerContext context)
            {
                this._context = context;
                foreach (var tree in context.Compilation.SyntaxTrees)
                {
                    var oldRoot = tree.GetRoot();
                    var newRoot = Visit(oldRoot);
                    if (newRoot != oldRoot)
                    {
                        context.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(newRoot, tree.Options));
                    }
                }
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                var firstStatement = node.Body!.Statements.First();
                _context!.ReportDiagnostic( Microsoft.CodeAnalysis.Diagnostic.Create(_warning, firstStatement.Location, firstStatement.ToString()));
                
                return node.WithBody(
                    SyntaxFactory.Block(SyntaxFactory.LockStatement(SyntaxFactory.ThisExpression(), node.Body!)));
            }
        }
    }
}
