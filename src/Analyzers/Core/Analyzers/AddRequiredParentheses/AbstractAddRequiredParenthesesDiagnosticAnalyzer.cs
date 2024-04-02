// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses;

internal abstract class AbstractAddRequiredParenthesesDiagnosticAnalyzer<
    TExpressionSyntax, TBinaryLikeExpressionSyntax, TLanguageKindEnum>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TExpressionSyntax : SyntaxNode
    where TBinaryLikeExpressionSyntax : TExpressionSyntax
    where TLanguageKindEnum : struct
{
    private static readonly Dictionary<(bool includeInFixAll, string equivalenceKey), ImmutableDictionary<string, string?>> s_cachedProperties = [];

    private readonly IPrecedenceService _precedenceService;

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

    protected AbstractAddRequiredParenthesesDiagnosticAnalyzer(IPrecedenceService precedenceService)
        : base(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId,
               EnforceOnBuildValues.AddRequiredParentheses,
               options: ParenthesesDiagnosticAnalyzersHelper.Options,
               new LocalizableResourceString(nameof(AnalyzersResources.Add_parentheses_for_clarity), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
               new LocalizableResourceString(nameof(AnalyzersResources.Parentheses_should_be_added_for_clarity), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
        _precedenceService = precedenceService;
    }

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
        {
            return;
        }

        var parentBinaryLike = (TBinaryLikeExpressionSyntax)parent;
        if (GetPrecedence(binaryLike) == GetPrecedence(parentBinaryLike))
        {
            return;
        }

        var options = context.GetAnalyzerOptions();
        var childPrecedenceKind = _precedenceService.GetPrecedenceKind(binaryLike);
        var parentPrecedenceKind = _precedenceService.GetPrecedenceKind(parentBinaryLike);

        var childEquivalenceKey = GetEquivalenceKey(childPrecedenceKind);
        var parentEquivalenceKey = GetEquivalenceKey(parentPrecedenceKind);

        // only add parentheses within the same precedence band.
        if (childEquivalenceKey != parentEquivalenceKey)
        {
            return;
        }

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
            additionalLocations, childEquivalenceKey, includeInFixAll: true);
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
