// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PatternMatching;
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
           IReadOnlyList<(CompletionItem, PatternMatch?)> itemsWithPatternMatch,
           string filterText,
           IList<CompletionItem> builder)
            => FilterItemsImpl(document, itemsWithPatternMatch, filterText, builder);

        internal virtual void FilterItemsImpl(
            Document document,
            IReadOnlyList<(CompletionItem, PatternMatch?)> itemsWithPatternMatch,
            string filterText,
            IList<CompletionItem> builder)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            builder.AddRange(FilterItems(document, itemsWithPatternMatch.SelectAsArray(item => item.Item1), filterText));
#pragma warning restore RS0030 // Do not used banned APIs
        }
    }
}
