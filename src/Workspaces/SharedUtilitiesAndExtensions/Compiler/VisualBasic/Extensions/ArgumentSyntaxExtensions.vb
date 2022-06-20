' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
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
            Optional allowUncertainCandidates As Boolean = False,
            Optional allowParamArray As Boolean = False,
            Optional cancellationToken As CancellationToken = Nothing
        ) As IParameterSymbol

            Dim argumentList = TryCast(argument.Parent, ArgumentListSyntax)
            If argumentList Is Nothing Then
                Return Nothing
            End If

            ' Get the symbol if it is not Nothing or if there is a singular candidate symbol
            Dim symbolInfo = semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken)
            Dim symbols = symbolInfo.GetBestOrAllSymbols()

            If symbols.Length >= 2 AndAlso Not allowUncertainCandidates Then
                Return Nothing
            End If

            For Each symbol In symbols
                Dim parameters = symbol.GetParameters()

                ' Handle named argument
                If argument.IsNamed Then
                    Dim namedArgument = DirectCast(argument, SimpleArgumentSyntax)
                    Dim name = namedArgument.NameColonEquals.Name.Identifier.ValueText
                    Dim parameter = parameters.FirstOrDefault(Function(p) p.Name = name)
                    If parameter IsNot Nothing Then
                        Return parameter
                    End If

                    Continue For
                End If

                ' Handle positional argument
                Dim index = argumentList.Arguments.IndexOf(argument)
                If index < 0 Then
                    Continue For
                End If

                If index < parameters.Length Then
                    Return parameters(index)
                End If

                If allowParamArray Then
                    Dim lastParameter = parameters.LastOrDefault()
                    If lastParameter Is Nothing Then
                        Continue For
                    End If

                    If lastParameter.IsParams Then
                        Return lastParameter
                    End If
                End If
            Next

            Return Nothing
        End Function

        <Extension()>
        Public Function GetArgumentExpression(argument As ArgumentSyntax) As ExpressionSyntax
            Return argument.GetExpression()
        End Function
    End Module
End Namespace
