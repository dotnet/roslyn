// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal static class FileBasedAppsOptionsStorage
{
    private static readonly OptionGroup s_optionGroup = new(name: "file_based_apps", description: "");

    /// <summary>
    /// Whether to automatically discover and load file-based app entry points.
    /// </summary>
    public static readonly Option2<bool> EnableAutomaticDiscovery = new("dotnet_enable_automatic_discovery", defaultValue: true, s_optionGroup);
}
