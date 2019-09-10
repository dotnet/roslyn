' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.NameTupleElement
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.NameTupleElement
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicNameTupleElementCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicNameTupleElementCodeRefactoringProvider
        Inherits AbstractNameTupleElementCodeRefactoringProvider(Of SimpleArgumentSyntax, TupleExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function WithName(argument As SimpleArgumentSyntax, name As String) As SimpleArgumentSyntax
            Return argument.WithNameColonEquals(SyntaxFactory.NameColonEquals(name.ToIdentifierName()))
        End Function

        Protected Overrides Function IsCloseParenOrComma(token As SyntaxToken) As Boolean
            Return token.IsKind(SyntaxKind.CloseParenToken, SyntaxKind.CommaToken)
        End Function
    End Class
End Namespace
