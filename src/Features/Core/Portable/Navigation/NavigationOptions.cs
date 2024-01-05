// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Navigation
{
    /// <summary>
    /// Navigation options.
    /// </summary>
    /// <param name="PreferProvisionalTab">
    /// This option can be passed to the <see cref="IDocumentNavigationService"/> APIs to request that a provisional (or preview) tab 
    /// be used for any document that needs to be opened, if one is available.
    /// </param>
    /// <param name="ActivateTab">
    /// This option can be passed to the <see cref="IDocumentNavigationService"/> APIs to request that the navigation should activate the tab.
    /// The default for the platform is to activate the tab, so turning the option off tells the platform to not activate the tab.
    /// </param>
    internal readonly record struct NavigationOptions(
        bool PreferProvisionalTab = false,
        bool ActivateTab = true)
    {
        public NavigationOptions()
            : this(PreferProvisionalTab: false)
        {
        }

        public static readonly NavigationOptions Default = new();
    }
}
