' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Dim awaiterType As TypeSymbol = node.GetAwaiter.Type.InternalSubstituteTypeParameters(TypeMap).Type
                Dim awaiterTemp As LocalSymbol = F.SynthesizedLocal(awaiterType, kind:=SynthesizedLocalKind.Awaiter, syntax:=node.Syntax)
                builder.AddLocal(awaiterTemp)

                ' Replace 'awaiter' with the local
                Dim awaiterInstancePlaceholder As BoundLValuePlaceholder = node.AwaiterInstancePlaceholder
                Debug.Assert(awaiterInstancePlaceholder IsNot Nothing)
                PlaceholderReplacementMap.Add(awaiterInstancePlaceholder, F.Local(awaiterTemp, True))

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
                    MakeAssignmentStatement(rewrittenGetAwaiter, awaiterTemp, builder))

                ' hidden sequence point facilitates EnC method remapping, see explanation on SynthesizedLocalKind.Awaiter
                builder.AddStatement(F.HiddenSequencePoint())

                ' STMT:   If Not $awaiterTemp.IsCompleted Then <await-for-incomplete-task>
                Dim awaitForIncompleteTask As BoundStatement = GenerateAwaitForIncompleteTask(awaiterTemp)

                ' NOTE: As node.GetAwaiter, node.IsCompleted and node.GetResult are already rewritten by 
                '       local rewriter we decide if the original calls were late bound by inspecting 
                '       the type of awaiter; actually rewritten expressions work well for GetAwaiter and 
                '       GetResult, but for IsCompleted we have to know this fact to produce code that
                '       matches Dev11.
                Dim isAwaiterTempImpliesLateBound As Boolean = awaiterType.IsObjectType

                If isAwaiterTempImpliesLateBound Then
                    ' late-bound case
                    builder.AddStatement(
                        F.If(
                            condition:=F.Convert(F.SpecialType(SpecialType.System_Boolean), rewrittenIsCompleted),
                            thenClause:=F.Block(),
                            elseClause:=awaitForIncompleteTask))
                Else
                    ' regular case
                    builder.AddStatement(
                        F.If(
                            condition:=F.Not(rewrittenIsCompleted),
                            thenClause:=awaitForIncompleteTask))
                End If

                Dim onAwaitFinished As BoundExpression = Nothing
                Dim clearAwaiterTemp As BoundExpression =
                    F.AssignmentExpression(F.Local(awaiterTemp, True), F.Null(awaiterTemp.Type))

                If rewrittenType.SpecialType <> SpecialType.System_Void Then
                    ' STMT:   $resultTemp = $awaiterTemp.GetResult()
                    ' STMT:   $awaiterTemp = Nothing
                    ' STMT:   $resultTemp
                    Dim resultTemp As LocalSymbol = F.SynthesizedLocal(rewrittenType)
                    onAwaitFinished = F.Sequence(resultTemp,
                                                    F.AssignmentExpression(F.Local(resultTemp, True), rewrittenGetResult),
                                                    clearAwaiterTemp,
                                                    F.Local(resultTemp, False))

                Else
                    ' STMT:   $awaiterTemp.GetResult()
                    ' STMT:   $awaiterTemp = Nothing
                    onAwaitFinished = F.Sequence(rewrittenGetResult, clearAwaiterTemp)
                End If

                Return builder.BuildSequenceAndFree(F, onAwaitFinished)
            End Function

            Private Function GenerateAwaitForIncompleteTask(awaiterTemp As LocalSymbol) As BoundBlock
                Dim state As StateInfo = AddState()

                Dim awaiterType As TypeSymbol = awaiterTemp.Type
                Dim awaiterFieldType As TypeSymbol = awaiterType
                If awaiterFieldType.IsVerifierReference() Then
                    awaiterFieldType = F.SpecialType(SpecialType.System_Object)
                End If
                Dim awaiterField As FieldSymbol = GetAwaiterField(awaiterFieldType)

                Dim blockBuilder = ArrayBuilder(Of BoundStatement).GetInstance()

                ' STMT:   Me.$State = CachedState = stateForLabel
                blockBuilder.Add(
                    F.Assignment(
                        F.Field(F.Me(), StateField, True),
                        F.AssignmentExpression(F.Local(CachedState, True), F.Literal(state.Number))))

                ' Emit Await yield point to be injected into PDB
                blockBuilder.Add(F.NoOp(NoOpStatementFlavor.AwaitYieldPoint))

                ' STMT:   Me.$awaiter = $awaiterTemp
                blockBuilder.Add(
                    F.Assignment(
                        F.Field(F.Me(), awaiterField, True),
                        If(awaiterField.Type = awaiterTemp.Type,
                           DirectCast(F.Local(awaiterTemp, False), BoundExpression),
                           F.Convert(awaiterFieldType, F.Local(awaiterTemp, False)))))

                ' NOTE: As it is mentioned above, Dev11 decides whether or not to use 'late binding' for 
                '       Await[Unsafe]OnCompleted based on the type of awaiter local variable
                '       See ResumableMethodLowerer::RewriteAwaitExpression(...) in ResumableMethodRewriter.cpp
                Dim isAwaiterTempImpliesLateBound As Boolean = awaiterType.IsObjectType

                Dim builderFieldAsRValue As BoundExpression = F.Field(F.Me(), _builder, False)

                If isAwaiterTempImpliesLateBound Then
                    ' STMT:   Dim dcast1 As ICriticalNotifyCompletion = TryCast(AwaiterLocalTemp, ICriticalNotifyCompletion)
                    ' STMT:   If dcast1 IsNot Nothing Then
                    ' STMT:       builder.AwaitUnsafeOnCompleted<ICriticalNotifyCompletion,TStateMachine>(ref dcast1, ref Me)
                    ' STMT:   Else
                    ' STMT:       Dim dcast2 As INotifyCompletion = DirectCast(AwaiterLocalTemp, INotifyCompletion)
                    ' STMT:       builder.AwaitOnCompleted<INotifyCompletion,TStateMachine>(ref dcast2, ref Me)
                    ' STMT:   End If
                    Dim asCriticalNotifyCompletion As LocalSymbol =
                        F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion))
                    Dim asNotifyCompletion As LocalSymbol =
                        F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion))

                    Dim awaiterTempAsRValue As BoundLocal = F.Local(awaiterTemp, False)
                    Dim criticalNotifyCompletionAsLValue As BoundLocal = F.Local(asCriticalNotifyCompletion, True)
                    Dim notifyCompletionAsLValue As BoundLocal = F.Local(asNotifyCompletion, True)

                    ' >>>>   dcast1 = TryCast(AwaiterLocalTemp, ICriticalNotifyCompletion)
                    Dim asCriticalNotifyCompletionAssignment As BoundStatement =
                        MakeAssignmentStatement(
                            F.TryCast(awaiterTempAsRValue, asCriticalNotifyCompletion.Type),
                            asCriticalNotifyCompletion)

                    ' >>>>   builder.AwaitUnsafeOnCompleted(Of TAwaiter,TSM)((ByRef) dcast1, (ByRef) Me)
                    Dim awaitUnsafeOnCompletedCall As BoundStatement =
                        F.ExpressionStatement(
                            _owner.GenerateMethodCall(
                                builderFieldAsRValue,
                                _owner._builderType,
                                "AwaitUnsafeOnCompleted",
                                ImmutableArray.Create(Of TypeSymbol)(asCriticalNotifyCompletion.Type, F.Me().Type),
                                {criticalNotifyCompletionAsLValue, F.ReferenceOrByrefMe()}))

                    ' >>>>   dcast2 = DirectCast(AwaiterLocalTemp, INotifyCompletion)
                    ' TODO: POSTPROCESS ASSIGNMENT
                    Dim asNotifyCompletionAssignment As BoundStatement =
                        MakeAssignmentStatement(
                            F.DirectCast(awaiterTempAsRValue, asNotifyCompletion.Type),
                            asNotifyCompletion)

                    ' >>>>   builder.AwaitOnCompleted(Of TAwaiter,TSM)((ByRef) dcast2, (ByRef) Me)
                    Dim awaitOnCompletedCall As BoundStatement =
                        F.ExpressionStatement(
                            _owner.GenerateMethodCall(
                                builderFieldAsRValue,
                                _owner._builderType,
                                "AwaitOnCompleted",
                                ImmutableArray.Create(Of TypeSymbol)(asNotifyCompletion.Type, F.Me().Type),
                                {notifyCompletionAsLValue, F.ReferenceOrByrefMe()}))

                    blockBuilder.Add(
                        F.Block(
                            ImmutableArray.Create(Of LocalSymbol)(asCriticalNotifyCompletion, asNotifyCompletion),
                            asCriticalNotifyCompletionAssignment,
                            F.If(
                                condition:=F.Not(F.ReferenceIsNothing(F.Local(asCriticalNotifyCompletion, False))),
                                thenClause:=awaitUnsafeOnCompletedCall,
                                elseClause:=F.Block(
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
                                F.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
                                useSiteDiagnostics:=Nothing))

                    blockBuilder.Add(
                        F.ExpressionStatement(
                            _owner.GenerateMethodCall(
                                builderFieldAsRValue,
                                _owner._builderType,
                                If(useUnsafeOnCompleted, "AwaitUnsafeOnCompleted", "AwaitOnCompleted"),
                                ImmutableArray.Create(Of TypeSymbol)(awaiterType, F.Me().Type),
                                {F.Local(awaiterTemp, True), F.ReferenceOrByrefMe()})))
                End If

                '----------------------------------------------
                ' Actual interruption point with return statement and 
                ' resume label, to be handled in codegen
                '
                '   RETURN
                blockBuilder.Add(F.Goto(_exitLabel))
                '----------------------------------------------
                '   RESUME LABEL
                blockBuilder.Add(F.Label(state.ResumeLabel))
                '----------------------------------------------

                ' Emit Await resume point to be injected into PDB
                blockBuilder.Add(F.NoOp(NoOpStatementFlavor.AwaitResumePoint))

                ' STMT:   Me.$State = CachedState = NotStartedStateMachine
                blockBuilder.Add(
                    F.Assignment(
                        F.Field(F.Me(), StateField, True),
                        F.AssignmentExpression(F.Local(CachedState, True), F.Literal(StateMachineStates.NotStartedStateMachine))))

                ' STMT:   $awaiterTemp = Me.$awaiter
                '  or   
                ' STMT:   $awaiterTemp = DirectCast(Me.$awaiter, AwaiterType) ' In case of late binding
                blockBuilder.Add(
                    F.Assignment(
                        F.Local(awaiterTemp, True),
                        If(awaiterTemp.Type = awaiterField.Type,
                           DirectCast(F.Field(F.Me(), awaiterField, False), BoundExpression),
                           F.Convert(awaiterTemp.Type, F.Field(F.Me(), awaiterField, False)))))

                ' Clear the field as it is not needed any more, also note that the local will 
                ' be cleared later after we call GetResult...
                ' STMT:   Me.$awaiter = Nothing
                blockBuilder.Add(
                    F.Assignment(
                        F.Field(F.Me(), awaiterField, True),
                        F.Null(awaiterField.Type)))

                Return F.Block(blockBuilder.ToImmutableAndFree())
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
