' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class implements the region data flow analysis operations.  Region data flow analysis provides information
    ''' how data flows into and out of a region.  The analysis is done lazily. When created, it performs no analysis, but
    ''' simply caches the arguments. Then, the first time one of the analysis results is used it computes that one
    ''' result and caches it. Each result is computed using a custom algorithm.
    ''' </summary>
    Friend Class VisualBasicDataFlowAnalysis
        Inherits DataFlowAnalysis

        Private ReadOnly _context As RegionAnalysisContext

        Private _variablesDeclared As IEnumerable(Of Symbol)
        Private _unassignedVariables As HashSet(Of Symbol)
        Private _dataFlowsIn As HashSet(Of Symbol)
        Private _dataFlowsOut As HashSet(Of Symbol)
        Private _alwaysAssigned As IEnumerable(Of Symbol)
        Private _readInside As IEnumerable(Of Symbol)
        Private _writtenInside As IEnumerable(Of Symbol)
        Private _readOutside As IEnumerable(Of Symbol)
        Private _writtenOutside As IEnumerable(Of Symbol)
        Private _captured As IEnumerable(Of Symbol)
        Private _succeeded As Boolean?
        Private _invalidRegionDetected As Boolean

        Friend Sub New(_context As RegionAnalysisContext)
            Me._context = _context
        End Sub

        ''' <summary>
        ''' A collection of the local variables that are declared within the region. Note that the region must be 
        ''' bounded by a method's body or a field's initializer, so parameter symbols are never included in the result.
        ''' </summary>
        Public Overrides ReadOnly Property VariablesDeclared As IEnumerable(Of ISymbol)
            Get
                If _variablesDeclared Is Nothing Then
                    Dim result = If(Me._context.Failed, Enumerable.Empty(Of Symbol)(),
                                    VariablesDeclaredWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo))
                    Interlocked.CompareExchange(_variablesDeclared, result, Nothing)
                End If
                Return _variablesDeclared
            End Get
        End Property

        Private ReadOnly Property UnassignedVariables As HashSet(Of Symbol)
            Get
                If _unassignedVariables Is Nothing Then
                    Dim result = If(Me._context.Failed, New HashSet(Of Symbol)(),
                                    UnassignedVariablesWalker.Analyze(_context.AnalysisInfo))
                    Interlocked.CompareExchange(_unassignedVariables, result, Nothing)
                End If
                Return _unassignedVariables
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables for which a value assigned outside the region may be used inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property DataFlowsIn As IEnumerable(Of ISymbol)
            Get
                If _dataFlowsIn Is Nothing Then
                    Me._succeeded = Not Me._context.Failed
                    Dim result = If(Me._context.Failed, New HashSet(Of Symbol)(),
                                    DataFlowsInWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo, UnassignedVariables, _succeeded, _invalidRegionDetected))
                    Interlocked.CompareExchange(_dataFlowsIn, result, Nothing)
                End If
                Return _dataFlowsIn
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables for which a value assigned inside the region may be used outside the region.
        ''' </summary>
        Public Overrides ReadOnly Property DataFlowsOut As IEnumerable(Of ISymbol)
            Get
                Dim discarded = DataFlowsIn
                If _dataFlowsOut Is Nothing Then
                    Dim result = If(Me._context.Failed, New HashSet(Of Symbol)(),
                                    DataFlowsOutWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo, UnassignedVariables, _dataFlowsIn))
                    Interlocked.CompareExchange(_dataFlowsOut, result, Nothing)
                End If
                Return _dataFlowsOut
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables for which a value is always assigned inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property AlwaysAssigned As IEnumerable(Of ISymbol)
            Get
                If _alwaysAssigned Is Nothing Then
                    Dim result = If(Me._context.Failed, Enumerable.Empty(Of Symbol)(),
                                    AlwaysAssignedWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo))
                    Interlocked.CompareExchange(_alwaysAssigned, result, Nothing)
                End If
                Return _alwaysAssigned
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables that are read inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property ReadInside As IEnumerable(Of ISymbol)
            Get
                If _readInside Is Nothing Then
                    AnalyzeReadWrite()
                End If
                Return _readInside
            End Get
        End Property

        ''' <summary>
        ''' A collection of local variables that are written inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property WrittenInside As IEnumerable(Of ISymbol)
            Get
                If _writtenInside Is Nothing Then
                    AnalyzeReadWrite()
                End If
                Return _writtenInside
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables that are read outside the region.
        ''' </summary>
        Public Overrides ReadOnly Property ReadOutside As IEnumerable(Of ISymbol)
            Get
                If _readOutside Is Nothing Then
                    AnalyzeReadWrite()
                End If
                Return _readOutside
            End Get
        End Property

        ''' <summary>
        ''' A collection of local variables that are written inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property WrittenOutside As IEnumerable(Of ISymbol)
            Get
                If _writtenOutside Is Nothing Then
                    AnalyzeReadWrite()
                End If
                Return _writtenOutside
            End Get
        End Property

        Private Sub AnalyzeReadWrite()
            Dim readInside As IEnumerable(Of Symbol) = Nothing
            Dim writtenInside As IEnumerable(Of Symbol) = Nothing
            Dim readOutside As IEnumerable(Of Symbol) = Nothing
            Dim writtenOutside As IEnumerable(Of Symbol) = Nothing
            Dim captured As IEnumerable(Of Symbol) = Nothing

            If Not Me.Succeeded Then
                readInside = Enumerable.Empty(Of Symbol)()
                writtenInside = readInside
                readOutside = readInside
                writtenOutside = readInside
                captured = readInside

            Else
                ReadWriteWalker.Analyze(
                    _context.AnalysisInfo, _context.RegionInfo,
                    readInside:=readInside,
                    writtenInside:=writtenInside,
                    readOutside:=readOutside,
                    writtenOutside:=writtenOutside,
                    captured:=captured)
            End If

            Interlocked.CompareExchange(Me._readInside, readInside, Nothing)
            Interlocked.CompareExchange(Me._writtenInside, writtenInside, Nothing)
            Interlocked.CompareExchange(Me._readOutside, readOutside, Nothing)
            Interlocked.CompareExchange(Me._writtenOutside, writtenOutside, Nothing)
            Interlocked.CompareExchange(Me._captured, captured, Nothing)
        End Sub

        ''' <summary>
        ''' A collection of the local variables that have been referenced in anonymous functions
        ''' and therefore must be moved to a field of a frame class.
        ''' </summary>
        Public Overrides ReadOnly Property Captured As IEnumerable(Of ISymbol)
            Get
                If Me._captured Is Nothing Then
                    AnalyzeReadWrite()
                End If

                Return Me._captured
            End Get
        End Property

        Friend ReadOnly Property InvalidRegionDetectedInternal As Boolean
            Get
                Return If(Me.Succeeded, False, Me._invalidRegionDetected)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Succeeded As Boolean
            Get
                If Me._succeeded Is Nothing Then
                    Dim discarded = DataFlowsIn
                End If

                Return Me._succeeded.Value
            End Get
        End Property

        Public Overrides ReadOnly Property UnsafeAddressTaken As IEnumerable(Of ISymbol)
            Get
                Return Enumerable.Empty(Of ISymbol)
            End Get
        End Property
    End Class

End Namespace
