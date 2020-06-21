// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    internal enum NavigationBehavior
    {
        /// <summary>
        /// The destination will attempt to open in a normal tab and activate
        /// </summary>
        Normal,

        /// <summary>
        /// The destination will navigate using a preview window and not activate that window.
        /// Useful for cases where the user might be going through a list of items and we want to
        /// make the context visible but not make focus changes
        /// </summary>
        PreviewWithoutFocus,

        /// <summary>
        /// The destination will navigate using a preview window and activate 
        /// </summary>
        PreviewWithFocus
    }
}
