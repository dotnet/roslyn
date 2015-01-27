' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.DocumentationCommentFormatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentationCommentFormatting

    <ExportLanguageService(GetType(IDocumentationCommentFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentationCommentFormattingService
        Inherits AbstractDocumentationCommentFormattingService

    End Class
End Namespace
