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
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLanguageService(typeof(IClassificationService), InternalLanguageNames.TypeScript), Shared]
internal sealed class VSTypeScriptClassificationService : IClassificationService
{
    private readonly IVSTypeScriptClassificationService? _classificationService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptClassificationService(
        [Import(AllowDefault = true)] IVSTypeScriptClassificationService? classificationService)
    {
        _classificationService = classificationService;
    }

    public void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
    }

    public void AddSyntacticClassifications(SolutionServices services, SyntaxNode? root, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
    }

    public Task AddSyntacticClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AddEmbeddedLanguageClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
    {
        return classifiedSpan;
    }

    public ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(Document oldDocument, Document newDocument, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return default;
    }

    public TextChangeRange? ComputeSyntacticChangeRange(SolutionServices workspace, SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return null;
    }

    public async Task AddSemanticClassificationsAsync(Document document, ImmutableArray<TextSpan> textSpans, ClassificationOptions options, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
    {
        if (_classificationService is null)
            return;

        using var _ = SharedPools.BigDefault<List<ClassifiedSpan>>().GetPooledObject(out var list);

        await _classificationService.AddSemanticClassificationsAsync(document, textSpans, list, cancellationToken).ConfigureAwait(false);

        foreach (var classifiedSpan in list)
            result.Add(classifiedSpan);
    }
}
