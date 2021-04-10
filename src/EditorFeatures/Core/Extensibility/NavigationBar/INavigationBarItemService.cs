// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigationBarItemService : ILanguageService
    {
        Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken);
        bool ShowItemGrayedIfNear(NavigationBarItem item);

        /// <summary>
        /// Legacy api for TypeScript.  Needed until we can move them to EA pattern for navbars.
        /// </summary>
        void NavigateToItem(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken);
    }

    internal interface INavigationBarItemService2 : INavigationBarItemService
    {
        Task NavigateToItemAsync(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken);
    }
}
