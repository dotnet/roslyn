// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax,
        TIfStatementSyntax,
        TLocalDeclarationStatement> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TIfStatementSyntax : TStatementSyntax
        where TLocalDeclarationStatement : TStatementSyntax
    {
        protected AbstractUseCoalesceExpressionForIfNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCoalesceExpressionForIfNullCheckDiagnosticId,
                   EnforceOnBuildValues.UseCoalesceExpression,
                   CodeStyleOptions2.PreferCoalesceExpression,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_coalesce_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract ISyntaxFacts GetSyntaxFacts();

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

            if (syntaxFacts.IsLocalDeclarationStatement(previousStatement))
            {
                // var v = Expr();
                // if (v == null)
                //    ...

                if (!AnalyzeLocalDeclarationForm((TLocalDeclarationStatement)previousStatement))
                    return;
            }
            else if (syntaxFacts.IsAnyAssignmentStatement(previousStatement))
            {
                // v = Expr();
                // if (v == null)
                //    ...
                if (!AnalyzeAssignmentForm(previousStatement))
            }
            else
            {
                return;
            }

            bool AnalyzeLocalDeclarationForm(TLocalDeclarationStatement localDeclarationStatement)
            {
                // var v = Expr();
                // if (v == null)
                //    ...

                if (!syntaxFacts.IsIdentifierName(condition))
                    return false;

                var conditionIdentifier = syntaxFacts.GetIdentifierOfIdentifierName(condition).ValueText;

                var declarators = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
                if (declarators.Count != 1)
                    return false;

                var declarator = declarators[0];
                var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(declarator);
                if (initializer is null)
                    return false;

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
                        var leftName = syntaxFacts.GetIdentifierOfIdentifierName(left);
                        return leftName == variableName;
                    }
                }

                return false;
            }
        }

        protected abstract bool IsNullCheck(TExpressionSyntax condition, [NotNullWhen(true)] out TExpressionSyntax? checkedExpression);
    }
}
