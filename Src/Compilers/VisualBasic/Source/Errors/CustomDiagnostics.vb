' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ' If a DiagnosticInfo contains symbols that should be returned through the binding API,
    ' it should implement this interface and return the symbols associated with the DiagnosticInfo
    ' via GetAssociatedSymbols
    Friend Interface IDiagnosticInfoWithSymbols
        ' Add the associated symbold to the given array builder.
        Sub GetAssociatedSymbols(builder As ArrayBuilder(Of Symbol))
    End Interface

    ''' <summary>
    ''' This diagnostic indicates when a symbol is not good for binding against.
    ''' 
    ''' Client code can obtain the bad symbol via the BadSymbol property.
    ''' </summary>
    <Serializable()>
    Friend NotInheritable Class BadSymbolDiagnostic
        Inherits DiagnosticInfo
        Implements IDiagnosticInfoWithSymbols

        ' not serialized
        Private m_badSymbol As Symbol

        ' Create a new bad symbol diagnostic with the given error id. This error message
        ' should have a single fill-in string, which is filled in with the symbol.
        Friend Sub New(badSymbol As Symbol, errid As ERRID)
            Me.New(badSymbol, errid, badSymbol)
        End Sub

        ' Create a new bad symbol diagnostic with the given error id and arguments. The symbols
        ' is not automatically filled in as an argument.
        Friend Sub New(badSymbol As Symbol, errid As ERRID, ParamArray additionalArgs As Object())
            MyBase.New(VisualBasic.MessageProvider.Instance, errid, additionalArgs)
            m_badSymbol = badSymbol
        End Sub

        Protected Sub New(info As SerializationInfo, context As StreamingContext)
            MyBase.New(info, context)

            ' symbol is not exposed publicly and thus not serialized
        End Sub

        Protected Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
            MyBase.GetObjectData(info, context)

            ' symbol is not exposed publicly and thus not serialized
        End Sub

        Public ReadOnly Property BadSymbol As Symbol
            Get
                Return m_badSymbol
            End Get
        End Property

        Private Sub GetAssociatedSymbols(builder As ArrayBuilder(Of Symbol)) Implements IDiagnosticInfoWithSymbols.GetAssociatedSymbols
            builder.Add(m_badSymbol)
        End Sub

        ' Get the locations of all the symbols.
        Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
            Get
                Return m_badSymbol.Locations
            End Get
        End Property

    End Class

    ''' <summary>
    ''' This diagnostics indicates when a lookup is ambiguous between multiple
    ''' equally good symbols, for example in different imported namespaces, or different
    ''' modules.
    ''' 
    ''' Client code can obtain the set of ambiguous symbols via the AmbiguousSymbols property.
    ''' </summary>
    <Serializable()>
    Friend NotInheritable Class AmbiguousSymbolDiagnostic
        Inherits DiagnosticInfo
        Implements IDiagnosticInfoWithSymbols

        ' not serialized:
        Private m_symbols As ImmutableArray(Of Symbol)

        ' Create a new ambiguous symbol diagnostic with the give error id and error arguments.
        Friend Sub New(errid As ERRID, symbols As ImmutableArray(Of Symbol), ParamArray args As Object())
            MyBase.New(VisualBasic.MessageProvider.Instance, errid, args)
            m_symbols = symbols
        End Sub

        Protected Sub New(info As SerializationInfo, context As StreamingContext)
            MyBase.New(info, context)

            ' symbols are not exposed publicly and thus not serialized
        End Sub

        Protected Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
            MyBase.GetObjectData(info, context)

            ' symbols are not exposed publicly and thus not serialized
        End Sub

        Public ReadOnly Property AmbiguousSymbols As ImmutableArray(Of Symbol)
            Get
                Return m_symbols
            End Get
        End Property

        Private Sub GetAssociatedSymbols(builder As ArrayBuilder(Of Symbol)) Implements IDiagnosticInfoWithSymbols.GetAssociatedSymbols
            builder.AddRange(m_symbols)
        End Sub

        ' Get the locations of all the symbols.
        Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
            Get
                Dim builder = ArrayBuilder(Of Location).GetInstance()
                For Each sym In m_symbols
                    For Each l In sym.Locations
                        builder.Add(l)
                    Next
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property
    End Class
End Namespace

