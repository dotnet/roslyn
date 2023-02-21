// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal static class MetadataAsSourceOptionsStorage
{
    public static MetadataAsSourceOptions GetMetadataAsSourceOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => new(GenerationOptions: globalOptions.GetCleanCodeGenerationOptions(languageServices))
        {
            NavigateToDecompiledSources = globalOptions.GetOption(NavigateToDecompiledSources),
            AlwaysUseDefaultSymbolServers = globalOptions.GetOption(AlwaysUseDefaultSymbolServers),
            NavigateToSourceLinkAndEmbeddedSources = globalOptions.GetOption(NavigateToSourceLinkAndEmbeddedSources),
        };

    public static Option2<bool> NavigateToDecompiledSources = new("FeatureOnOffOptions_NavigateToDecompiledSources", defaultValue: true);
    public static Option2<bool> AlwaysUseDefaultSymbolServers = new("dotnet_always_use_default_symbol_servers", defaultValue: true);
    public static Option2<bool> NavigateToSourceLinkAndEmbeddedSources = new("FeatureOnOffOptions_NavigateToSourceLinkAndEmbeddedSources", defaultValue: true);
}
