// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Classification
{
    [Shared]
    [ExportLanguageService(typeof(IClassificationService), LanguageNames.FSharp)]
    internal class FSharpClassificationService : IClassificationService
    {
        private readonly IFSharpClassificationService _service;
        private readonly ObjectPool<List<ClassifiedSpan>> s_listPool = new(() => new());

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpClassificationService(IFSharpClassificationService service)
        {
            _service = service;
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            using var _ = s_listPool.GetPooledObject(out var list);
            _service.AddLexicalClassifications(text, textSpan, list, cancellationToken);
            result.AddRange(list);
        }

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            using var _ = s_listPool.GetPooledObject(out var list);
            await _service.AddSemanticClassificationsAsync(document, textSpan, list, cancellationToken).ConfigureAwait(false);
            result.AddRange(list);
        }

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            using var _ = s_listPool.GetPooledObject(out var list);
            await _service.AddSyntacticClassificationsAsync(document, textSpan, list, cancellationToken).ConfigureAwait(false);
            result.AddRange(list);
        }

        public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return _service.AdjustStaleClassification(text, classifiedSpan);
        }

        public void AddSyntacticClassifications(Workspace workspace, SyntaxNode root, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            // F# does not support syntax.
        }

        public TextChangeRange? ComputeSyntacticChangeRange(Workspace workspace, SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // F# does not support syntax.
            return null;
        }

        public ValueTask<TextChangeRange?> ComputeSyntacticChangeRangeAsync(Document oldDocument, Document newDocument, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // not currently supported by F#.
            return new();
        }

        public Task AddEmbeddedLanguageClassificationsAsync(Document document, TextSpan textSpan, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
