// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Completion;

internal readonly record struct OmniSharpCompletionOptions(
    bool ShowItemsFromUnimportedNamespaces,
    bool ForceExpandedCompletionIndexCreation)
{
    internal CompletionOptions ToCompletionOptions()
        => CompletionOptions.Default with
        {
            ShowItemsFromUnimportedNamespaces = ShowItemsFromUnimportedNamespaces,
            ForceExpandedCompletionIndexCreation = ForceExpandedCompletionIndexCreation,
            UpdateImportCompletionCacheInBackground = true,
        };
}
