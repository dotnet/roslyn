// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics
{
    public partial class SuppressMessageAttributeTests
    {
        private const string TestDiagnosticKind = "Test";
        private const string TestDiagnosticMessageTemplate = "{0}";

        private abstract class AbstractMockAnalyzer : IDiagnosticAnalyzer
        {
            public abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        }

        private class WarningOnAnalysisCompletedAnalyzer : AbstractMockAnalyzer, ICompilationEndedAnalyzer
        {
            public const string Id = "AnalysisCompleted";
            private static DiagnosticDescriptor Rule = GetRule(Id);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public void OnCompilationEnded(Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                addDiagnostic(Diagnostic.Create(Rule, Location.None, Id));
            }
        }

        // Produces a warning on the declaration of any symbol whose name starts with a specified prefix
        private class WarningOnNamePrefixDeclarationAnalyzer : AbstractMockAnalyzer, ISymbolAnalyzer
        {
            public const string Id = "Declaration";
            private static DiagnosticDescriptor Rule = GetRule(Id);

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

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                if (symbol.Name.StartsWith(this.errorSymbolPrefix))
                {
                    addDiagnostic(Diagnostic.Create(Rule, symbol.Locations.First(), symbol.Name));
                }
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }
        }

        // Produces a warning on the declaration of any named type
        private class WarningOnTypeDeclarationAnalyzer : AbstractMockAnalyzer, ISymbolAnalyzer
        {
            public const string TypeId = "TypeDeclaration";
            private static DiagnosticDescriptor Rule = GetRule(TypeId);
            private static readonly ImmutableArray<SymbolKind> kindsOfInterest = ImmutableArray.Create(SymbolKind.NamedType);

            public virtual ImmutableArray<SymbolKind> SymbolKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                addDiagnostic(Diagnostic.Create(Rule, symbol.Locations.First(), symbol.Name));
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }
        }

        // Produces a warning for the start and end of every code body and every invocation expression within that code body
        private class WarningOnCodeBodyAnalyzer : AbstractMockAnalyzer, ICodeBlockStartedAnalyzer
        {
            public const string Id = "CodeBody";
            private static DiagnosticDescriptor Rule = GetRule(Id);

            private string language;

            public WarningOnCodeBodyAnalyzer(string language)
            {
                this.language = language;
            }

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public ICodeBlockEndedAnalyzer OnCodeBlockStarted(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                addDiagnostic(Diagnostic.Create(Rule, ownerSymbol.Locations.First(), ownerSymbol.Name + ":start"));

                if (this.language == LanguageNames.CSharp)
                {
                    return new CSharpCodeBodyAnalyzer();
                }
                else
                {
                    return new BasicCodeBodyAnalyzer();
                }
            }

            private class CSharpCodeBodyAnalyzer : ISyntaxNodeAnalyzer<CSharp.SyntaxKind>, ICodeBlockEndedAnalyzer
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
                        return ImmutableArray.Create(Rule);
                    }
                }

                public void OnCodeBlockEnded(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
                {
                    addDiagnostic(Diagnostic.Create(Rule, ownerSymbol.Locations.First(), ownerSymbol.Name + ":end"));
                }

                public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
                {
                    addDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), node.ToFullString()));
                }
            }

            private class BasicCodeBodyAnalyzer : ISyntaxNodeAnalyzer<VisualBasic.SyntaxKind>, ICodeBlockEndedAnalyzer
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
                        return ImmutableArray.Create(Rule);
                    }
                }

                public void OnCodeBlockEnded(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
                {
                    addDiagnostic(Diagnostic.Create(Rule, ownerSymbol.Locations.First(), ownerSymbol.Name + ":end"));
                }

                public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
                {
                    addDiagnostic(Diagnostic.Create(Rule, node.GetLocation(), node.ToFullString()));
                }
            }
        }

        // Produces a warning for each single line comment trivium in a syntax tree
        private class WarningOnSingleLineCommentAnalyzer : AbstractMockAnalyzer, ISyntaxTreeAnalyzer
        {
            public const string Id = "Syntax";
            private static DiagnosticDescriptor Rule = GetRule(Id);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public void AnalyzeSyntaxTree(SyntaxTree tree, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                var comments = tree.GetRoot().DescendantTrivia()
                    .Where(t =>
                        t.CSharpKind() == CSharp.SyntaxKind.SingleLineCommentTrivia ||
                        t.VisualBasicKind() == VisualBasic.SyntaxKind.CommentTrivia);

                foreach (var comment in comments)
                {
                    addDiagnostic(Diagnostic.Create(Rule, comment.GetLocation(), comment.ToFullString()));
                }
            }
        }

        private static DiagnosticDescriptor GetRule(string id)
        {
            return new DiagnosticDescriptor(
                id,
                TestDiagnosticKind,
                id,
                TestDiagnosticMessageTemplate,
                TestDiagnosticKind,
                DiagnosticSeverity.Warning);
        }
    }
}
