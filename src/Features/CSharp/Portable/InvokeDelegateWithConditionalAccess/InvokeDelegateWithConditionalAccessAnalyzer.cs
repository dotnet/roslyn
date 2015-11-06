using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess
{
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

            if (!ifStatement.Parent.IsKind(SyntaxKind.Block))
            {
                return;
            }

            var binaryExpression = (BinaryExpressionSyntax)ifStatement.Condition;
            if (!IsNotEqualsExpression(binaryExpression.Left, binaryExpression.Right) &&
                !IsNotEqualsExpression(binaryExpression.Right, binaryExpression.Left))
            {
                return;
            }

            // Check for both:  "if (...) { a(); }" and "if (...) a();"
            var statement = ifStatement.Statement;
            if (statement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)statement;
                if (block.Statements.Count != 1)
                {
                    return;
                }

                statement = block.Statements[0];
            }

            if (!statement.IsKind(SyntaxKind.ExpressionStatement))
            {
                return;
            }

            var expressionStatement = (ExpressionStatementSyntax)statement;

            // Check that it's of the form: "if (a != null) { a(); }
            var invocationExpression = ((ExpressionStatementSyntax)statement).Expression;
            if (!invocationExpression.IsKind(SyntaxKind.InvocationExpression))
            {
                return;
            }

            var expression = ((InvocationExpressionSyntax)invocationExpression).Expression;
            if (!expression.IsKind(SyntaxKind.IdentifierName))
            {
                return;
            }

            var conditionName = binaryExpression.Left is IdentifierNameSyntax
                ? (IdentifierNameSyntax)binaryExpression.Left
                : (IdentifierNameSyntax)binaryExpression.Right;

            var invocationName = (IdentifierNameSyntax)expression;
            if (!Equals(conditionName.Identifier.ValueText, invocationName.Identifier.ValueText))
            {
                return;
            }

            // Now make sure the previous statement is "var a = ..."
            var parentBlock = (BlockSyntax)ifStatement.Parent;
            var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
            if (ifIndex == 0)
            {
                return;
            }

            var previousStatement = parentBlock.Statements[ifIndex - 1];
            if (!previousStatement.IsKind(SyntaxKind.LocalDeclarationStatement))
            {
                return;
            }

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)previousStatement;
            var variableDeclaration = localDeclarationStatement.Declaration;

            if (variableDeclaration.Variables.Count != 1)
            {
                return;
            }

            var declarator = variableDeclaration.Variables[0];
            if (declarator.Initializer == null)
            {
                return;
            }

            if (!Equals(declarator.Identifier.ValueText, conditionName.Identifier.ValueText))
            {
                return;
            }

            // Syntactically this looks good.  Now make sure that the local is a delegate type.
            var semanticModel = syntaxContext.SemanticModel;
            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);
            if (localSymbol.Type.TypeKind != TypeKind.Delegate)
            {
                return;
            }

            // Ok, we made a local just to check it for null and invoke it.  Looks like something
            // we can suggest an improvement for!
            // But first make sure we're only using the local only within the body of this if statement.
            var analysis = semanticModel.AnalyzeDataFlow(localDeclarationStatement, ifStatement);
            if (analysis.ReadOutside.Contains(localSymbol) || analysis.WrittenOutside.Contains(localSymbol))
            {
                return;
            }

            // Looks good!
            var tree = semanticModel.SyntaxTree;
            var additionalLocations = new List<Location>
            {
                Location.Create(tree, localDeclarationStatement.Span),
                Location.Create(tree, ifStatement.Span),
                Location.Create(tree, expressionStatement.Span)
            };

            syntaxContext.ReportDiagnostic(Diagnostic.Create(descriptor,
                Location.Create(tree, TextSpan.FromBounds(localDeclarationStatement.SpanStart, invocationExpression.SpanStart)),
                additionalLocations));

            if (expressionStatement.Span.End != ifStatement.Span.End)
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(descriptor,
                    Location.Create(tree, TextSpan.FromBounds(expressionStatement.Span.End, ifStatement.Span.End)),
                    additionalLocations));
            }
        }

        private bool IsNotEqualsExpression(ExpressionSyntax left, ExpressionSyntax right) =>
            left.IsKind(SyntaxKind.IdentifierName) && right.IsKind(SyntaxKind.NullLiteralExpression);
    }
}