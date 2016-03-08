// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public class AnalysisContextInfoTests
    {
        [Fact]
        public void InitializeTest()
        {
            var code = @"class C { void M() { return; } }";
            var compilation = CreateCompilation(code);

            Verify(compilation, nameof(AnalysisContext.RegisterCodeBlockAction));
            Verify(compilation, nameof(AnalysisContext.RegisterCodeBlockStartAction));
            Verify(compilation, nameof(AnalysisContext.RegisterCompilationAction));
            Verify(compilation, nameof(AnalysisContext.RegisterCompilationStartAction));
            Verify(compilation, nameof(AnalysisContext.RegisterOperationAction));
            Verify(compilation, nameof(AnalysisContext.RegisterOperationBlockAction));
            Verify(compilation, nameof(AnalysisContext.RegisterSemanticModelAction));
            Verify(compilation, nameof(AnalysisContext.RegisterSymbolAction));
            Verify(compilation, nameof(AnalysisContext.RegisterSyntaxNodeAction));
            Verify(compilation, nameof(AnalysisContext.RegisterSyntaxTreeAction));
        }

        private static void Verify(Compilation compilation, string context)
        {
            var analyzer = new Analyzer(s => context == s);
            var diagnostics = compilation.GetAnalyzerDiagnostics(new DiagnosticAnalyzer[] { analyzer });

            Assert.Equal(1, diagnostics.Length);
            Assert.True(diagnostics[0].Descriptor.Description.ToString().IndexOf(analyzer.Info.GetContext()) >= 0);
        }

        private class Analyzer : DiagnosticAnalyzer
        {
            public const string Id = "expection";
            private static readonly DiagnosticDescriptor s_rule = GetRule(Id);

            private readonly Func<string, bool> _throwPredicate;
            private AnalysisContextInfo _info;

            public Analyzer(Func<string, bool> throwPredicate)
            {
                _throwPredicate = throwPredicate;
            }

            public AnalysisContextInfo Info => _info;

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

            public override void Initialize(AnalysisContext c)
            {
                c.RegisterCodeBlockAction(b => ThrowIfMatch(nameof(c.RegisterCodeBlockAction), new AnalysisContextInfo(b.SemanticModel.Compilation, b.OwningSymbol, b.CodeBlock)));
                c.RegisterCodeBlockStartAction<SyntaxKind>(b => ThrowIfMatch(nameof(c.RegisterCodeBlockStartAction), new AnalysisContextInfo(b.SemanticModel.Compilation, b.OwningSymbol, b.CodeBlock)));
                c.RegisterCompilationAction(b => ThrowIfMatch(nameof(c.RegisterCompilationAction), new AnalysisContextInfo(b.Compilation)));
                c.RegisterCompilationStartAction(b => ThrowIfMatch(nameof(c.RegisterCompilationStartAction), new AnalysisContextInfo(b.Compilation)));
                c.RegisterOperationAction(b => ThrowIfMatch(nameof(c.RegisterOperationAction), new AnalysisContextInfo(b.Compilation, b.Operation)), OperationKind.ReturnStatement);
                c.RegisterOperationBlockAction(b => ThrowIfMatch(nameof(c.RegisterOperationBlockAction), new AnalysisContextInfo(b.Compilation, b.OwningSymbol)));
                c.RegisterSemanticModelAction(b => ThrowIfMatch(nameof(c.RegisterSemanticModelAction), new AnalysisContextInfo(b.SemanticModel)));
                c.RegisterSymbolAction(b => ThrowIfMatch(nameof(c.RegisterSymbolAction), new AnalysisContextInfo(b.Compilation, b.Symbol)), SymbolKind.NamedType);
                c.RegisterSyntaxNodeAction(b => ThrowIfMatch(nameof(c.RegisterSyntaxNodeAction), new AnalysisContextInfo(b.SemanticModel.Compilation, b.Node)), SyntaxKind.ReturnStatement);
                c.RegisterSyntaxTreeAction(b => ThrowIfMatch(nameof(c.RegisterSyntaxTreeAction), new AnalysisContextInfo(b.Compilation, b.Tree)));
            }

            private void ThrowIfMatch(string context, AnalysisContextInfo info)
            {
                if (!_throwPredicate(context))
                {
                    return;
                }

                _info = info;
                throw new Exception("exception");
            }
        }

        private static DiagnosticDescriptor GetRule(string id)
        {
            return new DiagnosticDescriptor(
                id,
                id,
                "{0}",
                "Test",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }

        private static Compilation CreateCompilation(string source)
        {
            string fileName = "Test.cs";
            string projectName = "TestProject";

            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: fileName);

            return CSharpCompilation.Create(
                projectName,
                syntaxTrees: new[] { syntaxTree },
                references: new[] { TestBase.MscorlibRef });
        }
    }
}
