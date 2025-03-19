' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Partial Friend Class AsyncMethodToClassRewriter
            Inherits StateMachineMethodToClassRewriter

            ''' <summary>
            ''' The method being rewritten.
            ''' </summary>
            Private ReadOnly _method As MethodSymbol

            ''' <summary>
            ''' The field of the generated async class used to store the async method builder: an instance of
            ''' AsyncVoidMethodBuilder, AsyncTaskMethodBuilder, or AsyncTaskMethodBuilder(Of T) depending on the
            ''' return type of the async method.
            ''' </summary>
            Private ReadOnly _builder As FieldSymbol

            ''' <summary>
            ''' The exprReturnLabel is used to label the return handling code at the end of the async state-machine
            ''' method. Return expressions are rewritten as unconditional branches to exprReturnLabel.
            ''' </summary>
            Private ReadOnly _exprReturnLabel As LabelSymbol

            ''' <summary>
            ''' The exitLabel is used to label the final method body return at the end of the async state-machine
            ''' method. Is used in rewriting of return statements from Await expressions and a couple of other
            ''' places where the return is not accompanied by return of the value.
            ''' </summary>
            Private ReadOnly _exitLabel As LabelSymbol

            ''' <summary>
            ''' The field of the generated async class used in generic task returning async methods to store the value
            ''' of rewritten return expressions. The return-handling code then uses SetResult on the async method builder
            ''' to make the result available to the caller.
            ''' </summary>
            Private ReadOnly _exprRetValue As LocalSymbol = Nothing

            Private ReadOnly _asyncMethodKind As AsyncMethodKind

            Private ReadOnly _awaiterFields As New Dictionary(Of TypeSymbol, FieldSymbol)()

            Private _nextAwaiterId As Integer

            Private ReadOnly _owner As AsyncRewriter

            Private ReadOnly _spillFieldAllocator As SpillFieldAllocator

            Private ReadOnly _typesNeedingClearingCache As New Dictionary(Of TypeSymbol, Boolean)

            Friend Sub New(method As MethodSymbol,
                           F As SyntheticBoundNodeFactory,
                           state As FieldSymbol,
                           builder As FieldSymbol,
                           hoistedVariables As IReadOnlySet(Of Symbol),
                           nonReusableLocalProxies As Dictionary(Of Symbol, CapturedSymbolOrExpression),
                           stateMachineStateDebugInfoBuilder As ArrayBuilder(Of StateMachineStateDebugInfo),
                           slotAllocatorOpt As VariableSlotAllocator,
                           owner As AsyncRewriter,
                           diagnostics As BindingDiagnosticBag)

                MyBase.New(F, state, hoistedVariables, nonReusableLocalProxies, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, diagnostics)

                Me._method = method
                Me._builder = builder
                Me._exprReturnLabel = F.GenerateLabel("exprReturn")
                Me._exitLabel = F.GenerateLabel("exitLabel")
                Me._owner = owner
                Me._asyncMethodKind = GetAsyncMethodKind(Me._method)
                Me._spillFieldAllocator = New SpillFieldAllocator(F)

                If Me._asyncMethodKind = AsyncMethodKind.GenericTaskFunction Then
                    Me._exprRetValue = Me.F.SynthesizedLocal(Me._owner._resultType, SynthesizedLocalKind.StateMachineReturnValue, F.Syntax)
                End If

                Me._nextAwaiterId = If(slotAllocatorOpt IsNot Nothing, slotAllocatorOpt.PreviousAwaiterSlotCount, 0)
            End Sub

            Protected Overrides ReadOnly Property FirstFinalizeState As StateMachineState
                Get
                    Return StateMachineState.FirstAsyncFinalizeState
                End Get
            End Property

            Protected Overrides ReadOnly Property FirstIncreasingResumableState As StateMachineState
                Get
                    Return StateMachineState.FirstResumableAsyncState
                End Get
            End Property

            Protected Overrides ReadOnly Property EncMissingStateErrorCode As HotReloadExceptionCode
                Get
                    Return HotReloadExceptionCode.CannotResumeSuspendedAsyncMethod
                End Get
            End Property

            Private Function GetAwaiterField(awaiterType As TypeSymbol) As FieldSymbol
                Dim result As FieldSymbol = Nothing

                ' Awaiters of the same type always share the same slot, regardless of what await expressions they belong to.
                ' Even in case of nested await expressions only one awaiter Is active.
                ' So we don't need to tie the awaiter variable to a particular await expression and only use its type
                ' to find the previous awaiter field.
                If Not Me._awaiterFields.TryGetValue(awaiterType, result) Then
                    Dim slotIndex As Integer = -1
                    If Me.SlotAllocatorOpt Is Nothing OrElse Not Me.SlotAllocatorOpt.TryGetPreviousAwaiterSlotIndex(F.CompilationState.ModuleBuilderOpt.Translate(awaiterType, F.Syntax, F.Diagnostics.DiagnosticBag), F.Diagnostics.DiagnosticBag, slotIndex) Then
                        slotIndex = _nextAwaiterId
                        _nextAwaiterId = _nextAwaiterId + 1
                    End If

                    Dim fieldName As String = GeneratedNames.MakeStateMachineAwaiterFieldName(slotIndex)
                    result = Me.F.StateMachineField(awaiterType, Me._method, fieldName, SynthesizedLocalKind.AwaiterField, slotIndex, accessibility:=Accessibility.Friend)
                    Me._awaiterFields.Add(awaiterType, result)
                End If
                Return result
            End Function

            ''' <summary>
            ''' Generate the body for MoveNext()
            ''' </summary>
            Friend Sub GenerateMoveNext(body As BoundStatement, moveNextMethod As MethodSymbol)
                Me.F.CurrentMethod = moveNextMethod

                Dim rewrittenBody As BoundStatement = DirectCast(Visit(body), BoundStatement)

                Dim rootScopeHoistedLocals As ImmutableArray(Of FieldSymbol) = Nothing
                TryUnwrapBoundStateMachineScope(rewrittenBody, rootScopeHoistedLocals)

                Dim bodyBuilder = ArrayBuilder(Of BoundStatement).GetInstance()

                ' NOTE: We don't need to create/use any label after Dispatch inside Try block below
                ' NOTE: because 'Case' clauses inside dispatching Select statement map states to
                ' NOTE: correspondent Await points (or nested Try blocks) and 'NotStartedStateMachine'
                ' NOTE: falls to the 'Case Else' category and just falls through to the [body] part

                ' STMT:   Try
                ' STMT:       Select Me.$State  ' Dispatch
                ' STMT:         Case <state1> ' ...
                ' STMT:         ' ...
                ' STMT:       End Select
                ' STMT:       ' Fall through
                ' STMT:       [body]
                ' STMT:       ...
                ' STMT:   Catch $ex As Exception
                ' STMT:       state = finishedState
                ' STMT:       builder.SetException($ex)
                ' STMT:       Return
                ' STMT:   End Try

                ' STMT:   cachedState = state
                bodyBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Local(Me.CachedState, True),
                        Me.F.Field(Me.F.Me(), Me.StateField, False)))

                ' Note that the first real sequence point comes after the dispatch jump table is because
                ' the Begin construct should map to the logical beginning of the method.  A breakpoint
                ' there should only be hit once, upon first entry into the method, and subsequent calls
                ' to MoveNext to resume the method should not hit that breakpoint.
                Dim exceptionLocal = Me.F.SynthesizedLocal(Me.F.WellKnownType(WellKnownType.System_Exception))
                bodyBuilder.Add(
                    Me.F.Try(
                        Me.F.Block(
                            ImmutableArray(Of LocalSymbol).Empty,
                            SyntheticBoundNodeFactory.HiddenSequencePoint(),
                            Me.Dispatch(isOutermost:=True),
                            rewrittenBody
                        ),
                        Me.F.CatchBlocks(
                            Me.F.Catch(
                                exceptionLocal,
                                Me.F.Block(
                                    SyntheticBoundNodeFactory.HiddenSequencePoint(),
                                    Me.F.Assignment(Me.F.Field(Me.F.Me(), Me.StateField, True), Me.F.Literal(StateMachineState.AsyncFinishedState)),
                                    Me.F.ExpressionStatement(
                                        Me._owner.GenerateMethodCall(
                                            Me.F.Field(Me.F.Me(), Me._builder, False),
                                            Me._owner._builderType,
                                            "SetException",
                                            Me.F.Local(exceptionLocal, False))),
                                    Me.F.Goto(Me._exitLabel)),
                                isSynthesizedAsyncCatchAll:=True))))

                ' STMT:   ExprReturnLabel: ' for the rewritten 'Return <expressions>' statements in the user's method body
                bodyBuilder.Add(Me.F.Label(Me._exprReturnLabel))

                ' STMT:   state = cachedState = finishedState
                Dim stateDone = Me.F.Assignment(
                        Me.F.Field(Me.F.Me(), Me.StateField, True),
                        Me.F.AssignmentExpression(Me.F.Local(Me.CachedState, True), Me.F.Literal(StateMachineState.AsyncFinishedState)))
                Dim block As MethodBlockSyntax = TryCast(body.Syntax, MethodBlockSyntax)
                If (block Is Nothing) Then
                    bodyBuilder.Add(stateDone)
                Else
                    bodyBuilder.Add(Me.F.SequencePointWithSpan(block, block.EndBlockStatement.Span, stateDone))
                    bodyBuilder.Add(SyntheticBoundNodeFactory.HiddenSequencePoint())
                End If

                ' STMT: builder.SetResult([RetVal])
                bodyBuilder.Add(
                    Me.F.ExpressionStatement(
                        Me._owner.GenerateMethodCall(
                            Me.F.Field(Me.F.Me(), Me._builder, False),
                            Me._owner._builderType,
                            "SetResult",
                            If(Me._asyncMethodKind = AsyncMethodKind.GenericTaskFunction,
                               {Me.F.Local(Me._exprRetValue, False)}, Array.Empty(Of BoundExpression)()))))

                ' STMT:   ReturnLabel: ' for the forced exit from the method, such as return from catch block above
                bodyBuilder.Add(Me.F.Label(Me._exitLabel))
                bodyBuilder.Add(Me.F.Return())

                Dim newStatements As ImmutableArray(Of BoundStatement) = bodyBuilder.ToImmutableAndFree()
                Dim newBody = Me.F.Block(
                        If(Me._exprRetValue IsNot Nothing,
                           ImmutableArray.Create(Me._exprRetValue, Me.CachedState),
                           ImmutableArray.Create(Me.CachedState)),
                       newStatements)

                If rootScopeHoistedLocals.Length > 0 Then
                    newBody = MakeStateMachineScope(rootScopeHoistedLocals, newBody)
                End If

                Me._owner.CloseMethod(newBody)
            End Sub

            Protected Overrides ReadOnly Property ResumeLabelName As String
                Get
                    Return "asyncLabel"
                End Get
            End Property

            Protected Overrides Function GenerateReturn(finished As Boolean) As BoundStatement
                Return Me.F.Goto(Me._exitLabel)
            End Function

            Protected Overrides ReadOnly Property IsInExpressionLambda As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class
    End Class
End Namespace
