// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnusedParametersAndValues;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnusedParametersAndValues
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedValues), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal class CSharpRemoveUnusedValuesCodeFixProvider :
        AbstractRemoveUnusedValuesCodeFixProvider<ExpressionSyntax, StatementSyntax, BlockSyntax,
            ExpressionStatementSyntax, LocalDeclarationStatementSyntax, VariableDeclaratorSyntax,
            ForEachStatementSyntax, SwitchSectionSyntax, SwitchLabelSyntax, CatchClauseSyntax, CatchClauseSyntax>
    {
        [ImportingConstructor]
        public CSharpRemoveUnusedValuesCodeFixProvider()
        {
        }

        protected override BlockSyntax WrapWithBlockIfNecessary(IEnumerable<StatementSyntax> statements)
            => SyntaxFactory.Block(statements);

        protected override SyntaxToken GetForEachStatementIdentifier(ForEachStatementSyntax node)
            => node.Identifier;

        protected override SyntaxNode TryUpdateNameForFlaggedNode(SyntaxNode node, SyntaxToken newName)
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
                    return newName.ValueText == AbstractRemoveUnusedParametersAndValuesDiagnosticAnalyzer.DiscardVariableName
                        ? SyntaxFactory.DiscardDesignation().WithTriviaFrom(node)
                        : (SyntaxNode)SyntaxFactory.SingleVariableDesignation(newName).WithTriviaFrom(node);

                case SyntaxKind.CatchDeclaration:
                    var catchDeclaration = (CatchDeclarationSyntax)node;
                    return catchDeclaration.WithIdentifier(newName.WithTriviaFrom(catchDeclaration.Identifier));

                default:
                    Debug.Fail($"Unexpected node kind for local/parameter declaration or reference: '{node.Kind()}'");
                    return null;
            }
        }

        protected override void InsertAtStartOfSwitchCaseBlockForDeclarationInCaseLabelOrClause(SwitchSectionSyntax switchCaseBlock, SyntaxEditor editor, LocalDeclarationStatementSyntax declarationStatement)
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

        protected override SyntaxNode GetReplacementNodeForCompoundAssignment(
            SyntaxNode originalCompoundAssignment,
            SyntaxNode newAssignmentTarget,
            SyntaxEditor editor,
            ISyntaxFactsService syntaxFacts)
        {
            // 1. Compound assignment is changed to simple assignment.
            // For example, "x += MethodCall();", where assignment to 'x' is redundant
            // is replaced with "_ = MethodCall();" or "var unused = MethodCall();
            //
            // 2. Null coalesce assignment is changed to assignment with null coalesce
            // expression on the right.
            // For example, "x ??= MethodCall();", where assignment to 'x' is redundant
            // is replaced with "_ = x ?? MethodCall();" or "var unused = x ?? MethodCall();
            //
            // 3. However, if the node is not parented by an expression statement then we
            // don't generate an assignment, but just the expression.
            // For example, "return x += MethodCall();" is replaced with "return x + MethodCall();"
            // and "return x ??= MethodCall();" is replaced with "return x ?? MethodCall();"

            if (!(originalCompoundAssignment is AssignmentExpressionSyntax assignmentExpression))
            {
                Debug.Fail($"Unexpected kind for originalCompoundAssignment: {originalCompoundAssignment.Kind()}");
                return originalCompoundAssignment;
            }

            var leftOfAssignment = assignmentExpression.Left;
            var rightOfAssignment = assignmentExpression.Right;

            if (originalCompoundAssignment.Parent.IsKind(SyntaxKind.ExpressionStatement))
            {
                if (!originalCompoundAssignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                {
                    // Case 1. Simple compound assignment parented by an expression statement.
                    return editor.Generator.AssignmentStatement(newAssignmentTarget, rightOfAssignment);
                }
                else
                {
                    // Case 2. Null coalescing compound assignment parented by an expression statement.
                    // Remove leading trivia from 'leftOfAssignment' as it should have been moved to 'newAssignmentTarget'.
                    leftOfAssignment = leftOfAssignment.WithoutLeadingTrivia();
                    return editor.Generator.AssignmentStatement(newAssignmentTarget,
                        SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, leftOfAssignment, rightOfAssignment));
                }
            }
            else
            {
                // Case 3. Compound assignment not parented by an expression statement.
                var mappedBinaryExpressionKind = originalCompoundAssignment.Kind().MapCompoundAssignmentKindToBinaryExpressionKind();
                if (mappedBinaryExpressionKind == SyntaxKind.None)
                {
                    return originalCompoundAssignment;
                }

                return SyntaxFactory.BinaryExpression(mappedBinaryExpressionKind, leftOfAssignment, rightOfAssignment);
            }
        }
    }
}
