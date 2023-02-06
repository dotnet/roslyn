' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private ReadOnly _validResult As MemberResolutionResult(Of TMember)?
        Private ReadOnly _bestResult As MemberResolutionResult(Of TMember)?
        Private ReadOnly _results As ImmutableArray(Of MemberResolutionResult(Of TMember))

        Friend Sub New(
            results As ImmutableArray(Of MemberResolutionResult(Of TMember)),
            validResult As MemberResolutionResult(Of TMember)?,
            bestResult As MemberResolutionResult(Of TMember)?
        )
            _results = results
            _validResult = validResult
            _bestResult = bestResult
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
        Public ReadOnly Property ValidResult As MemberResolutionResult(Of TMember)?
            Get
                Return _validResult
            End Get
        End Property

        ''' <summary>
        ''' If there was a method that overload resolution considered better than all others,
        ''' returns information about that method. A method may be returned even if that method was
        ''' not considered a successful overload resolution, as long as it was better than any other
        ''' potential method considered.
        ''' </summary>
        Public ReadOnly Property BestResult As MemberResolutionResult(Of TMember)?
            Get
                Return _bestResult
            End Get
        End Property

        ''' <summary>
        ''' Returns information about each method that was considered during overload resolution,
        ''' and what the results of overload resolution were for that method.
        ''' </summary>
        Public ReadOnly Property Results As ImmutableArray(Of MemberResolutionResult(Of TMember))
            Get
                Return _results
            End Get
        End Property
    End Class
End Namespace
