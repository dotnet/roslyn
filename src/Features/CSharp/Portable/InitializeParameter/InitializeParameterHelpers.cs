// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    internal static class InitializeParameterHelpers
    {
        private static (BlockSyntax body, ArrowExpressionClauseSyntax expression, SyntaxToken semicolonToken) GetFunctionSyntaxElements(SyntaxNode functionDeclaration)
        {
            Debug.Assert(functionDeclaration is BaseMethodDeclarationSyntax || functionDeclaration is LocalFunctionStatementSyntax);

            return
                functionDeclaration is BaseMethodDeclarationSyntax methodDeclaration
                    ? (methodDeclaration.Body, methodDeclaration.ExpressionBody, methodDeclaration.SemicolonToken) :
                functionDeclaration is LocalFunctionStatementSyntax localFunction
                    ? (localFunction.Body, localFunction.ExpressionBody, localFunction.SemicolonToken) : default;
        }

        private static SyntaxNode FunctionDeclarationWithBody(SyntaxNode functionDeclaration, BlockSyntax body)
        {
            Debug.Assert(functionDeclaration is BaseMethodDeclarationSyntax || functionDeclaration is LocalFunctionStatementSyntax);

            return
                functionDeclaration is BaseMethodDeclarationSyntax methodDeclaration
                    ? (SyntaxNode)methodDeclaration.WithSemicolonToken(default).WithBody(body) :
                functionDeclaration is LocalFunctionStatementSyntax localFunction
                    ? (SyntaxNode)localFunction.WithSemicolonToken(default).WithBody(body) : default;
        }

        public static SyntaxNode GetFunctionDeclaration(ParameterListSyntax parameterList)
            => parameterList.FirstAncestorOrSelf<SyntaxNode>(node => node is BaseMethodDeclarationSyntax || node is LocalFunctionStatementSyntax);

        public static SyntaxNode GetBody(SyntaxNode functionDeclaration)
        {
            var (body, expressionBody, _) = GetFunctionSyntaxElements(functionDeclaration);
            return (SyntaxNode)body ?? expressionBody;
        }

        public static bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => compilation.ClassifyConversion(source: source, destination: destination).IsImplicit;

        public static SyntaxNode TryGetLastStatement(IBlockOperation blockStatementOpt)
            => blockStatementOpt?.Syntax is BlockSyntax block
                ? block.Statements.LastOrDefault()
                : blockStatementOpt?.Syntax;

        public static void InsertStatement(
            SyntaxEditor editor,
            SyntaxNode functionDeclaration,
            SyntaxNode statementToAddAfterOpt,
            StatementSyntax statement)
        {
            var (body, expressionBody, semicolonToken) = GetFunctionSyntaxElements(functionDeclaration);

            if (expressionBody != null)
            {
                // If this is a => method, then we'll have to convert the method to have a block
                // body.  Add the new statement as the first/last statement of the new block 
                // depending if we were asked to go after something or not.
                if (!expressionBody.TryConvertToStatement(semicolonToken, CreateReturnStatement(functionDeclaration), out var declaration))
                {
                    return;
                }

                if (statementToAddAfterOpt == null)
                {
                    editor.SetStatements(functionDeclaration, ImmutableArray.Create(statement, declaration));
                }
                else
                {
                    editor.SetStatements(functionDeclaration, ImmutableArray.Create(declaration, statement));
                }
            }
            else if (body != null)
            {
                // Look for the statement we were asked to go after.
                var block = body;
                var indexToAddAfter = block.Statements.IndexOf(s => s == statementToAddAfterOpt);
                if (indexToAddAfter >= 0)
                {
                    // If we find it, then insert the new statement after it.
                    editor.InsertAfter(block.Statements[indexToAddAfter], statement);
                }
                else if (block.Statements.Count > 0)
                {
                    // Otherwise, if we have multiple statements already, then insert ourselves
                    // before the first one.
                    editor.InsertBefore(block.Statements[0], statement);
                }
                else
                {
                    // Otherwise, we have no statements in this block.  Add the new statement
                    // as the single statement the block will have.
                    Debug.Assert(block.Statements.Count == 0);
                    editor.ReplaceNode(block, block.AddStatements(statement));
                }
            }
            else
            {
                var newDeclaration = FunctionDeclarationWithBody(functionDeclaration, SyntaxFactory.Block(statement));
                editor.ReplaceNode(functionDeclaration, newDeclaration);
            }
        }

        private static bool CreateReturnStatement(SyntaxNode declarationSyntax)
        {
            switch(declarationSyntax)
            {
                case LocalFunctionStatementSyntax function:
                    return !function.ReturnType.IsVoid();
                case MethodDeclarationSyntax method:
                    return !method.ReturnType.IsVoid();
                case ConversionOperatorDeclarationSyntax _:
                case OperatorDeclarationSyntax _:
                    return true;
                case DestructorDeclarationSyntax _:
                case ConstructorDeclarationSyntax _:
                    return false;
            }

            return false;
        }
    }
}
