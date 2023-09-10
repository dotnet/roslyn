// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace
{
    internal static class LanguageServerProjectSystemOptionsStorage
    {
        private static readonly OptionGroup s_optionGroup = new(name: "projects", description: "");

        /// <summary>
        /// A folder to log binlogs to when running design-time builds.
        /// </summary>
        public static readonly Option2<string?> BinaryLogPath = new Option2<string?>("dotnet_binary_log_path", defaultValue: null, s_optionGroup);

        /// <summary>
        /// Whether we are doing design-time builds in-process; this is only to offer a fallback if the OOP builds are broken, and should be removed once
        /// we don't have folks using this.
        /// </summary>
        public static readonly Option2<bool> LoadInProcess = new("dotnet_load_in_process", defaultValue: false, s_optionGroup);
    }
}
