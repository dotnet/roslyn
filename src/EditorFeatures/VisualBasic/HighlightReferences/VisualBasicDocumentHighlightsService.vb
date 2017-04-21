' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.HighlightReferences
    <ExportLanguageService(GetType(IDocumentHighlightsService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDocumentHighlightsService
        Inherits AbstractDocumentHighlightsService

    End Class
End Namespace
