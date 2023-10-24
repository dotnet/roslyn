' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertConcatenationToInterpolatedString), [Shared]>
    Friend NotInheritable Class VisualBasicConvertConcatenationToInterpolatedStringRefactoringProvider
        Inherits AbstractConvertConcatenationToInterpolatedStringRefactoringProvider(Of ExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function SupportsInterpolatedStringHandler(compilation As Compilation) As Boolean
            ' VB does not support interpolated string handlers at all.
            Return False
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
