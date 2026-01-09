// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Core.Imaging;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class OptimizedVSCompletionListJsonConverter : JsonConverter<OptimizedVSCompletionList>
{
    public static readonly OptimizedVSCompletionListJsonConverter Instance = new();
    private static readonly ConcurrentDictionary<ImageId, string> IconRawJson = new();

    public override OptimizedVSCompletionList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, OptimizedVSCompletionList value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var completionList = (VSInternalCompletionList)value;

        writer.WriteStartObject();

        if (completionList.SuggestionMode)
        {
            writer.WriteBoolean(VSInternalCompletionList.SuggestionModeSerializedName, completionList.SuggestionMode);
        }
        else
        {
            // Default is "false" so no need to serialize
        }

        if (completionList.ContinueCharacters != null && completionList.ContinueCharacters.Length > 0)
        {
            writer.WritePropertyName(VSInternalCompletionList.ContinueCharactersSerializedName);
            JsonSerializer.Serialize(writer, completionList.ContinueCharacters, options);
        }

        if (completionList.Data != null)
        {
            writer.WritePropertyName(VSInternalCompletionList.DataSerializedName);
            JsonSerializer.Serialize(writer, completionList.Data, options);
        }

        if (completionList.CommitCharacters != null)
        {
            writer.WritePropertyName(VSInternalCompletionList.CommitCharactersSerializedName);
            JsonSerializer.Serialize(writer, completionList.CommitCharacters, options);
        }

        // this is a required property per the LSP spec
        writer.WriteBoolean("isIncomplete", completionList.IsIncomplete);

        writer.WritePropertyName("items");
        if (completionList.Items == null || completionList.Items.Length == 0)
        {
            writer.WriteRawValue("[]");
        }
        else
        {
            writer.WriteStartArray();

            var itemRawJsonCache = new Dictionary<object, string>(capacity: 1);

            foreach (var completionItem in completionList.Items)
            {
                if (completionItem == null)
                {
                    continue;
                }

                WriteCompletionItem(writer, completionItem, options, itemRawJsonCache);
            }

            writer.WriteEndArray();
        }

        if (completionList.ItemDefaults != null)
        {
            writer.WritePropertyName("itemDefaults");
            JsonSerializer.Serialize(writer, completionList.ItemDefaults, options);
        }

        writer.WriteEndObject();
    }

    private static void WriteCompletionItem(Utf8JsonWriter writer, CompletionItem completionItem, JsonSerializerOptions options, Dictionary<object, string> itemRawJsonCache)
    {
        writer.WriteStartObject();

        if (completionItem is VSInternalCompletionItem vsCompletionItem)
        {
            if (vsCompletionItem.Icon != null)
            {
                if (!IconRawJson.TryGetValue(vsCompletionItem.Icon.ImageId, out var jsonString))
                {
                    jsonString = JsonSerializer.Serialize(vsCompletionItem.Icon, options);
                    IconRawJson.TryAdd(vsCompletionItem.Icon.ImageId, jsonString);
                }
                writer.WritePropertyName(VSInternalCompletionItem.IconSerializedName);
                writer.WriteRawValue(jsonString);
            }

            if (vsCompletionItem.Description != null)
            {
                writer.WritePropertyName(VSInternalCompletionItem.DescriptionSerializedName);
                JsonSerializer.Serialize(writer, vsCompletionItem.Description, options);
            }

            if (vsCompletionItem.VsCommitCharacters?.Value is string[] basicCommitCharacters
                && basicCommitCharacters.Length > 0)
            {
                if (!itemRawJsonCache.TryGetValue(basicCommitCharacters, out var jsonString))
                {
                    jsonString = JsonSerializer.Serialize(basicCommitCharacters, options);
                    itemRawJsonCache.Add(basicCommitCharacters, jsonString);
                }

                writer.WritePropertyName(VSInternalCompletionItem.VsCommitCharactersSerializedName);
                writer.WriteRawValue(jsonString);
            }
            else if (vsCompletionItem.VsCommitCharacters?.Value is VSInternalCommitCharacter[] augmentedCommitCharacters
                && augmentedCommitCharacters.Length > 0)
            {
                if (!itemRawJsonCache.TryGetValue(augmentedCommitCharacters, out var jsonString))
                {
                    jsonString = JsonSerializer.Serialize(augmentedCommitCharacters, options);
                    itemRawJsonCache.Add(augmentedCommitCharacters, jsonString);
                }

                writer.WritePropertyName(VSInternalCompletionItem.VsCommitCharactersSerializedName);
                writer.WriteRawValue(jsonString);
            }

            if (vsCompletionItem.VsResolveTextEditOnCommit)
            {
                writer.WriteBoolean(VSInternalCompletionItem.VsResolveTextEditOnCommitName, vsCompletionItem.VsResolveTextEditOnCommit);
            }
        }

        var label = completionItem.Label;
        writer.WriteString("label", label);

        if (completionItem.LabelDetails != null)
        {
            writer.WritePropertyName("labelDetails");
            JsonSerializer.Serialize(writer, completionItem.LabelDetails, options);
        }

        writer.WriteNumber("kind", (int)completionItem.Kind);

        if (completionItem.Detail != null)
        {
            writer.WriteString("detail", completionItem.Detail);
        }

        if (completionItem.Documentation != null)
        {
            writer.WritePropertyName("documentation");
            JsonSerializer.Serialize(writer, completionItem.Documentation, options);
        }

        // Only render preselect if it's "true"
        if (completionItem.Preselect)
        {
            writer.WriteBoolean("preselect", completionItem.Preselect);
        }

        if (completionItem.SortText != null && !string.Equals(completionItem.SortText, label, StringComparison.Ordinal))
        {
            writer.WriteString("sortText", completionItem.SortText);
        }

        if (completionItem.FilterText != null && !string.Equals(completionItem.FilterText, label, StringComparison.Ordinal))
        {
            writer.WriteString("filterText", completionItem.FilterText);
        }

        if (completionItem.InsertText != null && !string.Equals(completionItem.InsertText, label, StringComparison.Ordinal))
        {
            writer.WriteString("insertText", completionItem.InsertText);
        }

        if (completionItem.InsertTextFormat is not 0 and not InsertTextFormat.Plaintext)
        {
            writer.WriteNumber("insertTextFormat", (int)completionItem.InsertTextFormat);
        }

        if (completionItem.TextEdit != null)
        {
            writer.WritePropertyName("textEdit");
            JsonSerializer.Serialize(writer, completionItem.TextEdit, options);
        }

        if (completionItem.TextEditText != null)
        {
            writer.WritePropertyName("textEditText");
            JsonSerializer.Serialize(writer, completionItem.TextEditText, options);
        }

        if (completionItem.AdditionalTextEdits != null && completionItem.AdditionalTextEdits.Length > 0)
        {
            writer.WritePropertyName("additionalTextEdits");
            JsonSerializer.Serialize(writer, completionItem.AdditionalTextEdits, options);
        }

        if (completionItem.CommitCharacters != null && completionItem.CommitCharacters.Length > 0)
        {
            if (!itemRawJsonCache.TryGetValue(completionItem.CommitCharacters, out var jsonString))
            {
                jsonString = JsonSerializer.Serialize(completionItem.CommitCharacters, options);
                itemRawJsonCache.Add(completionItem.CommitCharacters, jsonString);
            }

            writer.WritePropertyName("commitCharacters");
            writer.WriteRawValue(jsonString);
        }

        if (completionItem.Command != null)
        {
            writer.WritePropertyName("command");
            JsonSerializer.Serialize(writer, completionItem.Command, options);
        }

        if (completionItem.Data != null)
        {
            writer.WritePropertyName("data");
            JsonSerializer.Serialize(writer, completionItem.Data, options);
        }

        writer.WriteEndObject();
    }
}
