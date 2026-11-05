// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A subclass of the LSP protocol <see cref="CompletionList"/> that ensures correct serialization between LSP servers.
/// </summary>
/// <remarks>
/// This is the same as the LSP protocol <see cref="CompletionList"/> except that it strongly types the <see cref="Items"/> property,
/// because our custom message target gets handled by a JsonRpc connection set up by the editor, that has no Roslyn converters.
/// </remarks>
internal sealed class RazorVSInternalCompletionList : CompletionList
{
    public RazorVSInternalCompletionList()
    {
    }

    [SetsRequiredMembers]
    public RazorVSInternalCompletionList(CompletionList completionList)
        : this(completionList, JsonHelpers.ConvertAll<CompletionItem, CompletionItem>(completionList.Items))
    {
    }

    [SetsRequiredMembers]
    public RazorVSInternalCompletionList(CompletionList completionList, CompletionItem[] items)
    {
        this.Data = completionList.Data;
        this.CommitCharacters = completionList.CommitCharacters;
        this.ContinueCharacters = completionList.ContinueCharacters;
        this.IsIncomplete = completionList.IsIncomplete;
        this.Items = items;
        this.ItemDefaults = completionList.ItemDefaults;
        this.SuggestionMode = completionList.SuggestionMode;
    }

    /// <summary>
    /// The completion items.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonRequired]
    public new required CompletionItem[] Items
    {
        get => (CompletionItem[])base.Items;
        set => base.Items = value;
    }
}
