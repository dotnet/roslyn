﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess
{
    internal static class Constants
    {
        public const string Kind = nameof(Kind);
        public const string VariableAndIfStatementForm = nameof(VariableAndIfStatementForm);
        public const string SingleIfStatementForm = nameof(SingleIfStatementForm);
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class InvokeDelegateWithConditionalAccessAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public InvokeDelegateWithConditionalAccessAnalyzer()
            : base(IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId,
                   new LocalizableResourceString(nameof(CSharpFeaturesResources.Delegate_invocation_can_be_simplified), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IfStatement);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options;
            var syntaxTree = syntaxContext.Node.SyntaxTree;
            var cancellationToken = syntaxContext.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var severity = styleOption.Notification.Severity;

            // look for the form "if (a != null)" or "if (null != a)"
            var ifStatement = (IfStatementSyntax)syntaxContext.Node;

            // ?. is only available in C# 6.0 and above.  Don't offer this refactoring
            // in projects targetting a lesser version.
            if (((CSharpParseOptions)ifStatement.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp6)
            {
                return;
            }

            if (!ifStatement.Condition.IsKind(SyntaxKind.NotEqualsExpression))
            {
                return;
            }

            if (ifStatement.Else != null)
            {
                return;
            }

            // Check for both:  "if (...) { a(); }" and "if (...) a();"
            var innerStatement = ifStatement.Statement;
            if (innerStatement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)innerStatement;
                if (block.Statements.Count != 1)
                {
                    return;
                }

                innerStatement = block.Statements[0];
            }

            if (!innerStatement.IsKind(SyntaxKind.ExpressionStatement))
            {
                return;
            }

            var expressionStatement = (ExpressionStatementSyntax)innerStatement;

            // Check that it's of the form: "if (a != null) { a(); }
            var invocationExpression = ((ExpressionStatementSyntax)innerStatement).Expression as InvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return;
            }

            var condition = (BinaryExpressionSyntax)ifStatement.Condition;
            if (TryCheckVariableAndIfStatementForm(
                    syntaxContext, ifStatement, condition,
                    expressionStatement, invocationExpression,
                    severity))
            {
                return;
            }

            TryCheckSingleIfStatementForm(
                syntaxContext, ifStatement, condition,
                expressionStatement, invocationExpression,
                severity);
        }

        private bool TryCheckSingleIfStatementForm(
            SyntaxNodeAnalysisContext syntaxContext,
            IfStatementSyntax ifStatement,
            BinaryExpressionSyntax condition,
            ExpressionStatementSyntax expressionStatement,
            InvocationExpressionSyntax invocationExpression,
            ReportDiagnostic severity)
        {
            var cancellationToken = syntaxContext.CancellationToken;

            // Look for the form:  "if (someExpr != null) someExpr()"
            if (condition.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                condition.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                var expr = condition.Left.IsKind(SyntaxKind.NullLiteralExpression)
                    ? condition.Right
                    : condition.Left;

                cancellationToken.ThrowIfCancellationRequested();
                if (SyntaxFactory.AreEquivalent(expr, invocationExpression.Expression, topLevel: false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Looks good!
                    var tree = syntaxContext.SemanticModel.SyntaxTree;
                    var additionalLocations = new List<Location>
                    {
                        Location.Create(tree, ifStatement.Span),
                        Location.Create(tree, expressionStatement.Span)
                    };

                    ReportDiagnostics(
                        syntaxContext, ifStatement, ifStatement,
                        expressionStatement, severity, additionalLocations,
                        Constants.SingleIfStatementForm);

                    return true;
                }
            }

            return false;
        }

        private void ReportDiagnostics(
            SyntaxNodeAnalysisContext syntaxContext,
            StatementSyntax firstStatement,
            IfStatementSyntax ifStatement,
            ExpressionStatementSyntax expressionStatement,
            ReportDiagnostic severity,
            List<Location> additionalLocations,
            string kind)
        {
            var tree = syntaxContext.Node.SyntaxTree;

            var properties = ImmutableDictionary<string, string>.Empty.Add(
                Constants.Kind, kind);

            var previousToken = expressionStatement.GetFirstToken().GetPreviousToken();
            var nextToken = expressionStatement.GetLastToken().GetNextToken();

            // Fade out the code up to the expression statement.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(this.UnnecessaryWithSuggestionDescriptor,
                Location.Create(tree, TextSpan.FromBounds(firstStatement.SpanStart, previousToken.Span.End)),
                additionalLocations, properties));

            // Put a diagnostic with the appropriate severity on the expression-statement itself.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                expressionStatement.GetLocation(),
                severity,
                additionalLocations, properties));

            // If the if-statement extends past the expression statement, then fade out the rest.
            if (nextToken.Span.Start < ifStatement.Span.End)
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(this.UnnecessaryWithSuggestionDescriptor,
                    Location.Create(tree, TextSpan.FromBounds(nextToken.Span.Start, ifStatement.Span.End)),
                    additionalLocations, properties));
            }
        }

        private bool TryCheckVariableAndIfStatementForm(
            SyntaxNodeAnalysisContext syntaxContext,
            IfStatementSyntax ifStatement,
            BinaryExpressionSyntax condition,
            ExpressionStatementSyntax expressionStatement,
            InvocationExpressionSyntax invocationExpression,
            ReportDiagnostic severity)
        {
            var cancellationToken = syntaxContext.CancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            // look for the form "if (a != null)" or "if (null != a)"
            if (!ifStatement.Parent.IsKind(SyntaxKind.Block))
            {
                return false;
            }

            if (!IsNullCheckExpression(condition.Left, condition.Right) &&
                !IsNullCheckExpression(condition.Right, condition.Left))
            {
                return false;
            }

            var expression = invocationExpression.Expression;
            if (!expression.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            var conditionName = condition.Left is IdentifierNameSyntax
                ? (IdentifierNameSyntax)condition.Left
                : (IdentifierNameSyntax)condition.Right;

            var invocationName = (IdentifierNameSyntax)expression;
            if (!Equals(conditionName.Identifier.ValueText, invocationName.Identifier.ValueText))
            {
                return false;
            }

            // Now make sure the previous statement is "var a = ..."
            var parentBlock = (BlockSyntax)ifStatement.Parent;
            var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
            if (ifIndex == 0)
            {
                return false;
            }

            var previousStatement = parentBlock.Statements[ifIndex - 1];
            if (!previousStatement.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                return false;
            }

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)previousStatement;
            var variableDeclaration = localDeclarationStatement.Declaration;

            if (variableDeclaration.Variables.Count != 1)
            {
                return false;
            }

            var declarator = variableDeclaration.Variables[0];
            if (declarator.Initializer == null)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!Equals(declarator.Identifier.ValueText, conditionName.Identifier.ValueText))
            {
                return false;
            }

            // Syntactically this looks good.  Now make sure that the local is a delegate type.
            var semanticModel = syntaxContext.SemanticModel;
            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator, cancellationToken);

            // Ok, we made a local just to check it for null and invoke it.  Looks like something
            // we can suggest an improvement for!
            // But first make sure we're only using the local only within the body of this if statement.
            var analysis = semanticModel.AnalyzeDataFlow(localDeclarationStatement, ifStatement);
            if (analysis.ReadOutside.Contains(localSymbol) || analysis.WrittenOutside.Contains(localSymbol))
            {
                return false;
            }

            // Looks good!
            var tree = semanticModel.SyntaxTree;
            var additionalLocations = new List<Location>
            {
                Location.Create(tree, localDeclarationStatement.Span),
                Location.Create(tree, ifStatement.Span),
                Location.Create(tree, expressionStatement.Span)
            };

            ReportDiagnostics(syntaxContext,
                localDeclarationStatement, ifStatement, expressionStatement,
                severity, additionalLocations, Constants.VariableAndIfStatementForm);

            return true;
        }

        private bool IsNullCheckExpression(ExpressionSyntax left, ExpressionSyntax right) =>
            left.IsKind(SyntaxKind.IdentifierName) && right.IsKind(SyntaxKind.NullLiteralExpression);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
