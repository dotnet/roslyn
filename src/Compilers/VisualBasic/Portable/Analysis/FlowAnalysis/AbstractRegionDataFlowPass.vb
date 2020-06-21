' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' Note: this code has a copy-and-paste sibling in AbstractRegionControlFlowPass.
    ' Any fix to one should be applied to the other.
    Friend MustInherit Class AbstractRegionDataFlowPass
        Inherits DataFlowPass

        Friend Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo,
                       Optional initiallyAssignedVariables As HashSet(Of Symbol) = Nothing,
                       Optional trackUnassignments As Boolean = False,
                       Optional trackStructsWithIntrinsicTypedFields As Boolean = False)

            MyBase.New(info, region, False, initiallyAssignedVariables, trackUnassignments, trackStructsWithIntrinsicTypedFields)
        End Sub

        Public Overrides Function VisitLambda(node As BoundLambda) As BoundNode
            MakeSlots(node.LambdaSymbol.Parameters)

            Dim result = MyBase.VisitLambda(node)
            Return result
        End Function

        Private Sub MakeSlots(parameters As ImmutableArray(Of ParameterSymbol))
            For Each parameter In parameters
                GetOrCreateSlot(parameter)
            Next
        End Sub

        Protected Overrides ReadOnly Property SuppressRedimOperandRvalueOnPreserve As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function VisitParameter(node As BoundParameter) As BoundNode
            If node.ParameterSymbol.ContainingSymbol.IsQueryLambdaMethod Then
                Return Nothing
            End If

            Return MyBase.VisitParameter(node)
        End Function

        Protected Overrides Function CreateLocalSymbolForVariables(declarations As ImmutableArray(Of BoundLocalDeclaration)) As LocalSymbol
            If declarations.Length = 1 Then
                Return declarations(0).LocalSymbol
            End If

            Dim locals(declarations.Length - 1) As LocalSymbol
            For i = 0 To declarations.Length - 1
                locals(i) = declarations(i).LocalSymbol
            Next
            Return AmbiguousLocalsPseudoSymbol.Create(locals.AsImmutableOrNull())
        End Function

        Protected Overrides ReadOnly Property IgnoreOutSemantics As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides ReadOnly Property EnableBreakingFlowAnalysisFeatures As Boolean
            Get
                Return True
            End Get
        End Property

    End Class

End Namespace
