// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// excerpt some part of <see cref="Document"/>
    /// </summary>
    internal interface IDocumentExcerptService : IDocumentService
    {
        /// <summary>
        /// return <see cref="ExcerptResult"/> of given <see cref="Document"/> and <see cref="TextSpan"/>
        /// 
        /// the result might not be an exact copy of the given source or contains more then given span
        /// </summary>
        Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, ClassificationOptions classificationOptions, CancellationToken cancellationToken);
    }

    /// <summary>
    /// this mode shows intention not actual behavior. it is up to implementation how to interpret the intention.
    /// </summary>
    internal enum ExcerptMode
    {
        SingleLine,
        Tooltip
    }

    /// <summary>
    /// Result of excerpt
    /// </summary>
    internal readonly struct ExcerptResult(SourceText content, TextSpan mappedSpan, ImmutableArray<ClassifiedSpan> classifiedSpans, Document document, TextSpan span)
    {
        /// <summary>
        /// excerpt content
        /// </summary>
        public readonly SourceText Content = content;

        /// <summary>
        /// span on <see cref="Content"/> that given <see cref="Span"/> got mapped to
        /// </summary>
        public readonly TextSpan MappedSpan = mappedSpan;

        /// <summary>
        /// classification information on the <see cref="Content"/>
        /// </summary>
        public readonly ImmutableArray<ClassifiedSpan> ClassifiedSpans = classifiedSpans;

        /// <summary>
        /// <see cref="Document"/> this excerpt is from
        /// 
        /// should be same document in <see cref="IDocumentExcerptService.TryExcerptAsync(Document, TextSpan, ExcerptMode, ClassificationOptions, CancellationToken)" />
        /// </summary>
        public readonly Document Document = document;

        /// <summary>
        /// span on <see cref="Document"/> this excerpt is from
        /// 
        /// should be same text span in <see cref="IDocumentExcerptService.TryExcerptAsync(Document, TextSpan, ExcerptMode, ClassificationOptions, CancellationToken)" />
        /// </summary>
        public readonly TextSpan Span = span;
    }
}
