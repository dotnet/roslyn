// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface INavigableDocumentLocation
    {
        /// <summary>
        /// Navigates to a location in a particular document, opening it if necessary.  This work must happen quickly.
        /// Any expensive async work must be done in the corresponding IDocumentNavigationService.GetNavigableLocation
        /// method.  This method is async only to allow final clients to call this from a non-UI thread while allowing
        /// the navigation to jump to the UI thread.
        /// </summary>
        Task<bool> NavigateToAsync(CancellationToken cancelletionToken);
    }

    internal class CallbackNavigableDocumentLocation : INavigableDocumentLocation
    {
        private readonly Func<CancellationToken, Task<bool>> _callback;

        public CallbackNavigableDocumentLocation(Func<CancellationToken, Task<bool>> callback)
            => _callback = callback;

        public CallbackNavigableDocumentLocation(Func<bool> callback)
            : this(_ => callback() ? SpecializedTasks.True : SpecializedTasks.False)
        {
        }

        public Task<bool> NavigateToAsync(CancellationToken cancellationToken)
            => _callback(cancellationToken);
    }
}
