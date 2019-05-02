// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments
{
    [ExportLanguageService(typeof(IDocumentationCommentFormattingService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentationCommentFormattingService : AbstractDocumentationCommentFormattingService
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpDocumentationCommentFormattingService()
        {
        }
    }
}
