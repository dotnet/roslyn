// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    internal static class GoToDefinitionOptionsStorage
    {
        public static bool GetNavigateToDecompiledSources(this IGlobalOptionService globalOptions)
            => globalOptions.GetOption(NavigateToDecompiledSources);

        public static bool GetAlwaysUseDefaultSymbolServers(this IGlobalOptionService globalOptions)
            => globalOptions.GetOption(AlwaysUseDefaultSymbolServers);

        public static Option2<bool> NavigateToDecompiledSources =
            new("FeatureOnOffOptions", "NavigateToDecompiledSources", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation($"TextEditor.NavigateToDecompiledSources"));

        public static Option2<bool> AlwaysUseDefaultSymbolServers =
            new("FeatureOnOffOptions", "AlwaysUseDefaultSymbolServers", defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation($"TextEditor.AlwaysUseDefaultSymbolServers"));
    }
}
