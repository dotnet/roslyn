// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal static class NavigationOptions
    {
        /// <summary>
        /// This option can be passed to the <see cref="IDocumentNavigationService"/> APIs to request that a provisional (or preview) tab 
        /// be used for any document that needs to be opened, if one is available.
        /// </summary>
        public static readonly Option<bool> PreferProvisionalTab = new Option<bool>(nameof(NavigationOptions), nameof(PreferProvisionalTab), defaultValue: false);
    }
}
