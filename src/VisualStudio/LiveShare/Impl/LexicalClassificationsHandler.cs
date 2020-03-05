// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal class LexicalClassificationsHandler : AbstractClassificationsHandler
    {
        internal const string LexicalClassificationsMethodName = "roslyn/lexicalClassifications";

        protected override async Task AddClassificationsAsync(IClassificationService classificationService, Document document, TextSpan textSpan, List<ClassifiedSpan> spans, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            classificationService.AddLexicalClassifications(text, textSpan, spans, cancellationToken);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, LexicalClassificationsMethodName)]
    internal class CSharpLexicalClassificationsHandler : LexicalClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, LexicalClassificationsMethodName)]
    internal class VisualBasicLexicalClassificationsHandler : LexicalClassificationsHandler
    {
    }
}
