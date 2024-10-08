// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal static class MetadataAsSourceOptionsStorage
{
    public static MetadataAsSourceOptions GetMetadataAsSourceOptions(this IGlobalOptionService globalOptions)
        => new()
        {
            NavigateToDecompiledSources = globalOptions.GetOption(NavigateToDecompiledSources),
            AlwaysUseDefaultSymbolServers = globalOptions.GetOption(AlwaysUseDefaultSymbolServers),
            NavigateToSourceLinkAndEmbeddedSources = globalOptions.GetOption(NavigateToSourceLinkAndEmbeddedSources),
        };

    private static readonly OptionGroup s_navigationOptionGroup = new(name: "navigation", description: "");

    public static Option2<bool> NavigateToDecompiledSources = new("dotnet_navigate_to_decompiled_sources", defaultValue: true, group: s_navigationOptionGroup);
    public static Option2<bool> AlwaysUseDefaultSymbolServers = new("dotnet_always_use_default_symbol_servers", defaultValue: true, group: s_navigationOptionGroup);
    public static Option2<bool> NavigateToSourceLinkAndEmbeddedSources = new("dotnet_navigate_to_source_link_and_embedded_sources", defaultValue: true, group: s_navigationOptionGroup);
}
