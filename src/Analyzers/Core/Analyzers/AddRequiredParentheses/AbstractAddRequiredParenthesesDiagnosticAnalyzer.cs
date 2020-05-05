// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses
{
    internal abstract class AbstractAddRequiredParenthesesDiagnosticAnalyzer<
        TExpressionSyntax, TBinaryLikeExpressionSyntax, TLanguageKindEnum>
        : AbstractParenthesesDiagnosticAnalyzer
        where TExpressionSyntax : SyntaxNode
        where TBinaryLikeExpressionSyntax : TExpressionSyntax
        where TLanguageKindEnum : struct
    {
        private static readonly Dictionary<(bool includeInFixAll, string equivalenceKey), ImmutableDictionary<string, string>> s_cachedProperties =
            new Dictionary<(bool includeInFixAll, string equivalenceKey), ImmutableDictionary<string, string>>();

        private readonly IPrecedenceService _precedenceService;

        static AbstractAddRequiredParenthesesDiagnosticAnalyzer()
        {
            var options = new[]
            {
                CodeStyleOptions2.ArithmeticBinaryParentheses, CodeStyleOptions2.OtherBinaryParentheses,
                CodeStyleOptions2.OtherParentheses, CodeStyleOptions2.RelationalBinaryParentheses
            };

            var includeArray = new[] { false, true };

            foreach (var option in options)
            {
                foreach (var includeInFixAll in includeArray)
                {
                    var properties = ImmutableDictionary<string, string>.Empty;
                    if (includeInFixAll)
                    {
                        properties = properties.Add(AddRequiredParenthesesConstants.IncludeInFixAll, "");
                    }

                    var equivalenceKey = GetEquivalenceKey(option);
                    properties = properties.Add(AddRequiredParenthesesConstants.EquivalenceKey, equivalenceKey);
                    s_cachedProperties.Add((includeInFixAll, equivalenceKey), properties);
                }
            }
        }

        private static string GetEquivalenceKey(PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> parentPrecedence)
            => parentPrecedence.Name;

        private static ImmutableDictionary<string, string> GetProperties(bool includeInFixAll, string equivalenceKey)
            => s_cachedProperties[(includeInFixAll, equivalenceKey)];

        protected abstract int GetPrecedence(TBinaryLikeExpressionSyntax binaryLike);
        protected abstract TExpressionSyntax? TryGetAppropriateParent(TBinaryLikeExpressionSyntax binaryLike);
        protected abstract bool IsBinaryLike(TExpressionSyntax node);
        protected abstract (TExpressionSyntax, SyntaxToken, TExpressionSyntax) GetPartsOfBinaryLike(TBinaryLikeExpressionSyntax binaryLike);

        protected AbstractAddRequiredParenthesesDiagnosticAnalyzer(IPrecedenceService precedenceService)
            : base(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId,
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

            var childPrecedence = GetLanguageOption(_precedenceService.GetPrecedenceKind(binaryLike));
            var parentPrecedence = GetLanguageOption(_precedenceService.GetPrecedenceKind(parentBinaryLike));

            // only add parentheses within the same precedence band.
            if (parentPrecedence != childPrecedence)
            {
                return;
            }

            var preference = context.GetOption(parentPrecedence, binaryLike.Language);
            if (preference.Value != ParenthesesPreference.AlwaysForClarity)
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(binaryLike.GetLocation());
            var precedence = GetPrecedence(binaryLike);
            var equivalenceKey = GetEquivalenceKey(parentPrecedence);

            // In a case like "a + b * c * d", we'll add parens to make "a + (b * c * d)".
            // To make this user experience more pleasant, we will place the diagnostic on
            // both *'s.
            AddDiagnostics(
                context, binaryLike, precedence, preference.Notification.Severity,
                additionalLocations, equivalenceKey, includeInFixAll: true);
        }

        private void AddDiagnostics(
            SyntaxNodeAnalysisContext context, TBinaryLikeExpressionSyntax? binaryLikeOpt, int precedence,
            ReportDiagnostic severity, ImmutableArray<Location> additionalLocations,
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
                    severity,
                    additionalLocations,
                    properties));

                // We're adding diagnostics for all subcomponents so that the user can get the
                // lightbulb on any of the operator tokens.  However, we don't actually want to
                // 'fix' all of these if the user does a fix-all.  if we did, we'd end up adding far
                // too many parens to the same expr.
                AddDiagnostics(context, left as TBinaryLikeExpressionSyntax, precedence, severity, additionalLocations, equivalenceKey, includeInFixAll: false);
                AddDiagnostics(context, right as TBinaryLikeExpressionSyntax, precedence, severity, additionalLocations, equivalenceKey, includeInFixAll: false);
            }
        }
    }
}
