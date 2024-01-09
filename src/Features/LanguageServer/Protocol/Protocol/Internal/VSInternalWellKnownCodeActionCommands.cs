// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Class which contains the string values for all well-known Visual Studion LSP code action commands.
    /// </summary>
    internal static class VSInternalWellKnownCodeActionCommands
    {
        /// <summary>
        /// Command name for '_ms_setClipboard'.
        /// </summary>
        public const string SetClipboard = "_ms_setClipboard";

        /// <summary>
        /// Command name for '_ms_openUrl'.
        /// </summary>
        public const string OpenUrl = "_ms_openUrl";
    }
}
