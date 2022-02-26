// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface INavigableDocumentLocation
    {
        /// <summary>
        /// Navigates to a location in a particular document, opening it if necessary.  This work must happen
        /// synchronously and quickly. Any expensive async work must be done in the corresponding
        /// IDocumentNavigationService.GetNavigableLocationXXX method.
        /// </summary>
        bool NavigateTo();
    }

    internal class CallbackNavigableDocumentLocation : INavigableDocumentLocation
    {
        private readonly Func<bool> _callback;

        public CallbackNavigableDocumentLocation(Func<bool> callback)
            => _callback = callback;

        public bool NavigateTo()
            => _callback();
    }
}
