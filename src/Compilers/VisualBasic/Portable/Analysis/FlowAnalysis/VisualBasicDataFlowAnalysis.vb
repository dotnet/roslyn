' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects

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

        Private _variablesDeclared As ImmutableArray(Of ISymbol)
        Private _unassignedVariables As HashSet(Of Symbol)
        Private _dataFlowsIn As ImmutableArray(Of ISymbol)
        Private _definitelyAssignedOnEntry As ImmutableArray(Of ISymbol)
        Private _definitelyAssignedOnExit As ImmutableArray(Of ISymbol)
        Private _dataFlowsOut As ImmutableArray(Of ISymbol)
        Private _alwaysAssigned As ImmutableArray(Of ISymbol)
        Private _readInside As ImmutableArray(Of ISymbol)
        Private _writtenInside As ImmutableArray(Of ISymbol)
        Private _readOutside As ImmutableArray(Of ISymbol)
        Private _writtenOutside As ImmutableArray(Of ISymbol)
        Private _captured As ImmutableArray(Of ISymbol)
        Private _capturedInside As ImmutableArray(Of ISymbol)
        Private _capturedOutside As ImmutableArray(Of ISymbol)
        Private _succeeded As Boolean?
        Private _invalidRegionDetected As Boolean

        Friend Sub New(_context As RegionAnalysisContext)
            Me._context = _context
        End Sub

        ''' <summary>
        ''' A collection of the local variables that are declared within the region. Note that the region must be 
        ''' bounded by a method's body or a field's initializer, so parameter symbols are never included in the result.
        ''' </summary>
        Public Overrides ReadOnly Property VariablesDeclared As ImmutableArray(Of ISymbol)
            Get
                If _variablesDeclared.IsDefault Then
                    Dim result = If(Me._context.Failed, ImmutableArray(Of ISymbol).Empty,
                                    Normalize(VariablesDeclaredWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo)))
                    ImmutableInterlocked.InterlockedCompareExchange(_variablesDeclared, result, Nothing)
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
        Public Overrides ReadOnly Property DataFlowsIn As ImmutableArray(Of ISymbol)
            Get
                If _dataFlowsIn.IsDefault Then
                    Me._succeeded = Not Me._context.Failed
                    Dim result = If(Me._context.Failed, ImmutableArray(Of ISymbol).Empty,
                                    Normalize(DataFlowsInWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo, UnassignedVariables, _succeeded, _invalidRegionDetected)))
                    ImmutableInterlocked.InterlockedCompareExchange(_dataFlowsIn, result, Nothing)
                End If
                Return _dataFlowsIn
            End Get
        End Property

        Public Overrides ReadOnly Property DefinitelyAssignedOnEntry As ImmutableArray(Of ISymbol)
            Get
                Return ComputeDefinitelyAssignedValues().onEntry
            End Get
        End Property

        Public Overrides ReadOnly Property DefinitelyAssignedOnExit As ImmutableArray(Of ISymbol)
            Get
                Return ComputeDefinitelyAssignedValues().onExit
            End Get
        End Property

        Private Function ComputeDefinitelyAssignedValues() As (onEntry As ImmutableArray(Of ISymbol), onExit As ImmutableArray(Of ISymbol))
            ' Check for _definitelyAssignedOnExit as that's the last thing we write to. If it's not
            ' Default, then we'll have written to both variables and can safely read from either of
            ' them.
            If _definitelyAssignedOnExit.IsDefault Then
                Dim entry = ImmutableArray(Of ISymbol).Empty
                Dim ex = ImmutableArray(Of ISymbol).Empty

                Dim discarded = DataFlowsIn
                If Not Me._context.Failed Then
                    Dim tuple = DefinitelyAssignedWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo)
                    entry = Normalize(tuple.entry)
                    ex = Normalize(tuple.ex)
                End If

                ImmutableInterlocked.InterlockedInitialize(_definitelyAssignedOnEntry, entry)
                ImmutableInterlocked.InterlockedInitialize(_definitelyAssignedOnExit, ex)
            End If

            Return (_definitelyAssignedOnEntry, _definitelyAssignedOnExit)
        End Function

        ''' <summary>
        ''' A collection of the local variables for which a value assigned inside the region may be used outside the region.
        ''' </summary>
        Public Overrides ReadOnly Property DataFlowsOut As ImmutableArray(Of ISymbol)
            Get
                Dim discarded = DataFlowsIn
                If _dataFlowsOut.IsDefault Then
                    Dim result = If(Me._context.Failed, ImmutableArray(Of ISymbol).Empty,
                                    Normalize(DataFlowsOutWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo, UnassignedVariables, _dataFlowsIn)))
                    ImmutableInterlocked.InterlockedCompareExchange(_dataFlowsOut, result, Nothing)
                End If
                Return _dataFlowsOut
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables for which a value is always assigned inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property AlwaysAssigned As ImmutableArray(Of ISymbol)
            Get
                If _alwaysAssigned.IsDefault Then
                    Dim result = If(Me._context.Failed, ImmutableArray(Of ISymbol).Empty,
                                    Normalize(AlwaysAssignedWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo)))
                    ImmutableInterlocked.InterlockedCompareExchange(_alwaysAssigned, result, Nothing)
                End If
                Return _alwaysAssigned
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables that are read inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property ReadInside As ImmutableArray(Of ISymbol)
            Get
                If _readInside.IsDefault Then
                    AnalyzeReadWrite()
                End If
                Return _readInside
            End Get
        End Property

        ''' <summary>
        ''' A collection of local variables that are written inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property WrittenInside As ImmutableArray(Of ISymbol)
            Get
                If _writtenInside.IsDefault Then
                    AnalyzeReadWrite()
                End If
                Return _writtenInside
            End Get
        End Property

        ''' <summary>
        ''' A collection of the local variables that are read outside the region.
        ''' </summary>
        Public Overrides ReadOnly Property ReadOutside As ImmutableArray(Of ISymbol)
            Get
                If _readOutside.IsDefault Then
                    AnalyzeReadWrite()
                End If
                Return _readOutside
            End Get
        End Property

        ''' <summary>
        ''' A collection of local variables that are written inside the region.
        ''' </summary>
        Public Overrides ReadOnly Property WrittenOutside As ImmutableArray(Of ISymbol)
            Get
                If _writtenOutside.IsDefault Then
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
            Dim capturedInside As IEnumerable(Of Symbol) = Nothing
            Dim capturedOutside As IEnumerable(Of Symbol) = Nothing

            If Not Me.Succeeded Then
                readInside = Enumerable.Empty(Of Symbol)()
                writtenInside = readInside
                readOutside = readInside
                writtenOutside = readInside
                captured = readInside
                capturedInside = readInside
                capturedOutside = readInside
            Else
                ReadWriteWalker.Analyze(
                    _context.AnalysisInfo, _context.RegionInfo,
                    readInside:=readInside,
                    writtenInside:=writtenInside,
                    readOutside:=readOutside,
                    writtenOutside:=writtenOutside,
                    captured:=captured,
                    capturedInside:=capturedInside,
                    capturedOutside:=capturedOutside)
            End If

            ImmutableInterlocked.InterlockedCompareExchange(Me._readInside, Normalize(readInside), Nothing)
            ImmutableInterlocked.InterlockedCompareExchange(Me._writtenInside, Normalize(writtenInside), Nothing)
            ImmutableInterlocked.InterlockedCompareExchange(Me._readOutside, Normalize(readOutside), Nothing)
            ImmutableInterlocked.InterlockedCompareExchange(Me._writtenOutside, Normalize(writtenOutside), Nothing)
            ImmutableInterlocked.InterlockedCompareExchange(Me._captured, Normalize(captured), Nothing)
            ImmutableInterlocked.InterlockedCompareExchange(Me._capturedInside, Normalize(capturedInside), Nothing)
            ImmutableInterlocked.InterlockedCompareExchange(Me._capturedOutside, Normalize(capturedOutside), Nothing)
        End Sub

        ''' <summary>
        ''' A collection of the local variables that have been referenced in anonymous functions
        ''' and therefore must be moved to a field of a frame class.
        ''' </summary>
        Public Overrides ReadOnly Property Captured As ImmutableArray(Of ISymbol)
            Get
                If Me._captured.IsDefault Then
                    AnalyzeReadWrite()
                End If

                Return Me._captured
            End Get
        End Property

        Public Overrides ReadOnly Property CapturedInside As ImmutableArray(Of ISymbol)
            Get
                If Me._capturedInside.IsDefault Then
                    AnalyzeReadWrite()
                End If

                Return Me._capturedInside
            End Get
        End Property

        Public Overrides ReadOnly Property CapturedOutside As ImmutableArray(Of ISymbol)
            Get
                If Me._capturedOutside.IsDefault Then
                    AnalyzeReadWrite()
                End If

                Return Me._capturedOutside
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

        Public Overrides ReadOnly Property UnsafeAddressTaken As ImmutableArray(Of ISymbol)
            Get
                Return ImmutableArray(Of ISymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property UsedLocalFunctions As ImmutableArray(Of IMethodSymbol)
            Get
                Return ImmutableArray(Of IMethodSymbol).Empty
            End Get
        End Property

        Friend Function Normalize(data As IEnumerable(Of Symbol)) As ImmutableArray(Of ISymbol)
            Dim builder = ArrayBuilder(Of Symbol).GetInstance()
            builder.AddRange(data.Where(Function(s) s.CanBeReferencedByName))
            builder.Sort(LexicalOrderSymbolComparer.Instance)
            Return builder.ToImmutableAndFree().As(Of ISymbol)()
        End Function
    End Class

End Namespace
