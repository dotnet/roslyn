// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptCompletionServiceWithProviders : CompletionServiceWithProviders
    {
        internal VSTypeScriptCompletionServiceWithProviders(Workspace workspace)
            : base(workspace)
        {
        }

        internal sealed override CompletionRules GetRules(CompletionOptions options)
            => GetRulesImpl();

        internal abstract CompletionRules GetRulesImpl();

        internal override Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionOptions options,
            OptionSet passThroughOptions,
            CompletionTrigger trigger,
            ImmutableHashSet<string>? roles,
            CancellationToken cancellationToken)
        {
            return GetCompletionsWithAvailabilityOfExpandedItemsAsync(document, caretPosition, options, passThroughOptions, trigger, roles, cancellationToken);
        }
    }
}
