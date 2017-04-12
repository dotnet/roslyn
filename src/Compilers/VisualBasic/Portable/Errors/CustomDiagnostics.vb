﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' If a DiagnosticInfo contains symbols that should be returned through the binding API,
    ' it should implement this interface and return the symbols associated with the DiagnosticInfo
    ' via GetAssociatedSymbols
    Friend Interface IDiagnosticInfoWithSymbols
        ' Add the associated symbols to the given array builder.
        Sub GetAssociatedSymbols(builder As ArrayBuilder(Of Symbol))
    End Interface

    ''' <summary>
    ''' This diagnostic indicates when a symbol is not good for binding against.
    ''' 
    ''' Client code can obtain the bad symbol via the BadSymbol property.
    ''' </summary>
    Friend NotInheritable Class BadSymbolDiagnostic
        Inherits DiagnosticInfo
        Implements IDiagnosticInfoWithSymbols

        ' not serialized
        Private ReadOnly _badSymbol As Symbol

        ' Create a new bad symbol diagnostic with the given error id. This error message
        ' should have a single fill-in string, which is filled in with the symbol.
        Friend Sub New(badSymbol As Symbol, errid As ERRID)
            Me.New(badSymbol, errid, badSymbol)
        End Sub

        ' Create a new bad symbol diagnostic with the given error id and arguments. The symbols
        ' is not automatically filled in as an argument.
        Friend Sub New(badSymbol As Symbol, errid As ERRID, ParamArray additionalArgs As Object())
            MyBase.New(VisualBasic.MessageProvider.Instance, errid, additionalArgs)
            _badSymbol = badSymbol
        End Sub

        Public ReadOnly Property BadSymbol As Symbol
            Get
                Return _badSymbol
            End Get
        End Property

        Private Sub GetAssociatedSymbols(builder As ArrayBuilder(Of Symbol)) Implements IDiagnosticInfoWithSymbols.GetAssociatedSymbols
            builder.Add(_badSymbol)
        End Sub

        ' Get the locations of all the symbols.
        Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
            Get
                Return _badSymbol.Locations
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim bsd = TryCast(obj, BadSymbolDiagnostic)
            If bsd IsNot Nothing Then
                Return Me._badSymbol = bsd._badSymbol AndAlso MyBase.Equals(obj)
            End If
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me._badSymbol, MyBase.GetHashCode())
        End Function
    End Class

    ''' <summary>
    ''' This diagnostics indicates when a lookup is ambiguous between multiple
    ''' equally good symbols, for example in different imported namespaces, or different
    ''' modules.
    ''' 
    ''' Client code can obtain the set of ambiguous symbols via the AmbiguousSymbols property.
    ''' </summary>
    Friend NotInheritable Class AmbiguousSymbolDiagnostic
        Inherits DiagnosticInfo
        Implements IDiagnosticInfoWithSymbols

        ' not serialized:
        Private _symbols As ImmutableArray(Of Symbol)

        ' Create a new ambiguous symbol diagnostic with the give error id and error arguments.
        Friend Sub New(errid As ERRID, symbols As ImmutableArray(Of Symbol), ParamArray args As Object())
            MyBase.New(VisualBasic.MessageProvider.Instance, errid, args)
            _symbols = symbols
        End Sub

        Public ReadOnly Property AmbiguousSymbols As ImmutableArray(Of Symbol)
            Get
                Return _symbols
            End Get
        End Property

        Private Sub GetAssociatedSymbols(builder As ArrayBuilder(Of Symbol)) Implements IDiagnosticInfoWithSymbols.GetAssociatedSymbols
            builder.AddRange(_symbols)
        End Sub

        ' Get the locations of all the symbols.
        Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
            Get
                Dim builder = ArrayBuilder(Of Location).GetInstance()
                For Each sym In _symbols
                    For Each l In sym.Locations
                        builder.Add(l)
                    Next
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim asd = TryCast(obj, AmbiguousSymbolDiagnostic)
            If asd IsNot Nothing Then
                Return _symbols.SequenceEqual(asd._symbols) AndAlso
                    MyBase.Equals(obj)
            End If
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Hash.CombineValues(Me._symbols), MyBase.GetHashCode())
        End Function
    End Class
End Namespace

