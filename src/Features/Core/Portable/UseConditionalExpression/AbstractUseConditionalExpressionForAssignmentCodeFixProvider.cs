// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentCodeFixProvider<
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TExpressionSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TLocalDeclarationStatementSyntax : SyntaxNode
        where TVariableDeclaratorSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
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
            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                await FixOneAsync(
                    document, diagnostic, editor, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixOneAsync(
            Document document, Diagnostic diagnostic, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var ifStatement = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement);

            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                    syntaxFacts, ifOperation, 
                    out var trueAssignment, out var falseAssignment))
            {
                return;
            }

            var generator = editor.Generator;

            var conditionalExpression = (TExpressionSyntax)generator.ConditionalExpression(
                ifOperation.Condition.Syntax.WithoutTrivia(),
                GetValueFromAssignment(generator, trueAssignment),
                GetValueFromAssignment(generator, falseAssignment));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);

            if (TryConvertWhenAssignmentToLocalDeclaredImmediateAbove(
                    editor, ifOperation, trueAssignment, falseAssignment, conditionalExpression))
            {
                return;
            }

            ConvertOnlyIfToConditionalExpression(
                editor, ifOperation, trueAssignment, falseAssignment, conditionalExpression);
        }

        private SyntaxNode GetValueFromAssignment(SyntaxGenerator generator, ISimpleAssignmentOperation assignment)
            => assignment.Target.Type != null && assignment.Target.Type.TypeKind != TypeKind.Error
                ? generator.CastExpression(assignment.Target.Type, assignment.Value.Syntax.WithoutTrivia())
                : assignment.Value.Syntax.WithoutTrivia();

        private void ConvertOnlyIfToConditionalExpression(
            SyntaxEditor editor, IConditionalOperation ifOperation,
            ISimpleAssignmentOperation trueAssignment, ISimpleAssignmentOperation falseAssignment,
            TExpressionSyntax conditionalExpression)
        {
            var generator = editor.Generator;
            var assignment = generator.ExpressionStatement(generator.AssignmentStatement(
                trueAssignment.Target.Syntax, conditionalExpression).WithTriviaFrom(ifOperation.Syntax));
            editor.ReplaceNode(ifOperation.Syntax, assignment);
        }

        private bool TryConvertWhenAssignmentToLocalDeclaredImmediateAbove(
            SyntaxEditor editor, IConditionalOperation ifOperation,
            ISimpleAssignmentOperation trueAssignment, ISimpleAssignmentOperation falseAssignment,
            TExpressionSyntax conditionalExpression)
        {
            if (!TryFindMatchingLocalDeclarationImmediatelyAbove(
                    ifOperation, trueAssignment, falseAssignment,
                    out var localDeclarationOperation))
            {
                return false;
            }

            var localDeclaration = localDeclarationOperation.Syntax;
            var declarator = localDeclarationOperation.Declarations[0].Declarators[0];
            var variable = GetDeclaratorSyntax(declarator);

            var updatedVariable = WithInitializer(variable, conditionalExpression);

            var updatedLocalDeclaration = localDeclaration.ReplaceNode(variable, updatedVariable);
            updatedLocalDeclaration = AddSimplificationToType(
                (TLocalDeclarationStatementSyntax)updatedLocalDeclaration);

            editor.ReplaceNode(localDeclaration, updatedLocalDeclaration);
            editor.RemoveNode(ifOperation.Syntax, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepExteriorTrivia);
            return true;
        }

        private bool TryFindMatchingLocalDeclarationImmediatelyAbove(
            IConditionalOperation ifOperation, ISimpleAssignmentOperation trueAssignment, ISimpleAssignmentOperation falseAssignment, 
            out IVariableDeclarationGroupOperation localDeclaration)
        {
            localDeclaration = null;

            // See if both assignments are to the same local.
            if (!(trueAssignment.Target is ILocalReferenceOperation trueLocal) ||
                !(falseAssignment.Target is ILocalReferenceOperation falseLocal) ||
                !Equals(trueLocal.Local, falseLocal.Local))
            {
                return false;
            }

            // If so, see if that local was declared immediately above the if-statement.
            var parentBlock = ifOperation.Parent as IBlockOperation;
            if (parentBlock == null)
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

            if (localDeclaration.Declarations.Length != 1)
            {
                return false;
            }

            var declarationOperation = localDeclaration.Declarations[0];
            var declarators = declarationOperation.Declarators;
            if (declarators.Length != 1)
            {
                return false;
            }

            var declarator = declarators[0];
            var variable = declarator.Symbol;
            if (!Equals(variable, trueLocal.Local))
            {
                // wasn't a declaration of the local we're assigning to.
                return false;
            }

            var variableName = variable.Name;

            var variableInitializer = declarator.Initializer;
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

            return true;
        }

        private static IOperation UnwrapImplicitConversion(IOperation value)
            => value is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : value;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Convert_to_conditional_expression, createChangedDocument, IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId)
            {
            }
        }
    }
}
