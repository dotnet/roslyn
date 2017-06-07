'Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.ValidateFormatString
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicValidateFormatStringDiagnosticAnalyzer
        Inherits AbstractValidateFormatStringDiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Protected Overrides Function GetInvocationExpressionSyntaxKind() As SyntaxKind
            Return SyntaxKind.InvocationExpression
        End Function

        Protected Overrides Function GetArgumentExpressionType(
                semanticModel As SemanticModel,
                argsArgument As SyntaxNode) As ITypeSymbol

            Dim argumentSyntax = DirectCast(argsArgument, ArgumentSyntax)
            Return semanticModel.GetTypeInfo(argumentSyntax.GetArgumentExpression()).Type
        End Function

        Protected Overrides Function GetMatchingNamedArgument(
                arguments As SeparatedSyntaxList(Of SyntaxNode),
                searchArgumentName As String) As SyntaxNode

            For Each argument In arguments.Cast(Of ArgumentSyntax)
                Dim simpleArgumentSyntax = DirectCast(argument, SimpleArgumentSyntax)
                If simpleArgumentSyntax.NameColonEquals?.Name.Identifier.ValueText.Equals(searchArgumentName) Then
                    Return argument
                End If
            Next

            Return Nothing
        End Function

        Protected Overrides Function ArgumentExpressionIsStringLiteral(syntaxNode As SyntaxNode) As Boolean
            Return DirectCast(syntaxNode, ArgumentSyntax).GetArgumentExpression().IsKind(
                SyntaxKind.StringLiteralExpression)
        End Function

        Protected Overrides Function GetArgumentExpression(syntaxNode As SyntaxNode) As SyntaxNode
            Return DirectCast(syntaxNode, ArgumentSyntax).GetArgumentExpression
        End Function
    End Class
End Namespace