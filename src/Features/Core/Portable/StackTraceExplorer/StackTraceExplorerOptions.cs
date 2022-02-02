// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal static class StackTraceExplorerOptions
    {
        /// <summary>
        /// Used to determine if a user focusing VS should look at the clipboard for a callstack and automatically
        /// open the tool window with the callstack inserted
        /// </summary>
        public static readonly Option2<bool> OpenOnFocus = new(
            nameof(StackTraceExplorerOptions),
            nameof(OpenOnFocus),
            true,
            storageLocation: new RoamingProfileStorageLocation("StackTraceExplorer.Options.OpenOnFocus"));
    }
}
