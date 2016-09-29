// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConvertToConditionalCS
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

            var conditional = new ReturnConditionalAnalyzer(ifStatement, semanticModel).CreateConditional();
            if (conditional == null)
            {
                return false;
            }

            returnStatement = SyntaxFactory.ReturnStatement(conditional);

            return true;
        }

        protected override ExpressionSyntax CreateConditional()
        {
            ReturnStatementSyntax whenTrueStatement;
            ReturnStatementSyntax whenFalseStatement;
            if (!TryGetReturnStatements(IfStatement, out whenTrueStatement, out whenFalseStatement))
            {
                return null;
            }

            var whenTrue = whenTrueStatement.Expression;
            var whenFalse = whenFalseStatement.Expression;
            if (whenTrue == null || whenFalse == null)
            {
                return null;
            }

            var parentMember = IfStatement.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            var memberSymbol = SemanticModel.GetDeclaredSymbol(parentMember);
            switch (memberSymbol.Kind)
            {
                case SymbolKind.Method:
                    var methodSymbol = (IMethodSymbol)memberSymbol;
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

            var statement = ifStatement.Statement.SingleStatementOrSelf() as ReturnStatementSyntax;
            if (statement == null)
            {
                return false;
            }

            var elseStatement = ifStatement.Else.Statement.SingleStatementOrSelf() as ReturnStatementSyntax;
            if (elseStatement == null)
            {
                return false;
            }

            whenTrueStatement = statement;
            whenFalseStatement = elseStatement;
            return true;
        }
    }
}
