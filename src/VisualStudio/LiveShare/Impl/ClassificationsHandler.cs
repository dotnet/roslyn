// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    /// <summary>
    /// Handler for a request to classify the document. This is used for semantic colorization and only works for C#\VB.
    /// TODO - Move once defined as a custom protocol.
    /// Note, this must return object instead of ClassificationSpan b/c liveshare uses dynamic to convert handler results.
    /// Unfortunately, ClassificationSpan is an internal type and cannot be defined in the external access layer.
    /// </summary>
    internal class ClassificationsHandler : AbstractClassificationsHandler
    {
        public ClassificationsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        protected override async Task AddClassificationsAsync(IClassificationService classificationService, Document document, TextSpan textSpan, List<ClassifiedSpan> spans, CancellationToken cancellationToken)
            => await classificationService.AddSemanticClassificationsAsync(document, textSpan, spans, cancellationToken).ConfigureAwait(false);
    }

    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, RoslynMethods.ClassificationsName)]
    [Obsolete("Used for backwards compatibility with old liveshare clients.")]
    internal class RoslynClassificationsHandler : ClassificationsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynClassificationsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.CSharpContractName, RoslynMethods.ClassificationsName)]
    internal class CSharpClassificationsHandler : ClassificationsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpClassificationsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, RoslynMethods.ClassificationsName)]
    internal class VisualBasicClassificationsHandler : ClassificationsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicClassificationsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }
    }

    [ExportLspRequestHandler(LiveShareConstants.TypeScriptContractName, RoslynMethods.ClassificationsName)]
    internal class TypeScriptClassificationsHandler : ClassificationsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeScriptClassificationsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }
    }
}
