// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal abstract class AbstractSpanMappingService : ISpanMappingService
{
    public abstract bool SupportsMappingImportDirectives { get; }

    public abstract Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
        Document oldDocument,
        Document newDocument,
        CancellationToken cancellationToken);

    public abstract Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(
        Document document,
        IEnumerable<TextSpan> spans,
        CancellationToken cancellationToken);

    protected static ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)> MatchMappedSpansToTextChanges(
        ImmutableArray<TextChange> textChanges,
        ImmutableArray<MappedSpanResult> mappedSpanResults)
    {
        Contract.ThrowIfFalse(mappedSpanResults.Length == textChanges.Length);

        using var _ = ArrayBuilder<(string, TextChange)>.GetInstance(out var mappedFilePathAndTextChange);
        for (var i = 0; i < mappedSpanResults.Length; i++)
        {
            // Only include changes that could be mapped.
            var newText = textChanges[i].NewText;
            if (!mappedSpanResults[i].IsDefault && newText != null)
            {
                var newTextChange = new TextChange(mappedSpanResults[i].Span, newText);
                mappedFilePathAndTextChange.Add((mappedSpanResults[i].FilePath, newTextChange));
            }
        }

        return mappedFilePathAndTextChange.ToImmutableAndClear();
    }
}
