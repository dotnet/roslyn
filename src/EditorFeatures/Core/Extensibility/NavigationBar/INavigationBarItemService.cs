// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigationBarItemService : ILanguageService
    {
        Task<IList<NavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken);
        bool ShowItemGrayedIfNear(NavigationBarItem item);
        void NavigateToItem(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken);
    }
}
