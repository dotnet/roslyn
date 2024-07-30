// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Naming;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class NamingExtensions
{
    public static async Task<NamingRule> GetApplicableNamingRuleAsync(
        this Document document, SymbolKind symbolKind, Accessibility accessibility, CancellationToken cancellationToken)
    {
        var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var rule in rules)
        {
            if (rule.SymbolSpecification.AppliesTo(symbolKind, accessibility))
                return rule;
        }

        throw ExceptionUtilities.Unreachable();
    }

    /// <summary>
    /// Gets the set of naming rules the user has set for this document.  Will include a set of default naming rules
    /// that match if the user hasn't specified any for a particular symbol type.  The are added at the end so they
    /// will only be used if the user hasn't specified a preference.
    /// </summary>
    public static async Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(
        this Document document, CancellationToken cancellationToken)
    {
        var options = await document.GetNamingStylePreferencesAsync(cancellationToken).ConfigureAwait(false);
        return options.CreateRules().NamingRules.AddRange(FallbackNamingRules.Default);
    }
}
