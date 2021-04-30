' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities.Completion
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.ArgumentProviders
    Public MustInherit Class AbstractVisualBasicArgumentProviderTests
        Inherits AbstractArgumentProviderTests(Of VisualBasicTestWorkspaceFixture)

        Protected Overrides Function GetParameterSymbolInfo(semanticModel As SemanticModel, root As SyntaxNode, position As Integer, cancellationToken As CancellationToken) As IParameterSymbol

            Dim token = root.FindToken(position)
            Dim argumentList = token.GetRequiredParent().GetAncestorsOrThis(Of ArgumentListSyntax)().First()
            Dim symbols = semanticModel.GetSymbolInfo(argumentList.GetRequiredParent(), cancellationToken).GetAllSymbols()

            ' if more than one symbol is found, filter to only include symbols with a matching number of arguments
            If symbols.Length > 1 Then
                symbols = symbols.WhereAsArray(
                    Function(symbol1)
                        Dim parameters1 = symbol1.GetParameters()
                        If argumentList.Arguments.Count < GetMinimumArgumentCount(parameters1) Then
                            Return False
                        End If

                        If argumentList.Arguments.Count > GetMaximumArgumentCount(parameters1) Then
                            Return False
                        End If

                        Return True
                    End Function)
            End If

            Dim symbol = symbols.Single()
            Dim parameters = symbol.GetParameters()

            Contract.ThrowIfTrue(argumentList.Arguments.Any(Function(argument) argument.IsNamed), "Named arguments are not currently supported by this test.")
            Contract.ThrowIfTrue(parameters.Any(Function(parameter) parameter.IsParams), "'params' parameters are not currently supported by this test.")

            Dim index = If(argumentList.Arguments.Any(),
                argumentList.Arguments.IndexOf(argumentList.Arguments.Single(Function(argument) argument.FullSpan.Start <= position AndAlso argument.FullSpan.End >= position)),
                0)

            Return parameters(index)
        End Function

        Private Shared Function GetMinimumArgumentCount(parameters As ImmutableArray(Of IParameterSymbol)) As Integer
            Return parameters.Count(Function(parameter) Not parameter.IsOptional AndAlso Not parameter.IsParams)
        End Function

        Private Shared Function GetMaximumArgumentCount(parameters As ImmutableArray(Of IParameterSymbol)) As Integer
            If parameters.Any(Function(parameter) parameter.IsParams) Then
                Return Integer.MaxValue
            End If

            Return parameters.Length
        End Function
    End Class
End Namespace
