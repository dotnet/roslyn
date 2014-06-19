' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Summarizes the results of an overload resolution analysis. Describes whether overload resolution 
    ''' succeeded, and which method was selected if overload resolution succeeded.
    ''' </summary>
    Friend Class OverloadResolutionResult(Of TMember As Symbol)

        Private ReadOnly m_ValidResult As MemberResolutionResult(Of TMember) ?
        Private ReadOnly m_BestResult As MemberResolutionResult(Of TMember) ?
        Private m_Results As ImmutableArray(Of MemberResolutionResult(Of TMember))

        Friend Sub New(
            results As ImmutableArray(Of MemberResolutionResult(Of TMember)),
            validResult As MemberResolutionResult(Of TMember) ?,
            bestResult As MemberResolutionResult(Of TMember) ?
        )
            m_Results = results
            m_ValidResult = validResult
            m_BestResult = bestResult
        End Sub

        ''' <summary>
        ''' True if overload resolution successfully selected a single best method.
        ''' </summary>
        Public ReadOnly Property Succeeded As Boolean
            Get
                Return ValidResult.HasValue
            End Get
        End Property

        ''' <summary>
        ''' If overload resolution successfully selected a single best method, returns information
        ''' about that method. Otherwise returns Nothing.
        ''' </summary>
        Public ReadOnly Property ValidResult As MemberResolutionResult(Of TMember) ?
            Get
                Return m_ValidResult
            End Get
        End Property

        ''' <summary>
        ''' If there was a method that overload resolution considered better than all others,
        ''' returns information about that method. A method may be returned even if that method was
        ''' not considered a successful overload resolution, as long as it was better than any other
        ''' potential method considered.
        ''' </summary>
        Public ReadOnly Property BestResult As MemberResolutionResult(Of TMember) ?
            Get
                Return m_BestResult
            End Get
        End Property

        ''' <summary>
        ''' Returns information about each method that was considered during overload resolution,
        ''' and what the results of overload resolution were for that method.
        ''' </summary>
        Public ReadOnly Property Results As ImmutableArray(Of MemberResolutionResult(Of TMember))
            Get
                Return m_Results
            End Get
        End Property

        Friend Function ToCommon(Of TSymbol As ISymbol)() As CommonOverloadResolutionResult(Of TSymbol)
            Return New CommonOverloadResolutionResult(Of TSymbol)(
                Me.Succeeded,
                If(Me.ValidResult.HasValue, Me.ValidResult.Value.ToCommon(Of TSymbol)(), New CommonMemberResolutionResult(Of TSymbol) ?()),
                If(Me.BestResult.HasValue, Me.BestResult.Value.ToCommon(Of TSymbol)(), New CommonMemberResolutionResult(Of TSymbol) ?()),
                Me.Results.SelectAsArray(Function(r) r.ToCommon(Of TSymbol)()))
        End Function
    End Class
End Namespace