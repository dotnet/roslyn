﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            Compilation = _compilation
            Symbol = _symbol
            Node = _node
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

            FirstInRegion = _firstInRegion
            LastInRegion = _lastInRegion
            Region = _region
        End Sub

    End Structure

End Namespace
