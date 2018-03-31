// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionForAssignmentHelpers 
    {
        public static bool TryMatchPattern(
            IConditionalOperation ifOperation,
            out IVariableDeclarationGroupOperation localDeclarationStatement,
            out ISimpleAssignmentOperation trueAssignmentOperation,
            out ISimpleAssignmentOperation falseAssignmentOperation)
        {
            localDeclarationStatement = null;
            trueAssignmentOperation = null;
            falseAssignmentOperation = null;

            var parentBlock = ifOperation.Parent as IBlockOperation;
            if (parentBlock == null)
            {
                return false;
            }

            // var syntaxFacts = GetSyntaxFactsService();
            var ifIndex = parentBlock.Operations.IndexOf(ifOperation);
            if (ifIndex <= 0)
            {
                return false;
            }

            localDeclarationStatement = parentBlock.Operations[ifIndex - 1] as IVariableDeclarationGroupOperation;
            if (localDeclarationStatement == null)
            {
                return false;
            }

            if (localDeclarationStatement.Declarations.Length != 1)
            {
                return false;
            }

            var declarationOperation = localDeclarationStatement.Declarations[0];
            var declarators = declarationOperation.Declarators;
            if (declarators.Length != 1)
            {
                return false;
            }

            var declarator = declarators[0];
            var variable = declarator.Symbol;
            var variableName = variable.Name;

            var variableInitialier = declarator.Initializer;
            if (variableInitialier?.Value != null)
            {
                var unwrapped = UnwrapImplicitConversion(variableInitialier.Value);
                // the variable has to either not have an initializer, or it needs to be basic
                // literal/default expression.
                if (!(unwrapped is ILiteralOperation) &&
                    !(unwrapped is IDefaultValueOperation))
                {
                    return false;
                }
            }

            var trueStatement = ifOperation.WhenTrue;
            var falseStatement = ifOperation.WhenFalse;

            trueStatement = UnwrapSingleStatementBlock(trueStatement);
            falseStatement = UnwrapSingleStatementBlock(falseStatement);

            if (!(trueStatement is IExpressionStatementOperation trueExprStatement) ||
                !(falseStatement is IExpressionStatementOperation falseExprStatement) ||
                !(trueExprStatement.Operation is ISimpleAssignmentOperation trueAssignment) ||
                !(falseExprStatement.Operation is ISimpleAssignmentOperation falseAssignment) ||
                !(trueAssignment.Target is ILocalReferenceOperation trueReference) ||
                !(falseAssignment.Target is ILocalReferenceOperation falseReference) ||
                !trueReference.Local.Equals(variable) ||
                !falseReference.Local.Equals(variable))
            {
                return false;
            }

            trueAssignmentOperation = trueAssignment;
            falseAssignmentOperation = falseAssignment;
            return true;
        }

        private static IOperation UnwrapSingleStatementBlock(IOperation statement)
            => statement is IBlockOperation block && block.Operations.Length == 1
                ? block.Operations[0]
                : statement;

        private static IOperation UnwrapImplicitConversion(IOperation value)
            => value is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : value;
    }

    internal abstract class AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<
        TSyntaxKind>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() 
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_assignment), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Assignment_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }
         
        // protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract ImmutableArray<TSyntaxKind> GetIfStatementKinds();
        // protected abstract (TStatementSyntax, TStatementSyntax) GetTrueFalseStatements(TIfStatementSyntax ifStatement);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetIfStatementKinds());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = context.Node;
            if (ifStatement == null)
            {
                return;
            }

            var language = ifStatement.Language;
            var syntaxTree = ifStatement.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferConditionalExpressionOverAssignment, language);
            if (!option.Value)
            {
                return;
            }

            var ifOperation = (IConditionalOperation)context.SemanticModel.GetOperation(ifStatement);
            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                    ifOperation, out _, out _, out _))
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(ifStatement.GetLocation());
            context.ReportDiagnostic(Diagnostic.Create(
                this.CreateDescriptorWithSeverity(option.Notification.Value),
                ifStatement.GetFirstToken().GetLocation(),
                additionalLocations));
        }
    }

    internal abstract class AbstractUseConditionalExpressionForAssignmentCodeFixProvider<
        TLocalDeclarationStatement>
        : SyntaxEditorBasedCodeFixProvider
        where TLocalDeclarationStatement : SyntaxNode
    {
        protected abstract TLocalDeclarationStatement AddSimplificationToType(TLocalDeclarationStatement updatedLocalDeclaration);

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
            var ifStatement = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var ifOperation = (IConditionalOperation)semanticModel.GetOperation(ifStatement);

            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(ifOperation, 
                    out var localDeclarationOperation, 
                    out var trueAssignment, 
                    out var falseAssignment))
            {
                return;
            }

            var localDeclaration = localDeclarationOperation.Syntax;
            var declarator = localDeclarationOperation.Declarations[0].Declarators[0];
            var variable = declarator.Syntax;
            var generator = editor.Generator;

            var conditionalExpression = generator.ConditionalExpression(
                ifOperation.Condition.Syntax,
                generator.CastExpression(declarator.Symbol.Type, trueAssignment.Value.Syntax),
                generator.CastExpression(declarator.Symbol.Type, falseAssignment.Value.Syntax));

            conditionalExpression = conditionalExpression.WithAdditionalAnnotations(Simplifier.Annotation);

            var updatedVariable = generator.WithInitializer(
                variable, generator.EqualsValueClause(conditionalExpression));

            var updatedLocalDeclaration = localDeclaration.ReplaceNode(variable, updatedVariable);
            updatedLocalDeclaration = AddSimplificationToType((TLocalDeclarationStatement)updatedLocalDeclaration);

            editor.ReplaceNode(localDeclaration, updatedLocalDeclaration);
            editor.RemoveNode(ifStatement, SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepExteriorTrivia);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Assignment_can_be_simplified, createChangedDocument, FeaturesResources.Assignment_can_be_simplified)
            {
            }
        }
    }
}
