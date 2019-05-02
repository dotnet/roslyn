' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentationComments

    <ExportLanguageService(GetType(IDocumentationCommentFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentationCommentFormattingService
        Inherits AbstractDocumentationCommentFormattingService

#Disable Warning RS0033 ' Importing constructor should be [Obsolete]
        <ImportingConstructor>
        Public Sub New()
#Enable Warning RS0033 ' Importing constructor should be [Obsolete]
        End Sub
    End Class
End Namespace
