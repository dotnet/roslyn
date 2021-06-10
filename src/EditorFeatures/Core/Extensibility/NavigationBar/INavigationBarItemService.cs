// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [Obsolete("Legacy API for TypeScript.  Once TypeScript moves to IVSTypeScriptNavigationBarItemService", error: false)]
    internal interface INavigationBarItemService : ILanguageService
    {
        Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken);
        bool ShowItemGrayedIfNear(NavigationBarItem item);
        void NavigateToItem(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken);
    }

    internal interface INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess : ILanguageService
    {
        Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken);
        bool ShowItemGrayedIfNear(NavigationBarItem item);
        /// <summary>
        /// Returns <see langword="true"/> if navigation (or generation) happened.  <see langword="false"/> otherwise.
        /// </summary>
        Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken);
    }

    internal class NavigationBarItemServiceWrapper : INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly INavigationBarItemService _service;

        public NavigationBarItemServiceWrapper(INavigationBarItemService service)
            => _service = service;
#pragma warning restore CS0618 // Type or member is obsolete

        public Task<IList<NavigationBarItem>?> GetItemsAsync(Document document, CancellationToken cancellationToken)
            => _service.GetItemsAsync(document, cancellationToken);

        public bool ShowItemGrayedIfNear(NavigationBarItem item)
            => _service.ShowItemGrayedIfNear(item);

        public Task<bool> TryNavigateToItemAsync(Document document, NavigationBarItem item, ITextView view, CancellationToken cancellationToken)
        {
            _service.NavigateToItem(document, item, view, cancellationToken);
            return SpecializedTasks.True;
        }
    }
}
