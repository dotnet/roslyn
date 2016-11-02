' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
Partial Friend Class VisualBasicConvertPlaceholderToInterpolatedStringRefactoringProvider
    Inherits AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider(Of InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax)

    Protected Overrides Function GetInterpolatedString(text As String) As SyntaxNode
        Return TryCast(SyntaxFactory.ParseExpression("$" + text), InterpolatedStringExpressionSyntax)
    End Function
End Class