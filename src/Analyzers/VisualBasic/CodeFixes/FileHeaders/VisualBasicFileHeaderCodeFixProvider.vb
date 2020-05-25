' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicFileHeaderCodeFixProvider))>
    <[Shared]>
    Friend Class VisualBasicFileHeaderCodeFixProvider
        Inherits AbstractFileHeaderCodeFixProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property FileHeaderHelper As AbstractFileHeaderHelper
            Get
                Return VisualBasicFileHeaderHelper.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property SyntaxKinds As ISyntaxKinds
            Get
                Return VisualBasicSyntaxKinds.Instance
            End Get
        End Property

        Protected Overrides Function EndOfLine(text As String) As SyntaxTrivia
            Return SyntaxFactory.EndOfLine(text)
        End Function
    End Class
End Namespace
