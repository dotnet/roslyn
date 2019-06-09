' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SolutionCrawler

Namespace Microsoft.CodeAnalysis.VisualBasic.SolutionCrawler
    <ExportLanguageService(GetType(IDocumentDifferenceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentDifferenceService
        Inherits AbstractDocumentDifferenceService

        <ImportingConstructor>
        Public Sub New()
        End Sub
    End Class
End Namespace
