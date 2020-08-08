' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

' NOTE: VB does not support constant expressions in flow analysis during command-line compilation, but supports them when 
'       analysis is being called via public API. This distinction is governed by 'suppressConstantExpressions' flag

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class AbstractFlowPass(Of LocalState As AbstractLocalState)
        Inherits BoundTreeVisitor

        ''' <summary> Start of the region being analyzed, or Nothing if it is not a region based analysis </summary>
        Protected ReadOnly _firstInRegion As BoundNode
        ''' <summary> End of the region being analyzed, or Nothing if it is not a region based analysis </summary>
        Protected ReadOnly _lastInRegion As BoundNode
        ''' <summary> Current region span, valid only for region based analysis </summary>
        Protected ReadOnly _region As TextSpan

        ' For region analysis, we maintain some extra data. 
        Protected Enum RegionPlace
            Before
            Inside
            After
        End Enum

        ''' <summary> Tells whether we are analyzing the position before, during, or after the region </summary>
        Protected _regionPlace As RegionPlace

        ''' <summary>
        ''' A cache of the state at the backward branch point of each loop.  This is not needed
        ''' during normal flow analysis, but is needed for region analysis.
        ''' </summary>
        Private ReadOnly _loopHeadState As Dictionary(Of BoundLoopStatement, LocalState)

        Protected ReadOnly Property IsInside As Boolean
            Get
                Return Me._regionPlace = RegionPlace.Inside
            End Get
        End Property

        ''' <summary> Checks if the text span passed is inside the region </summary>
        Protected Function IsInsideRegion(span As TextSpan) As Boolean
            ' Ensuring this method only being used in region analysis
            Debug.Assert(Me._firstInRegion IsNot Nothing)
            If span.Length = 0 Then
                Return Me._region.Contains(span.Start)
            End If
            Return Me._region.Contains(span)
        End Function

        ''' <summary>
        ''' Subclasses may override EnterRegion to perform any actions at the entry to the region.
        ''' </summary>
        Protected Overridable Sub EnterRegion()
            Debug.Assert(Me._regionPlace = RegionPlace.Before)
            Me._regionPlace = RegionPlace.Inside
        End Sub

        ''' <summary>
        ''' Subclasses may override LeaveRegion to perform any action at the end of the region.
        ''' </summary>
        Protected Overridable Sub LeaveRegion()
            Debug.Assert(IsInside)
            Me._regionPlace = RegionPlace.After
        End Sub

        ''' <summary>
        ''' If invalid region is dynamically detected this string contains text description of the reason.
        ''' 
        ''' Currently only the following case can cause the region to be invalidated:
        ''' 
        '''   - We have declaration of several variables using 'As New' having object
        '''     initializer with implicit receiver; if region included such a receiver,
        '''     it should include the whole declaration. Example:
        '''         Dim a, b As New Clazz(...) With { .X = [| .Y |] }
        ''' 
        '''   - Part of With statement expression which was not captured into locals and
        '''     was not evaluated during With statement body execution. Example:
        '''     initializer with implicit receiver; if region included such a receiver,
        '''     it should include the whole declaration. Example:
        '''         Dim sArray() As StructType = ...
        '''         With sArray([| 0 |])
        '''         End With
        ''' 
        ''' </summary>
        Private _invalidRegion As Boolean = False

        Protected ReadOnly Property InvalidRegionDetected As Boolean
            Get
                Return _invalidRegion
            End Get
        End Property

        Protected Sub SetInvalidRegion()
            _invalidRegion = True
        End Sub

    End Class

End Namespace
