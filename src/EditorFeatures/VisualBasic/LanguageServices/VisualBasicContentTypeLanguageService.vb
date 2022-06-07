' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    <ExportContentTypeLanguageService(ContentTypeNames.VisualBasicContentType, LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicContentTypeLanguageService
        Implements IContentTypeLanguageService

        Private ReadOnly _contentTypeRegistry As IContentTypeRegistryService

        <ImportingConstructor()>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(contentTypeRegistry As IContentTypeRegistryService)
            Me._contentTypeRegistry = contentTypeRegistry
        End Sub

        Public Function GetDefaultContentType() As IContentType Implements IContentTypeLanguageService.GetDefaultContentType
            Return Me._contentTypeRegistry.GetContentType(ContentTypeNames.VisualBasicContentType)
        End Function
    End Class
End Namespace
