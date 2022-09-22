// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting.Rules;

internal static class FormattingRuleUtilities
{
    public static ImmutableArray<AbstractFormattingRule> GetFormattingRules(
        ParsedDocument document, TextSpan span, IEnumerable<AbstractFormattingRule>? additionalRules)
    {
        var formattingRuleFactory = document.SolutionServices.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
        // Not sure why this is being done... there aren't any docs on CreateRule either.
        var position = (span.Start + span.End) / 2;

        var rules = ImmutableArray.Create(formattingRuleFactory.CreateRule(document, position));
        if (additionalRules != null)
        {
            rules = rules.AddRange(additionalRules);
        }

        return rules.AddRange(Formatter.GetDefaultFormattingRules(document.LanguageServices));
    }
}
