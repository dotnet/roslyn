'Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.ValidateFormatString
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicValidateFormatStringDiagnosticAnalyzer
        Inherits AbstractValidateFormatStringDiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Function GetSyntaxFactsService() As ISyntaxFactsService
            Return VisualBasicSyntaxFactsService.Instance
        End Function

        Friend Overrides Function TryGetArgsArgumentType(
            semanticModel As SemanticModel,
            arguments As SeparatedSyntaxList(Of SyntaxNode),
            parameters As ImmutableArray(Of IParameterSymbol),
            ByRef argsArgumentType As ITypeSymbol) As Boolean

            Dim argsArgument As ArgumentSyntax = Nothing
            If Not TryGetArgument(semanticModel, "args", arguments, parameters, argsArgument) Then
                argsArgumentType = Nothing
                Return False
            End If

            argsArgumentType = semanticModel.GetTypeInfo(argsArgument.GetArgumentExpression()).Type
            Return True
        End Function

        Protected Overrides Function TryGetFormatStringLiteralExpressionSyntax(
            semanticModel As SemanticModel,
            arguments As SeparatedSyntaxList(Of SyntaxNode),
            parameters As ImmutableArray(Of IParameterSymbol),
            ByRef formatStringLiteralExpressionSyntax As SyntaxNode) As Boolean

            formatStringLiteralExpressionSyntax = Nothing

            Dim formatArgumentSyntax As ArgumentSyntax = Nothing

            If Not TryGetArgument(semanticModel, "format", arguments, parameters, formatArgumentSyntax) Then
                Return False
            End If

            If Not formatArgumentSyntax.IsKind(SyntaxKind.SimpleArgument) Then
                Return False
            End If

            Dim simpleArguementSyntax = DirectCast(formatArgumentSyntax, SimpleArgumentSyntax)

            If simpleArguementSyntax.Expression Is Nothing Then
                Return False
            End If

            If Not simpleArguementSyntax.Expression.IsKind(SyntaxKind.StringLiteralExpression) Then
                Return False
            End If

            formatStringLiteralExpressionSyntax = DirectCast(simpleArguementSyntax.Expression,
                LiteralExpressionSyntax)

            Return True
        End Function

        Private Function TryGetArgument(
            semanticModel As SemanticModel,
            searchArgumentName As String,
            arguments As SeparatedSyntaxList(Of ArgumentSyntax),
            parameters As ImmutableArray(Of IParameterSymbol),
            ByRef argumentSyntax As ArgumentSyntax) As Boolean

            argumentSyntax = Nothing

            ' First, look for a named argument that matches
            Dim namedArgument = arguments.SingleOrDefault(
                Function(p)
                    Return Nullable.Equals(
                        TryCast(p, SimpleArgumentSyntax)?.NameColonEquals?.Name.Identifier.ValueText.Equals(searchArgumentName),
                        True)
                End Function)

            If namedArgument IsNot Nothing Then
                argumentSyntax = namedArgument
                Return True
            End If

            ' If no named argument exists, look for the named parameter and return the corresponding
            ' argument
            Dim namedParameter = parameters.SingleOrDefault(Function(p) p.Name.Equals(searchArgumentName))
            If namedParameter IsNot Nothing Then

                ' For the case string.Format("Test string"), there Is only one argument
                ' but the compiler created an empty parameter array to bind to an overload
                If namedParameter.Ordinal >= arguments.Count Then
                    Return False
                End If

                ' Multiple arguments could have been converted to a single params array, 
                ' so there wouldn't be a corresponding argument
                If namedParameter.IsParams AndAlso parameters.Length <> arguments.Count Then
                    Return False
                End If

                argumentSyntax = arguments(namedParameter.Ordinal)
                Return True
            End If

            Return False
        End Function

        Protected Overrides Function GetInvocationExpressionSyntaxKind() As SyntaxKind
            Return SyntaxKind.InvocationExpression
        End Function

        Protected Overrides Function GetLiteralExpressionSyntaxAsString(syntaxNode As SyntaxNode) As String
            Return TryCast(syntaxNode, LiteralExpressionSyntax).Token.Text
        End Function

        Protected Overrides Function GetLiteralExpressionSyntaxSpanStart(syntaxNode As SyntaxNode) As Integer
            Return TryCast(syntaxNode, LiteralExpressionSyntax).Token.SpanStart
        End Function

    End Class
End Namespace