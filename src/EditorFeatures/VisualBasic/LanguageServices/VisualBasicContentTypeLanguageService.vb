' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    <ExportContentTypeLanguageService(ContentTypeNames.VisualBasicContentType, LanguageNames.VisualBasic), [Shared]>
    Class VisualBasicContentTypeLanguageService
        Implements IContentTypeLanguageService

        Dim contentTypeRegistry As IContentTypeRegistryService

        <ImportingConstructor()>
        Public Sub New(contentTypeRegistry As IContentTypeRegistryService)
            Me.contentTypeRegistry = contentTypeRegistry
        End Sub

        Public Function GetDefaultContentType() As IContentType Implements IContentTypeLanguageService.GetDefaultContentType
            Return Me.contentTypeRegistry.GetContentType(ContentTypeNames.VisualBasicContentType)
        End Function
    End Class
End Namespace
