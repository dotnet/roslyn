' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars

Namespace Microsoft.CodeAnalysis.VisualBasic.EmbeddedLanguages.VirtualChars
    <ExportLanguageService(GetType(IVirtualCharService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicVirtualCharService
        Inherits AbstractVirtualCharService

        Public Shared ReadOnly Instance As IVirtualCharService = New VisualBasicVirtualCharService()

        Protected Overrides Function IsStringLiteralToken(token As SyntaxToken) As Boolean
            Return token.Kind() = SyntaxKind.StringLiteralToken
        End Function

        Protected Overrides Function TryConvertToVirtualCharsWorker(token As SyntaxToken) As ImmutableArray(Of VirtualChar)
            Debug.Assert(Not token.ContainsDiagnostics)

            If token.Kind() = SyntaxKind.StringLiteralToken Then
                Return TryConvertSimpleDoubleQuoteString(token, """", """", escapeBraces:=False)
            ElseIf token.Kind() = SyntaxKind.InterpolatedStringTextToken Then
                Return TryConvertSimpleDoubleQuoteString(token, "", "", escapeBraces:=True)
            End If

            Return Nothing
        End Function
    End Class
End Namespace
