// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Snippets;

/// <summary>
/// Stores only the data needed for the creation of a CompletionItem.
/// Avoids using the Snippet and creating a TextChange/finding cursor
/// position before we know it was the selected CompletionItem.
/// </summary>
internal readonly struct SnippetData(string description, string identifier, ImmutableArray<string> additionalFilterTexts)
{
    public readonly string Description = description;
    public readonly string Identifier = identifier;
    public readonly ImmutableArray<string> AdditionalFilterTexts = additionalFilterTexts;
}
