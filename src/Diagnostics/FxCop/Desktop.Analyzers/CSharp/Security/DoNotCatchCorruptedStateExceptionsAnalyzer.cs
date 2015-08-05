// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Desktop.Analyzers.Common;

namespace Desktop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpDoNotCatchCorruptedStateExceptionsAnalyzer : DoNotCatchCorruptedStateExceptionsAnalyzer<SyntaxKind, CatchClauseSyntax, ThrowStatementSyntax>
    {
        protected override Analyzer GetAnalyzer(CompilationSecurityTypes compilationTypes, ISymbol owningSymbol, SyntaxNode codeBlock)
        {
            return new CSharpAnalyzer(compilationTypes, owningSymbol, codeBlock);        
        } 

        private sealed class CSharpAnalyzer : Analyzer
        {
            
            public CSharpAnalyzer(CompilationSecurityTypes compilationTypes, ISymbol owningSymbol, SyntaxNode codeBlock)
                : base(compilationTypes, owningSymbol, codeBlock)
            {}

            public override SyntaxKind CatchClauseKind
            {
                get
                {
                    return SyntaxKind.CatchClause;
                }
            }

            public override SyntaxKind ThrowStatementKind
            {
                get
                {
                    return SyntaxKind.ThrowStatement;
                }
            }

            protected override ISymbol GetExceptionTypeSymbolFromCatchClause(CatchClauseSyntax catchNode, SemanticModel model)
            {
                Debug.Assert(catchNode != null);
                var typeDeclNode = catchNode.Declaration;
                return typeDeclNode == null ? TypesOfInterest.SystemObject : SyntaxNodeHelper.GetSymbol(typeDeclNode.Type, model);
            }

            protected override bool IsThrowStatementWithNoArgument(ThrowStatementSyntax throwNode)
            {
                Debug.Assert(throwNode != null);
                return throwNode.Expression == null;
            }

            protected override bool IsCatchClause(SyntaxNode node)
            {
                Debug.Assert(node != null);
                return node.Kind() == SyntaxKind.CatchClause;
            }

            protected override bool IslambdaExpression(SyntaxNode node)
            {
                Debug.Assert(node != null);
                SyntaxKind kind = node.Kind();
                return kind == SyntaxKind.AnonymousMethodExpression ||
                       kind == SyntaxKind.SimpleLambdaExpression ||
                       kind == SyntaxKind.ParenthesizedLambdaExpression;
            }
        }
    }
}
