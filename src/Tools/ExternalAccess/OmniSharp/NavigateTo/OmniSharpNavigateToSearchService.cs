// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Navigation;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.NavigateTo;

internal static class OmniSharpNavigateToSearcher
{
    public delegate Task OmniSharpNavigateToCallback(Project project, in OmniSharpNavigateToSearchResult result, CancellationToken cancellationToken);

    public static Task SearchAsync(
        Solution solution,
        OmniSharpNavigateToCallback callback,
        string searchPattern,
        IImmutableSet<string> kinds,
        CancellationToken cancellationToken)
    {
        var searcher = NavigateToSearcher.Create(
            solution,
            AsynchronousOperationListenerProvider.NullListener,
            new OmniSharpNavigateToCallbackImpl(callback),
            searchPattern,
            kinds,
            disposalToken: CancellationToken.None);

        return searcher.SearchAsync(searchCurrentDocument: false, cancellationToken);
    }

    private sealed class OmniSharpNavigateToCallbackImpl : INavigateToSearchCallback
    {
        private readonly OmniSharpNavigateToCallback _callback;

        public OmniSharpNavigateToCallbackImpl(OmniSharpNavigateToCallback callback)
        {
            _callback = callback;
        }

        public async Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
        {
            var document = await result.NavigableItem.Document.GetRequiredDocumentAsync(project.Solution, cancellationToken).ConfigureAwait(false);
            var omniSharpResult = new OmniSharpNavigateToSearchResult(
                result.AdditionalInformation,
                result.Kind,
                (OmniSharpNavigateToMatchKind)result.MatchKind,
                result.IsCaseSensitive,
                result.Name,
                result.NameMatchSpans,
                result.SecondarySort,
                result.Summary!,
                new OmniSharpNavigableItem(result.NavigableItem.DisplayTaggedParts, document, result.NavigableItem.SourceSpan));

            await _callback(project, omniSharpResult, cancellationToken).ConfigureAwait(false);
        }

        public void Done(bool isFullyLoaded)
        {
        }

        public void ReportProgress(int current, int maximum)
        {
        }

        public void ReportIncomplete()
        {
        }
    }
}
