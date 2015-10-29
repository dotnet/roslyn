// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal static class NavigationOptions
    {
        public const string FeatureName = "Navigation";

        /// <summary>
        /// This option can be passed to the <see cref="IDocumentNavigationService"/> APIs to request that a provisional (or preview) tab 
        /// be used for any document that needs to be opened, if one is available.
        /// </summary>
        public static readonly Option<bool> PreferProvisionalTab = new Option<bool>(FeatureName, "PreferProvisionalTab", defaultValue: false);
    }
}
