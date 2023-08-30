' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
    Friend Class VisualBasicVirtualCharService
        Inherits AbstractVirtualCharService

        Public Shared ReadOnly Instance As IVirtualCharService = New VisualBasicVirtualCharService()

        Protected Sub New()
        End Sub

        Public Overrides Function TryGetEscapeCharacter(ch As VirtualChar, ByRef escapedChar As Char) As Boolean
            ' Not needed yet for VB.  Implement when there is an appropriate consumer that needs
            ' this.
            Throw New NotImplementedException()
        End Function

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides Function IsMultiLineRawStringToken(token As SyntaxToken) As Boolean
            Return False
        End Function

        Protected Overrides Function TryConvertToVirtualCharsWorker(token As SyntaxToken) As VirtualCharSequence
            Debug.Assert(Not token.ContainsDiagnostics)

            If token.Kind() = SyntaxKind.StringLiteralToken Then
                Return TryConvertSimpleDoubleQuoteString(token, """", """", escapeBraces:=False)
            End If

            If token.Kind() = SyntaxKind.InterpolatedStringTextToken Then
                Return TryConvertSimpleDoubleQuoteString(token, "", "", escapeBraces:=True)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
