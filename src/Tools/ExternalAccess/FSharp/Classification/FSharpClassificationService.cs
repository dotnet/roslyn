// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Classification
{
    [ExportLanguageService(typeof(IClassificationService), LanguageNames.FSharp), Shared]
    internal class FSharpClassificationService : IClassificationService
    {
        private readonly IFSharpClassificationService _delegatee;

        [ImportingConstructor]
        public FSharpClassificationService([Import(AllowDefault = true)]IFSharpClassificationService classificationService)
        {
            // no need to load IFSharpClassificationService service lazy since this IClassificationService will be loaded lazy
            _delegatee = classificationService ?? new NullService();
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _delegatee.AddLexicalClassifications(text, textSpan, result, cancellationToken);
        }

        public Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return _delegatee.AddSemanticClassificationsAsync(document, textSpan, result, cancellationToken);
        }

        public Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return _delegatee.AddSyntacticClassificationsAsync(document, textSpan, result, cancellationToken);
        }

        public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return _delegatee.AdjustStaleClassification(text, classifiedSpan);
        }

        private class NullService : IFSharpClassificationService
        {
            public void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken) { }
            public Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken) => Task.CompletedTask;
            public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan) => classifiedSpan;
        }
    }
}
