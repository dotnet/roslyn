' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class SymbolInfoFactory
        Friend Shared Function Create(symbols As ImmutableArray(Of Symbol), resultKind As LookupResultKind) As SymbolInfo
            Return Create(StaticCast(Of ISymbol).From(symbols), resultKind)
        End Function

        Friend Shared Function Create(symbols As ImmutableArray(Of ISymbol), resultKind As LookupResultKind) As SymbolInfo
            Dim reason = If(resultKind = LookupResultKind.Good, CandidateReason.None, resultKind.ToCandidateReason())
            Return Create(symbols, reason)
        End Function

        Friend Shared Function Create(symbols As ImmutableArray(Of ISymbol), reason As CandidateReason) As SymbolInfo
            symbols = symbols.NullToEmpty()
            If symbols.IsEmpty AndAlso Not (reason = CandidateReason.None OrElse reason = CandidateReason.LateBound) Then
                reason = CandidateReason.None
            End If

            If symbols.Length = 1 AndAlso (reason = CandidateReason.None OrElse reason = CandidateReason.LateBound) Then
                Return New SymbolInfo(symbols(0), reason)
            Else
                Return New SymbolInfo(symbols, reason)
            End If
        End Function
    End Class

#If False Then
    Public Structure SymbolInfo
        Implements IEquatable(Of SymbolInfo)

        Private ReadOnly _symbols As ImmutableArray(Of Symbol)
        Private ReadOnly _resultKind As LookupResultKind

        Friend ReadOnly Property AllSymbols As ImmutableArray(Of Symbol)
            Get
                Return _symbols
            End Get
        End Property

        Friend ReadOnly Property ResultKind As LookupResultKind
            Get
                Return _resultKind
            End Get
        End Property

        Friend Shared None As SymbolInfo = New SymbolInfo(ImmutableArray(Of Symbol).Empty, LookupResultKind.Empty)
        Friend Shared NotNeeded As SymbolInfo = New SymbolInfo(ImmutableArray(Of Symbol).Empty, LookupResultKind.Good)

        ''' <summary>
        ''' The symbol that was referred to by the syntax node, if any. Returns null if the given
        ''' expression did not bind successfully to a single symbol. If null is returned, it may
        ''' still be that case that we have one or more "best guesses" as to what symbol was
        ''' intended. These best guesses are available via the CandidateSymbols property.
        ''' </summary>
        Public ReadOnly Property Symbol As Symbol
            Get
                If _resultKind = LookupResultKind.Good AndAlso _symbols.Length > 0 Then
                    Debug.Assert(_symbols.Length = 1)
                    Return _symbols(0)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ''' <summary>
        ''' If the expression did not successfully resolve to a symbol, but there were one or more
        ''' symbols that may have been considered but discarded, this property returns those
        ''' symbols. The reason that the symbols did not successfully resolve to a symbol are
        ''' available in the CandidateReason property. For example, if the symbol was inaccessible,
        ''' ambiguous, or used in the wrong context.
        ''' </summary>
        Public ReadOnly Property CandidateSymbols As ImmutableArray(Of Symbol)
            Get
                If _resultKind <> LookupResultKind.Good AndAlso Not _symbols.IsDefaultOrEmpty Then
                    Return _symbols
                Else
                    Return ImmutableArray(Of Symbol).Empty
                End If
            End Get
        End Property

        '''<summary>
        ''' If the expression did not successfully resolve to a symbol, but there were one or more
        ''' symbols that may have been considered but discarded, this property describes why those
        ''' symbol or symbols were not considered suitable.
        ''' </summary>
        Public ReadOnly Property CandidateReason As CandidateReason
            Get
                Return If(_resultKind = LookupResultKind.Good, CandidateReason.None, _resultKind.ToCandidateReason())
            End Get
        End Property

        Friend Sub New(symbols As ImmutableArray(Of Symbol), resultKind As LookupResultKind)
            Me._symbols = If(symbols.IsDefault, ImmutableArray(Of Symbol).Empty, symbols)
            Me._resultKind = resultKind

            If Not symbols.Any() AndAlso resultKind <> LookupResultKind.Good AndAlso resultKind <> LookupResultKind.LateBound Then
                Me._resultKind = LookupResultKind.Empty
            End If
        End Sub

        Public Overloads Function Equals(other As SymbolInfo) As Boolean Implements IEquatable(Of SymbolInfo).Equals
            Return _symbols.SequenceEqual(other._symbols) AndAlso
                _resultKind = other._resultKind
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is SymbolInfo AndAlso
                Equals(DirectCast(obj, SymbolInfo))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Hash.CombineValues(_symbols, 4), CInt(_resultKind))
        End Function

        Public Shared Widening Operator CType(info As SymbolInfo) As SymbolInfo
            Return New SymbolInfo(info.Symbol, StaticCast(Of ISymbol).From(info.CandidateSymbols), info.CandidateReason)
        End Operator
    End Structure
#End If
End Namespace
