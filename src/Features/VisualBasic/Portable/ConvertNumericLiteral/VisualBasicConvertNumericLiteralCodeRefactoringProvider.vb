﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertNumericLiteral
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertNumericLiteral
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertNumericLiteralCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicConvertNumericLiteralCodeRefactoringProvider
        Inherits AbstractConvertNumericLiteralCodeRefactoringProvider(Of LiteralExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetNumericLiteralPrefixes() As (hexPrefix As String, binaryPrefix As String)
            Return (hexPrefix:="&H", binaryPrefix:="&B")
        End Function
    End Class
End Namespace
