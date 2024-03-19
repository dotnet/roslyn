// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Roslyn.Core.Imaging;
    using Newtonsoft.Json;

    internal class OptimizedVSCompletionListJsonConverter : JsonConverter
    {
        public static readonly OptimizedVSCompletionListJsonConverter Instance = new OptimizedVSCompletionListJsonConverter();
        private static readonly ConcurrentDictionary<ImageId, string> IconRawJson = new ConcurrentDictionary<ImageId, string>();

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType) => typeof(OptimizedVSCompletionList) == objectType;

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            var completionList = (VSInternalCompletionList)value;

            writer.WriteStartObject();

            if (completionList.SuggestionMode)
            {
                writer.WritePropertyName(VSInternalCompletionList.SuggestionModeSerializedName);
                writer.WriteValue(completionList.SuggestionMode);
            }
            else
            {
                // Default is "false" so no need to serialize
            }

            if (completionList.ContinueCharacters != null && completionList.ContinueCharacters.Length > 0)
            {
                writer.WritePropertyName(VSInternalCompletionList.ContinueCharactersSerializedName);
                serializer.Serialize(writer, completionList.ContinueCharacters);
            }

            if (completionList.Data != null)
            {
                writer.WritePropertyName(VSInternalCompletionList.DataSerializedName);
                serializer.Serialize(writer, completionList.Data);
            }

            if (completionList.CommitCharacters != null)
            {
                writer.WritePropertyName(VSInternalCompletionList.CommitCharactersSerializedName);
                serializer.Serialize(writer, completionList.CommitCharacters);
            }

            if (completionList.IsIncomplete)
            {
                writer.WritePropertyName("isIncomplete");
                writer.WriteValue(completionList.IsIncomplete);
            }
            else
            {
                // Default is "false" so no need to serialize
            }

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

                    WriteCompletionItem(writer, completionItem, serializer, itemRawJsonCache);
                }

                writer.WriteEndArray();
            }

            if (completionList.ItemDefaults != null)
            {
                writer.WritePropertyName("itemDefaults");
                serializer.Serialize(writer, completionList.ItemDefaults);
            }

            writer.WriteEndObject();
        }

        private static void WriteCompletionItem(JsonWriter writer, CompletionItem completionItem, JsonSerializer serializer, Dictionary<object, string> itemRawJsonCache)
        {
            writer.WriteStartObject();

            if (completionItem is VSInternalCompletionItem vsCompletionItem)
            {
                if (vsCompletionItem.Icon != null)
                {
                    if (!IconRawJson.TryGetValue(vsCompletionItem.Icon.ImageId, out var jsonString))
                    {
                        jsonString = JsonConvert.SerializeObject(vsCompletionItem.Icon, Formatting.None, ImageElementConverter.Instance);
                        IconRawJson.TryAdd(vsCompletionItem.Icon.ImageId, jsonString);
                    }

                    writer.WritePropertyName(VSInternalCompletionItem.IconSerializedName);
                    writer.WriteRawValue(jsonString);
                }

                if (vsCompletionItem.Description != null)
                {
                    writer.WritePropertyName(VSInternalCompletionItem.DescriptionSerializedName);
                    ClassifiedTextElementConverter.Instance.WriteJson(writer, vsCompletionItem.Description, serializer);
                }

                if (vsCompletionItem.VsCommitCharacters?.Value is string[] basicCommitCharacters
                    && basicCommitCharacters.Length > 0)
                {
                    if (!itemRawJsonCache.TryGetValue(basicCommitCharacters, out var jsonString))
                    {
                        jsonString = JsonConvert.SerializeObject(basicCommitCharacters);
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
                        jsonString = JsonConvert.SerializeObject(augmentedCommitCharacters);
                        itemRawJsonCache.Add(augmentedCommitCharacters, jsonString);
                    }

                    writer.WritePropertyName(VSInternalCompletionItem.VsCommitCharactersSerializedName);
                    writer.WriteRawValue(jsonString);
                }

                if (vsCompletionItem.VsResolveTextEditOnCommit)
                {
                    writer.WritePropertyName(VSInternalCompletionItem.VsResolveTextEditOnCommitName);
                    writer.WriteValue(vsCompletionItem.VsResolveTextEditOnCommit);
                }
            }

            var label = completionItem.Label;
            if (label != null)
            {
                writer.WritePropertyName("label");
                writer.WriteValue(label);
            }

            if (completionItem.LabelDetails != null)
            {
                writer.WritePropertyName("labelDetails");
                serializer.Serialize(writer, completionItem.LabelDetails);
            }

            writer.WritePropertyName("kind");
            writer.WriteValue(completionItem.Kind);

            if (completionItem.Detail != null)
            {
                writer.WritePropertyName("detail");
                writer.WriteValue(completionItem.Detail);
            }

            if (completionItem.Documentation != null)
            {
                writer.WritePropertyName("documentation");
                serializer.Serialize(writer, completionItem.Documentation);
            }

            // Only render preselect if it's "true"
            if (completionItem.Preselect)
            {
                writer.WritePropertyName("preselect");
                writer.WriteValue(completionItem.Preselect);
            }

            if (completionItem.SortText != null && !string.Equals(completionItem.SortText, label, StringComparison.Ordinal))
            {
                writer.WritePropertyName("sortText");
                writer.WriteValue(completionItem.SortText);
            }

            if (completionItem.FilterText != null && !string.Equals(completionItem.FilterText, label, StringComparison.Ordinal))
            {
                writer.WritePropertyName("filterText");
                writer.WriteValue(completionItem.FilterText);
            }

            if (completionItem.InsertText != null && !string.Equals(completionItem.InsertText, label, StringComparison.Ordinal))
            {
                writer.WritePropertyName("insertText");
                writer.WriteValue(completionItem.InsertText);
            }

            if (completionItem.InsertTextFormat != default && completionItem.InsertTextFormat != InsertTextFormat.Plaintext)
            {
                writer.WritePropertyName("insertTextFormat");
                writer.WriteValue(completionItem.InsertTextFormat);
            }

            if (completionItem.TextEdit != null)
            {
                writer.WritePropertyName("textEdit");
                serializer.Serialize(writer, completionItem.TextEdit);
            }

            if (completionItem.TextEditText != null)
            {
                writer.WritePropertyName("textEditText");
                serializer.Serialize(writer, completionItem.TextEditText);
            }

            if (completionItem.AdditionalTextEdits != null && completionItem.AdditionalTextEdits.Length > 0)
            {
                writer.WritePropertyName("additionalTextEdits");
                serializer.Serialize(writer, completionItem.AdditionalTextEdits);
            }

            if (completionItem.CommitCharacters != null && completionItem.CommitCharacters.Length > 0)
            {
                if (!itemRawJsonCache.TryGetValue(completionItem.CommitCharacters, out var jsonString))
                {
                    jsonString = JsonConvert.SerializeObject(completionItem.CommitCharacters);
                    itemRawJsonCache.Add(completionItem.CommitCharacters, jsonString);
                }

                writer.WritePropertyName("commitCharacters");
                writer.WriteRawValue(jsonString);
            }

            if (completionItem.Command != null)
            {
                writer.WritePropertyName("command");
                serializer.Serialize(writer, completionItem.Command);
            }

            if (completionItem.Data != null)
            {
                writer.WritePropertyName("data");
                serializer.Serialize(writer, completionItem.Data);
            }

            writer.WriteEndObject();
        }
    }
}
