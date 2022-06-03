// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface IClassificationService : ILanguageService
    {
        /// <summary>
        /// Produce the classifications for the span of text specified.  Classification should be
        /// performed as quickly as possible, and should process the text in a lexical fashion.
        /// This allows classification results to be shown to the user when a file is opened before
        /// any additional compiler information is available for the text.
        /// 
        /// Important: The classification should not consider the context the text exists in, and how
        /// that may affect the final classifications.  This may result in incorrect classification
        /// (i.e. identifiers being classified as keywords).  These incorrect results will be patched
        /// up when the lexical results are superseded by the calls to AddSyntacticClassifications.
        /// </summary>
        void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <inheritdoc cref="AddSyntacticClassificationsAsync"/>
        /// <remarks>This method is optional and only should be implemented by languages that support
        /// syntax.  If the language does not support syntax, callers should use
        /// <see cref="AddSyntacticClassificationsAsync"/> instead.</remarks>
        void AddSyntacticClassifications(Workspace workspace, SyntaxNode root, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Produce the classifications for the span of text specified.  The syntax of the document 
        /// can be accessed to provide more correct classifications.  For example, the syntax can
        /// be used to determine if a piece of text that looks like a keyword should actually be
        /// considered an identifier in its current context.
        /// </summary>
        Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Produce the classifications for the span of text specified.  Semantics of the language can be used to
        /// provide richer information for constructs where syntax is insufficient. For example, semantic information
        /// can be used to determine if an identifier should be classified as a type, structure, or something else
        /// entirely.
        /// </summary>
        /// <remarks>
        /// This will not include classifications for embedded language constructs in string literals.  For that use
        /// <see cref="AddEmbeddedLanguageClassificationsAsync"/>.
        /// </remarks>
        Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Produce the classifications for embedded language string literals (e.g. Regex/Json strings) in the span of
        /// text specified.
        /// </summary>
        Task AddEmbeddedLanguageClassificationsAsync(Document document, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Adjust a classification from a previous version of text accordingly based on the current
        /// text.  For example, if a piece of text was classified as an identifier in a previous version,
        /// but a character was added that would make it into a keyword, then indicate that here.
        ///
        /// This allows the classified to quickly fix up old classifications as the user types.  These
        /// adjustments are allowed to be incorrect as they will be superseded by calls to get the
        /// syntactic and semantic classifications for this version later.
        /// </summary>
        ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        /// <summary>
        /// Determines the range of the documents that should be considered syntactically changed after an edit.  In
        /// language systems that can reuse major parts of a document after an edit, and which would not need to
        /// recompute classifications for those reused parts, this can speed up processing on a host by not requiring
        /// the host to reclassify all the source in view, but only the source that could have changed.
        /// <para>
        /// If determining this is not possible, or potentially expensive, <see langword="null"/> can be returned to
        /// indicate that the entire document should be considered changed and should be syntactically reclassified.
        /// </para>
        /// <para>
        /// Implementations should attempt to abide by the provided timeout as much as they can, returning the best
        /// information available at that point.  As this can be called in performance critical scenarios, it is better
        /// to return quickly with potentially larger change span (including that of the full document) rather than
        /// spend too much time computing a very precise result.
        /// </para>
        /// </summary>
        ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(
            Document oldDocument, Document newDocument,
            TimeSpan timeout, CancellationToken cancellationToken);

        /// <inheritdoc cref="ComputeSyntacticChangeRangeAsync"/>
        /// <remarks>This method is optional and only should be implemented by languages that support
        /// syntax.  If the language does not support syntax, callers should use
        /// <see cref="ComputeSyntacticChangeRangeAsync"/> instead.</remarks>
        TextChangeRange? ComputeSyntacticChangeRange(
            Workspace workspace, SyntaxNode oldRoot, SyntaxNode newRoot,
            TimeSpan timeout, CancellationToken cancellationToken);
    }
}
