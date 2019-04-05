// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Classification
{
    [ExportLanguageServiceFactory(typeof(IClassificationService), LanguageNames.FSharp), Shared]
    internal class FSharpClassificationServiceFactory : ILanguageServiceFactory
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new ProxyService(languageServices.GetService<IEditorClassificationService>());
        }

        private class ProxyService : IClassificationService
        {
            private readonly IEditorClassificationService _delegatee;

            public ProxyService(IEditorClassificationService classificationService)
            {
                // connect to existing FSharp classification service that uses old obsolete service and 
                // export as new classification service.
                // this is a temporary until fsharp team get our new bits
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

            private class NullService : IEditorClassificationService
            {
                public void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken) { }
                public Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken) => Task.CompletedTask;
                public Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken) => Task.CompletedTask;
                public ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan) => classifiedSpan;
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
