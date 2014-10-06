' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Represents analysis context attributes such as compilation, region, etc...
    ''' </summary>
    Friend Structure RegionAnalysisContext

        ''' <summary> Current compilation </summary>
        Private ReadOnly compilation As VBCompilation

        ''' <summary> Method, field or property symbol </summary>
        Private ReadOnly symbol As Symbol

        ''' <summary> Bound node defining the root of the bound subtree to be analyzed </summary>
        Private ReadOnly boundNode As BoundNode

        ''' <summary> Region being analyzed: start node </summary>
        Private ReadOnly firstInRegion As BoundNode

        ''' <summary> Region being analyzed: end node </summary>
        Private ReadOnly lastInRegion As BoundNode

        ''' <summary> Region itself </summary>
        Private ReadOnly region As TextSpan

        ''' <summary> True if the input was bad, such as no first and last nodes </summary>
        Public ReadOnly Failed As Boolean

        ''' <summary>
        ''' Construct context from model and region
        ''' 
        ''' 'boundNode' defines a bound sub-tree to be analyzed and is being used in 
        ''' both region-based and not region based analysis processes. 
        ''' 
        ''' The last three parameters define a region. In most cases firstInRegion and lastInRegion 
        ''' are being used for identifying when we should enter or leave the region. 
        ''' 
        ''' Text span is also being passed to define the region which is used in few places. Those 
        ''' places can be rewritten to use first/last bound nodes, but simple [region.Contains(...)] 
        ''' check simplifies the code significantly. (Note, C# implementation uses the same logic, 
        ''' but calculates the region's text span based on first/last node; in VB to perform such 
        ''' calculation would have to traverse bound subtree under first/last nodes to detect 
        ''' region boundaries; we avoid this additional cost by passing the original text span as 
        ''' a separate parameter because we do have it anyways)
        ''' </summary>
        Friend Sub New(compilation As VBCompilation, member As Symbol, boundNode As BoundNode, firstInRegion As BoundNode, lastInRegion As BoundNode, region As textspan)
            Me.compilation = compilation
            Me.symbol = member
            Me.boundNode = boundNode

            Me.region = region
            Me.firstInRegion = firstInRegion
            Me.lastInRegion = lastInRegion
            Me.Failed = Me.symbol Is Nothing OrElse Me.boundNode Is Nothing OrElse Me.firstInRegion Is Nothing OrElse Me.lastInRegion Is Nothing

            If Not Me.Failed AndAlso Me.firstInRegion Is Me.lastInRegion Then

                Select Case Me.firstInRegion.Kind
                    Case BoundKind.NamespaceExpression,
                         BoundKind.TypeExpression

                        ' Some bound nodes are still considered to be invalid for flow analysis
                        Me.Failed = True
                End Select

            End If
        End Sub

        ''' <summary>
        ''' Construct context wiht Failed flag
        ''' </summary>
        Friend Sub New(compilation As VBCompilation)
            Me.compilation = compilation
            Me.symbol = Nothing
            Me.boundNode = Nothing
            Me.region = Nothing
            Me.firstInRegion = Nothing
            Me.lastInRegion = Nothing
            Me.Failed = True
        End Sub

        Friend ReadOnly Property AnalysisInfo As FlowAnalysisInfo
            Get
                Debug.Assert(Not Me.Failed)
                Return New FlowAnalysisInfo(Me.compilation, Me.symbol, Me.boundNode)
            End Get
        End Property

        Friend ReadOnly Property RegionInfo As FlowAnalysisRegionInfo
            Get
                Debug.Assert(Not Me.Failed)
                Return New FlowAnalysisRegionInfo(Me.firstInRegion, Me.lastInRegion, Me.region)
            End Get
        End Property

    End Structure
End Namespace

