// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Search.Data;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal sealed partial class RoslynSearchItemsSourceProvider
{
    private sealed class RoslynSearchResultView : CodeSearchResultViewBase
    {
        private readonly RoslynSearchItemsSourceProvider _provider;
        private readonly INavigateToSearchResult _searchResult;

        public RoslynSearchResultView(
            RoslynSearchItemsSourceProvider provider,
            INavigateToSearchResult searchResult,
            HighlightedText title,
            HighlightedText description,
            ImageId primaryIcon)
            : base(title, description, primaryIcon: primaryIcon)
        {
            _provider = provider;
            _searchResult = searchResult;

            var filePath = _searchResult.NavigableItem.Document.FilePath;
            if (filePath != null)
                this.FileLocation = new HighlightedText(filePath, Array.Empty<VisualStudio.Text.Span>());
        }

        public override void Invoke(CancellationToken cancellationToken)
            => NavigateToHelpers.NavigateTo(
                _searchResult, _provider._threadingContext, _provider._threadOperationExecutor, _provider._asyncListener);
    }
}
