// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Handler for a request to classify the document. This is used for semantic colorization and only works for C#\VB.
    /// TODO - Move once defined as a custom protocol.
    /// Note, this must return object instead of ClassificationSpan b/c liveshare uses dynamic to convert handler results.
    /// Unfortunately, ClassificationSpan is an internal type and cannot be defined in the external access layer.
    /// </summary>
    internal abstract class AbstractClassificationsHandler : ILspRequestHandler<ClassificationParams, object[], Solution>
    {
        protected abstract Task AddClassificationsAsync(IClassificationService classificationService, Document document, TextSpan textSpan, List<ClassifiedSpan> spans, CancellationToken cancellationToken);

        public async Task<object[]> HandleAsync(ClassificationParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var actualDocumentURI = requestContext.ProtocolConverter.FromProtocolUri(request.TextDocument.Uri);
            var document = requestContext.Context.GetDocumentFromURI(actualDocumentURI);
            var classificationService = document?.Project.LanguageServices.GetService<IClassificationService>();

            if (document == null || classificationService == null)
            {
                return Array.Empty<ClassificationSpan>();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(request.Range, text);

            var spans = new List<ClassifiedSpan>();
            await AddClassificationsAsync(classificationService, document, textSpan, spans, cancellationToken).ConfigureAwait(false);

            return spans.Select(c => new ClassificationSpan { Classification = c.ClassificationType, Range = ProtocolConversions.TextSpanToRange(c.TextSpan, text) }).ToArray();
        }
    }
}
