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

            Dim argumentSyntax = TryCast(argsArgument, ArgumentSyntax)
            Return semanticModel.GetTypeInfo(argumentSyntax.GetArgumentExpression()).Type
        End Function

        Protected Overrides Function GetMatchingNamedArgument(
                arguments As SeparatedSyntaxList(Of SyntaxNode),
                searchArgumentName As String) As SyntaxNode

            Dim matchingArgs = arguments.Cast(Of ArgumentSyntax).Where(
                Function(p)
                    Return Nullable.Equals(
                        TryCast(p,
                        SimpleArgumentSyntax)?.NameColonEquals?.Name.Identifier.ValueText.Equals(searchArgumentName),
                        True)
                End Function)

            If matchingArgs.Count <> 1 Then
                Return Nothing
            End If

            Return matchingArgs.Single()

        End Function

        Protected Overrides Function ArgumentExpressionIsStringLiteral(syntaxNode As SyntaxNode) As Boolean
            Return TryCast(syntaxNode, ArgumentSyntax).GetArgumentExpression().IsKind(
                SyntaxKind.StringLiteralExpression)
        End Function

        Protected Overrides Function GetArgumentExpression(syntaxNode As SyntaxNode) As SyntaxNode
            Return TryCast(syntaxNode, ArgumentSyntax).GetArgumentExpression
        End Function
    End Class
End Namespace