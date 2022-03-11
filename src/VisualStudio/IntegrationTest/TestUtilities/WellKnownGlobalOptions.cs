﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Options settable by integration tests.
    /// 
    /// TODO: Options are currently explicitly listed since <see cref="OptionKey"/> is not serializable.
    /// https://github.com/dotnet/roslyn/issues/59267
    /// </summary>
    public enum WellKnownGlobalOption
    {
        CompletionOptions_ShowItemsFromUnimportedNamespaces,
        CompletionViewOptions_EnableArgumentCompletionSnippets,
        CompletionOptions_TriggerInArgumentLists,
        InlineRenameSessionOptions_RenameInComments,
        InlineRenameSessionOptions_RenameInStrings,
        InlineRenameSessionOptions_RenameOverloads,
        InlineRenameSessionOptions_RenameFile,
        InlineRenameSessionOptions_PreviewChanges,
    }

    public static class WellKnownGlobalOptions
    {
        public static IOption GetOption(this WellKnownGlobalOption option)
            => option switch
            {
                WellKnownGlobalOption.CompletionOptions_ShowItemsFromUnimportedNamespaces => CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
                WellKnownGlobalOption.CompletionOptions_TriggerInArgumentLists => CompletionOptionsStorage.TriggerInArgumentLists,
                WellKnownGlobalOption.CompletionViewOptions_EnableArgumentCompletionSnippets => CompletionViewOptions.EnableArgumentCompletionSnippets,
                WellKnownGlobalOption.InlineRenameSessionOptions_RenameInComments => InlineRenameSessionOptionsStorage.RenameInComments,
                WellKnownGlobalOption.InlineRenameSessionOptions_RenameInStrings => InlineRenameSessionOptionsStorage.RenameInStrings,
                WellKnownGlobalOption.InlineRenameSessionOptions_RenameOverloads => InlineRenameSessionOptionsStorage.RenameOverloads,
                WellKnownGlobalOption.InlineRenameSessionOptions_RenameFile => InlineRenameSessionOptionsStorage.RenameFile,
                WellKnownGlobalOption.InlineRenameSessionOptions_PreviewChanges => InlineRenameSessionOptionsStorage.PreviewChanges,
                _ => throw ExceptionUtilities.Unreachable
            };

        public static OptionKey GetKey(this WellKnownGlobalOption option, string? language)
            => new OptionKey(GetOption(option), language);
    }
}
