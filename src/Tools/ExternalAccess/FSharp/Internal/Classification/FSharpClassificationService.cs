// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Classification
{
    [Shared]
    [ExportLanguageService(typeof(IClassificationService), LanguageNames.FSharp)]
    internal class FSharpClassificationService : IClassificationService
    {
        private readonly IFSharpClassificationService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpClassificationService(IFSharpClassificationService service)
        {
            _service = service;
        }

        public void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _service.AddLexicalClassifications(text, textSpan, result, cancellationToken);
        }

        public Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return _service.AddSemanticClassificationsAsync(document, textSpan, result, cancellationToken);
        }

        public Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            return _service.AddSyntacticClassificationsAsync(document, textSpan, result, cancellationToken);
        }

        public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return _service.AdjustStaleClassification(text, classifiedSpan);
        }
    }
}
