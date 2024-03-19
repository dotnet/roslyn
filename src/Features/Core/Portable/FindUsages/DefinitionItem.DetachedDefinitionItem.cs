// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.FindUsages.DefinitionItem;

namespace Microsoft.CodeAnalysis.FindUsages;

[DataContract]
internal sealed class DetachedDefinitionItem(
    ImmutableArray<string> tags,
    ImmutableArray<TaggedText> displayParts,
    ImmutableArray<TaggedText> nameDisplayParts,
    ImmutableArray<DocumentIdSpan> sourceSpans,
    ImmutableArray<AssemblyLocation> metadataLocations,
    ImmutableDictionary<string, string> properties,
    ImmutableDictionary<string, string> displayableProperties,
    bool displayIfNoReferences) : IEquatable<DetachedDefinitionItem>
{
    [DataMember(Order = 0)]
    public readonly ImmutableArray<string> Tags = tags;
    [DataMember(Order = 1)]
    public readonly ImmutableArray<TaggedText> DisplayParts = displayParts;
    [DataMember(Order = 2)]
    public readonly ImmutableArray<TaggedText> NameDisplayParts = nameDisplayParts;
    [DataMember(Order = 3)]
    public readonly ImmutableArray<DocumentIdSpan> SourceSpans = sourceSpans;
    [DataMember(Order = 4)]
    public readonly ImmutableArray<AssemblyLocation> MetadataLocations = metadataLocations;
    [DataMember(Order = 5)]
    public readonly ImmutableDictionary<string, string> Properties = properties;
    [DataMember(Order = 6)]
    public readonly ImmutableDictionary<string, string> DisplayableProperties = displayableProperties;
    [DataMember(Order = 7)]
    public readonly bool DisplayIfNoReferences = displayIfNoReferences;

    private int _hashCode;

    public override bool Equals(object? obj)
        => Equals(obj as DetachedDefinitionItem);

    public bool Equals(DetachedDefinitionItem? other)
        => other != null &&
           this.DisplayIfNoReferences == other.DisplayIfNoReferences &&
           this.Tags.SequenceEqual(other.Tags) &&
           this.DisplayParts.SequenceEqual(other.DisplayParts) &&
           this.SourceSpans.SequenceEqual(other.SourceSpans) &&
           this.MetadataLocations.SequenceEqual(other.MetadataLocations) &&
           this.Properties.SetEquals(other.Properties) &&
           this.DisplayableProperties.SetEquals(other.DisplayableProperties);

    public override int GetHashCode()
    {
        if (_hashCode == 0)
        {
            // Combine enough to have a low chance of collision.
            var hash =
                Hash.Combine(this.DisplayIfNoReferences,
                Hash.CombineValues(this.Tags,
                Hash.CombineValues(this.DisplayParts)));

            _hashCode = hash == 0 ? 1 : hash;
        }

        return _hashCode;
    }

    public async Task<DefaultDefinitionItem?> TryRehydrateAsync(Solution solution, CancellationToken cancellationToken)
    {
        using var converted = TemporaryArray<DocumentSpan>.Empty;
        using var convertedClassifiedSpans = TemporaryArray<ClassifiedSpansAndHighlightSpan?>.Empty;
        foreach (var ss in SourceSpans)
        {
            var documentSpan = await ss.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
            if (documentSpan == null)
                return null;

            converted.Add(documentSpan.Value);

            // todo: consider serializing this data.
            convertedClassifiedSpans.Add(null);
        }

        return new DefaultDefinitionItem(
            Tags, DisplayParts, NameDisplayParts,
            converted.ToImmutableAndClear(), convertedClassifiedSpans.ToImmutableAndClear(),
            MetadataLocations,
            Properties, DisplayableProperties, DisplayIfNoReferences);
    }
}
