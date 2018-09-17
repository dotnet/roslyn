' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.InvertConditional

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertConditional
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertConditional), [Shared]>
    Friend Class VisualBasicInvertConditionalCodeRefactoringProvider
        Inherits AbstractInvertConditionalCodeRefactoringProvider(Of TernaryConditionalExpressionSyntax)

        Protected Overrides Function ShouldOffer(
            conditional As TernaryConditionalExpressionSyntax, position As Integer) As Boolean

            Return position <= conditional.FirstCommaToken.Span.Start AndAlso
                   Not conditional.FirstCommaToken.IsMissing AndAlso
                   Not conditional.SecondCommaToken.IsMissing AndAlso
                   Not conditional.CloseParenToken.IsMissing
        End Function
    End Class
End Namespace
