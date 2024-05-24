// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.Completion;

internal sealed class CompletionHelper(bool isCaseSensitive)
{
    private static CompletionHelper CaseSensitiveInstance { get; } = new CompletionHelper(isCaseSensitive: true);
    private static CompletionHelper CaseInsensitiveInstance { get; } = new CompletionHelper(isCaseSensitive: false);

    private readonly bool _isCaseSensitive = isCaseSensitive;

    public static CompletionHelper GetHelper(Document document)
    {
        var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
        var caseSensitive = syntaxFacts?.IsCaseSensitive ?? true;

        return caseSensitive
            ? CaseSensitiveInstance
            : CaseInsensitiveInstance;
    }

    public int CompareMatchResults(MatchResult matchResult1, MatchResult matchResult2, bool filterTextHasNoUpperCase)
    {
        var item1 = matchResult1.CompletionItem;
        var match1 = matchResult1.PatternMatch;

        var item2 = matchResult2.CompletionItem;
        var match2 = matchResult2.PatternMatch;

        if (match1 != null && match2 != null)
        {
            var result = CompareItems(match1.Value, match2.Value, item1, item2, _isCaseSensitive, filterTextHasNoUpperCase);
            if (result != 0)
            {
                return result;
            }
        }
        else if (match1 != null)
        {
            return -1;
        }
        else if (match2 != null)
        {
            return 1;
        }

        var matchPriorityDiff = CompareSpecialMatchPriorityValues(item1, item2);
        if (matchPriorityDiff != 0)
        {
            return matchPriorityDiff;
        }

        // Prefer things with a keyword tag, if the filter texts are the same.
        if (!TagsEqual(item1, item2) && item1.FilterText == item2.FilterText)
        {
            return (!IsKeywordItem(item1)).CompareTo(!IsKeywordItem(item2));
        }

        return 0;

        static bool IsKeywordItem(CompletionItem item)
            => item.Tags.Contains(WellKnownTags.Keyword);

        static bool TagsEqual(CompletionItem item1, CompletionItem item2)
            => System.Linq.ImmutableArrayExtensions.SequenceEqual(item1.Tags, item2.Tags);
    }

    private static int CompareItems(
        PatternMatch match1,
        PatternMatch match2,
        CompletionItem item1,
        CompletionItem item2,
        bool isCaseSensitive,
        bool filterTextHasNoUpperCase)
    {
        // *Almost* always prefer non-expanded item regardless of the pattern matching result.
        // Except when all non-expanded items are worse than prefix matching and there's
        // a complete match from expanded ones. 
        //
        // For example, In the scenarios below, `NS2.Designer` would be selected over `System.Security.Cryptography.DES`
        //
        //  namespace System.Security.Cryptography
        //  {
        //      class DES {}
        //  }
        //  namespace NS2
        //  {
        //      class Designer {}
        //      class C
        //      {
        //          des$$
        //      }
        //  }
        //
        // But in this case, `System.Security.Cryptography.DES` would be selected over `NS2.MyDesigner`
        //
        //  namespace System.Security.Cryptography
        //  {
        //      class DES {}
        //  }
        //  namespace NS2
        //  {
        //      class MyDesigner {}
        //      class C
        //      {
        //          des$$
        //      }
        //  }
        //
        // This currently means items from unimported namespaces (those are the only expanded items now) 
        // are treated as "2nd tier" results, which forces users to be more explicit about selecting them.
        var expandedDiff = CompareExpandedItem(item1, match1, item2, match2);
        if (expandedDiff != 0)
        {
            return expandedDiff;
        }

        // Then see how the two items compare in a case insensitive fashion.  Matches that 
        // are strictly better (ignoring case) should prioritize the item.  i.e. if we have
        // a prefix match, that should always be better than a substring match.
        //
        // The reason we ignore case is that it's very common for people to type expecting
        // completion to fix up their casing.  i.e. 'false' will be written with the 
        // expectation that it will get fixed by the completion list to 'False'.  
        var caseInsensitiveComparison = match1.CompareTo(match2, ignoreCase: true);
        if (caseInsensitiveComparison != 0)
        {
            return caseInsensitiveComparison;
        }

        // Now we have two items match in case-insensitive manner,
        //
        // 1. if we are in a case-insensitive language, we'd first check if either item has the MatchPriority set to one of
        // the two special values ("Preselect" and "Deprioritize"). If so and these two items have different MatchPriority,
        // then we'd select the one of "Preselect", or the one that's not of "Deprioritize". Otherwise we will prefer the one
        // matches case-sensitively. This is to make sure common items in VB like "True" and "False" are prioritized for selection
        // when user types "t" and "f" (see https://github.com/dotnet/roslyn/issues/4892)
        //
        // 2. or similarly, if the filter text contains only lowercase letters, we want to relax our filtering standard a tiny
        // bit to account for the sceanrio that users expect completion to fix the casing. This only happens if one of the item's
        // MatchPriority is "Deprioritize". Otherwise we will always prefer the one matches case-sensitively.
        // This is to make sure uncommon items like conversion "(short)" are not selected over `Should` when user types `sho`
        // (see https://github.com/dotnet/roslyn/issues/55546)

        var specialMatchPriorityValuesDiff = 0;
        if (!isCaseSensitive)
        {
            specialMatchPriorityValuesDiff = CompareSpecialMatchPriorityValues(item1, item2);
        }
        else if (filterTextHasNoUpperCase)
        {
            specialMatchPriorityValuesDiff = CompareDeprioritization(item1, item2);
        }

        if (specialMatchPriorityValuesDiff != 0)
            return specialMatchPriorityValuesDiff;

        // At this point we have two items which we're matching in a rather similar fashion.
        // If one is a prefix of the other, prefer the prefix.  i.e. if we have 
        // "Table" and "table:=" and the user types 't' and we are in a case insensitive 
        // language, then we prefer the former.
        if (item1.GetEntireDisplayText().Length != item2.GetEntireDisplayText().Length)
        {
            var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (item2.GetEntireDisplayText().StartsWith(item1.GetEntireDisplayText(), comparison))
            {
                return -1;
            }
            else if (item1.GetEntireDisplayText().StartsWith(item2.GetEntireDisplayText(), comparison))
            {
                return 1;
            }
        }

        // Now compare the matches again in a case sensitive manner.  If everything was
        // equal up to this point, we prefer the item that better matches based on case.
        return match1.CompareTo(match2, ignoreCase: false);
    }

