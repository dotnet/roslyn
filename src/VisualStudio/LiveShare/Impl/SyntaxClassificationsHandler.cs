// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class SyntaxClassificationsHandler : AbstractClassificationsHandler
    {
        internal const string SyntaxClassificationsMethodName = "roslyn/syntaxClassifications";

        protected override async Task AddClassificationsAsync(IClassificationService classificationService, Document document, TextSpan textSpan, List<ClassifiedSpan> spans, CancellationToken cancellationToken)
            => await classificationService.AddSyntacticClassificationsAsync(document, textSpan, spans, cancellationToken).ConfigureAwait(false);
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, SyntaxClassificationsMethodName)]
    internal class CSharpSyntaxClassificationsHandler : SyntaxClassificationsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyntaxClassificationsHandler()
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, SyntaxClassificationsMethodName)]
    internal class VisualBasicSyntaxClassificationsHandler : SyntaxClassificationsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicSyntaxClassificationsHandler()
        {
        }
    }
}
