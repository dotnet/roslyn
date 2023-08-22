// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Options settable by integration tests.
    /// 
    /// TODO: Options are currently explicitly listed since <see cref="OptionKey2"/> is not serializable.
    /// https://github.com/dotnet/roslyn/issues/59267
    /// </summary>
    public enum WellKnownGlobalOption
    {
        CompletionOptions_ShowItemsFromUnimportedNamespaces,
        CompletionOptions_TriggerInArgumentLists,
        MetadataAsSourceOptions_NavigateToDecompiledSources,
    }

    internal static class WellKnownGlobalOptions
    {
        public static IOption2 GetOption(this WellKnownGlobalOption option)
            => option switch
            {
                WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces => CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
                WellKnownGlobalOption.CompletionOptions_TriggerInArgumentLists => CompletionOptionsStorage.TriggerInArgumentLists,
                WellKnownGlobalOption.MetadataAsSourceOptions_NavigateToDecompiledSources => MetadataAsSourceOptionsStorage.NavigateToDecompiledSources,
                _ => throw ExceptionUtilities.Unreachable()
            };

        public static OptionKey2 GetKey(this WellKnownGlobalOption option, string? language)
            => new OptionKey2(GetOption(option), language);
    }
}
