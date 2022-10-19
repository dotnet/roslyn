// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal static class MetadataAsSourceOptionsStorage
    {
        public static MetadataAsSourceOptions GetMetadataAsSourceOptions(this IGlobalOptionService globalOptions)
            => new(
                NavigateToDecompiledSources: globalOptions.GetOption(NavigateToDecompiledSources),
                AlwaysUseDefaultSymbolServers: globalOptions.GetOption(AlwaysUseDefaultSymbolServers));

        public static Option2<bool> NavigateToDecompiledSources =
            new("FeatureOnOffOptions", "NavigateToDecompiledSources", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation($"TextEditor.NavigateToDecompiledSources"));

        public static Option2<bool> AlwaysUseDefaultSymbolServers =
            new("FeatureOnOffOptions", "AlwaysUseDefaultSymbolServers", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation($"TextEditor.AlwaysUseDefaultSymbolServers"));
    }
}