    private static int CompareSpecialMatchPriorityValues(CompletionItem item1, CompletionItem item2)
    {
        if (item1.Rules.MatchPriority == item2.Rules.MatchPriority)
            return 0;

        var deprioritizationCompare = CompareDeprioritization(item1, item2);
        return deprioritizationCompare == 0
            ? ComparePreselection(item1, item2)
            : deprioritizationCompare;
    }

    /// <summary>
    ///  If 2 items differ on preselection, then item1 is better if it is preselected, otherwise it is worse.
    /// </summary>
    private static int ComparePreselection(CompletionItem item1, CompletionItem item2)
        => (item1.Rules.MatchPriority != MatchPriority.Preselect).CompareTo(item2.Rules.MatchPriority != MatchPriority.Preselect);

    /// <summary>
    /// If 2 items differ on depriorization, then item1 is worse if it is depriozritized, otherwise it is better.
    /// </summary>
    private static int CompareDeprioritization(CompletionItem item1, CompletionItem item2)
        => (item1.Rules.MatchPriority == MatchPriority.Deprioritize).CompareTo(item2.Rules.MatchPriority == MatchPriority.Deprioritize);

    private static int CompareExpandedItem(CompletionItem item1, PatternMatch match1, CompletionItem item2, PatternMatch match2)
    {
        var isItem1Expanded = item1.Flags.IsExpanded();
        var isItem2Expanded = item2.Flags.IsExpanded();

        // Consider them equal if both items are of the same kind (i.e. both expanded or non-expanded)
        if (isItem1Expanded == isItem2Expanded)
        {
            return 0;
        }

        // Now we have two items of different kind.
        // If neither item is exact match, we always prefer non-expanded one.
        // For example, `NS2.MyTask` would be selected over `NS1.Tasks` 
        //
        //  namespace NS1
        //  {
        //      class Tasks {}
        //  }
        //  namespace NS2
        //  {
        //      class MyTask {}
        //      class C
        //      {
        //          task$$
        //      }
        //  }
        if (match1.Kind != PatternMatchKind.Exact && match2.Kind != PatternMatchKind.Exact)
        {
            return isItem1Expanded ? 1 : -1;
        }

        // Now we have two items of different kind and at least one is exact match.
        // Prefer non-expanded item if it is prefix match or better.
        // In the scenarios below, `NS2.Designer` would be selected over `System.Security.Cryptography.DES`
        //
        //  namespace System.Security.Cryptography
        //  {
        //      class DES {}
        //  }
        //  namespace NS2
        //  {
        //      class Designer {}
        //      class C
        //      {
        //          des$$
        //      }
        //  }
        if (!isItem1Expanded && match1.Kind <= PatternMatchKind.Prefix)
        {
            return -1;
        }

        if (!isItem2Expanded && match2.Kind <= PatternMatchKind.Prefix)
        {
            return 1;
        }

        // Now we are left with an expanded item with exact match and a non-expanded item with worse than prefix match.
        // Prefer non-expanded item with exact match.
        Debug.Assert(isItem1Expanded && match1.Kind == PatternMatchKind.Exact && !isItem2Expanded && match2.Kind > PatternMatchKind.Prefix ||
                     isItem2Expanded && match2.Kind == PatternMatchKind.Exact && !isItem1Expanded && match1.Kind > PatternMatchKind.Prefix);
        return isItem1Expanded ? -1 : 1;
    }
}
