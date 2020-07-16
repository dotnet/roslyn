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
        public static readonly Option2<bool> PreferProvisionalTab = new Option2<bool>(nameof(NavigationOptions), nameof(PreferProvisionalTab), defaultValue: false);

        /// <summary>
        /// This option can be passed to the <see cref="IDocumentNavigationService"/> APIs to request that the navigation should activate the tab.
        /// The default for the platform is to activate the tab, so turning the option off tells the platform to not activate the tab.
        /// </summary>
        public static readonly Option2<bool> ActivateTab = new Option2<bool>(nameof(NavigationOptions), nameof(ActivateTab), defaultValue: true);
    }
}
