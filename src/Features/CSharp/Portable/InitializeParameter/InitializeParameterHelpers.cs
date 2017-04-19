// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    internal static class InitializeParameterHelpers
    {
        public static SyntaxNode GetBody(BaseMethodDeclarationSyntax containingMember)
            => containingMember.Body ?? (SyntaxNode)containingMember.ExpressionBody;

        public static bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
            => compilation.ClassifyConversion(source: source, destination: destination).IsImplicit;

        public static SyntaxNode GetLastStatement(IBlockStatement blockStatement)
            => blockStatement.Syntax is BlockSyntax block
                ? block.Statements.LastOrDefault()
                : blockStatement.Syntax;

        public static void InsertStatement(
            SyntaxEditor editor,
            SyntaxNode body,
            SyntaxNode statementToAddAfterOpt,
            StatementSyntax statement)
        {
            var generator = editor.Generator;

            if (body is ArrowExpressionClauseSyntax arrowExpression)
            {
                // If this is a => method, then we'll have to convert the method to have a block
                // body.  Add the new statement as the first/last statement of the new block 
                // depending if we were asked to go after something or not.
                var methodBase = (BaseMethodDeclarationSyntax)body.Parent;

                if (statementToAddAfterOpt == null)
                {
                    editor.SetStatements(methodBase,
                        ImmutableArray.Create(
                            statement,
                            generator.ExpressionStatement(arrowExpression.Expression)));
                }
                else
                {
                    editor.SetStatements(methodBase,
                        ImmutableArray.Create(
                            generator.ExpressionStatement(arrowExpression.Expression),
                            statement));
                }
            }
            else if (body is BlockSyntax block)
            {
                // Look for the statement we were asked to go after.
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
                throw new InvalidOperationException();
            }
        }
    }
}