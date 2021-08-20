// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal partial class DefinitionItem
    {
        internal readonly struct DetachedDefinitionItem
        {
            public readonly ImmutableArray<string> Tags;
            public readonly ImmutableDictionary<string, string> Properties;
            public readonly ImmutableDictionary<string, string> DisplayableProperties;
            public readonly ImmutableArray<TaggedText> NameDisplayParts;
            public readonly ImmutableArray<TaggedText> DisplayParts;
            public readonly ImmutableArray<TaggedText> OriginationParts;
            public readonly bool DisplayIfNoReferences;

            public readonly ImmutableArray<DocumentIdSpan> SourceSpans;

            public DetachedDefinitionItem(
                ImmutableArray<string> tags,
                ImmutableArray<TaggedText> displayParts,
                ImmutableArray<TaggedText> nameDisplayParts,
                ImmutableArray<TaggedText> originationParts,
                ImmutableArray<DocumentSpan> sourceSpans,
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
                SourceSpans = sourceSpans.SelectAsArray(ss => new DocumentIdSpan(ss));
            }

            public async Task<DefaultDefinitionItem?> TryRehydrateAsync(CancellationToken cancellationToken)
            {
                using var converted = TemporaryArray<DocumentSpan>.Empty;
                foreach (var ss in SourceSpans)
                {
                    var documentSpan = await ss.TryRehydrateAsync(cancellationToken).ConfigureAwait(false);
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
}
