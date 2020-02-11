' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This is used while computing the values of constant symbols.  Since they can depend on each other,
    ''' we need to keep track of which ones we are currently computing in order to avoid (and report) cycles.
    ''' </summary>
    Friend NotInheritable Class SymbolsInProgress(Of T As Symbol)

        Private ReadOnly _symbols As ImmutableSetWithInsertionOrder(Of T)

        Friend Shared ReadOnly Empty As SymbolsInProgress(Of T) = New SymbolsInProgress(Of T)(ImmutableSetWithInsertionOrder(Of T).Empty)

        Private Sub New(fields As ImmutableSetWithInsertionOrder(Of T))
            Me._symbols = fields
        End Sub

        Friend Function Add(symbol As T) As SymbolsInProgress(Of T)
            Debug.Assert(symbol IsNot Nothing)

            Return New SymbolsInProgress(Of T)(Me._symbols.Add(symbol))
        End Function

        Friend Function Contains(symbol As T) As Boolean
            Return _symbols.Contains(symbol)
        End Function

        Friend Function GetStartOfCycleIfAny(symbol As T) As T
            If Not _symbols.Contains(symbol) Then
                Return Nothing
            End If

            ' Return the symbol from the cycle with the best error location.
            ' _symbols will contain all dependent symbols, potentially including
            ' symbols that are not part of the cycle. (For instance, when evaluating A
            ' in Enum E : A = B : B = C : C = B : End Enum, the set of symbols will be
            ' { A, B, C } although only { B, C } represent a cycle.) The loop below
            ' skips any symbols before the cycle (before the occurrence of 'symbol').
            Dim errorSymbol As T = Nothing
            For Each orderedSymbol In _symbols.InInsertionOrder
                If orderedSymbol = symbol Then
                    Debug.Assert(errorSymbol Is Nothing)
                    errorSymbol = orderedSymbol
                ElseIf errorSymbol IsNot Nothing Then
                    If IsBetterErrorLocation(errorSymbol, orderedSymbol) Then
                        errorSymbol = orderedSymbol
                    End If
                End If
            Next

            Debug.Assert(errorSymbol IsNot Nothing)
            Return errorSymbol
        End Function

        Private Shared Function IsBetterErrorLocation(errorSymbol As T, symbol As T) As Boolean
            ' Ignore locations from other compilations.
            Dim compilation = symbol.DeclaringCompilation
            Dim errorFieldCompilation = errorSymbol.DeclaringCompilation
            Return (compilation Is errorFieldCompilation) AndAlso (compilation.CompareSourceLocations(errorSymbol.Locations(0), symbol.Locations(0)) > 0)
        End Function

    End Class

End Namespace
