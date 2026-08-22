// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConvertToConditional
{
    internal class ReturnConditionalAnalyzer : ConditionalAnalyzer
    {
        private ReturnConditionalAnalyzer(IfStatementSyntax ifStatement, SemanticModel semanticModel)
            : base(ifStatement, semanticModel)
        {
        }

        public static bool TryGetNewReturnStatement(IfStatementSyntax ifStatement, SemanticModel semanticModel, out ReturnStatementSyntax returnStatement)
        {
            returnStatement = null;

            ExpressionSyntax conditional = new ReturnConditionalAnalyzer(ifStatement, semanticModel).CreateConditional();
            if (conditional == null)
            {
                return false;
            }

            returnStatement = SyntaxFactory.ReturnStatement(conditional);

            return true;
        }

        protected override ExpressionSyntax CreateConditional()
        {
            if (!TryGetReturnStatements(IfStatement, out ReturnStatementSyntax whenTrueStatement, out ReturnStatementSyntax whenFalseStatement))
            {
                return null;
            }

            ExpressionSyntax whenTrue = whenTrueStatement.Expression;
            ExpressionSyntax whenFalse = whenFalseStatement.Expression;
            if (whenTrue == null || whenFalse == null)
            {
                return null;
            }

            MemberDeclarationSyntax parentMember = IfStatement.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            ISymbol memberSymbol = SemanticModel.GetDeclaredSymbol(parentMember);
            switch (memberSymbol.Kind)
            {
                case SymbolKind.Method:
                    IMethodSymbol methodSymbol = (IMethodSymbol)memberSymbol;
                    return !methodSymbol.ReturnsVoid
                        ? CreateConditional(whenTrue, whenFalse, methodSymbol.ReturnType)
                        : null;

                default:
                    return null;
            }
        }

        private static bool TryGetReturnStatements(IfStatementSyntax ifStatement, out ReturnStatementSyntax whenTrueStatement, out ReturnStatementSyntax whenFalseStatement)
        {
            Debug.Assert(ifStatement != null);
            Debug.Assert(ifStatement.Else != null);

            whenTrueStatement = null;
            whenFalseStatement = null;

            if (!(ifStatement.Statement.SingleStatementOrSelf() is ReturnStatementSyntax statement))
            {
                return false;
            }

            if (!(ifStatement.Else.Statement.SingleStatementOrSelf() is ReturnStatementSyntax elseStatement))
            {
                return false;
            }

            whenTrueStatement = statement;
            whenFalseStatement = elseStatement;
            return true;
        }
    }
}
