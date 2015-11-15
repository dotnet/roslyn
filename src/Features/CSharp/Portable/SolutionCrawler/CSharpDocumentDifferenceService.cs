// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.CSharp.SolutionCrawler
{
    [ExportLanguageService(typeof(IDocumentDifferenceService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentDifferenceService : AbstractDocumentDifferenceService
    {
    }
}
