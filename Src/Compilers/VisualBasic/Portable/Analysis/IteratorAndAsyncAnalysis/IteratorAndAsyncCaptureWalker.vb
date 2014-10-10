' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A walker that computes the set of local variables of an iterator 
    ''' method that must be moved to fields of the generated class.
    ''' </summary>
    Friend NotInheritable Class IteratorAndAsyncCaptureWalker
        Inherits DataFlowPass

        Public Structure Result
            Public ReadOnly CapturedLocals As HashSet(Of Symbol)
            Public ReadOnly ByRefLocalsInitializers As Dictionary(Of LocalSymbol, BoundExpression)

            Friend Sub New(cl As HashSet(Of Symbol), initializers As Dictionary(Of LocalSymbol, BoundExpression))
                Me.CapturedLocals = cl
                Me.ByRefLocalsInitializers = initializers
            End Sub
        End Structure

        Private _capturedLocals As HashSet(Of Symbol)
        Private _byRefLocalsInitializers As Dictionary(Of LocalSymbol, BoundExpression)

        Public Sub New(info As FlowAnalysisInfo)
            MyBase.New(info, Nothing, suppressConstExpressionsSupport:=False, trackStructsWithIntrinsicTypedFields:=True, trackUnassignments:=True)
        End Sub

        Public Overloads Shared Function Analyze(info As FlowAnalysisInfo) As Result
            Dim walker As New IteratorAndAsyncCaptureWalker(info)
            walker.Analyze()
            Debug.Assert(Not walker.InvalidRegionDetected)

            Dim result As New Result(walker._capturedLocals, walker._byRefLocalsInitializers)
            walker.Free()
            Return result
        End Function

        Protected Overrides Function Scan() As Boolean
            Me._capturedLocals = New HashSet(Of Symbol)()
            Me._byRefLocalsInitializers = New Dictionary(Of LocalSymbol, BoundExpression)()
            Return MyBase.Scan()
        End Function

        Protected Overrides Sub EnterParameter(parameter As ParameterSymbol)
            ' parameters are NOT intitially assigned here - if that is a problem, then
            ' the parameters must be captured.
            MakeSlot(parameter)

            ' Instead of analysing of which parameters are actually being referenced
            ' we add all of them; this might need to be revised later
            Me._capturedLocals.Add(parameter)
        End Sub

        Protected Overrides Sub ReportUnassigned(symbol As Symbol, node As VisualBasicSyntaxNode, rwContext As ReadWriteContext, Optional slot As Integer = -1, Optional boundFieldAccess As BoundFieldAccess = Nothing)
            If symbol.Kind = SymbolKind.Field Then
                Dim sym As Symbol = GetNodeSymbol(boundFieldAccess)

                ' Unreachable for AmbiguousLocalsPseudoSymbol: ambiguous implicit 
                ' receiver should not ever be considered unassigned
                Debug.Assert(Not TypeOf sym Is AmbiguousLocalsPseudoSymbol)

                If sym IsNot Nothing Then
                    Me._capturedLocals.Add(sym)
                End If

            ElseIf symbol.Kind = SymbolKind.Parameter OrElse symbol.Kind = SymbolKind.Local Then
                Me._capturedLocals.Add(symbol)
            End If
        End Sub

        Protected Overrides Function UnreachableState() As DataFlowPass.LocalState
            ' The iterator transformation causes some unreachable code to become
            ' reachable from the code gen's point of view, so we analyze the unreachable code too.
            Return Me.State
        End Function

        Protected Overrides ReadOnly Property IgnoreOutSemantics As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function IsEmptyStructType(type As TypeSymbol) As Boolean
            Return False
        End Function

        Protected Overrides ReadOnly Property EnableBreakingFlowAnalysisFeatures As Boolean
            Get
                Return True
            End Get
        End Property

        Private Sub MarkLocalsUnassigned()
            For i = SlotKind.FirstAvailable To nextVariableSlot - 1
                Dim symbol As Symbol = variableBySlot(i).Symbol
                Select Case symbol.Kind
                    Case SymbolKind.Local
                        If Not DirectCast(symbol, LocalSymbol).IsConst Then
                            SetSlotState(i, False)
                        End If
                    Case SymbolKind.Parameter
                        SetSlotState(i, False)
                End Select
            Next
        End Sub

        Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
            MyBase.VisitAwaitOperator(node)
            MarkLocalsUnassigned()
            Return Nothing
        End Function

        Public Overrides Function VisitSequence(node As BoundSequence) As BoundNode
            Dim result As BoundNode = Nothing

            For Each local In node.Locals
                SetSlotState(MakeSlot(local), True)
            Next
            result = MyBase.VisitSequence(node)
            For Each local In node.Locals
                CheckAssigned(local, node.Syntax)
            Next

            Return result
        End Function

        Protected Overrides ReadOnly Property ProcessCompilerGeneratedLocals As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function VisitReferenceAssignment(node As BoundReferenceAssignment) As BoundNode
            Dim local As LocalSymbol = node.ByRefLocal.LocalSymbol

            Debug.Assert(Not Me._byRefLocalsInitializers.ContainsKey(local))
            Me._byRefLocalsInitializers.Add(local, node.Target)

            Return MyBase.VisitReferenceAssignment(node)
        End Function

        Public Overrides Function VisitYieldStatement(node As BoundYieldStatement) As BoundNode
            MyBase.VisitYieldStatement(node)
            MarkLocalsUnassigned()
            Return Nothing
        End Function

        Protected Overrides Function TreatTheLocalAsAssignedWithinTheLambda(local As LocalSymbol, right As BoundExpression) As Boolean
            ' By the time this analysis is invoked, Lambda conversion 
            ' is already rewritten into an objectc creation
            If right.Kind = BoundKind.ObjectCreationExpression Then
                Dim objCreation = DirectCast(right, BoundObjectCreationExpression)
                If TypeOf objCreation.Type Is LambdaFrame AndAlso objCreation.Arguments.Length = 1 Then
                    Dim arg0 As BoundExpression = objCreation.Arguments(0)
                    If arg0.Kind = BoundKind.Local AndAlso DirectCast(arg0, BoundLocal).LocalSymbol Is local Then
                        Return True
                    End If
                End If
            End If
            Return False
        End Function
    End Class

End Namespace
