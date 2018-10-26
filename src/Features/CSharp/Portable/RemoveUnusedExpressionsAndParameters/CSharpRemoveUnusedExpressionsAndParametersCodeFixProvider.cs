// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnusedExpressionsAndParameters;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedExpressionsAndParameters
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedExpressionsAndParameters), Shared]
    internal class CSharpRemoveUnusedExpressionsAndParametersCodeFixProvider:
        AbstractRemoveUnusedExpressionsAndParametersCodeFixProvider<ExpressionSyntax, StatementSyntax, BlockSyntax, 
                                                                    ExpressionStatementSyntax, LocalDeclarationStatementSyntax,
                                                                    VariableDeclaratorSyntax, ForEachStatementSyntax,
                                                                    SwitchSectionSyntax, SwitchLabelSyntax,
                                                                    CatchClauseSyntax, CatchClauseSyntax>
    {
        protected override BlockSyntax GenerateBlock(IEnumerable<StatementSyntax> statements)
            => SyntaxFactory.Block(statements);

        protected override SyntaxToken GetForEachStatementIdentifier(ForEachStatementSyntax node)
            => node.Identifier;

        protected override SyntaxNode UpdateNameForFlaggedNode(SyntaxNode node, SyntaxToken newName)
        {
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                    var identifierName = (IdentifierNameSyntax)node;
                    return identifierName.WithIdentifier(newName.WithTriviaFrom(identifierName.Identifier));

                case SyntaxKind.VariableDeclarator:
                    var variableDeclarator = (VariableDeclaratorSyntax)node;
                    return variableDeclarator.WithIdentifier(newName.WithTriviaFrom(variableDeclarator.Identifier));

                case SyntaxKind.SingleVariableDesignation:
                    return SyntaxFactory.SingleVariableDesignation(newName).WithTriviaFrom(node);

                case SyntaxKind.CatchDeclaration:
                    var catchDeclaration = (CatchDeclarationSyntax)node;
                    return catchDeclaration.WithIdentifier(newName.WithTriviaFrom(catchDeclaration.Identifier));

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        protected override ILocalSymbol GetSingleDeclaredLocal(LocalDeclarationStatementSyntax localDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(localDeclaration.Declaration.Variables.Count == 1);
            return (ILocalSymbol)semanticModel.GetDeclaredSymbol(localDeclaration.Declaration.Variables[0]);
        }

        protected override void InsertAtStartOfSwitchCaseBlock(SwitchSectionSyntax switchCaseBlock, SyntaxEditor editor, LocalDeclarationStatementSyntax declarationStatement)
        {
            var firstStatement = switchCaseBlock.Statements.FirstOrDefault();
            if (firstStatement != null)
            {
                editor.InsertBefore(firstStatement, declarationStatement);
            }
            else
            {
                // Switch section without any statements is an error case.
                // Insert before containing switch statement.
                editor.InsertBefore(switchCaseBlock.Parent, declarationStatement);
            }
        }

        protected override Task RemoveDiscardDeclarationsAsync(
            SyntaxNode memberDeclaration,
            SyntaxEditor editor,
            Document document,
            CancellationToken cancellationToken)
        {
            foreach (var child in memberDeclaration.DescendantNodes(descendIntoChildren: n => !(n is ExpressionSyntax)))
            {
                if (child is LocalDeclarationStatementSyntax localDeclarationStatement &&
                    localDeclarationStatement.Declaration.Variables.Any(v => v.Identifier.Text == "_"))
                {
                    ProcessVariableDeclarationWithDiscard(localDeclarationStatement, editor);
                }
                else if (child is CatchDeclarationSyntax catchDeclaration &&
                    catchDeclaration.Identifier.Text == "_")
                {
                    // "catch (Exception _)" => "catch (Exception)"
                    var newCatchDeclaration = catchDeclaration.WithIdentifier(default)
                                              .WithAdditionalAnnotations(Formatter.Annotation);
                    editor.ReplaceNode(catchDeclaration, newCatchDeclaration);
                }
            }

            return Task.CompletedTask;
        }

        private static void ProcessVariableDeclarationWithDiscard(
            LocalDeclarationStatementSyntax localDeclarationStatement,
            SyntaxEditor editor)
        {
            var statementsBuilder = ArrayBuilder<StatementSyntax>.GetInstance();
            var variableDeclaration = localDeclarationStatement.Declaration;
            var currentNonDiscardVariables = new SeparatedSyntaxList<VariableDeclaratorSyntax>();

            try
            {
                foreach (var variable in variableDeclaration.Variables)
                {
                    if (variable.Identifier.Text != "_")
                    {
                        currentNonDiscardVariables = currentNonDiscardVariables.Add(variable);
                    }
                    else
                    {
                        ProcessCurrentNonDiscardVariables();
                        ProcessDiscardVariable(variable);
                    }
                }

                ProcessCurrentNonDiscardVariables();

                if (statementsBuilder.Count == 0)
                {
                    return;
                }

                var leadingTrivia = variableDeclaration.Type.GetLeadingTrivia()
                                    .Concat(variableDeclaration.Type.GetTrailingTrivia());
                statementsBuilder[0] = statementsBuilder[0].WithLeadingTrivia(leadingTrivia);

                var last = statementsBuilder.Count - 1;
                var trailingTrivia = localDeclarationStatement.SemicolonToken.GetAllTrivia();
                statementsBuilder[last] = statementsBuilder[last].WithTrailingTrivia(trailingTrivia);

                if (localDeclarationStatement.Parent is BlockSyntax)
                {
                    editor.InsertAfter(localDeclarationStatement, statementsBuilder.Skip(1));
                    editor.ReplaceNode(localDeclarationStatement, statementsBuilder[0]);
                }
                else
                {
                    editor.ReplaceNode(localDeclarationStatement, SyntaxFactory.Block(statementsBuilder));
                }
            }
            finally
            {
                statementsBuilder.Free();
            }

            return;

            // Local functions.
            void ProcessCurrentNonDiscardVariables()
            {
                if (currentNonDiscardVariables.Count > 0)
                {
                    var statement = SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(variableDeclaration.Type, currentNonDiscardVariables))
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    statementsBuilder.Add(statement);
                    currentNonDiscardVariables = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
                }
            }

            void ProcessDiscardVariable(VariableDeclaratorSyntax variable)
            {
                if (variable.Initializer != null)
                {
                    statementsBuilder.Add(SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: SyntaxFactory.IdentifierName(variable.Identifier),
                            operatorToken: variable.Initializer.EqualsToken,
                            right: variable.Initializer.Value))
                            .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
        }
    }
}
