' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class IteratorRewriter
        Inherits StateMachineRewriter(Of IteratorStateMachineTypeSymbol, FieldSymbol)

        Private Class IteratorMethodToClassRewriter
            Inherits StateMachineMethodToClassRewriter

            ''' <summary>
            ''' The field of the generated iterator class that underlies the Current property.
            ''' </summary>
            Private ReadOnly _current As FieldSymbol
            Private ReadOnly _originalMethodDeclaration As VisualBasicSyntaxNode

            Private _exitLabel As LabelSymbol
            Private _methodValue As LocalSymbol
            Private _tryNestingLevel As Integer

            Friend Sub New(method As MethodSymbol,
                           F As SyntheticBoundNodeFactory,
                           state As FieldSymbol,
                           current As FieldSymbol,
                           localProxies As Dictionary(Of Symbol, FieldSymbol),
                           diagnostics As DiagnosticBag,
                           generateDebugInfo As Boolean)

                MyBase.New(F, state, localProxies, diagnostics, generateDebugInfo)

                Me._current = current

                Me._originalMethodDeclaration = method.DeclaringSyntaxReferences(0).GetVisualBasicSyntax
            End Sub

            Sub GenerateMoveNextAndDispose(Body As BoundStatement,
                                           moveNextMethod As SynthesizedStateMachineMethod,
                                           disposeMethod As SynthesizedStateMachineMethod)

                ' Generate the body for MoveNext()
                F.CurrentMethod = moveNextMethod

                Dim initialStateInfo = AddState()
                Dim initialState As Integer = initialStateInfo.Number
                Dim initialLabel As GeneratedLabelSymbol = initialStateInfo.ResumeLabel

                Me._methodValue = Me.F.SynthesizedNamedLocal(F.CurrentMethod.ReturnType, TempKind.StateMachineReturnValue, Nothing)

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
                        ImmutableArray.Create(Of LocalSymbol)(Me._methodValue, Me.CachedState),
                        F.HiddenSequencePoint(Me.GenerateDebugInfo),
                        F.Assignment(Me.F.Local(Me.CachedState, True), F.Field(F.[Me](), Me.StateField, False)),
                        Dispatch(),
                        GenerateReturn(finished:=True),
                        F.Label(initialLabel),
                        F.Assignment(F.Field(F.[Me](), Me.StateField, True), Me.F.AssignmentExpression(Me.F.Local(Me.CachedState, True), Me.F.Literal(StateMachineStates.NotStartedStateMachine))),
                        F.SequencePoint(GenerateDebugInfo, _originalMethodDeclaration),
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
                                    F.Assignment(F.Field(F.[Me](), Me.StateField, True), F.Literal(Value)),
                                    F.Goto(breakLabel))).ToArray()

                If (sections.Length > 0) Then
                    F.CloseMethod(F.Block(
                        F.Select(
                            F.Field(F.[Me](), Me.StateField, False),
                            sections),
                        F.Assignment(F.Field(F.[Me](), Me.StateField, True), F.Literal(StateMachineStates.NotStartedStateMachine)),
                        F.Label(breakLabel),
                        F.ExpressionStatement(F.Call(F.[Me](), moveNextMethod)),
                        F.Return()
                        ))
                Else
                    F.CloseMethod(F.Return())
                End If
            End Sub

            Private Function HandleReturn() As BoundStatement
                If Me._exitLabel Is Nothing Then
                    ' did not see indirect returns
                    Return F.Block()
                Else
                    '  _methodValue = False
                    ' exitlabel:
                    '  Return _methodValue
                    Return F.Block(
                            F.HiddenSequencePoint(Me.GenerateDebugInfo),
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
                Dim newState = AddState()
                Return F.SequencePoint(
                    Me.GenerateDebugInfo,
                    node.Syntax,
                    F.Block(
                        F.Assignment(F.Field(F.[Me](), Me._current, True), DirectCast(Visit(node.Expression), BoundExpression)),
                        F.Assignment(F.Field(F.[Me](), Me.StateField, True), F.AssignmentExpression(F.Local(Me.CachedState, True), F.Literal(newState.Number))),
                        GenerateReturn(finished:=False),
                        F.Label(newState.ResumeLabel),
                        F.Assignment(F.Field(F.[Me](), Me.StateField, True), F.AssignmentExpression(F.Local(Me.CachedState, True), F.Literal(StateMachineStates.NotStartedStateMachine)))
                    )
                )

            End Function

#End Region 'Visitors

            Friend Overrides Sub AddProxyFieldsForStateMachineScope(proxy As FieldSymbol, proxyFields As ArrayBuilder(Of FieldSymbol))
                proxyFields.Add(proxy)
            End Sub

            Protected Overrides Function MaterializeProxy(origExpression As BoundExpression, proxy As FieldSymbol) As BoundNode
                Dim syntax As VisualBasicSyntaxNode = Me.F.Syntax
                Dim framePointer As BoundExpression = Me.FramePointer(syntax, proxy.ContainingType)
                Dim proxyFieldParented = proxy.AsMember(DirectCast(framePointer.Type, NamedTypeSymbol))
                Return Me.F.Field(framePointer, proxyFieldParented, origExpression.IsLValue)
            End Function
        End Class
    End Class
End Namespace

