// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA2213DiagnosticAnalyzer : CA2213DiagnosticAnalyzer
    {
        protected override AbstractAnalyzer GetAnalyzer(INamedTypeSymbol disposableType)
        {
            return new Analyzer(disposableType);
        }

        private sealed class Analyzer : AbstractAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
        {
            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.UsingStatement);

            public Analyzer(INamedTypeSymbol disposableType)
                : base(disposableType)
            {
            }

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                if (node.CSharpKind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)node;
                    if (memberAccess.Name != null && memberAccess.Name.Identifier.ValueText == Dispose)
                    {
                        var methodSymbol = semanticModel.GetSymbolInfo(memberAccess.Name).Symbol as IMethodSymbol;
                        if (methodSymbol != null && methodSymbol.MetadataName == Dispose)
                        {
                            var fieldSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol as IFieldSymbol;
                            if (fieldSymbol != null)
                            {
                                NoteFieldDisposed(fieldSymbol);
                            }
                        }
                    }
                }
                else if (node.CSharpKind() == SyntaxKind.UsingStatement)
                {
                    var usingStatementExpression = ((UsingStatementSyntax)node).Expression;
                    if (usingStatementExpression != null)
                    {
                        var fieldSymbol = semanticModel.GetSymbolInfo(usingStatementExpression).Symbol as IFieldSymbol;
                        if (fieldSymbol != null)
                        {
                            NoteFieldDisposed(fieldSymbol);
                        }
                    }
                }
            }
        }
    }
}
