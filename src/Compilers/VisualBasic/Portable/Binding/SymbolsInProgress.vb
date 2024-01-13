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
    End Class

End Namespace
