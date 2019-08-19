// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
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
    internal class ClassificationsHandler : AbstractClassificationsHandler
    {
        protected override async Task AddClassificationsAsync(IClassificationService classificationService, Document document, TextSpan textSpan, List<ClassifiedSpan> spans, CancellationToken cancellationToken)
        {
            await classificationService.AddSemanticClassificationsAsync(document, textSpan, spans, cancellationToken).ConfigureAwait(false);
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ClassificationsName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynClassificationsHandler : ClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.ClassificationsName)]
    internal class CSharpClassificationsHandler : ClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.ClassificationsName)]
    internal class VisualBasicClassificationsHandler : ClassificationsHandler
    {
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.ClassificationsName)]
    internal class TypeScriptClassificationsHandler : ClassificationsHandler
    {
    }
}
