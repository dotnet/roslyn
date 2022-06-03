' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    <ExportLanguageService(GetType(INewDocumentFormattingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicNewDocumentFormattingService
        Inherits AbstractNewDocumentFormattingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(<ImportMany> providers As IEnumerable(Of Lazy(Of INewDocumentFormattingProvider, LanguageMetadata)))
            MyBase.New(providers)
        End Sub

        Protected Overrides ReadOnly Property Language As String = LanguageNames.VisualBasic
    End Class
End Namespace
