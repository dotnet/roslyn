// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
{
    /// <summary>
    /// Creates a classification service for a liveshare session.
    /// This service calls into the <see cref="ISyntaxClassificationService"/> to handle semantic/syntactic classifications, but does not for lexical.
    /// So for the liveshare case, call into the <see cref="RoslynSyntaxClassificationService"/> to handle lexical classifications.
    /// Otherwise forward to the original <see cref="IClassificationService"/> which will call into <see cref="ISyntaxClassificationService"/>
    /// </summary>
    internal class RoslynClassificationService : IClassificationService
    {
        private readonly IClassificationService _originalService;
        private readonly ISyntaxClassificationService _liveshareSyntaxClassificationService;

        public RoslynClassificationService(IClassificationService originalService, ISyntaxClassificationService liveshareSyntaxClassificationService)
        {
            _originalService = originalService ?? throw new ArgumentNullException(nameof(originalService));
            _liveshareSyntaxClassificationService = liveshareSyntaxClassificationService ?? throw new ArgumentNullException(nameof(liveshareSyntaxClassificationService));
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var liveshareResults = new ArrayBuilder<ClassifiedSpan>();
            _liveshareSyntaxClassificationService.AddLexicalClassifications(text, textSpan, liveshareResults, cancellationToken);
            result.AddRange(liveshareResults);
            liveshareResults.Free();
        }

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            await _originalService.AddSemanticClassificationsAsync(document, textSpan, result, cancellationToken).ConfigureAwait(false);
        }

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            await _originalService.AddSyntacticClassificationsAsync(document, textSpan, result, cancellationToken).ConfigureAwait(false);
        }

        public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return _originalService.AdjustStaleClassification(text, classifiedSpan);
        }
    }
}
