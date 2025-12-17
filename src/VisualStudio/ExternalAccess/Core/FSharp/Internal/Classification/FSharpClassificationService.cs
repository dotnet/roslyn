// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Classification;

[Shared]
[ExportLanguageService(typeof(IClassificationService), LanguageNames.FSharp)]
internal class FSharpClassificationService : IClassificationService
{
    private readonly IFSharpClassificationService _service;
    private readonly ObjectPool<List<ClassifiedSpan>> s_listPool = new(() => []);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpClassificationService(IFSharpClassificationService service)
    {
        _service = service;
    }

    public void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        using var _ = s_listPool.GetPooledObject(out var list);
        _service.AddLexicalClassifications(text, textSpan, list, cancellationToken);
        result.AddRange(list);
    }

    public async Task AddSemanticClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        foreach (var textSpan in textSpans)
        {
            using var _ = s_listPool.GetPooledObject(out var list);
            await _service.AddSemanticClassificationsAsync(document, textSpan, list, cancellationToken).ConfigureAwait(false);
            result.AddRange(list);
        }
    }

    public async Task AddSyntacticClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        foreach (var textSpan in textSpans)
        {
            using var _ = s_listPool.GetPooledObject(out var list);
            await _service.AddSyntacticClassificationsAsync(document, textSpan, list, cancellationToken).ConfigureAwait(false);
            result.AddRange(list);
        }
    }

    public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
    {
        return _service.AdjustStaleClassification(text, classifiedSpan);
    }

    public void AddSyntacticClassifications(SolutionServices services, SyntaxNode? root, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        // F# does not support syntax.
    }

    public TextChangeRange? ComputeSyntacticChangeRange(SolutionServices services, SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
    {
        // F# does not support syntax.
        return null;
    }

    public ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(Document oldDocument, Document newDocument, TimeSpan timeout, CancellationToken cancellationToken)
    {
        // not currently supported by F#.
        return new();
    }

    public Task AddEmbeddedLanguageClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
