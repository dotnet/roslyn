﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Highlights
{
    [ExportLanguageService(typeof(IDocumentHighlightsService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspDocumentHighlightsService : RoslynDocumentHighlightsService
    {
        [ImportingConstructor]
        public CSharpLspDocumentHighlightsService(RoslynLspClientServiceFactory roslynLspClientServiceFactory)
            : base(roslynLspClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(IDocumentHighlightsService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspDocumentHighlightsService : RoslynDocumentHighlightsService
    {
        [ImportingConstructor]
        public VBLspDocumentHighlightsService(RoslynLspClientServiceFactory roslynLspClientServiceFactory)
            : base(roslynLspClientServiceFactory)
        {
        }
    }
}
