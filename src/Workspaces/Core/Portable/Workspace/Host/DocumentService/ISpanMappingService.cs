// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Map spans in a document to other spans even in other document
///
/// this will be used by various features if provided to convert span in one document to other spans.
/// 
/// for example, it is used to show spans users expect in a razor file rather than spans in 
/// auto generated file that is implementation detail or navigate to the right place rather 
/// than the generated file and etc.
/// </summary>
internal interface ISpanMappingService : IDocumentService
{
    /// <summary>
    /// Whether this span mapping service can handle mapping import directives added to a document.
    /// </summary>
    bool SupportsMappingImportDirectives { get; }

    Task<ImmutableArray<(string mappedFilePath, TextChange mappedTextChange)>> GetMappedTextChangesAsync(
        Document oldDocument,
        Document newDocument,
        CancellationToken cancellationToken);

    /// <summary>
    /// Map spans in the document to more appropriate locations
    /// 
    /// in current design, this can NOT map a span to a span that is not backed by a file.
    /// for example, roslyn supports someone to have a document that is not backed by a file. and current design doesn't allow
    /// such document to be returned from this API
    /// for example, span on razor secondary buffer document in roslyn solution mapped to a span on razor cshtml file is possible but
    /// a span on razor cshtml file to a span on secondary buffer document is not possible since secondary buffer document is not backed by a file
    /// </summary>
    /// <param name="document">Document given spans belong to</param>
    /// <param name="spans">Spans in the document</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Return mapped span. order of result should be same as the given span</returns>
    Task<ImmutableArray<MappedSpanResult>> MapSpansAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken);
}

/// <summary>
/// Result of span mapping
/// </summary>
internal readonly struct MappedSpanResult
{
    /// <summary>
    /// Path to mapped file
    /// </summary>
    public readonly string FilePath;

    /// <summary>
    /// LinePosition representation of the Span
    /// </summary>
    public readonly LinePositionSpan LinePositionSpan;

    /// <summary>
    /// Mapped span
    /// </summary>
    public readonly TextSpan Span;

    public MappedSpanResult(string filePath, LinePositionSpan linePositionSpan, TextSpan span)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new System.ArgumentException(nameof(filePath));
        }

        FilePath = filePath;
        LinePositionSpan = linePositionSpan;
        Span = span;
    }

    public bool IsDefault => FilePath == null;
}
