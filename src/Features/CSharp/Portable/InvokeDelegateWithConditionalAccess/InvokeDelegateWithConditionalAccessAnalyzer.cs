using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    internal class InvokeDelegateWithConditionalAccessAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId,
            CSharpFeaturesResources.DelegateInvocationCanBeSimplified,
            CSharpFeaturesResources.DelegateInvocationCanBeSimplified,
            DiagnosticCategory.Style,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(SyntaxNodeAction, SyntaxKind.IfStatement);
        }

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            // look for the form "if (a != null)" or "if (null != a)"
            var ifStatement = (IfStatementSyntax)syntaxContext.Node;
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
            if (TryCheckVariableAndIfStatementForm(syntaxContext, ifStatement, condition, expressionStatement, invocationExpression))
            {
                return;
            }

            TryCheckSingleIfStatementForm(syntaxContext, ifStatement, condition, expressionStatement, invocationExpression);
        }

        private bool TryCheckSingleIfStatementForm(
            SyntaxNodeAnalysisContext syntaxContext,
            IfStatementSyntax ifStatement,
            BinaryExpressionSyntax condition,
            ExpressionStatementSyntax expressionStatement,
            InvocationExpressionSyntax invocationExpression)
        {
            // Look for the form:  "if (someExpr != null) someExpr()"
            if (condition.Left.IsKind(SyntaxKind.NullLiteralExpression) ||
                condition.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                var expr = condition.Left.IsKind(SyntaxKind.NullLiteralExpression)
                    ? condition.Right
                    : condition.Left;

                if (SyntaxFactory.AreEquivalent(expr, invocationExpression.Expression, topLevel: false))
                {
                    // Looks good!
                    var tree = syntaxContext.SemanticModel.SyntaxTree;
                    var additionalLocations = new List<Location>
                    {
                        Location.Create(tree, ifStatement.Span),
                        Location.Create(tree, expressionStatement.Span)
                    };

                    var properties = ImmutableDictionary<string, string>.Empty.Add(Constants.Kind, Constants.SingleIfStatementForm);

                    syntaxContext.ReportDiagnostic(Diagnostic.Create(descriptor,
                        Location.Create(tree, TextSpan.FromBounds(ifStatement.SpanStart, expressionStatement.SpanStart)),
                        additionalLocations, properties));

                    if (expressionStatement.Span.End != ifStatement.Span.End)
                    {
                        syntaxContext.ReportDiagnostic(Diagnostic.Create(descriptor,
                            Location.Create(tree, TextSpan.FromBounds(expressionStatement.Span.End, ifStatement.Span.End)),
                            additionalLocations, properties));
                    }
                }
            }

            return false;
        }

        private bool TryCheckVariableAndIfStatementForm(
            SyntaxNodeAnalysisContext syntaxContext,
            IfStatementSyntax ifStatement,
            BinaryExpressionSyntax condition,
            ExpressionStatementSyntax expressionStatement,
            InvocationExpressionSyntax invocationExpression)
        { 
            // look for the form "if (a != null)" or "if (null != a)"
            if (!ifStatement.Parent.IsKind(SyntaxKind.Block))
            {
                return false;
            }

            if (!IsNotEqualsExpression(condition.Left, condition.Right) &&
                !IsNotEqualsExpression(condition.Right, condition.Left))
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

            if (!Equals(declarator.Identifier.ValueText, conditionName.Identifier.ValueText))
            {
                return false;
            }

            // Syntactically this looks good.  Now make sure that the local is a delegate type.
            var semanticModel = syntaxContext.SemanticModel;
            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);

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

            var properties = ImmutableDictionary<string,string>.Empty.Add(Constants.Kind, Constants.VariableAndIfStatementForm);

            syntaxContext.ReportDiagnostic(Diagnostic.Create(descriptor,
                Location.Create(tree, TextSpan.FromBounds(localDeclarationStatement.SpanStart, expressionStatement.SpanStart)),
                additionalLocations, properties));

            if (expressionStatement.Span.End != ifStatement.Span.End)
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(descriptor,
                    Location.Create(tree, TextSpan.FromBounds(expressionStatement.Span.End, ifStatement.Span.End)),
                    additionalLocations, properties));
            }

            return true;
        }

        private bool IsNotEqualsExpression(ExpressionSyntax left, ExpressionSyntax right) =>
            left.IsKind(SyntaxKind.IdentifierName) && right.IsKind(SyntaxKind.NullLiteralExpression);
    }
}