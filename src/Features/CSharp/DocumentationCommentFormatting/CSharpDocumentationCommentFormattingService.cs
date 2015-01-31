// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationCommentFormatting
{
    [ExportLanguageService(typeof(IDocumentationCommentFormattingService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentationCommentFormattingService : AbstractDocumentationCommentFormattingService
    {
    }
}
