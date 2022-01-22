// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
                   EnforceOnBuildValues.InvokeDelegateWithConditionalAccess,
                   CSharpCodeStyleOptions.PreferConditionalDelegateCall,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Delegate_invocation_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IfStatement);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var options = syntaxContext.Options;
            var syntaxTree = syntaxContext.Node.SyntaxTree;
            var cancellationToken = syntaxContext.CancellationToken;

            var styleOption = options.GetOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, syntaxTree, cancellationToken);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var severity = styleOption.Notification.Severity;

            // look for the form "if (a != null)" or "if (null != a)"
            var ifStatement = (IfStatementSyntax)syntaxContext.Node;

            // ?. is only available in C# 6.0 and above.  Don't offer this refactoring
            // in projects targeting a lesser version.
            if (ifStatement.SyntaxTree.Options.LanguageVersion() < LanguageVersion.CSharp6)
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
            if (innerStatement.IsKind(SyntaxKind.Block, out BlockSyntax? block))
            {
                if (block.Statements.Count != 1)
                {
                    return;
                }

                innerStatement = block.Statements[0];
            }

            if (!innerStatement.IsKind(SyntaxKind.ExpressionStatement, out ExpressionStatementSyntax? expressionStatement))
            {
                return;
            }

            // Check that it's of the form: "if (a != null) { a(); }
            if (expressionStatement.Expression is not InvocationExpressionSyntax invocationExpression)
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
                    var additionalLocations = ImmutableArray.Create<Location>(
                        Location.Create(tree, ifStatement.Span),
                        Location.Create(tree, expressionStatement.Span));

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
            ImmutableArray<Location> additionalLocations,
            string kind)
        {
            var tree = syntaxContext.Node.SyntaxTree;

            var properties = ImmutableDictionary<string, string?>.Empty.Add(
                Constants.Kind, kind);

            var previousToken = expressionStatement.GetFirstToken().GetPreviousToken();
            var nextToken = expressionStatement.GetLastToken().GetNextToken();

            // Fade out the code up to the expression statement.
            var fadeLocation = Location.Create(tree, TextSpan.FromBounds(firstStatement.SpanStart, previousToken.Span.End));
            syntaxContext.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                Descriptor,
                fadeLocation,
                ReportDiagnostic.Default,
                additionalLocations,
                additionalUnnecessaryLocations: ImmutableArray.Create(fadeLocation),
                properties));

            // Put a diagnostic with the appropriate severity on the expression-statement itself.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                expressionStatement.GetLocation(),
                severity,
                additionalLocations, properties));

            // If the if-statement extends past the expression statement, then fade out the rest.
            if (nextToken.Span.Start < ifStatement.Span.End)
            {
                fadeLocation = Location.Create(tree, TextSpan.FromBounds(nextToken.Span.Start, ifStatement.Span.End));
                syntaxContext.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                    Descriptor,
                    fadeLocation,
                    ReportDiagnostic.Default,
                    additionalLocations,
                    additionalUnnecessaryLocations: ImmutableArray.Create(fadeLocation),
                    properties));
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
            if (!previousStatement.IsKind(SyntaxKind.LocalDeclarationStatement, out LocalDeclarationStatementSyntax? localDeclarationStatement))
            {
                return false;
            }

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

            // The initializer can't be inlined if it's an actual lambda/method reference.
            // These cannot be invoked with `?.` (only delegate *values* can be).
            var initializer = declarator.Initializer.Value.WalkDownParentheses();
            if (initializer.IsAnyLambdaOrAnonymousMethod())
            {
                return false;
            }

            var initializerSymbol = semanticModel.GetSymbolInfo(initializer, cancellationToken).GetAnySymbol();
            if (initializerSymbol is IMethodSymbol)
            {
                return false;
            }

            var localSymbol = (ILocalSymbol)semanticModel.GetRequiredDeclaredSymbol(declarator, cancellationToken);

            // Ok, we made a local just to check it for null and invoke it.  Looks like something
            // we can suggest an improvement for!
            // But first make sure we're only using the local only within the body of this if statement.
            var analysis = semanticModel.AnalyzeDataFlow(localDeclarationStatement, ifStatement);
            if (analysis == null || analysis.ReadOutside.Contains(localSymbol) || analysis.WrittenOutside.Contains(localSymbol))
                return false;

            // Looks good!
            var tree = semanticModel.SyntaxTree;
            var additionalLocations = ImmutableArray.Create<Location>(
                Location.Create(tree, localDeclarationStatement.Span),
                Location.Create(tree, ifStatement.Span),
                Location.Create(tree, expressionStatement.Span));

            ReportDiagnostics(syntaxContext,
                localDeclarationStatement, ifStatement, expressionStatement,
                severity, additionalLocations, Constants.VariableAndIfStatementForm);

            return true;
        }

        private static bool IsNullCheckExpression(ExpressionSyntax left, ExpressionSyntax right) =>
            left.IsKind(SyntaxKind.IdentifierName) && right.IsKind(SyntaxKind.NullLiteralExpression);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
