﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionCodeFixHelpers;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentCodeFixProvider<
        TStatementSyntax,
        TIfStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TExpressionSyntax,
        TConditionalExpressionSyntax>
        : AbstractUseConditionalExpressionCodeFixProvider<TStatementSyntax, TIfStatementSyntax, TExpressionSyntax, TConditionalExpressionSyntax>
        where TStatementSyntax : SyntaxNode
        where TIfStatementSyntax : TStatementSyntax
        where TLocalDeclarationStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
    {
        protected abstract TVariableDeclaratorSyntax WithInitializer(TVariableDeclaratorSyntax variable, TExpressionSyntax value);
        protected abstract TVariableDeclaratorSyntax GetDeclaratorSyntax(IVariableDeclaratorOperation declarator);
        protected abstract TLocalDeclarationStatementSyntax AddSimplificationToType(TLocalDeclarationStatementSyntax updatedLocalDeclaration);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns 'true' if a multi-line conditional was created, and thus should be
        /// formatted specially.
        /// </summary>
        protected override async Task FixOneAsync(
            Document document, Diagnostic diagnostic,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var ifStatement = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement, cancellationToken)!;

            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                    syntaxFacts, ifOperation,
                    out var trueStatement, out var falseStatement,
                    out var trueAssignment, out var falseAssignment))
            {
                return;
            }

            var conditionalExpression = await CreateConditionalExpressionAsync(
                document, ifOperation,
                trueStatement, falseStatement,
                trueAssignment?.Value ?? trueStatement,
                falseAssignment?.Value ?? falseStatement,
                trueAssignment?.IsRef == true, cancellationToken).ConfigureAwait(false);

            // See if we're assigning to a variable declared directly above the if statement. If so,
            // try to inline the conditional directly into the initializer for that variable.
            if (TryConvertWhenAssignmentToLocalDeclaredImmediateAbove(
                    syntaxFacts, editor, ifOperation,
                    trueAssignment, falseAssignment, conditionalExpression))
            {
                return;

            }

            // If not, just replace the if-statement with a single assignment of the new
            // conditional.
            ConvertOnlyIfToConditionalExpression(
                editor, ifOperation, (trueAssignment ?? falseAssignment)!, conditionalExpression);
        }

        private void ConvertOnlyIfToConditionalExpression(
            SyntaxEditor editor,
            IConditionalOperation ifOperation,
            ISimpleAssignmentOperation assignment,
            TExpressionSyntax conditionalExpression)
        {
            var generator = editor.Generator;
            var ifStatement = (TIfStatementSyntax)ifOperation.Syntax;
            var expressionStatement = (TStatementSyntax)generator.ExpressionStatement(
                generator.AssignmentStatement(
                    assignment.Target.Syntax,
                    conditionalExpression)).WithTriviaFrom(ifStatement);

            editor.ReplaceNode(
                ifOperation.Syntax,
                WrapWithBlockIfAppropriate(ifStatement, expressionStatement));
        }

        private bool TryConvertWhenAssignmentToLocalDeclaredImmediateAbove(
            ISyntaxFactsService syntaxFacts, SyntaxEditor editor, IConditionalOperation ifOperation,
            ISimpleAssignmentOperation? trueAssignment,
            ISimpleAssignmentOperation? falseAssignment,
            TExpressionSyntax conditionalExpression)
        {
            if (!TryFindMatchingLocalDeclarationImmediatelyAbove(
                    ifOperation, trueAssignment, falseAssignment,
                    out var localDeclarationOperation, out var declarator))
            {
                return false;
            }

            // We found a valid local declaration right above the if-statement.
            var localDeclaration = localDeclarationOperation.Syntax;
            var variable = GetDeclaratorSyntax(declarator);

            // Initialize that variable with the conditional expression.
            var updatedVariable = WithInitializer(variable, conditionalExpression);

            // Because we merged the initialization and the variable, the variable may now be able
            // to use 'var' (c#), or elide its type (vb).  Add the simplification annotation
            // appropriately so that can happen later down the line.
            var updatedLocalDeclaration = localDeclaration.ReplaceNode(variable, updatedVariable);
            updatedLocalDeclaration = AddSimplificationToType(
                (TLocalDeclarationStatementSyntax)updatedLocalDeclaration);

            editor.ReplaceNode(localDeclaration, updatedLocalDeclaration);
            editor.RemoveNode(ifOperation.Syntax, GetRemoveOptions(syntaxFacts, ifOperation.Syntax));
            return true;
        }

        private bool TryFindMatchingLocalDeclarationImmediatelyAbove(
            IConditionalOperation ifOperation,
            ISimpleAssignmentOperation? trueAssignment,
            ISimpleAssignmentOperation? falseAssignment,
            [NotNullWhen(true)] out IVariableDeclarationGroupOperation? localDeclaration,
            [NotNullWhen(true)] out IVariableDeclaratorOperation? declarator)
        {
            localDeclaration = null;
            declarator = null;

            ILocalSymbol? local = null;
            if (trueAssignment != null)
            {
                if (!(trueAssignment.Target is ILocalReferenceOperation trueLocal))
                    return false;

                local = trueLocal.Local;
            }

            if (falseAssignment != null)
            {
                if (!(falseAssignment.Target is ILocalReferenceOperation falseLocal))
                    return false;

                // See if both assignments are to the same local.
                if (local != null && !Equals(local, falseLocal.Local))
                    return false;

                local = falseLocal.Local;
            }

            // We weren't assigning to a local.
            if (local == null)
                return false;

            // If so, see if that local was declared immediately above the if-statement.
            if (!(ifOperation.Parent is IBlockOperation parentBlock))
            {
                return false;
            }

            var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
            if (ifIndex <= 0)
            {
                return false;
            }

            localDeclaration = parentBlock.Operations[ifIndex - 1] as IVariableDeclarationGroupOperation;
            if (localDeclaration == null)
            {
                return false;
            }

            if (localDeclaration.IsImplicit)
            {
                return false;
            }

            if (localDeclaration.Declarations.Length != 1)
            {
                return false;
            }

            var declaration = localDeclaration.Declarations[0];
            var declarators = declaration.Declarators;
            if (declarators.Length != 1)
            {
                return false;
            }

            declarator = declarators[0];
            var variable = declarator.Symbol;
            if (!Equals(variable, local))
            {
                // wasn't a declaration of the local we're assigning to.
                return false;
            }

            var variableInitializer = declarator.Initializer ?? declaration.Initializer;
            if (variableInitializer?.Value != null)
            {
                var unwrapped = UnwrapImplicitConversion(variableInitializer.Value);
                // the variable has to either not have an initializer, or it needs to be basic
                // literal/default expression.
                if (!(unwrapped is ILiteralOperation) &&
                    !(unwrapped is IDefaultValueOperation))
                {
                    return false;
                }
            }

            // If the variable is referenced in the condition of the 'if' block, we can't merge the
            // declaration and assignments.
            return !ReferencesLocalVariable(ifOperation.Condition, variable);
        }

        private bool ReferencesLocalVariable(IOperation operation, ILocalSymbol variable)
        {
            if (operation is ILocalReferenceOperation localReference &&
                Equals(variable, localReference.Local))
            {
                return true;
            }

            foreach (var child in operation.Children)
            {
                if (ReferencesLocalVariable(child, variable))
                {
                    return true;
                }
            }

            return false;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Convert_to_conditional_expression, createChangedDocument, IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId)
            {
            }
        }
    }
}
