// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses;

internal abstract class AbstractAddRequiredParenthesesDiagnosticAnalyzer<
    TExpressionSyntax, TBinaryLikeExpressionSyntax, TLanguageKindEnum>(IPrecedenceService precedenceService)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId,
        EnforceOnBuildValues.AddRequiredParentheses,
        options: ParenthesesDiagnosticAnalyzersHelper.Options,
        new LocalizableResourceString(nameof(AnalyzersResources.Add_parentheses_for_clarity), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Parentheses_should_be_added_for_clarity), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    where TExpressionSyntax : SyntaxNode
    where TBinaryLikeExpressionSyntax : TExpressionSyntax
    where TLanguageKindEnum : struct
{
    private static readonly Dictionary<(bool includeInFixAll, string equivalenceKey), ImmutableDictionary<string, string?>> s_cachedProperties = [];

    private readonly IPrecedenceService _precedenceService = precedenceService;

    static AbstractAddRequiredParenthesesDiagnosticAnalyzer()
    {
        var includeArray = new[] { false, true };

        foreach (var equivalenceKey in GetAllEquivalenceKeys())
        {
            foreach (var includeInFixAll in includeArray)
            {
                var properties = ImmutableDictionary<string, string?>.Empty;
                if (includeInFixAll)
                {
                    properties = properties.Add(AddRequiredParenthesesConstants.IncludeInFixAll, "");
                }

                properties = properties.Add(AddRequiredParenthesesConstants.EquivalenceKey, equivalenceKey);
                s_cachedProperties.Add((includeInFixAll, equivalenceKey), properties);
            }
        }
    }

    private static PrecedenceKind CollapsePrecedenceGroups(PrecedenceKind precedenceKind)
        => precedenceKind switch
        {
            PrecedenceKind.Arithmetic or PrecedenceKind.Shift or PrecedenceKind.Bitwise => PrecedenceKind.Arithmetic,
            PrecedenceKind.Relational or PrecedenceKind.Equality => PrecedenceKind.Relational,
            _ => precedenceKind,
        };

    protected static string GetEquivalenceKey(PrecedenceKind precedenceKind)
        => precedenceKind switch
        {
            PrecedenceKind.Arithmetic or PrecedenceKind.Shift or PrecedenceKind.Bitwise => "ArithmeticBinary",
            PrecedenceKind.Relational or PrecedenceKind.Equality => "RelationalBinary",
            PrecedenceKind.Logical or PrecedenceKind.Coalesce => "OtherBinary",
            PrecedenceKind.Other => "Other",
            _ => throw ExceptionUtilities.UnexpectedValue(precedenceKind),
        };

    protected static ImmutableArray<string> GetAllEquivalenceKeys()
        => ["ArithmeticBinary", "RelationalBinary", "OtherBinary", "Other"];

    private static ImmutableDictionary<string, string?> GetProperties(bool includeInFixAll, string equivalenceKey)
        => s_cachedProperties[(includeInFixAll, equivalenceKey)];

    protected abstract int GetPrecedence(TBinaryLikeExpressionSyntax binaryLike);
    protected abstract TExpressionSyntax? TryGetAppropriateParent(TBinaryLikeExpressionSyntax binaryLike);
    protected abstract bool IsBinaryLike(TExpressionSyntax node);
    protected abstract (TExpressionSyntax, SyntaxToken, TExpressionSyntax) GetPartsOfBinaryLike(TBinaryLikeExpressionSyntax binaryLike);
    protected abstract bool IsAsExpression(TBinaryLikeExpressionSyntax node);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxNodeKinds());

    protected abstract ImmutableArray<TLanguageKindEnum> GetSyntaxNodeKinds();

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var binaryLike = (TBinaryLikeExpressionSyntax)context.Node;
        var parent = TryGetAppropriateParent(binaryLike);
        if (parent == null || !IsBinaryLike(parent))
            return;

        var parentBinaryLike = (TBinaryLikeExpressionSyntax)parent;
        if (GetPrecedence(binaryLike) == GetPrecedence(parentBinaryLike))
            return;

        var options = context.GetAnalyzerOptions();
        var childPrecedenceKind = _precedenceService.GetPrecedenceKind(binaryLike);
        var parentPrecedenceKind = _precedenceService.GetPrecedenceKind(parentBinaryLike);

        var collapsedChildPrecedenceKind = CollapsePrecedenceGroups(childPrecedenceKind);
        var collapsedParentPrecedenceKind = CollapsePrecedenceGroups(parentPrecedenceKind);

        if (IsClearPrecedenceBoundary())
            return;

        var preference = ParenthesesDiagnosticAnalyzersHelper.GetLanguageOption(options, childPrecedenceKind);
        if (preference.Value != ParenthesesPreference.AlwaysForClarity
            || ShouldSkipAnalysis(context, preference.Notification))
        {
            return;
        }

        var additionalLocations = ImmutableArray.Create(binaryLike.GetLocation());
        var precedence = GetPrecedence(binaryLike);

        // In a case like "a + b * c * d", we'll add parens to make "a + (b * c * d)".
        // To make this user experience more pleasant, we will place the diagnostic on
        // both *'s.
        AddDiagnostics(
            context, binaryLike, precedence, preference.Notification,
            additionalLocations, GetEquivalenceKey(childPrecedenceKind), includeInFixAll: true);

        bool IsClearPrecedenceBoundary()
        {
            // Generally, we only add parentheses within the same precedence band, as normally it is clear
            // between bands that there is no precedence concern.  For example, `a + b == c + d`.  Users 
            // generally understand that `==` will have lower precedence and will not somehow group the 
            // expression like `a + (b == c) + d`.  This is also generally quite clear as the type domains
            // are commonly different.  e.g. `a + b` will operate on some numeric type domain, while `==` is
            // operating in the boolean domain.  So you would immediately have a type error in the common
            // case if grouping didn't operate as expected.
            //
            // this is not always the case though.  `??` in particular can be quite confusing as it generally
            // operates in teh same type domain (or the nullable extension of that type).  For example:
            //
            //      a + b ?? c
            //
            // Is this `(a + b) ?? c` or `a + (b ?? c)`.   It is not particularly clear, and both interpretations
            // can often work due to the compatibility of the type domains.

            // If the expressions have the same precedence, then they definitely don't have a clear precedence
            // boundary between then.
            if (collapsedChildPrecedenceKind == collapsedParentPrecedenceKind)
                return false;

            // They are in different precedence classes, but 'coalesce' is itself quite confusing, so this should
            // still be parenthesized for clarity if the user has that option on.
            if (collapsedParentPrecedenceKind is PrecedenceKind.Coalesce)
            {
                // Note: we have an exception for `a as b ?? c`.  In this case, because `as` so clearly only accepts
                // a type on the RHS, this is idiomatically understood to be `(a as b) ?? c`, not `a as (b ?? c).
                // So we don't parenthesize this case.
                if (collapsedChildPrecedenceKind < PrecedenceKind.Coalesce && !IsAsExpression(binaryLike))
                    return false;
            }

            // Otherwise, this is clear enough on its face, and we should do nothing
            return true;
        }
    }

    private void AddDiagnostics(
        SyntaxNodeAnalysisContext context, TBinaryLikeExpressionSyntax? binaryLikeOpt, int precedence,
        NotificationOption2 notificationOption, ImmutableArray<Location> additionalLocations,
        string equivalenceKey, bool includeInFixAll)
    {
        if (binaryLikeOpt != null &&
            IsBinaryLike(binaryLikeOpt) &&
            GetPrecedence(binaryLikeOpt) == precedence)
        {
            var (left, operatorToken, right) = GetPartsOfBinaryLike(binaryLikeOpt);

            var properties = GetProperties(includeInFixAll, equivalenceKey);

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                operatorToken.GetLocation(),
                notificationOption,
                context.Options,
                additionalLocations,
                properties));

            // We're adding diagnostics for all subcomponents so that the user can get the
            // lightbulb on any of the operator tokens.  However, we don't actually want to
            // 'fix' all of these if the user does a fix-all.  if we did, we'd end up adding far
            // too many parens to the same expr.
            AddDiagnostics(context, left as TBinaryLikeExpressionSyntax, precedence, notificationOption, additionalLocations, equivalenceKey, includeInFixAll: false);
            AddDiagnostics(context, right as TBinaryLikeExpressionSyntax, precedence, notificationOption, additionalLocations, equivalenceKey, includeInFixAll: false);
        }
    }
}
