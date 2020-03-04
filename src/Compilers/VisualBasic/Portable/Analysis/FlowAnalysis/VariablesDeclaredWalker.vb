' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A region analysis walker that records declared variables.
    ''' </summary>
    Friend Class VariablesDeclaredWalker
        Inherits AbstractRegionControlFlowPass

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo) As IEnumerable(Of Symbol)
            Dim walker = New VariablesDeclaredWalker(info, region)
            Try
                Return If(walker.Analyze(), walker._variablesDeclared, SpecializedCollections.EmptyEnumerable(Of Symbol)())
            Finally
                walker.Free()
            End Try
        End Function

        Private ReadOnly _variablesDeclared As New HashSet(Of Symbol)

        Private Overloads Function Analyze() As Boolean
            ' only one pass needed.
            Return Scan()
        End Function

        Private Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Public Overrides Function VisitLocalDeclaration(node As BoundLocalDeclaration) As BoundNode
            If IsInside Then
                _variablesDeclared.Add(node.LocalSymbol)
            End If
            Return MyBase.VisitLocalDeclaration(node)
        End Function

        Protected Overrides Sub VisitForStatementVariableDeclaration(node As BoundForStatement)
            If IsInside AndAlso
                    node.DeclaredOrInferredLocalOpt IsNot Nothing Then
                _variablesDeclared.Add(node.DeclaredOrInferredLocalOpt)
            End If
            MyBase.VisitForStatementVariableDeclaration(node)
        End Sub

        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            If IsInside Then
                For Each parameter In node.LambdaSymbol.Parameters
                    _variablesDeclared.Add(parameter)
                Next
            End If

            Return MyBase.VisitLambda(node)
        End Function

        Public Overrides Function VisitQueryableSource(node As BoundQueryableSource) As BoundNode
            MyBase.VisitQueryableSource(node)

            If Not node.WasCompilerGenerated AndAlso node.RangeVariables.Length > 0 AndAlso IsInside Then
                Debug.Assert(node.RangeVariables.Length = 1)
                _variablesDeclared.Add(node.RangeVariables(0))
            End If

            Return Nothing
        End Function

        Public Overrides Function VisitRangeVariableAssignment(node As BoundRangeVariableAssignment) As BoundNode
            If Not node.WasCompilerGenerated AndAlso IsInside Then
                _variablesDeclared.Add(node.RangeVariable)
            End If

            MyBase.VisitRangeVariableAssignment(node)
            Return Nothing
        End Function

        Protected Overrides Sub VisitCatchBlock(catchBlock As BoundCatchBlock, ByRef finallyState As LocalState)
            If IsInsideRegion(catchBlock.Syntax.Span) Then
                If catchBlock.LocalOpt IsNot Nothing Then
                    _variablesDeclared.Add(catchBlock.LocalOpt)
                End If

            End If

            MyBase.VisitCatchBlock(catchBlock, finallyState)
        End Sub
    End Class
End Namespace
