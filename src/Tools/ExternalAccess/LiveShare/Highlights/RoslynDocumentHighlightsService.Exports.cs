// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Highlights
{
    [ExportLanguageService(typeof(IDocumentHighlightsService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspDocumentHighlightsService : RoslynDocumentHighlightsService
    {
        [ImportingConstructor]
        public CSharpLspDocumentHighlightsService(CSharpLspClientServiceFactory csharpLspClientServiceFactory)
            : base(csharpLspClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(IDocumentHighlightsService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspDocumentHighlightsService : RoslynDocumentHighlightsService
    {
        [ImportingConstructor]
        public VBLspDocumentHighlightsService(VisualBasicLspClientServiceFactory vbLspClientServiceFactory)
            : base(vbLspClientServiceFactory)
        {
        }
    }
}
