﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptCompletionServiceWithProviders : CompletionService
    {
        internal VSTypeScriptCompletionServiceWithProviders(Workspace workspace)
            : base(workspace.Services.SolutionServices)
        {
        }

        internal sealed override CompletionRules GetRules(CompletionOptions options)
            => GetRulesImpl();

        internal abstract CompletionRules GetRulesImpl();

        internal sealed override void FilterItems(
           Document document,
           IReadOnlyList<MatchResult> matchResults,
           string filterText,
           IList<MatchResult> builder)
            => FilterItemsImpl(document, matchResults, filterText, builder);

        internal virtual void FilterItemsImpl(
            Document document,
            IReadOnlyList<MatchResult> matchResults,
            string filterText,
            IList<MatchResult> builder)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            var filteredItems = FilterItems(document, matchResults.SelectAsArray(item => item.CompletionItem), filterText);
#pragma warning restore RS0030 // Do not used banned APIs

            var helper = CompletionHelper.GetHelper(document);
            builder.AddRange(filteredItems.Select(item => helper.GetMatchResult(item, filterText, includeMatchSpans: false, CultureInfo.CurrentCulture)));
        }
    }
}
