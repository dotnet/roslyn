// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Tags;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal sealed class SearchExceptionDefinitionItem(string message, ImmutableArray<TaggedText> exceptionTypeName, ImmutableArray<TaggedText> stackTrace, DocumentSpan documentSpan)
    : DefinitionItem(
        tags:
        [
            WellKnownTags.Error
        ],
        displayParts:
        [
            .. exceptionTypeName,
            new TaggedText(TextTags.Punctuation, ":"),
            new TaggedText(TextTags.Space, " "),
            new TaggedText(TextTags.StringLiteral, message),
            new TaggedText(TextTags.Space, Environment.NewLine),
            .. stackTrace
        ],
        nameDisplayParts: exceptionTypeName,
        sourceSpans: [documentSpan],
        classifiedSpans: [],
        metadataLocations: [],
        properties: null,
        displayableProperties: [],
        displayIfNoReferences: true)
{
    internal override bool IsExternal => false;

    public override Task<INavigableLocation?> GetNavigableLocationAsync(Workspace workspace, CancellationToken cancellationToken)
        => Task.FromResult<INavigableLocation?>(null);
}

