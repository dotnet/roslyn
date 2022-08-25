' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private ReadOnly _context As RegionAnalysisContext

        Private _entryPoints As ImmutableArray(Of SyntaxNode)
        Private _exitPoints As ImmutableArray(Of SyntaxNode)
        Private _regionStartPointIsReachable As Object
        Private _regionEndPointIsReachable As Object
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
                If _regionStartPointIsReachable Is Nothing Then
                    ComputeReachability()
                End If
                Return DirectCast(_regionEndPointIsReachable, Boolean)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property StartPointIsReachable As Boolean
            Get
                If _regionStartPointIsReachable Is Nothing Then
                    ComputeReachability()
                End If
                Return DirectCast(_regionStartPointIsReachable, Boolean)
            End Get
        End Property

        Private Sub ComputeReachability()
            Dim startPointIsReachable As Boolean = False
            Dim endPointIsReachable As Boolean = False

            If Me._context.Failed Then
                startPointIsReachable = True
                endPointIsReachable = True
            Else
                RegionReachableWalker.Analyze(_context.AnalysisInfo, _context.RegionInfo, startPointIsReachable, endPointIsReachable)
            End If

            Interlocked.CompareExchange(_regionStartPointIsReachable, startPointIsReachable, Nothing)
            Interlocked.CompareExchange(_regionEndPointIsReachable, endPointIsReachable, Nothing)
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
