' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class IteratorRewriter
        Inherits StateMachineRewriter(Of FieldSymbol)

        Private Class IteratorMethodToClassRewriter
            Inherits StateMachineMethodToClassRewriter

            ''' <summary>
            ''' The field of the generated iterator class that underlies the Current property.
            ''' </summary>
            Private ReadOnly _current As FieldSymbol

            Private _exitLabel As LabelSymbol
            Private _methodValue As LocalSymbol
            Private _tryNestingLevel As Integer

            Friend Sub New(F As SyntheticBoundNodeFactory,
                           state As FieldSymbol,
                           current As FieldSymbol,
                           hoistedVariables As IReadOnlySet(Of Symbol),
                           localProxies As Dictionary(Of Symbol, FieldSymbol),
                           stateMachineStateDebugInfoBuilder As ArrayBuilder(Of StateMachineStateDebugInfo),
                           slotAllocatorOpt As VariableSlotAllocator,
                           diagnostics As BindingDiagnosticBag)

                MyBase.New(F, state, hoistedVariables, localProxies, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, diagnostics)

                _current = current
            End Sub

            Protected Overrides ReadOnly Property FirstIncreasingResumableState As StateMachineState
                Get
                    Return StateMachineState.FirstResumableIteratorState
                End Get
            End Property

            Protected Overrides ReadOnly Property EncMissingStateMessage As String
                Get
                    Return CodeAnalysisResources.EncCannotResumeSuspendedIteratorMethod
                End Get
            End Property

            Public Sub GenerateMoveNextAndDispose(Body As BoundStatement,
                                           moveNextMethod As SynthesizedMethod,
                                           disposeMethod As SynthesizedMethod)

                ' Generate the body for MoveNext()
                F.CurrentMethod = moveNextMethod

                Dim initialLabel As GeneratedLabelSymbol = Nothing
                AddState(StateMachineState.InitialIteratorState, initialLabel)

                Me._methodValue = Me.F.SynthesizedLocal(F.CurrentMethod.ReturnType, SynthesizedLocalKind.StateMachineReturnValue, F.Syntax)

                Dim newBody = DirectCast(Visit(Body), BoundStatement)
                ' Select Me.state
                '    Case 0:
                '       GoTo state_0
                '    Case 1:
                '       GoTo state_1
                '    'etc
                '    Case Else:
                '       return false
                ' }
                ' state_0:
                ' state = -1
                ' [[rewritten body]]
                F.CloseMethod(
                    F.Block(
                        ImmutableArray.Create(Me._methodValue, Me.CachedState),
                        SyntheticBoundNodeFactory.HiddenSequencePoint(),
                        F.Assignment(Me.F.Local(Me.CachedState, True), F.Field(F.Me, Me.StateField, False)),
                        Dispatch(isOutermost:=True),
                        GenerateReturn(finished:=True),
                        F.Label(initialLabel),
                        F.Assignment(F.Field(F.Me, Me.StateField, True), Me.F.AssignmentExpression(Me.F.Local(Me.CachedState, True), Me.F.Literal(StateMachineState.NotStartedOrRunningState))),
                        newBody,
                        HandleReturn()
                    ))

                Me._exitLabel = Nothing
                Me._methodValue = Nothing

                ' Generate the body for Dispose().
                F.CurrentMethod = disposeMethod
                Dim breakLabel = F.GenerateLabel("break")
                Dim sections = (From ft In FinalizerStateMap
                                Where ft.Value <> -1
                                Group ft.Key By ft.Value Into Group
                                Select F.SwitchSection(
                                    New List(Of Integer)(Group),
                                    F.Assignment(F.Field(F.Me, Me.StateField, True), F.Literal(Value)),
                                    F.Goto(breakLabel))).ToArray()

                If (sections.Length > 0) Then
                    F.CloseMethod(F.Block(
                        F.Select(
                            F.Field(F.Me, Me.StateField, False),
                            sections),
                        F.Assignment(F.Field(F.Me, Me.StateField, True), F.Literal(StateMachineState.NotStartedOrRunningState)),
                        F.Label(breakLabel),
                        F.ExpressionStatement(F.Call(F.Me, moveNextMethod)),
                        F.Return()
                        ))
                Else
                    F.CloseMethod(F.Return())
                End If
            End Sub

            Private Function HandleReturn() As BoundStatement
                If Me._exitLabel Is Nothing Then
                    ' did not see indirect returns
                    Return F.StatementList()
                Else
                    '  _methodValue = False
                    ' exitlabel:
                    '  Return _methodValue
                    Return F.Block(
                            SyntheticBoundNodeFactory.HiddenSequencePoint(),
                            F.Assignment(F.Local(Me._methodValue, True), F.Literal(True)),
                            F.Label(Me._exitLabel),
                            F.Return(Me.F.Local(Me._methodValue, False))
                        )
                End If
            End Function

            Protected Overrides Function GenerateReturn(finished As Boolean) As BoundStatement
                Dim result = F.Literal(Not finished)

                If Me._tryNestingLevel = 0 Then
                    ' direct return
                    Return F.Return(result)

                Else
                    ' indirect return

                    If Me._exitLabel Is Nothing Then
                        Me._exitLabel = F.GenerateLabel("exitLabel")
                    End If

                    Return Me.F.Block(
                        Me.F.Assignment(Me.F.Local(Me._methodValue, True), result),
                        Me.F.Goto(Me._exitLabel)
                    )
                End If
            End Function

            Public Overrides Function VisitTryStatement(node As BoundTryStatement) As BoundNode
                Me._tryNestingLevel += 1
                Dim result = MyBase.VisitTryStatement(node)
                Me._tryNestingLevel -= 1

                Return result
            End Function

            Protected Overrides ReadOnly Property IsInExpressionLambda As Boolean
                Get
                    Return False
                End Get
            End Property

            Protected Overrides ReadOnly Property ResumeLabelName As String
                Get
                    Return "iteratorLabel"
                End Get
            End Property

#Region "Visitors"

            Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
                Return GenerateReturn(finished:=True)
            End Function

            Public Overrides Function VisitYieldStatement(node As BoundYieldStatement) As BoundNode
                '     Yield expression
                ' is translated to -
                '     Me.current = expression
                '     Me.state = <next_state>
                '     return true
                ' <next_state_label>:
                '     Me.state = -1

                Dim stateNumber As StateMachineState = 0
                Dim resumeLabel As GeneratedLabelSymbol = Nothing
                AddResumableState(node.Syntax, stateNumber, resumeLabel)

                Return F.SequencePoint(
                    node.Syntax,
                    F.Block(
                        F.Assignment(F.Field(F.Me, Me._current, True), DirectCast(Visit(node.Expression), BoundExpression)),
                        F.Assignment(F.Field(F.Me, Me.StateField, True), F.AssignmentExpression(F.Local(Me.CachedState, True), F.Literal(stateNumber))),
                        GenerateReturn(finished:=False),
                        F.Label(resumeLabel),
                        F.Assignment(F.Field(F.Me, Me.StateField, True), F.AssignmentExpression(F.Local(Me.CachedState, True), F.Literal(StateMachineState.NotStartedOrRunningState)))
                    )
                )

            End Function

#End Region 'Visitors

            Friend Overrides Sub AddProxyFieldsForStateMachineScope(proxy As FieldSymbol, proxyFields As ArrayBuilder(Of FieldSymbol))
                proxyFields.Add(proxy)
            End Sub

            Protected Overrides Function MaterializeProxy(origExpression As BoundExpression, proxy As FieldSymbol) As BoundNode
                Dim syntax As SyntaxNode = Me.F.Syntax
                Dim framePointer As BoundExpression = Me.FramePointer(syntax, proxy.ContainingType)
                Dim proxyFieldParented = proxy.AsMember(DirectCast(framePointer.Type, NamedTypeSymbol))
                Return Me.F.Field(framePointer, proxyFieldParented, origExpression.IsLValue)
            End Function
        End Class
    End Class
End Namespace

