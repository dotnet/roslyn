' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.InvertConditional
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.InvertConditional
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InvertConditional), [Shared]>
    Friend Class VisualBasicInvertConditionalCodeRefactoringProvider
        Inherits AbstractInvertConditionalCodeRefactoringProvider(Of TernaryConditionalExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function ShouldOffer(
            conditional As TernaryConditionalExpressionSyntax) As Boolean

            Return Not conditional.FirstCommaToken.IsMissing AndAlso
                   Not conditional.SecondCommaToken.IsMissing AndAlso
                   Not conditional.CloseParenToken.IsMissing
        End Function
    End Class
End Namespace
