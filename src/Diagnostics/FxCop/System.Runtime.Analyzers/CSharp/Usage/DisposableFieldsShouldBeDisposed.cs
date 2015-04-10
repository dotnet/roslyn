// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpCA2213DiagnosticAnalyzer : DisposableFieldsShouldBeDisposedAnalyzer
    {
        protected override AbstractAnalyzer GetAnalyzer(CompilationStartAnalysisContext context, INamedTypeSymbol disposableType)
        {
            Analyzer analyzer = new Analyzer(disposableType);
            context.RegisterSyntaxNodeAction(analyzer.AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.UsingStatement);
            return analyzer;
        }

        private sealed class Analyzer : AbstractAnalyzer
        {
            public Analyzer(INamedTypeSymbol disposableType)
                : base(disposableType)
            {
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                if (context.Node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)context.Node;
                    if (memberAccess.Name != null && memberAccess.Name.Identifier.ValueText == Dispose)
                    {
                        var methodSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Name).Symbol as IMethodSymbol;
                        if (methodSymbol != null && methodSymbol.MetadataName == Dispose)
                        {
                            var fieldSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol as IFieldSymbol;
                            if (fieldSymbol != null)
                            {
                                NoteFieldDisposed(fieldSymbol);
                            }
                        }
                    }
                }
                else if (context.Node.Kind() == SyntaxKind.UsingStatement)
                {
                    var usingStatementExpression = ((UsingStatementSyntax)context.Node).Expression;
                    if (usingStatementExpression != null)
                    {
                        var fieldSymbol = context.SemanticModel.GetSymbolInfo(usingStatementExpression).Symbol as IFieldSymbol;
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
