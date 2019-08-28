' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
    Friend Class VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider
        Inherits AbstractConvertConcatenationToInterpolatedStringRefactoringProvider(Of ExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateInterpolatedStringStartToken(isVerbatim As Boolean) As SyntaxToken
            Return SyntaxFactory.Token(SyntaxKind.DollarSignDoubleQuoteToken)
        End Function

        Protected Overrides Function CreateInterpolatedStringEndToken() As SyntaxToken
            Return SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken)
        End Function

        Protected Overrides Function GetTextWithoutQuotes(text As String, isVerbatim As Boolean, isCharacterLiteral As Boolean) As String
            If isCharacterLiteral Then
                Return text.Substring("'".Length, text.Length - "''C".Length)
            Else
                Return text.Substring("'".Length, text.Length - "''".Length)
            End If
        End Function
    End Class
End Namespace
