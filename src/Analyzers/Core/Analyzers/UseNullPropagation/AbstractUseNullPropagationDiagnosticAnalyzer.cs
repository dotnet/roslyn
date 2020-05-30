// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseNullPropagation
{
    internal static class UseNullPropagationConstants
    {
        public const string WhenPartIsNullable = nameof(WhenPartIsNullable);
    }

    internal abstract class AbstractUseNullPropagationDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax,
        TInvocationExpression,
        TMemberAccessExpression,
        TConditionalAccessExpression,
        TElementAccessExpression> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TInvocationExpression : TExpressionSyntax
        where TMemberAccessExpression : TExpressionSyntax
        where TConditionalAccessExpression : TExpressionSyntax
        where TElementAccessExpression : TExpressionSyntax
    {
        protected AbstractUseNullPropagationDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseNullPropagationDiagnosticId,
                   CodeStyleOptions2.PreferNullPropagation,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_null_propagation), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract bool ShouldAnalyze(ParseOptions options);

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol? expressionTypeOpt, CancellationToken cancellationToken);

        protected abstract bool TryAnalyzePatternCondition(
            ISyntaxFacts syntaxFacts, SyntaxNode conditionNode,
            [NotNullWhen(true)] out SyntaxNode? conditionPartToCheck, out bool isEquals);

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(startContext =>
            {
                var expressionTypeOpt = startContext.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

                var objectType = startContext.Compilation.GetSpecialType(SpecialType.System_Object);
                var referenceEqualsMethodOpt = objectType?.GetMembers(nameof(ReferenceEquals))
                                                          .OfType<IMethodSymbol>()
                                                          .FirstOrDefault(m => m.DeclaredAccessibility == Accessibility.Public &&
                                                                               m.Parameters.Length == 2);

                var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
                startContext.RegisterSyntaxNodeAction(
                    c => AnalyzeSyntax(c, expressionTypeOpt, referenceEqualsMethodOpt),
                    syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.TernaryConditionalExpression));
            });

        }

        private void AnalyzeSyntax(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? expressionTypeOpt,
            IMethodSymbol? referenceEqualsMethodOpt)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;
            if (!ShouldAnalyze(conditionalExpression.SyntaxTree.Options))
            {
                return;
            }

            var option = context.GetOption(CodeStyleOptions2.PreferNullPropagation, conditionalExpression.Language);
            if (!option.Value)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFacts();
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var conditionNode, out var whenTrueNode, out var whenFalseNode);

            conditionNode = syntaxFacts.WalkDownParentheses(conditionNode);

            var conditionIsNegated = false;
            if (syntaxFacts.IsLogicalNotExpression(conditionNode))
            {
                conditionIsNegated = true;
                conditionNode = syntaxFacts.WalkDownParentheses(
                    syntaxFacts.GetOperandOfPrefixUnaryExpression(conditionNode));
            }

            if (!TryAnalyzeCondition(
                    context, syntaxFacts, referenceEqualsMethodOpt, conditionNode,
                    out var conditionPartToCheck, out var isEquals))
            {
                return;
            }

            if (conditionIsNegated)
            {
                isEquals = !isEquals;
            }

            // Needs to be of the form:
            //      x == null ? null : ...    or
            //      x != null ? ...  : null;
            if (isEquals && !syntaxFacts.IsNullLiteralExpression(whenTrueNode))
            {
                return;
            }

            if (!isEquals && !syntaxFacts.IsNullLiteralExpression(whenFalseNode))
            {
                return;
            }

            var whenPartToCheck = isEquals ? whenFalseNode : whenTrueNode;

            var semanticModel = context.SemanticModel;
            var whenPartMatch = GetWhenPartMatch(syntaxFacts, semanticModel, conditionPartToCheck, whenPartToCheck);
            if (whenPartMatch == null)
            {
                return;
            }

            // ?. is not available in expression-trees.  Disallow the fix in that case.

            var type = semanticModel.GetTypeInfo(conditionalExpression).Type;
            if (type?.IsValueType == true)
            {
                if (!(type is INamedTypeSymbol namedType) || namedType.ConstructedFrom.SpecialType != SpecialType.System_Nullable_T)
                {
                    // User has something like:  If(str is nothing, nothing, str.Length)
                    // In this case, converting to str?.Length changes the type of this from
                    // int to int?
                    return;
                }
                // But for a nullable type, such as  If(c is nothing, nothing, c.nullable)
                // converting to c?.nullable doesn't affect the type
            }

            if (IsInExpressionTree(semanticModel, conditionNode, expressionTypeOpt, context.CancellationToken))
            {
                return;
            }

            var locations = ImmutableArray.Create(
                conditionalExpression.GetLocation(),
                conditionPartToCheck.GetLocation(),
                whenPartToCheck.GetLocation());

            var properties = ImmutableDictionary<string, string>.Empty;
            var whenPartIsNullable = semanticModel.GetTypeInfo(whenPartMatch).Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            if (whenPartIsNullable)
            {
                properties = properties.Add(UseNullPropagationConstants.WhenPartIsNullable, "");
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                conditionalExpression.GetLocation(),
                option.Notification.Severity,
                locations,
                properties));
        }

        private bool TryAnalyzeCondition(
            SyntaxNodeAnalysisContext context,
            ISyntaxFacts syntaxFacts,
            IMethodSymbol? referenceEqualsMethodOpt,
            SyntaxNode conditionNode,
            [NotNullWhen(true)] out SyntaxNode? conditionPartToCheck,
            out bool isEquals)
        {
            switch (conditionNode)
            {
                case TBinaryExpressionSyntax binaryExpression:
                    return TryAnalyzeBinaryExpressionCondition(
                        syntaxFacts, binaryExpression, out conditionPartToCheck, out isEquals);

                case TInvocationExpression invocation:
                    return TryAnalyzeInvocationCondition(
                        context, syntaxFacts, referenceEqualsMethodOpt, invocation,
                        out conditionPartToCheck, out isEquals);

                default:
                    return TryAnalyzePatternCondition(
                        syntaxFacts, conditionNode, out conditionPartToCheck, out isEquals);
            }
        }

        private static bool TryAnalyzeBinaryExpressionCondition(
            ISyntaxFacts syntaxFacts, TBinaryExpressionSyntax condition,
            [NotNullWhen(true)] out SyntaxNode? conditionPartToCheck, out bool isEquals)
        {
            var syntaxKinds = syntaxFacts.SyntaxKinds;
            isEquals = syntaxKinds.ReferenceEqualsExpression == condition.RawKind;
            var isNotEquals = syntaxKinds.ReferenceNotEqualsExpression == condition.RawKind;
            if (!isEquals && !isNotEquals)
            {
                conditionPartToCheck = null;
                return false;
            }
            else
            {
                syntaxFacts.GetPartsOfBinaryExpression(condition, out var conditionLeft, out var conditionRight);
                conditionPartToCheck = GetConditionPartToCheck(syntaxFacts, conditionLeft, conditionRight);
                return conditionPartToCheck != null;
            }
        }

        private static bool TryAnalyzeInvocationCondition(
            SyntaxNodeAnalysisContext context,
            ISyntaxFacts syntaxFacts,
            IMethodSymbol? referenceEqualsMethodOpt,
            TInvocationExpression invocation,
            [NotNullWhen(true)] out SyntaxNode? conditionPartToCheck,
            out bool isEquals)
        {
            conditionPartToCheck = null;
            isEquals = true;

            var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            var nameNode = syntaxFacts.IsIdentifierName(expression)
                ? expression
                : syntaxFacts.IsSimpleMemberAccessExpression(expression)
                    ? syntaxFacts.GetNameOfMemberAccessExpression(expression)
                    : null;

            if (!syntaxFacts.IsIdentifierName(nameNode))
            {
                return false;
            }

            syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out _);
            if (!syntaxFacts.StringComparer.Equals(name, nameof(ReferenceEquals)))
            {
                return false;
            }

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            if (arguments.Count != 2)
            {
                return false;
            }

            var conditionLeft = syntaxFacts.GetExpressionOfArgument(arguments[0]);
            var conditionRight = syntaxFacts.GetExpressionOfArgument(arguments[1]);
            if (conditionLeft == null || conditionRight == null)
            {
                return false;
            }

            conditionPartToCheck = GetConditionPartToCheck(syntaxFacts, conditionLeft, conditionRight);
            if (conditionPartToCheck == null)
            {
                return false;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
            return referenceEqualsMethodOpt != null && referenceEqualsMethodOpt.Equals(symbol);
        }

        private static SyntaxNode? GetConditionPartToCheck(ISyntaxFacts syntaxFacts, SyntaxNode conditionLeft, SyntaxNode conditionRight)
        {
            var conditionLeftIsNull = syntaxFacts.IsNullLiteralExpression(conditionLeft);
            var conditionRightIsNull = syntaxFacts.IsNullLiteralExpression(conditionRight);

            if (conditionRightIsNull && conditionLeftIsNull)
            {
                // null == null    nothing to do here.
                return null;
            }

            if (!conditionRightIsNull && !conditionLeftIsNull)
            {
                return null;
            }

            return conditionRightIsNull ? conditionLeft : conditionRight;
        }

        internal static SyntaxNode? GetWhenPartMatch(
            ISyntaxFacts syntaxFacts, SemanticModel semanticModel, SyntaxNode expressionToMatch, SyntaxNode whenPart)
        {
            expressionToMatch = RemoveObjectCastIfAny(syntaxFacts, semanticModel, expressionToMatch);
            var current = whenPart;
            while (true)
            {
                var unwrapped = Unwrap(syntaxFacts, current);
                if (unwrapped == null)
                {
                    return null;
                }

                if (current is TMemberAccessExpression ||
                    current is TElementAccessExpression)
                {
                    if (syntaxFacts.AreEquivalent(unwrapped, expressionToMatch))
                    {
                        return unwrapped;
                    }
                }

                current = unwrapped;
            }
        }

        private static SyntaxNode RemoveObjectCastIfAny(ISyntaxFacts syntaxFacts, SemanticModel semanticModel, SyntaxNode node)
        {
            if (syntaxFacts.IsCastExpression(node))
            {
                syntaxFacts.GetPartsOfCastExpression(node, out var type, out var expression);
                var typeSymbol = semanticModel.GetTypeInfo(type).Type;

                if (typeSymbol?.SpecialType == SpecialType.System_Object)
                {
                    return expression;
                }
            }

            return node;
        }

        private static SyntaxNode? Unwrap(ISyntaxFacts syntaxFacts, SyntaxNode node)
        {
            if (node is TInvocationExpression invocation)
            {
                return syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            }

            if (node is TMemberAccessExpression memberAccess)
            {
                return syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess);
            }

            if (node is TConditionalAccessExpression conditionalAccess)
            {
                return syntaxFacts.GetExpressionOfConditionalAccessExpression(conditionalAccess);
            }

            if (node is TElementAccessExpression elementAccess)
            {
                return syntaxFacts.GetExpressionOfElementAccessExpression(elementAccess);
            }

            return null;
        }
    }
}
