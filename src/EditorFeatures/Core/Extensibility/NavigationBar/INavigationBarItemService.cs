// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigationBarItemService : ILanguageService
    {
        Task<ImmutableArray<NavigationBarItem>> GetItemsAsync(Document document, bool forceFrozenPartialSemanticsForCrossProcessOperations, ITextVersion textVersion, CancellationToken cancellationToken);
        bool ShowItemGrayedIfNear(NavigationBarItem item);

        /// <summary>
        /// Returns <see langword="true"/> if navigation (or generation) happened.  <see langword="false"/> otherwise.
        /// </summary>
        Task<bool> TryNavigateToItemAsync(
            Document document, NavigationBarItem item, ITextView view, ITextVersion textVersion, CancellationToken cancellationToken);
    }
}
