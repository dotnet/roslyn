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

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Structure FlowAnalysisInfo

        Public ReadOnly Compilation As VisualBasicCompilation

        Public ReadOnly Symbol As Symbol

        Public ReadOnly Node As BoundNode

        Public Sub New(_compilation As VisualBasicCompilation, _symbol As Symbol, _node As BoundNode)
            Debug.Assert(_compilation IsNot Nothing)
            Debug.Assert(_symbol IsNot Nothing)
            Debug.Assert(_node IsNot Nothing)

            Me.Compilation = _compilation
            Me.Symbol = _symbol
            Me.Node = _node
        End Sub

    End Structure

    Friend Structure FlowAnalysisRegionInfo

        ''' <summary> Region being analyzed: start node </summary>
        Public ReadOnly FirstInRegion As BoundNode

        ''' <summary> Region being analyzed: end node </summary>
        Public ReadOnly LastInRegion As BoundNode

        ''' <summary> Region itself </summary>
        Public ReadOnly Region As TextSpan

        Public Sub New(_firstInRegion As BoundNode, _lastInRegion As BoundNode, _region As TextSpan)
            Debug.Assert(_firstInRegion IsNot Nothing)
            Debug.Assert(_lastInRegion IsNot Nothing)

            Me.FirstInRegion = _firstInRegion
            Me.LastInRegion = _lastInRegion
            Me.Region = _region
        End Sub

    End Structure

End Namespace
