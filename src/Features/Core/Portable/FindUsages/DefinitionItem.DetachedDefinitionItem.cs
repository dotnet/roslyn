// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using static Microsoft.CodeAnalysis.FindUsages.DefinitionItem;

namespace Microsoft.CodeAnalysis.FindUsages
{
    [DataContract]
    internal readonly struct DetachedDefinitionItem
    {
        [DataMember(Order = 0)]
        public readonly ImmutableArray<string> Tags;
        [DataMember(Order = 1)]
        public readonly ImmutableArray<TaggedText> DisplayParts;
        [DataMember(Order = 2)]
        public readonly ImmutableArray<TaggedText> NameDisplayParts;
        [DataMember(Order = 3)]
        public readonly ImmutableArray<TaggedText> OriginationParts;
        [DataMember(Order = 4)]
        public readonly ImmutableArray<DocumentIdSpan> SourceSpans;
        [DataMember(Order = 5)]
        public readonly ImmutableDictionary<string, string> Properties;
        [DataMember(Order = 6)]
        public readonly ImmutableDictionary<string, string> DisplayableProperties;
        [DataMember(Order = 7)]
        public readonly bool DisplayIfNoReferences;

        public DetachedDefinitionItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<TaggedText> nameDisplayParts,
            ImmutableArray<TaggedText> originationParts,
            ImmutableArray<DocumentIdSpan> sourceSpans,
            ImmutableDictionary<string, string> properties,
            ImmutableDictionary<string, string> displayableProperties,
            bool displayIfNoReferences)
        {
            Tags = tags;
            DisplayParts = displayParts;
            NameDisplayParts = nameDisplayParts;
            OriginationParts = originationParts;
            Properties = properties;
            DisplayableProperties = displayableProperties;
            DisplayIfNoReferences = displayIfNoReferences;
            SourceSpans = sourceSpans;
        }

        public async Task<DefaultDefinitionItem?> TryRehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            using var converted = TemporaryArray<DocumentSpan>.Empty;
            foreach (var ss in SourceSpans)
            {
                var documentSpan = await ss.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (documentSpan == null)
                    return null;

                converted.Add(documentSpan.Value);
            }

            return new DefaultDefinitionItem(
                Tags, DisplayParts, NameDisplayParts, OriginationParts,
                converted.ToImmutableAndClear(),
                Properties, DisplayableProperties, DisplayIfNoReferences);
        }
    }
}
