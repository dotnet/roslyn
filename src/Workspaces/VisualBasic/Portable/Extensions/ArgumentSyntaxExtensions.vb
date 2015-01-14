' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module ArgumentSyntaxExtensions
        <Extension()>
        Public Function DetermineType(argument As ArgumentSyntax,
                                      semanticModel As SemanticModel,
                                      cancellationToken As CancellationToken) As ITypeSymbol
            ' If a parameter appears to have a void return type, then just use 'object' instead.
            Return argument.GetArgumentExpression().DetermineType(semanticModel, cancellationToken)
        End Function

        <Extension()>
        Public Function DetermineParameter(
            argument As ArgumentSyntax,
            semanticModel As SemanticModel,
            Optional allowParamArray As Boolean = False,
            Optional cancellationToken As CancellationToken = Nothing
        ) As IParameterSymbol

            Dim argumentList = TryCast(argument.Parent, ArgumentListSyntax)
            If argumentList Is Nothing Then
                Return Nothing
            End If

            Dim invocableExpression = TryCast(argumentList.Parent, ExpressionSyntax)
            If invocableExpression Is Nothing Then
                Return Nothing
            End If

            Dim symbol = semanticModel.GetSymbolInfo(invocableExpression, cancellationToken).Symbol
            If symbol Is Nothing Then
                Return Nothing
            End If

            Dim parameters = symbol.GetParameters()

            ' Handle named argument
            If argument.IsNamed Then
                Dim namedArgument = DirectCast(argument, SimpleArgumentSyntax)
                Dim name = namedArgument.NameColonEquals.Name.Identifier.ValueText
                Return parameters.FirstOrDefault(Function(p) p.Name = name)
            End If

            ' Handle positional argument
            Dim index = argumentList.Arguments.IndexOf(argument)
            If index < 0 Then
                Return Nothing
            End If

            If index < parameters.Length Then
                Return parameters(index)
            End If

            If allowParamArray Then
                Dim lastParameter = parameters.LastOrDefault()
                If lastParameter Is Nothing Then
                    Return Nothing
                End If

                If lastParameter.IsParams Then
                    Return lastParameter
                End If
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetArgumentExpression(argument As ArgumentSyntax) As ExpressionSyntax
            Return argument.GetExpression()
        End Function
    End Module
End Namespace
