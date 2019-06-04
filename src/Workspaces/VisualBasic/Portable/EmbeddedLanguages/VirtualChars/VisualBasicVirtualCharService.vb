' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
    <ExportLanguageService(GetType(IVirtualCharService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicVirtualCharService
        Inherits AbstractVirtualCharService

        Public Shared ReadOnly Instance As IVirtualCharService = New VisualBasicVirtualCharService()

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function IsStringLiteralToken(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.StringLiteralToken
        End Function

        Protected Overrides Function TryConvertToVirtualCharsWorker(token As SyntaxToken) As VirtualCharSequence
            Debug.Assert(Not token.ContainsDiagnostics)

            If token.Kind() = SyntaxKind.StringLiteralToken Then
                Return TryConvertSimpleDoubleQuoteString(token, """", """", escapeBraces:=False)
            End If

            If token.Kind() = SyntaxKind.InterpolatedStringTextToken AndAlso
               TypeOf token.Parent.Parent Is InterpolatedStringExpressionSyntax Then
                Return TryConvertSimpleDoubleQuoteString(token, "", "", escapeBraces:=True)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
