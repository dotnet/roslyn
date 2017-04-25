// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.DocumentHighlighting
{
    [ExportLanguageService(typeof(IDocumentHighlightsService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentHighlightsService : AbstractDocumentHighlightsService
    {
    }
}