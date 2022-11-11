// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Analyzers.UseCoalesceExpression
{
    internal abstract class AbstractUseCoalesceExpressionForIfNullCheckDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TIfStatementSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TIfStatementSyntax : TStatementSyntax
    {
        protected AbstractUseCoalesceExpressionForIfNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCoalesceExpressionForIfNullCheckDiagnosticId,
                   EnforceOnBuildValues.UseCoalesceExpression,
                   CodeStyleOptions2.PreferCoalesceExpression,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_coalesce_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract bool IsNullCheck(TExpressionSyntax condition, [NotNullWhen(true)] out TExpressionSyntax? checkedExpression);
        protected abstract bool TryGetEmbeddedStatement(TIfStatementSyntax ifStatement, [NotNullWhen(true)] out TStatementSyntax? whenTrueStatement);
        protected abstract bool HasElseBlock(TIfStatementSyntax ifStatement);

        protected abstract TStatementSyntax? TryGetPreviousStatement(TIfStatementSyntax ifStatement);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
            context.RegisterSyntaxNodeAction(AnalyzeSyntax,
                syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.IfStatement));
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var ifStatement = (TIfStatementSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var option = context.GetAnalyzerOptions().PreferCoalesceExpression;
            if (!option.Value)
                return;

            var syntaxFacts = this.GetSyntaxFacts();
            var condition = (TExpressionSyntax)syntaxFacts.GetConditionOfIfStatement(ifStatement);

            if (!IsNullCheck(condition, out var checkedExpression))
                return;

            var previousStatement = TryGetPreviousStatement(ifStatement);
            if (previousStatement is null)
                return;

            if (HasElseBlock(ifStatement))
                return;

            if (!TryGetEmbeddedStatement(ifStatement, out var whenTrueStatement))
                return;

            if (syntaxFacts.IsThrowStatement(whenTrueStatement))
            {
                if (!syntaxFacts.SupportsThrowExpression(ifStatement.SyntaxTree.Options))
                    return;

                var thrownExpression = syntaxFacts.GetExpressionOfThrowStatement(whenTrueStatement);
                if (thrownExpression is null)
                    return;
            }

            TExpressionSyntax? expressionToCoalesce;

            if (syntaxFacts.IsLocalDeclarationStatement(previousStatement))
            {
                // var v = Expr();
                // if (v == null)
                //    ...

                if (!AnalyzeLocalDeclarationForm(previousStatement, out expressionToCoalesce))
                    return;
            }
            else if (syntaxFacts.IsAnyAssignmentStatement(previousStatement))
            {
                // v = Expr();
                // if (v == null)
                //    ...
                if (!AnalyzeAssignmentForm(previousStatement, out expressionToCoalesce))
                    return;
            }
            else
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                ifStatement.GetFirstToken().GetLocation(),
                option.Notification.Severity,
                ImmutableArray.Create(
                    expressionToCoalesce.GetLocation(),
                    ifStatement.GetLocation(),
                    whenTrueStatement.GetLocation()),
                properties: null));

            bool AnalyzeLocalDeclarationForm(
                TStatementSyntax localDeclarationStatement,
                [NotNullWhen(true)] out TExpressionSyntax? expressionToCoalesce)
            {
                expressionToCoalesce = null;

                // var v = Expr();
                // if (v == null)
                //    ...

                if (!syntaxFacts.IsIdentifierName(checkedExpression))
                    return false;

                var conditionIdentifier = syntaxFacts.GetIdentifierOfIdentifierName(checkedExpression).ValueText;

                var declarators = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                if (declarators.Count != 1)
                    return false;

                var declarator = declarators[0];
                var equalsValue = syntaxFacts.GetInitializerOfVariableDeclarator(declarator);
                if (equalsValue is null)
                    return false;

                if (syntaxFacts.GetValueOfEqualsValueClause(equalsValue) is not TExpressionSyntax initializer)
                    return false;

                expressionToCoalesce = initializer;

                var variableName = syntaxFacts.GetIdentifierOfVariableDeclarator(declarator).ValueText;
                if (conditionIdentifier != variableName)
                    return false;

                // if 'Expr()' is a value type, we can't use `??` on it.
                var exprType = semanticModel.GetTypeInfo(initializer, cancellationToken).Type;
                if (exprType is null || exprType.IsNonNullableValueType())
                    return false;

                // var v = Expr();
                // if (v == null)
                //    throw ...
                //
                // can always convert this to `var v = Expr() ?? throw
                if (syntaxFacts.IsThrowStatement(whenTrueStatement))
                    return true;

                // var v = Expr();
                // if (v == null)
                //    v = ...
                //
                // can convert if embedded statement is assigning to same variable
                if (syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
                {
                    syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out var left, out _);
                    if (syntaxFacts.IsIdentifierName(left))
                    {
                        var leftName = syntaxFacts.GetIdentifierOfIdentifierName(left).ValueText;
                        return leftName == variableName;
                    }
                }

                return false;
            }

            bool AnalyzeAssignmentForm(
                TStatementSyntax assignmentStatement,
                [NotNullWhen(true)] out TExpressionSyntax? expressionToCoalesce)
            {
                expressionToCoalesce = null;

                // expr = Expr();
                // if (expr == null)
                //    ...

                syntaxFacts.GetPartsOfAssignmentStatement(assignmentStatement, out var topAssignmentLeft, out var topAssignmentRight);
                if (!syntaxFacts.AreEquivalent(topAssignmentLeft, checkedExpression))
                    return false;

                expressionToCoalesce = topAssignmentRight as TExpressionSyntax;
                if (expressionToCoalesce is null)
                    return false;

                // expr = Expr();
                // if (expr == null)
                //    throw ...
                //
                // can always convert this to `var v = Expr() ?? throw
                if (syntaxFacts.IsThrowStatement(whenTrueStatement))
                    return true;

                // expr = Expr();
                // if (expr == null)
                //    expr = ...
                //
                // can convert if embedded statement is assigning to same variable
                if (syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
                {
                    syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out var innerAssignmentLeft, out _);
                    return syntaxFacts.AreEquivalent(innerAssignmentLeft, checkedExpression);
                }

                return false;
            }
        }
    }
}
