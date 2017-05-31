﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal class SerializableDefinitionItem
    {
        public int SerializationId;

        public string[] Tags;
        public Dictionary<string, string> Properties;
        public SerializableTaggedText[] NameDisplayParts;
        public SerializableTaggedText[] DisplayParts;
        public SerializableTaggedText[] OriginationParts;
        public SerializableDocumentSpan[] SourceSpans;
        public bool DisplayIfNoReferences;

        internal static SerializableDefinitionItem Dehydrate(DefinitionItem definition, int serializationId)
        {
            return new SerializableDefinitionItem
            {
                SerializationId = serializationId,
                Tags = definition.Tags.NullToEmpty().ToArray(),
                Properties = definition.Properties?.Count > 0 ? definition.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) : null,
                NameDisplayParts = SerializableTaggedText.Dehydrate(definition.NameDisplayParts.NullToEmpty()),
                DisplayParts = SerializableTaggedText.Dehydrate(definition.DisplayParts.NullToEmpty()),
                OriginationParts = SerializableTaggedText.Dehydrate(definition.OriginationParts.NullToEmpty()),
                SourceSpans = SerializableDocumentSpan.Dehydrate(definition.SourceSpans.NullToEmpty()),
                DisplayIfNoReferences = definition.DisplayIfNoReferences,
            };
        }

        public DefinitionItem Rehydrate(Solution solution)
        {
            return DefinitionItem.Create(
                tags: Tags.ToImmutableArray(),
                displayParts: SerializableTaggedText.Rehydrate(DisplayParts),
                nameDisplayParts: SerializableTaggedText.Rehydrate(NameDisplayParts),
                originationParts: SerializableTaggedText.Rehydrate(OriginationParts),
                sourceSpans: SerializableDocumentSpan.Rehydrate(solution, SourceSpans),
                properties: Properties == null ? null : ImmutableDictionary.CreateRange(Properties),
                displayIfNoReferences: DisplayIfNoReferences);
        }
    }
}