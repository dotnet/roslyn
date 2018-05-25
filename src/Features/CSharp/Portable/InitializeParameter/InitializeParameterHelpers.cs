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
        public static SyntaxNode GetBody(BaseMethodDeclarationSyntax containingMember)
            => containingMember.Body ?? (SyntaxNode)containingMember.ExpressionBody;

        public static bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => compilation.ClassifyConversion(source: source, destination: destination).IsImplicit;

        public static SyntaxNode TryGetLastStatement(IBlockOperation blockStatementOpt)
            => blockStatementOpt?.Syntax is BlockSyntax block
                ? block.Statements.LastOrDefault()
                : blockStatementOpt?.Syntax;

        public static void InsertStatement(
            SyntaxEditor editor,
            BaseMethodDeclarationSyntax methodDeclaration,
            SyntaxNode statementToAddAfterOpt,
            StatementSyntax statement)
        {
            if (methodDeclaration.ExpressionBody != null)
            {
                // If this is a => method, then we'll have to convert the method to have a block
                // body.  Add the new statement as the first/last statement of the new block 
                // depending if we were asked to go after something or not.
                if (!methodDeclaration.ExpressionBody.TryConvertToStatement(methodDeclaration.SemicolonToken, CreateReturnStatement(methodDeclaration), out var declaration))
                {
                    return;
                }

                if (statementToAddAfterOpt == null)
                {
                    editor.SetStatements(methodDeclaration, ImmutableArray.Create(statement, declaration));
                }
                else
                {
                    editor.SetStatements(methodDeclaration, ImmutableArray.Create(declaration, statement));
                }
            }
            else if (methodDeclaration.Body != null)
            {
                // Look for the statement we were asked to go after.
                var block = methodDeclaration.Body;
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
                editor.ReplaceNode(
                    methodDeclaration,
                    methodDeclaration.WithSemicolonToken(default)
                                     .WithBody(SyntaxFactory.Block(statement)));
            }
        }

        private static bool CreateReturnStatement(BaseMethodDeclarationSyntax declarationSyntax)
        {
            switch(declarationSyntax)
            {
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
