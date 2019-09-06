using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    /// <summary>
    /// <para>
    /// Represents priority boost for <see cref="SuggestedActionSet"/> given to those that are explicitly offered.
    /// </para>
    /// <para>
    /// E.g. fixes for diagnostics with <see cref="DiagnosticSeverity.Warning"/> have boost <see cref="ExplicitRecommendation"/>.
    /// </para>
    /// </summary>
    internal enum SuggestedActionSetPriorityBoost
    {
        None,
        Suggestion,
        ExplicitRecommendation
    }

    internal static class SuggestedActionSetPriorityBoostHelper
    {
        /// <summary>
        /// Gets <see cref="SuggestedActionSetPriorityBoost"/> for <see cref="CodeFix"/> based on <param name="diagnostics"/> that generated them. 
        /// </summary>
        internal static SuggestedActionSetPriorityBoost GetSuggestedActionSetPriorityBoost(DiagnosticData diagnostics)
        {
            return diagnostics?.Severity switch
            {
                null => SuggestedActionSetPriorityBoost.None,
                DiagnosticSeverity.Hidden when diagnostics.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary) => SuggestedActionSetPriorityBoost.Suggestion,
                DiagnosticSeverity.Hidden => SuggestedActionSetPriorityBoost.None,
                DiagnosticSeverity.Info => SuggestedActionSetPriorityBoost.Suggestion,
                DiagnosticSeverity.Warning => SuggestedActionSetPriorityBoost.ExplicitRecommendation,
                DiagnosticSeverity.Error => SuggestedActionSetPriorityBoost.ExplicitRecommendation,
                _ => throw ExceptionUtilities.Unreachable
            };
        }
    }
}
