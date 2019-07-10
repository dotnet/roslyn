' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertNumericLiteral
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertNumericLiteral
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertNumericLiteralCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicConvertNumericLiteralCodeRefactoringProvider
        Inherits AbstractConvertNumericLiteralCodeRefactoringProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetNumericLiteralPrefixes() As (hexPrefix As String, binaryPrefix As String)
            Return (hexPrefix:="&H", binaryPrefix:="&B")
        End Function

        Friend Overrides Async Function GetNumericTokenAsync(ByVal document As Document, ByVal span As TextSpan, ByVal cancellationToken As CancellationToken) As Task(Of SyntaxToken)
            Dim expressionNode = Await GetNumericLiteralExpression(Of LiteralExpressionSyntax)(document, span, cancellationToken).ConfigureAwait(False)
            Return If(expressionNode IsNot Nothing, expressionNode.Token, New SyntaxToken())
        End Function
    End Class
End Namespace
