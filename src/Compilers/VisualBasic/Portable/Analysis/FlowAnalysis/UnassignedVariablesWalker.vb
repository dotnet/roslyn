' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' An analysis that computes the set of variables that may be used
    ''' before being assigned anywhere within a method.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class UnassignedVariablesWalker
        Inherits DataFlowPass

        ' TODO: normalize the result by removing variables that are unassigned in an unmodified flow analysis.
        Private Sub New(info As FlowAnalysisInfo)
            MyBase.New(info, suppressConstExpressionsSupport:=False, trackStructsWithIntrinsicTypedFields:=True)
        End Sub

        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo) As HashSet(Of Symbol)
            Dim walker = New UnassignedVariablesWalker(info)
            Try
                Return If(walker.Analyze(), walker._result, New HashSet(Of Symbol)())
            Finally
                walker.Free()
            End Try
        End Function

        Private ReadOnly _result As HashSet(Of Symbol) = New HashSet(Of Symbol)()

        Protected Overrides Sub ReportUnassigned(local As Symbol,
                                                 node As SyntaxNode,
                                                 rwContext As ReadWriteContext,
                                                 Optional slot As Integer = SlotKind.NotTracked,
                                                 Optional boundFieldAccess As BoundFieldAccess = Nothing)

            Debug.Assert(local.Kind <> SymbolKind.Field OrElse boundFieldAccess IsNot Nothing)

            If local.Kind = SymbolKind.Field Then
                Dim sym As Symbol = GetNodeSymbol(boundFieldAccess)

                ' Ambiguous implicit receiver with should not even considered to be unassigned
                Debug.Assert(Not TypeOf sym Is AmbiguousLocalsPseudoSymbol)

                If sym IsNot Nothing Then
                    _result.Add(sym)
                End If

            Else
                _result.Add(local)
            End If

            MyBase.ReportUnassigned(local, node, rwContext, slot, boundFieldAccess)
        End Sub

        Protected Overrides ReadOnly Property SuppressRedimOperandRvalueOnPreserve As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Sub AssignLocalOnDeclaration(local As LocalSymbol, node As BoundLocalDeclaration)
            ' NOTE: static locals should not be considered assigned even in presence of initializer
            If Not local.IsStatic Then
                MyBase.AssignLocalOnDeclaration(local, node)
            End If
        End Sub

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
