' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.FileHeaders
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicEditorFactoryIdString)>
    Friend Class VisualBasicEditorFactory
        Inherits AbstractEditorFactory

        Public Sub New(componentModel As IComponentModel)
            MyBase.New(componentModel)
        End Sub

        Protected Overrides ReadOnly Property ContentTypeName As String
            Get
                Return ContentTypeNames.VisualBasicContentType
            End Get
        End Property

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxGenerator As SyntaxGenerator
            Get
                Return VisualBasicSyntaxGenerator.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property FileHeaderHelper As AbstractFileHeaderHelper
            Get
                Return VisualBasicFileHeaderHelper.Instance
            End Get
        End Property
    End Class
End Namespace
