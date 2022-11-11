' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.ValidateFormatString
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicValidateFormatStringDiagnosticAnalyzer
        Inherits AbstractValidateFormatStringDiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Function GetSyntaxFacts() As ISyntaxFacts
            Return VisualBasicSyntaxFacts.Instance
        End Function

        Protected Overrides Function TryGetMatchingNamedArgument(
                arguments As SeparatedSyntaxList(Of SyntaxNode),
                searchArgumentName As String) As SyntaxNode

            For Each argument In arguments
                Dim simpleArgumentSyntax = TryCast(argument, SimpleArgumentSyntax)
                If Not simpleArgumentSyntax Is Nothing AndAlso simpleArgumentSyntax.NameColonEquals?.Name.Identifier.ValueText.Equals(searchArgumentName) Then
                    Return argument
                End If
            Next

            Return Nothing
        End Function

        Protected Overrides Function GetArgumentExpression(syntaxNode As SyntaxNode) As SyntaxNode
            Return DirectCast(syntaxNode, ArgumentSyntax).GetArgumentExpression
        End Function
    End Class
End Namespace
