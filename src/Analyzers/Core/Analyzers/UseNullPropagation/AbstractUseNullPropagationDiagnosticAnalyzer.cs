// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseNullPropagation;

internal static class UseNullPropagationHelpers
{
    public const string IsTrivialNullableValueAccess = nameof(IsTrivialNullableValueAccess);
    public const string WhenPartIsNullable = nameof(WhenPartIsNullable);

    public static bool IsSystemNullableValueProperty([NotNullWhen(true)] ISymbol? symbol)
        => symbol is
        {
            Name: nameof(Nullable<>.Value),
            ContainingType.OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
        };
}

/// <summary>
/// Looks for code snippets similar to <c>x == null ? null : x.Y()</c> and converts it to <c>x?.Y()</c>.  This form is also supported:
/// <code>
/// if (x != null)
///     x.Y();
/// </code>
/// </summary>
internal abstract partial class AbstractUseNullPropagationDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TInvocationExpressionSyntax,
    TConditionalAccessExpressionSyntax,
    TElementAccessExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TIfStatementSyntax,
    TExpressionStatementSyntax>() : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseNullPropagationDiagnosticId,
        EnforceOnBuildValues.UseNullPropagation,
        CodeStyleOptions2.PreferNullPropagation,
        new LocalizableResourceString(nameof(AnalyzersResources.Use_null_propagation), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
    where TBinaryExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TConditionalAccessExpressionSyntax : TExpressionSyntax
    where TElementAccessExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TIfStatementSyntax : TStatementSyntax
    where TExpressionStatementSyntax : TStatementSyntax
{
    private static readonly ImmutableDictionary<string, string?> s_whenPartIsNullableProperties =
        ImmutableDictionary<string, string?>.Empty.Add(UseNullPropagationHelpers.WhenPartIsNullable, "");

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected abstract bool ShouldAnalyze(Compilation compilation);

    protected abstract TSyntaxKind IfStatementSyntaxKind { get; }
    protected abstract ISemanticFacts SemanticFacts { get; }
    protected ISyntaxFacts SyntaxFacts => SemanticFacts.SyntaxFacts;

    protected abstract bool TryAnalyzePatternCondition(
        ISyntaxFacts syntaxFacts, TExpressionSyntax conditionNode,
        [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck, out bool isEquals);

    public (INamedTypeSymbol? expressionType, IMethodSymbol? referenceEqualsMethod) GetAnalysisSymbols(Compilation compilation)
    {
        var expressionType = compilation.ExpressionOfTType();
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        var referenceEqualsMethod = objectType?.GetMembers(nameof(ReferenceEquals))
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m is { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 2 });
        return (expressionType, referenceEqualsMethod);
    }

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (!ShouldAnalyze(context.Compilation))
                return;

            var expressionType = context.Compilation.ExpressionOfTType();

            var (objectType, referenceEqualsMethod) = GetAnalysisSymbols(context.Compilation);

            var syntaxKinds = this.SyntaxFacts.SyntaxKinds;
            context.RegisterSyntaxNodeAction(
                context => AnalyzeTernaryConditionalExpressionAndReportDiagnostic(context, expressionType, referenceEqualsMethod),
                syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.TernaryConditionalExpression));
            context.RegisterSyntaxNodeAction(
                context => AnalyzeIfStatementAndReportDiagnostic(context, referenceEqualsMethod),
                IfStatementSyntaxKind);
        });
    }

    public (TExpressionSyntax conditionalPart, SyntaxNode whenPart)? GetPartsOfConditionalExpression(
        SemanticModel semanticModel,
        TConditionalExpressionSyntax conditionalExpression,
        CancellationToken cancellationToken)
    {
        var (objectType, referenceEqualsMethod) = GetAnalysisSymbols(semanticModel.Compilation);
        var analysisResult = AnalyzeTernaryConditionalExpression(
            semanticModel, objectType, referenceEqualsMethod, conditionalExpression, cancellationToken);
        if (analysisResult is null)
            return null;

        return (analysisResult.Value.ConditionPartToCheck, analysisResult.Value.WhenPartToCheck);
    }

    private void AnalyzeTernaryConditionalExpressionAndReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? expressionType,
        IMethodSymbol? referenceEqualsMethod)
    {
        var cancellationToken = context.CancellationToken;
        var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

        var option = context.GetAnalyzerOptions().PreferNullPropagation;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var analysisResult = AnalyzeTernaryConditionalExpression(
            context.SemanticModel, expressionType, referenceEqualsMethod, conditionalExpression, cancellationToken);
        if (analysisResult is null)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            conditionalExpression.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: [conditionalExpression.GetLocation()],
            analysisResult.Value.Properties));
    }

    public ConditionalExpressionAnalysisResult? AnalyzeTernaryConditionalExpression(
        SemanticModel semanticModel,
        INamedTypeSymbol? expressionType,
        IMethodSymbol? referenceEqualsMethod,
        TConditionalExpressionSyntax conditionalExpression,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = this.SyntaxFacts;
        syntaxFacts.GetPartsOfConditionalExpression(
            conditionalExpression, out var condition, out var whenTrue, out var whenFalse);

        var conditionNode = (TExpressionSyntax)condition;

        var whenTrueNode = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(whenTrue);
        var whenFalseNode = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(whenFalse);

        if (!TryAnalyzeCondition(
                semanticModel, referenceEqualsMethod, conditionNode,
                out var conditionPartToCheck, out var isEquals, cancellationToken))
        {
            return null;
        }

        // Needs to be of the form:
        //      x == null ? null : ...    or
        //      x != null ? ...  : null;
        if (isEquals && !syntaxFacts.IsNullLiteralExpression(whenTrueNode))
            return null;

        if (!isEquals && !syntaxFacts.IsNullLiteralExpression(whenFalseNode))
            return null;

        var whenPartToCheck = isEquals ? whenFalseNode : whenTrueNode;

        var whenPartMatch = GetWhenPartMatch(syntaxFacts, semanticModel, conditionPartToCheck, whenPartToCheck, cancellationToken);
        if (whenPartMatch == null)
            return null;

        // can't use ?. on a pointer
        var whenPartType = semanticModel.GetTypeInfo(whenPartMatch, cancellationToken).Type;
        if (whenPartType is IPointerTypeSymbol)
            return null;

        var type = semanticModel.GetTypeInfo(conditionalExpression, cancellationToken).Type;
        if (type?.IsValueType == true)
        {
            if (type is not INamedTypeSymbol namedType || namedType.ConstructedFrom.SpecialType != SpecialType.System_Nullable_T)
            {
                // User has something like:  If(str is nothing, nothing, str.Length)
                // In this case, converting to str?.Length changes the type of this from
                // int to int?
                return null;
            }
            // But for a nullable type, such as  If(c is nothing, nothing, c.nullable)
            // converting to c?.nullable doesn't affect the type
        }

        var isTrivialNullableValueAccess = false;
        if (syntaxFacts.IsSimpleMemberAccessExpression(whenPartToCheck))
        {
            // `x == null ? x : x.M` cannot be converted to `x?.M` when M is a method symbol.
            var memberSymbol = semanticModel.GetSymbolInfo(whenPartToCheck, cancellationToken).GetAnySymbol();
            if (memberSymbol is IMethodSymbol)
                return null;

            // we're converting from `x.M` to `x?.M`.  This is not legal if 'M' is an unconstrained type parameter as
            // the lang/compiler doesn't know what final type to make out of this.

            var memberType = semanticModel.GetTypeInfo(whenPartToCheck, cancellationToken).Type;
            if (memberType is null or ITypeParameterSymbol
                {
                    IsReferenceType: false,
                    IsValueType: false,
                })
            {
                return null;
            }

            // `x == null ? x : x.Value` will be converted to just 'x'.
            if (UseNullPropagationHelpers.IsSystemNullableValueProperty(memberSymbol))
                isTrivialNullableValueAccess = true;
        }

        // ?. is not available in expression-trees.  Disallow the fix in that case.
        if (this.SemanticFacts.IsInExpressionTree(semanticModel, conditionNode, expressionType, cancellationToken))
            return null;

        var whenPartIsNullable = whenPartType?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        var properties = whenPartIsNullable
            ? s_whenPartIsNullableProperties
            : ImmutableDictionary<string, string?>.Empty;

        if (isTrivialNullableValueAccess)
            properties = properties.Add(UseNullPropagationHelpers.IsTrivialNullableValueAccess, UseNullPropagationHelpers.IsTrivialNullableValueAccess);

        return new(
            conditionPartToCheck,
            whenPartToCheck,
            properties);
    }

    private bool TryAnalyzeCondition(
        SemanticModel semanticModel,
        IMethodSymbol? referenceEqualsMethod,
        TExpressionSyntax condition,
        [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck,
        out bool isEquals,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = this.SyntaxFacts;
        condition = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(condition);
        var conditionIsNegated = false;
        if (syntaxFacts.IsLogicalNotExpression(condition))
        {
            conditionIsNegated = true;
            condition = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(
                syntaxFacts.GetOperandOfPrefixUnaryExpression(condition));
        }

        var result = condition switch
        {
            TBinaryExpressionSyntax binaryExpression => TryAnalyzeBinaryExpressionCondition(
                    syntaxFacts, binaryExpression, out conditionPartToCheck, out isEquals),

            TInvocationExpressionSyntax invocation => TryAnalyzeInvocationCondition(
                semanticModel, syntaxFacts, referenceEqualsMethod, invocation, out conditionPartToCheck, out isEquals, cancellationToken),

            _ => TryAnalyzePatternCondition(syntaxFacts, condition, out conditionPartToCheck, out isEquals),
        };

        if (conditionIsNegated)
            isEquals = !isEquals;

        return result;
    }

    private static bool TryAnalyzeBinaryExpressionCondition(
        ISyntaxFacts syntaxFacts, TBinaryExpressionSyntax condition,
        [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck, out bool isEquals)
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
            conditionPartToCheck = GetConditionPartToCheck(syntaxFacts, (TExpressionSyntax)conditionLeft, (TExpressionSyntax)conditionRight);
            return conditionPartToCheck != null;
        }
    }

    private static bool TryAnalyzeInvocationCondition(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        IMethodSymbol? referenceEqualsMethod,
        TInvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out TExpressionSyntax? conditionPartToCheck,
        out bool isEquals,
        CancellationToken cancellationToken)
    {
        conditionPartToCheck = null;
        isEquals = true;

        if (referenceEqualsMethod == null)
            return false;

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

        var conditionLeft = (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(arguments[0]);
        var conditionRight = (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(arguments[1]);
        if (conditionLeft == null || conditionRight == null)
        {
            return false;
        }

        conditionPartToCheck = GetConditionPartToCheck(syntaxFacts, conditionLeft, conditionRight);
        if (conditionPartToCheck == null)
        {
            return false;
        }

        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
        return referenceEqualsMethod.Equals(symbol);
    }

    private static TExpressionSyntax? GetConditionPartToCheck(
        ISyntaxFacts syntaxFacts, TExpressionSyntax conditionLeft, TExpressionSyntax conditionRight)
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

#pragma warning disable CA1822 // Mark members as static.  Helper method that doesn't want to call through generic form.
    public TExpressionSyntax? GetWhenPartMatch(
        ISyntaxFacts syntaxFacts,
        SemanticModel semanticModel,
        TExpressionSyntax expressionToMatch,
        TExpressionSyntax whenPart,
        CancellationToken cancellationToken)
    {
        expressionToMatch = RemoveObjectCastIfAny(syntaxFacts, semanticModel, expressionToMatch, cancellationToken);
        var current = whenPart;
        while (true)
        {
            var unwrapped = Unwrap(syntaxFacts, current);
            if (unwrapped == null)
                return null;

            if (syntaxFacts.IsSimpleMemberAccessExpression(current) || current is TElementAccessExpressionSyntax)
            {
                if (syntaxFacts.AreEquivalent(unwrapped, expressionToMatch))
                    return unwrapped;
            }

            current = unwrapped;
        }
    }
#pragma warning restore CA1822 // Mark members as static

    private static TExpressionSyntax RemoveObjectCastIfAny(
        ISyntaxFacts syntaxFacts, SemanticModel semanticModel, TExpressionSyntax node, CancellationToken cancellationToken)
    {
        if (syntaxFacts.IsCastExpression(node))
        {
            syntaxFacts.GetPartsOfCastExpression(node, out var type, out var expression);
            var typeSymbol = semanticModel.GetTypeInfo(type, cancellationToken).Type;

            if (typeSymbol?.SpecialType == SpecialType.System_Object)
                return (TExpressionSyntax)expression;
        }

        return node;
    }

    private static TExpressionSyntax? Unwrap(ISyntaxFacts syntaxFacts, TExpressionSyntax node)
    {
        node = (TExpressionSyntax)syntaxFacts.WalkDownParentheses(node);

        if (node is TInvocationExpressionSyntax invocation)
            return (TExpressionSyntax)syntaxFacts.GetExpressionOfInvocationExpression(invocation);

        if (syntaxFacts.IsSimpleMemberAccessExpression(node))
            return (TExpressionSyntax?)syntaxFacts.GetExpressionOfMemberAccessExpression(node);

        if (node is TConditionalAccessExpressionSyntax conditionalAccess)
            return (TExpressionSyntax)syntaxFacts.GetExpressionOfConditionalAccessExpression(conditionalAccess);

        if (node is TElementAccessExpressionSyntax elementAccess)
            return (TExpressionSyntax?)syntaxFacts.GetExpressionOfElementAccessExpression(elementAccess);

        // Check if node itself is an assignment (simple or compound). We do this by checking if the first child node
        // is on the left side of any assignment (which means the node is the assignment itself).
        // Simple and compound assignments with null conditional (?.) are only supported in C# 14+.
        var firstChild = node.ChildNodesAndTokens().FirstOrDefault().AsNode();
        if (firstChild != null && syntaxFacts.IsLeftSideOfAnyAssignment(firstChild) && syntaxFacts.SupportsNullConditionalAssignment(node.SyntaxTree.Options))
        {
            syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(node, out var left, out _, out _);
            return (TExpressionSyntax)left;
        }

        return null;
    }
}
