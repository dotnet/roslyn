// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class SuppressMessageAttributeTests
    {
        protected const string TestDiagnosticCategory = "Test";
        protected const string TestDiagnosticMessageTemplate = "{0}";

        protected abstract class AbstractMockAnalyzer : IDiagnosticAnalyzer
        {
            public abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        }

        protected class WarningOnCompilationEndedAnalyzer : AbstractMockAnalyzer, ICompilationAnalyzer
        {
            public const string Id = "CompilationEnded";
            private static DiagnosticDescriptor rule = GetRule(Id);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(rule);
                }
            }

            public void AnalyzeCompilation(Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, Location.None, Id));
            }
        }

        // Produces a warning on the declaration of any symbol whose name starts with a specified prefix
        protected class WarningOnNamePrefixDeclarationAnalyzer : AbstractMockAnalyzer, ISymbolAnalyzer
        {
            public const string Id = "Declaration";
            private static DiagnosticDescriptor rule = GetRule(Id);

            private string errorSymbolPrefix;
            private static readonly ImmutableArray<SymbolKind> kindsOfInterest = ImmutableArray.Create(SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.NamedType, SymbolKind.Namespace, SymbolKind.Property);

            public WarningOnNamePrefixDeclarationAnalyzer(string errorSymbolPrefix)
            {
                this.errorSymbolPrefix = errorSymbolPrefix;
            }

            public virtual ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                if (symbol.Name.StartsWith(this.errorSymbolPrefix))
                {
                    addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, symbol.Locations.First(), symbol.Name));
                }
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(rule);
                }
            }
        }

        // Produces a warning on the declaration of any named type
        protected class WarningOnTypeDeclarationAnalyzer : AbstractMockAnalyzer, ISymbolAnalyzer
        {
            public const string TypeId = "TypeDeclaration";
            private static DiagnosticDescriptor rule = GetRule(TypeId);
            private static readonly ImmutableArray<SymbolKind> kindsOfInterest = ImmutableArray.Create(SymbolKind.NamedType);

            public virtual ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, symbol.Locations.First(), symbol.Name));
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(rule);
                }
            }
        }

        // Produces a warning for the end of every code body and every invocation expression within that code body
        protected class WarningOnCodeBodyAnalyzer : AbstractMockAnalyzer, ICodeBlockNestedAnalyzerFactory
        {
            public const string Id = "CodeBody";
            private static DiagnosticDescriptor rule = GetRule(Id);

            private string language;

            public WarningOnCodeBodyAnalyzer(string language)
            {
                this.language = language;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(rule);
                }
            }

            public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                if (this.language == LanguageNames.CSharp)
                {
                    return new CSharpCodeBodyAnalyzer();
                }
                else
                {
                    return new BasicCodeBodyAnalyzer();
                }
            }

            protected class CSharpCodeBodyAnalyzer : ISyntaxNodeAnalyzer<CSharp.SyntaxKind>, ICodeBlockAnalyzer
            {
                public ImmutableArray<CSharp.SyntaxKind> SyntaxKindsOfInterest
                {
                    get
                    {
                        return ImmutableArray.Create(CSharp.SyntaxKind.InvocationExpression);
                    }
                }

                public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                {
                    get
                    {
                        return ImmutableArray.Create(rule);
                    }
                }

                public void AnalyzeCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
                {
                    addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, ownerSymbol.Locations.First(), ownerSymbol.Name + ":end"));
                }

                public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
                {
                    addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, node.GetLocation(), node.ToFullString()));
                }
            }

            protected class BasicCodeBodyAnalyzer : ISyntaxNodeAnalyzer<VisualBasic.SyntaxKind>, ICodeBlockAnalyzer
            {
                public ImmutableArray<VisualBasic.SyntaxKind> SyntaxKindsOfInterest
                {
                    get
                    {
                        return ImmutableArray.Create(VisualBasic.SyntaxKind.InvocationExpression);
                    }
                }

                public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
                {
                    get
                    {
                        return ImmutableArray.Create(rule);
                    }
                }

                public void AnalyzeCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
                {
                    addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, ownerSymbol.Locations.First(), ownerSymbol.Name + ":end"));
                }

                public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
                {
                    addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, node.GetLocation(), node.ToFullString()));
                }
            }
        }

        // Produces a warning for each comment trivium in a syntax tree
        protected class WarningOnCommentAnalyzer : AbstractMockAnalyzer, ISyntaxTreeAnalyzer
        {
            public const string Id = "Comment";
            private static DiagnosticDescriptor rule = GetRule(Id);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(rule);
                }
            }

            public void AnalyzeSyntaxTree(SyntaxTree tree, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var comments = tree.GetRoot().DescendantTrivia()
                    .Where(t =>
                        t.CSharpKind() == CSharp.SyntaxKind.SingleLineCommentTrivia ||
                        t.CSharpKind() == CSharp.SyntaxKind.MultiLineCommentTrivia ||
                        t.VisualBasicKind() == VisualBasic.SyntaxKind.CommentTrivia);

                foreach (var comment in comments)
                {
                    addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, comment.GetLocation(), comment.ToFullString()));
                }
            }
        }

        // Produces a warning for each token overlapping the given span in a syntax tree
        protected class WarningOnTokenAnalyzer : AbstractMockAnalyzer, ISyntaxTreeAnalyzer
        {
            public const string Id = "Token";
            private static DiagnosticDescriptor rule = GetRule(Id);
            private IList<TextSpan> spans;

            public WarningOnTokenAnalyzer(IList<TextSpan> spans)
            {
                this.spans = spans;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(rule);
                }
            }

            public void AnalyzeSyntaxTree(SyntaxTree tree, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                foreach (var nodeOrToken in tree.GetRoot().DescendantNodesAndTokens())
                {
                    if (nodeOrToken.IsToken && this.spans.Any(s => s.OverlapsWith(nodeOrToken.FullSpan)))
                    {
                        addDiagnostic(CodeAnalysis.Diagnostic.Create(rule, nodeOrToken.GetLocation(), nodeOrToken.ToFullString()));
                    }
                }
            }
        }

        protected static DiagnosticDescriptor GetRule(string id)
        {
            return new DiagnosticDescriptor(
                id,
                id,
                TestDiagnosticMessageTemplate,
                TestDiagnosticCategory,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }
    }
}