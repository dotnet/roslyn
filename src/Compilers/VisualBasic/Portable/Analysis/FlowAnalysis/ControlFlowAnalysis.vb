' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' This class implements the region control flow analysis operations.  Region control flow analysis provides
    ''' information about statements which enter and leave a region. The analysis done lazily. When created, it performs
    ''' no analysis, but simply caches the arguments. Then, the first time one of the analysis results is used it
    ''' computes that one result and caches it. Each result is computed using a custom algorithm.
    ''' </summary>
    Friend Class VisualBasicControlFlowAnalysis
        Inherits ControlFlowAnalysis

        Friend Enum ReachableStates As Integer
            Unreachable = -1
            Unknown = 0
            Reachable = 1
        End Enum

        Friend Shared Function AsReachableState(state As Boolean) As ReachableStates
            Return If(state, ReachableStates.Reachable, ReachableStates.Unreachable)
        End Function

        Private ReadOnly _context As RegionAnalysisContext

        Private _entryPoints As ImmutableArray(Of SyntaxNode)
        Private _exitPoints As ImmutableArray(Of SyntaxNode)
        Private _regionIsReachable As (StartPoint As ReachableStates, EndPoint As ReachableStates)
        Private _returnStatements As ImmutableArray(Of SyntaxNode)
        Private _succeeded As Boolean?

        Friend Sub New(_context As RegionAnalysisContext)
            Me._context = _context
        End Sub

        ''' <summary>
        ''' A collection of statements outside the region that jump into the region.
        ''' </summary>
        Public Overrides ReadOnly Property EntryPoints As ImmutableArray(Of SyntaxNode)
            Get
                If _entryPoints.IsDefault Then
                    Me._succeeded = Not Me._context.Failed
                    Dim result = If(Me._context.Failed, ImmutableArray(Of SyntaxNode).Empty,
                                    DirectCast(EntryPointsWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo, _succeeded), IEnumerable(Of SyntaxNode)).ToImmutableArray())
                    ImmutableInterlocked.InterlockedCompareExchange(_entryPoints, result, Nothing)
                End If
                Return _entryPoints
            End Get
        End Property

        ''' <summary>
        ''' A collection of statements inside the region that jump to locations outside the region.
        ''' </summary>
        Public Overrides ReadOnly Property ExitPoints As ImmutableArray(Of SyntaxNode)
            Get
                If _exitPoints.IsDefault Then
                    Dim result = If(Me._context.Failed, ImmutableArray(Of SyntaxNode).Empty,
                                    DirectCast(ExitPointsWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo), IEnumerable(Of SyntaxNode)).ToImmutableArray())
                    ImmutableInterlocked.InterlockedCompareExchange(_exitPoints, result, Nothing)
                End If
                Return _exitPoints
            End Get
        End Property

        ''' <summary>
        ''' Returns true if and only if the last statement in the region can complete normally or the region contains no
        ''' statements.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property EndPointIsReachable As Boolean
            Get
                If _regionIsReachable.EndPoint = ReachableStates.Unknown Then
                    ComputeReachability()
                End If
                Return (_regionIsReachable.EndPoint = ReachableStates.Reachable)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property StartPointIsReachable As Boolean
            Get
                If _regionIsReachable.StartPoint = ReachableStates.Unknown Then
                    ComputeReachability()
                End If
                Return (_regionIsReachable.StartPoint = ReachableStates.Reachable)
            End Get
        End Property

        Private Sub ComputeReachability()
            Dim regionIsReachable = (StartPoint:=ReachableStates.Reachable, EndPoint:=ReachableStates.Reachable)
            If Not _context.Failed Then
                regionIsReachable = RegionReachableWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo)
            End If
            _regionIsReachable = regionIsReachable
        End Sub

        ''' <summary>
        ''' A collection of return, exit sub, exit function, exit operator and exit property statements found within the region that return to the enclosing method.
        ''' </summary>
        Public Overrides ReadOnly Property ReturnStatements As ImmutableArray(Of SyntaxNode)
            Get
                Return ExitPoints.WhereAsArray(Function(s As SyntaxNode) As Boolean
                                                   Return s.IsKind(SyntaxKind.ReturnStatement) Or
                                                s.IsKind(SyntaxKind.ExitSubStatement) Or
                                                s.IsKind(SyntaxKind.ExitFunctionStatement) Or
                                                s.IsKind(SyntaxKind.ExitOperatorStatement) Or
                                                s.IsKind(SyntaxKind.ExitPropertyStatement)
                                               End Function)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Succeeded As Boolean
            Get
                If Me._succeeded Is Nothing Then
                    Dim discarded = EntryPoints
                End If

                Return Me._succeeded.Value
            End Get
        End Property

    End Class

End Namespace
