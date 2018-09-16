' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.InvertConditional

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertConditional
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicInvertConditionalCodeRefactoringProvider
        Inherits AbstractInvertConditionalCodeRefactoringProvider(Of TernaryConditionalExpressionSyntax)

        Protected Overrides Function ShouldOffer(
            conditional As TernaryConditionalExpressionSyntax, position As Integer) As Boolean

            If position > conditional.FirstCommaToken.Span.Start Then
                Return False
            End If

            If conditional.FirstCommaToken.IsMissing OrElse
               conditional.SecondCommaToken.IsMissing OrElse
               conditional.CloseParenToken.IsMissing Then

                Return False
            End If

            Return True
        End Function
    End Class
End Namespace
