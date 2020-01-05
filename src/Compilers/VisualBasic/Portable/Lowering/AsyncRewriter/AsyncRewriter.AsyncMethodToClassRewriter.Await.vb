' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Partial Friend Class AsyncMethodToClassRewriter
            Inherits StateMachineMethodToClassRewriter

            Public Overrides Function VisitAwaitOperator(node As BoundAwaitOperator) As BoundNode
                Dim builder As New SpillBuilder

                ' TODO: We may have problems in case 
                ' TODO: the awaiter is being mutated in the code flow below; revise and get rid 
                ' TODO: of local *or* make sure mutable awaiter works

                ' The awaiter temp facilitates EnC method remapping and thus have to be long-lived.
                ' It transfers the awaiter objects from the old version of the MoveNext method to the New one.
                Debug.Assert(node.Syntax.IsKind(SyntaxKind.AwaitExpression))
                Dim awaiterType As TypeSymbol = node.GetAwaiter.Type.InternalSubstituteTypeParameters(Me.TypeMap).Type
                Dim awaiterTemp As LocalSymbol = Me.F.SynthesizedLocal(awaiterType, kind:=SynthesizedLocalKind.Awaiter, syntax:=node.Syntax)
                builder.AddLocal(awaiterTemp)

                ' Replace 'awaiter' with the local
                Dim awaiterInstancePlaceholder As BoundLValuePlaceholder = node.AwaiterInstancePlaceholder
                Debug.Assert(awaiterInstancePlaceholder IsNot Nothing)
                PlaceholderReplacementMap.Add(awaiterInstancePlaceholder, Me.F.Local(awaiterTemp, True))

                ' Replace 'awaitable' with rewritten expression
                Dim awaitableInstancePlaceholder As BoundRValuePlaceholder = node.AwaitableInstancePlaceholder
                Debug.Assert(awaitableInstancePlaceholder IsNot Nothing)
                PlaceholderReplacementMap.Add(awaitableInstancePlaceholder, VisitExpression(node.Operand))

                ' Rewrite GetAwaiter, IsCompleted and GetResult expressions
                Dim rewrittenGetAwaiter As BoundExpression = VisitExpression(node.GetAwaiter)
                Dim rewrittenIsCompleted As BoundExpression = VisitExpression(node.IsCompleted)
                Dim rewrittenGetResult As BoundExpression = VisitExpression(node.GetResult)
                Dim rewrittenType As TypeSymbol = VisitType(node.Type)

                PlaceholderReplacementMap.Remove(awaiterInstancePlaceholder)
                PlaceholderReplacementMap.Remove(awaitableInstancePlaceholder)

                ' STMT:   Dim $awaiterTemp = <expr>.GetAwaiter()
                builder.AddStatement(
                    Me.MakeAssignmentStatement(rewrittenGetAwaiter, awaiterTemp, builder))

                ' hidden sequence point facilitates EnC method remapping, see explanation on SynthesizedLocalKind.Awaiter
                builder.AddStatement(SyntheticBoundNodeFactory.HiddenSequencePoint())

                ' STMT:   If Not $awaiterTemp.IsCompleted Then <await-for-incomplete-task>
                Dim awaitForIncompleteTask As BoundStatement = Me.GenerateAwaitForIncompleteTask(awaiterTemp)

                ' NOTE: As node.GetAwaiter, node.IsCompleted and node.GetResult are already rewritten by 
                '       local rewriter we decide if the original calls were late bound by inspecting 
                '       the type of awaiter; actually rewritten expressions work well for GetAwaiter and 
                '       GetResult, but for IsCompleted we have to know this fact to produce code that
                '       matches Dev11.
                Dim isAwaiterTempImpliesLateBound As Boolean = awaiterType.IsObjectType

                If isAwaiterTempImpliesLateBound Then
                    ' late-bound case
                    builder.AddStatement(
                        Me.F.If(
                            condition:=Me.F.Convert(Me.F.SpecialType(SpecialType.System_Boolean), rewrittenIsCompleted),
                            thenClause:=Me.F.StatementList(),
                            elseClause:=awaitForIncompleteTask))
                Else
                    ' regular case
                    builder.AddStatement(
                        Me.F.If(
                            condition:=Me.F.Not(rewrittenIsCompleted),
                            thenClause:=awaitForIncompleteTask))
                End If

                Dim onAwaitFinished As BoundExpression = Nothing
                Dim clearAwaiterTemp As BoundExpression =
                    Me.F.AssignmentExpression(Me.F.Local(awaiterTemp, True), Me.F.Null(awaiterTemp.Type))

                If rewrittenType.SpecialType <> SpecialType.System_Void Then
                    ' STMT:   $resultTemp = $awaiterTemp.GetResult()
                    ' STMT:   $awaiterTemp = Nothing
                    ' STMT:   $resultTemp
                    Dim resultTemp As LocalSymbol = Me.F.SynthesizedLocal(rewrittenType)
                    onAwaitFinished = Me.F.Sequence(resultTemp,
                                                    Me.F.AssignmentExpression(Me.F.Local(resultTemp, True), rewrittenGetResult),
                                                    clearAwaiterTemp,
                                                    Me.F.Local(resultTemp, False))

                Else
                    ' STMT:   $awaiterTemp.GetResult()
                    ' STMT:   $awaiterTemp = Nothing
                    onAwaitFinished = Me.F.Sequence(rewrittenGetResult, clearAwaiterTemp)
                End If

                Return builder.BuildSequenceAndFree(Me.F, onAwaitFinished)
            End Function

            Private Function GenerateAwaitForIncompleteTask(awaiterTemp As LocalSymbol) As BoundBlock
                Dim state As StateInfo = Me.AddState()

                Dim awaiterType As TypeSymbol = awaiterTemp.Type
                Dim awaiterFieldType As TypeSymbol = awaiterType
                If awaiterFieldType.IsVerifierReference() Then
                    awaiterFieldType = Me.F.SpecialType(SpecialType.System_Object)
                End If
                Dim awaiterField As FieldSymbol = GetAwaiterField(awaiterFieldType)

                Dim blockBuilder = ArrayBuilder(Of BoundStatement).GetInstance()

                ' STMT:   Me.$State = CachedState = stateForLabel
                blockBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Field(Me.F.Me(), Me.StateField, True),
                        Me.F.AssignmentExpression(Me.F.Local(Me.CachedState, True), Me.F.Literal(state.Number))))

                ' Emit Await yield point to be injected into PDB
                blockBuilder.Add(Me.F.NoOp(NoOpStatementFlavor.AwaitYieldPoint))

                ' STMT:   Me.$awaiter = $awaiterTemp
                blockBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Field(Me.F.Me(), awaiterField, True),
                        If(TypeSymbol.Equals(awaiterField.Type, awaiterTemp.Type, TypeCompareKind.ConsiderEverything),
                           DirectCast(Me.F.Local(awaiterTemp, False), BoundExpression),
                           Me.F.Convert(awaiterFieldType, Me.F.Local(awaiterTemp, False)))))

                ' NOTE: As it is mentioned above, Dev11 decides whether or not to use 'late binding' for 
                '       Await[Unsafe]OnCompleted based on the type of awaiter local variable
                '       See ResumableMethodLowerer::RewriteAwaitExpression(...) in ResumableMethodRewriter.cpp
                Dim isAwaiterTempImpliesLateBound As Boolean = awaiterType.IsObjectType

                Dim builderFieldAsRValue As BoundExpression = Me.F.Field(Me.F.Me(), Me._builder, False)

                Dim ICriticalNotifyCompletion = Me.F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion)

                If Not ICriticalNotifyCompletion.IsErrorType Then
                    If isAwaiterTempImpliesLateBound Then
                        ' STMT:   Dim dcast1 As ICriticalNotifyCompletion = TryCast(AwaiterLocalTemp, ICriticalNotifyCompletion)
                        ' STMT:   If dcast1 IsNot Nothing Then
                        ' STMT:       builder.AwaitUnsafeOnCompleted<ICriticalNotifyCompletion,TStateMachine>(ref dcast1, ref Me)
                        ' STMT:   Else
                        ' STMT:       Dim dcast2 As INotifyCompletion = DirectCast(AwaiterLocalTemp, INotifyCompletion)
                        ' STMT:       builder.AwaitOnCompleted<INotifyCompletion,TStateMachine>(ref dcast2, ref Me)
                        ' STMT:   End If
                        Dim asCriticalNotifyCompletion As LocalSymbol = Me.F.SynthesizedLocal(ICriticalNotifyCompletion)
                        Dim asNotifyCompletion As LocalSymbol =
                            Me.F.SynthesizedLocal(Me.F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion))

                        Dim awaiterTempAsRValue As BoundLocal = Me.F.Local(awaiterTemp, False)
                        Dim criticalNotifyCompletionAsLValue As BoundLocal = Me.F.Local(asCriticalNotifyCompletion, True)
                        Dim notifyCompletionAsLValue As BoundLocal = Me.F.Local(asNotifyCompletion, True)

                        ' >>>>   dcast1 = TryCast(AwaiterLocalTemp, ICriticalNotifyCompletion)
                        Dim asCriticalNotifyCompletionAssignment As BoundStatement =
                            Me.MakeAssignmentStatement(
                                Me.F.TryCast(awaiterTempAsRValue, asCriticalNotifyCompletion.Type),
                                asCriticalNotifyCompletion)

                        ' >>>>   builder.AwaitUnsafeOnCompleted(Of TAwaiter,TSM)((ByRef) dcast1, (ByRef) Me)
                        Dim awaitUnsafeOnCompletedCall As BoundStatement =
                            Me.F.ExpressionStatement(
                                Me._owner.GenerateMethodCall(
                                    builderFieldAsRValue,
                                    Me._owner._builderType,
                                    "AwaitUnsafeOnCompleted",
                                    ImmutableArray.Create(Of TypeSymbol)(asCriticalNotifyCompletion.Type, Me.F.Me().Type),
                                    {criticalNotifyCompletionAsLValue, Me.F.ReferenceOrByrefMe()}))

                        ' >>>>   dcast2 = DirectCast(AwaiterLocalTemp, INotifyCompletion)
                        ' TODO: POSTPROCESS ASSIGNMENT
                        Dim asNotifyCompletionAssignment As BoundStatement =
                            Me.MakeAssignmentStatement(
                                Me.F.DirectCast(awaiterTempAsRValue, asNotifyCompletion.Type),
                                asNotifyCompletion)

                        ' >>>>   builder.AwaitOnCompleted(Of TAwaiter,TSM)((ByRef) dcast2, (ByRef) Me)
                        Dim awaitOnCompletedCall As BoundStatement =
                            Me.F.ExpressionStatement(
                                Me._owner.GenerateMethodCall(
                                    builderFieldAsRValue,
                                    Me._owner._builderType,
                                    "AwaitOnCompleted",
                                    ImmutableArray.Create(Of TypeSymbol)(asNotifyCompletion.Type, Me.F.Me().Type),
                                    {notifyCompletionAsLValue, Me.F.ReferenceOrByrefMe()}))

                        blockBuilder.Add(
                            Me.F.Block(
                                ImmutableArray.Create(Of LocalSymbol)(asCriticalNotifyCompletion, asNotifyCompletion),
                                asCriticalNotifyCompletionAssignment,
                                Me.F.If(
                                    condition:=Me.F.Not(Me.F.ReferenceIsNothing(Me.F.Local(asCriticalNotifyCompletion, False))),
                                    thenClause:=awaitUnsafeOnCompletedCall,
                                    elseClause:=Me.F.Block(
                                        asNotifyCompletionAssignment,
                                        awaitOnCompletedCall))))

                    Else
                        ' STMT:   this.builder.AwaitUnsafeOnCompleted(Of TAwaiter,TSM)((ByRef) $awaiterTemp, (ByRef) Me)
                        '  or
                        ' STMT:   this.builder.AwaitOnCompleted(Of TAwaiter,TSM)((ByRef) $awaiterTemp, (ByRef) Me)
                        Dim useUnsafeOnCompleted As Boolean =
                            Conversions.IsWideningConversion(
                                Conversions.ClassifyDirectCastConversion(
                                    awaiterType,
                                    ICriticalNotifyCompletion,
                                    useSiteDiagnostics:=Nothing))

                        blockBuilder.Add(
                            Me.F.ExpressionStatement(
                                Me._owner.GenerateMethodCall(
                                    builderFieldAsRValue,
                                    Me._owner._builderType,
                                    If(useUnsafeOnCompleted, "AwaitUnsafeOnCompleted", "AwaitOnCompleted"),
                                    ImmutableArray.Create(Of TypeSymbol)(awaiterType, Me.F.Me().Type),
                                    {Me.F.Local(awaiterTemp, True), Me.F.ReferenceOrByrefMe()})))
                    End If
                End If

                '----------------------------------------------
                ' Actual interruption point with return statement and 
                ' resume label, to be handled in codegen
                '
                '   RETURN
                blockBuilder.Add(Me.F.Goto(Me._exitLabel))
                '----------------------------------------------
                '   RESUME LABEL
                blockBuilder.Add(Me.F.Label(state.ResumeLabel))
                '----------------------------------------------

                ' Emit Await resume point to be injected into PDB
                blockBuilder.Add(Me.F.NoOp(NoOpStatementFlavor.AwaitResumePoint))

                ' STMT:   Me.$State = CachedState = NotStartedStateMachine
                blockBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Field(Me.F.Me(), Me.StateField, True),
                        Me.F.AssignmentExpression(Me.F.Local(Me.CachedState, True), Me.F.Literal(StateMachineStates.NotStartedStateMachine))))

                ' STMT:   $awaiterTemp = Me.$awaiter
                '  or   
                ' STMT:   $awaiterTemp = DirectCast(Me.$awaiter, AwaiterType) ' In case of late binding
                blockBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Local(awaiterTemp, True),
                        If(TypeSymbol.Equals(awaiterTemp.Type, awaiterField.Type, TypeCompareKind.ConsiderEverything),
                           DirectCast(Me.F.Field(Me.F.Me(), awaiterField, False), BoundExpression),
                           Me.F.Convert(awaiterTemp.Type, Me.F.Field(Me.F.Me(), awaiterField, False)))))

                ' Clear the field as it is not needed any more, also note that the local will 
                ' be cleared later after we call GetResult...
                ' STMT:   Me.$awaiter = Nothing
                blockBuilder.Add(
                    Me.F.Assignment(
                        Me.F.Field(Me.F.Me(), awaiterField, True),
                        Me.F.Null(awaiterField.Type)))

                Return Me.F.Block(blockBuilder.ToImmutableAndFree())
            End Function

            Protected Overrides Function MaterializeProxy(origExpression As BoundExpression, proxy As CapturedSymbolOrExpression) As BoundNode
                Return proxy.Materialize(Me, origExpression.IsLValue)
            End Function

            Friend Overrides Sub AddProxyFieldsForStateMachineScope(proxy As CapturedSymbolOrExpression, proxyFields As ArrayBuilder(Of FieldSymbol))
                proxy.AddProxyFieldsForStateMachineScope(proxyFields)
            End Sub
        End Class
    End Class

End Namespace
