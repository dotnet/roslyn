' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' This structure holds the relevant bound node information relating to a particular syntax
    ''' node, used temporarily for GetSemanticInfo and similar APIs. 
    ''' </summary>
    Friend Structure BoundNodeSummary
        ' The lowest bound node in the bound tree associated with a particular
        ' syntax node.
        Public ReadOnly LowestBoundNode As BoundNode

        ' The highest bound node in the bound tree associated with a particular 
        ' syntax node
        Public ReadOnly HighestBoundNode As BoundNode

        ' The lowest bound node in the bound tree associated with the syntactic parent of this 
        ' syntax node (if any). This is needed in a few cases where the correct bound symbol
        ' information might be associated with the parent, but we can't always go to the parent. For
        ' example, x.f(4) might be an invocation of the method f, or an indexing of the property f,
        ' or indexing of the parameterless method f, or call to default property after a call to the
        ' parameterless method f, and we can't know from the syntax which it is. We need to check
        ' the bound nodes associated with both to give the right answer in all the cases, because
        ' bound nodes can only be associated with a single syntax node.
        Public ReadOnly LowestBoundNodeOfSyntacticParent As BoundNode

        Public Sub New(lowestBound As BoundNode,
                       highestBound As BoundNode,
                       lowestBoundOfSyntacticParent As BoundNode)
            Me.LowestBoundNode = lowestBound
            Me.HighestBoundNode = highestBound
            Me.LowestBoundNodeOfSyntacticParent = lowestBoundOfSyntacticParent
        End Sub
    End Structure
End Namespace
