' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.DocumentHighlighting
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentHighlighting
    <ExportLanguageService(GetType(IDocumentHighlightsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentHighlightsService
        Inherits AbstractDocumentHighlightsService

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
