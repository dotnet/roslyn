' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertToInterpolatedString
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertToInterpolatedString
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ExtractMethod), [Shared]>
    Partial Friend Class VisualBasicConvertPlaceholderToInterpolatedStringRefactoringProvider
        Inherits AbstractConvertPlaceholderToInterpolatedStringRefactoringProvider(Of InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax)

        Protected Overrides Function GetInterpolatedString(text As String) As SyntaxNode
            Return TryCast(SyntaxFactory.ParseExpression("$" + text), InterpolatedStringExpressionSyntax)
        End Function

        Protected Overrides Function GetArgumentName(argument As ArgumentSyntax) As String
            If argument Is Nothing Or Not argument.IsNamed Then
                Return Nothing
            End If

            Dim simpleArgumentSyntax As SimpleArgumentSyntax = CType(argument, SimpleArgumentSyntax)
            If simpleArgumentSyntax Is Nothing Then
                Return Nothing
            End If

            Return simpleArgumentSyntax.NameColonEquals.Name.Identifier.Text
        End Function
    End Class
End Namespace
