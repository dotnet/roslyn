' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        Public Overrides Function VisitLocalDeclaration(node As BoundLocalDeclaration) As BoundNode

            Dim localSymbol = node.LocalSymbol
            Dim staticLocalBackingFields As KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField) = Nothing
            Dim initializerOpt As BoundExpression = node.InitializerOpt
            Dim hasInitializer As Boolean = (initializerOpt IsNot Nothing)

            ' only if we have initializer we will produce something
            Dim result As BoundStatement = Nothing

            If localSymbol.IsStatic Then
                staticLocalBackingFields = CreateBackingFieldsForStaticLocal(localSymbol, hasInitializer)
            End If

            If hasInitializer Then
                ' Note: A variable declaration with "AsNew" and just one variable gets bound to a BoundLocalDeclaration instead of a
                ' BoundAsNewLocalDeclaration to simplify things.
                '
                ' We need to fill the replacement map in case the initializer is a object member initializer and does not need a
                ' temporary.
                Dim placeholder As BoundWithLValueExpressionPlaceholder = Nothing

                If initializerOpt.Kind = BoundKind.ObjectCreationExpression OrElse initializerOpt.Kind = BoundKind.NewT Then
                    Dim objectCreationExpression = DirectCast(initializerOpt, BoundObjectCreationExpressionBase)

                    If objectCreationExpression.InitializerOpt IsNot Nothing AndAlso
                        objectCreationExpression.InitializerOpt.Kind = BoundKind.ObjectInitializerExpression Then

                        Dim objectInitializer = DirectCast(objectCreationExpression.InitializerOpt, BoundObjectInitializerExpression)

                        If Not objectInitializer.CreateTemporaryLocalForInitialization Then
                            Debug.Assert(objectInitializer.PlaceholderOpt IsNot Nothing)

                            placeholder = objectInitializer.PlaceholderOpt
                            AddPlaceholderReplacement(placeholder, VisitExpressionNode(New BoundLocal(node.Syntax, localSymbol, localSymbol.Type)))
                        End If
                    End If
                End If

                ' Create an initializer for the local if the local is not a constant.
                If Not localSymbol.IsConst Then
                    Dim rewrittenInitializer As BoundExpression = VisitAndGenerateObjectCloneIfNeeded(initializerOpt)
                    result = RewriteLocalDeclarationAsInitializer(node, rewrittenInitializer, staticLocalBackingFields, placeholder Is Nothing)
                End If

                If placeholder IsNot Nothing Then
                    RemovePlaceholderReplacement(placeholder)
                End If
            End If

            Return result
        End Function

        ''' <summary>
        ''' Replaces local declaration with its initializer
        ''' Also marks resulting statement with seq point that matches original declaration.
        ''' </summary>
        Private Function RewriteLocalDeclarationAsInitializer(
            node As BoundLocalDeclaration,
            rewrittenInitializer As BoundExpression,
            staticLocalBackingFields As KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField),
            Optional objectInitializerNeedsTemporary As Boolean = True
        ) As BoundStatement
            Debug.Assert(rewrittenInitializer IsNot Nothing)

            Dim saveState As UnstructuredExceptionHandlingContext = LeaveUnstructuredExceptionHandlingContext(node)

            Dim result As BoundStatement

            ' If the rewritten initializer only rewrites into assignment operators, then just make it a statement and return it.
            ' This should only be the case for a AsNew declaration with a object member declaration
            If Not objectInitializerNeedsTemporary Then

                Debug.Assert((node.InitializerOpt Is Nothing AndAlso node.InitializedByAsNew) OrElse
                             (DirectCast(node.InitializerOpt, BoundObjectCreationExpressionBase).InitializerOpt.Kind = BoundKind.ObjectInitializerExpression AndAlso
                             Not DirectCast(DirectCast(node.InitializerOpt, BoundObjectCreationExpressionBase).InitializerOpt,
                                            BoundObjectInitializerExpression).CreateTemporaryLocalForInitialization))

                result = New BoundExpressionStatement(rewrittenInitializer.Syntax, rewrittenInitializer)

            Else
                result = New BoundExpressionStatement(
                    rewrittenInitializer.Syntax,
                    New BoundAssignmentOperator(
                        rewrittenInitializer.Syntax,
                        VisitExpressionNode(
                            New BoundLocal(
                                node.Syntax,
                                node.LocalSymbol,
                                node.LocalSymbol.Type
                            )
                        ),
                        rewrittenInitializer,
                        suppressObjectClone:=True,
                        type:=node.LocalSymbol.Type
                    )
                )
            End If

            If node.LocalSymbol.IsStatic Then
                result = EnforceStaticLocalInitializationSemantics(staticLocalBackingFields, result)
            End If

            RestoreUnstructuredExceptionHandlingContext(node, saveState)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                result = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, result, canThrow:=True)
            End If

            If Instrument(node) Then
                result = _instrumenterOpt.InstrumentLocalInitialization(node, result)
            End If

            Return result
        End Function

        Private Function CreateBackingFieldsForStaticLocal(localSymbol As LocalSymbol, hasInitializer As Boolean) As KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField)
            Debug.Assert(localSymbol.IsStatic)

            If _staticLocalMap Is Nothing Then
                _staticLocalMap = New Dictionary(Of LocalSymbol, KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField))(ReferenceEqualityComparer.Instance)
            End If

            Dim result As New KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField)(
                                                New SynthesizedStaticLocalBackingField(localSymbol, isValueField:=True, reportErrorForLongNames:=Not hasInitializer),
                                                If(hasInitializer, New SynthesizedStaticLocalBackingField(localSymbol, isValueField:=False, reportErrorForLongNames:=True), Nothing))

            If _emitModule IsNot Nothing Then
                _emitModule.AddSynthesizedDefinition(Me._topMethod.ContainingType, result.Key.GetCciAdapter())

                If result.Value IsNot Nothing Then
                    _emitModule.AddSynthesizedDefinition(Me._topMethod.ContainingType, result.Value.GetCciAdapter())
                End If
            End If

            _staticLocalMap.Add(localSymbol, result)

            Return result
        End Function

        Public Overrides Function VisitLocal(node As BoundLocal) As BoundNode
            If node.LocalSymbol.IsStatic Then
                Dim backingValueField As SynthesizedStaticLocalBackingField = _staticLocalMap(node.LocalSymbol).Key

                Return New BoundFieldAccess(node.Syntax,
                                            If(_topMethod.IsShared,
                                               Nothing,
                                               New BoundMeReference(node.Syntax, _topMethod.ContainingType)),
                                               backingValueField, isLValue:=node.IsLValue, type:=backingValueField.Type)
            Else
                Return MyBase.VisitLocal(node)
            End If
        End Function

        Private Function EnforceStaticLocalInitializationSemantics(
            staticLocalBackingFields As KeyValuePair(Of SynthesizedStaticLocalBackingField, SynthesizedStaticLocalBackingField),
            rewrittenInitialization As BoundStatement
        ) As BoundStatement
            Dim syntax = rewrittenInitialization.Syntax
            Dim objectType = GetSpecialTypeWithUseSiteDiagnostics(SpecialType.System_Object, syntax)
            Dim booleanType = GetSpecialTypeWithUseSiteDiagnostics(SpecialType.System_Boolean, syntax)

            Dim staticLocalInitFlag__ctor As MethodSymbol = Nothing
            Dim compareExchange As MethodSymbol = Nothing
            Dim state As FieldSymbol = Nothing
            Dim ctorIncompleteInitialization As MethodSymbol = Nothing

            ' Note, use of 'Or' rather than 'OrElse' in the following 'If' is intentional.
            ' The goal as to report as many errors as possible, simplifies testing as well.
            If (objectType.IsErrorType() OrElse booleanType.IsErrorType()) Or
               Not TryGetWellknownMember(staticLocalInitFlag__ctor, WellKnownMember.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__ctor, syntax) Or
               Not TryGetWellknownMember(compareExchange, WellKnownMember.System_Threading_Interlocked__CompareExchange_T, syntax) Or
               Not TryGetWellknownMember(state, WellKnownMember.Microsoft_VisualBasic_CompilerServices_StaticLocalInitFlag__State, syntax) Or
               Not TryGetWellknownMember(ctorIncompleteInitialization, WellKnownMember.Microsoft_VisualBasic_CompilerServices_IncompleteInitialization__ctor, syntax) Then
                Return rewrittenInitialization
            End If

            Dim statements = ArrayBuilder(Of BoundStatement).GetInstance()

            Dim flag = New BoundFieldAccess(syntax,
                                            If(_topMethod.IsShared,
                                               Nothing,
                                               New BoundMeReference(syntax, _topMethod.ContainingType)),
                                            staticLocalBackingFields.Value, isLValue:=True, type:=staticLocalBackingFields.Value.Type)

            Dim useSiteInfo = GetNewCompoundUseSiteInfo()
            Dim flagAsObject = New BoundDirectCast(syntax,
                                                   flag.MakeRValue(),
                                                   Conversions.ClassifyDirectCastConversion(flag.Type, objectType, useSiteInfo),
                                                   objectType)
            _diagnostics.Add(syntax, useSiteInfo)

            ' If flag Is Nothing
            '    Interlocked.CompareExchange(flag, New StaticLocalInitFlag, Nothing)
            ' End If

            Dim flagIsNothing = New BoundBinaryOperator(syntax,
                                                        BinaryOperatorKind.Is, flagAsObject,
                                                        New BoundLiteral(syntax, ConstantValue.Nothing, objectType),
                                                        False,
                                                        booleanType)

            Dim newFlagInstance = New BoundObjectCreationExpression(syntax,
                                                                    staticLocalInitFlag__ctor,
                                                                    ImmutableArray(Of BoundExpression).Empty,
                                                                    Nothing,
                                                                    flag.Type)

            Dim interlockedCompareExchangeFlagWithNewInstance =
                    New BoundCall(syntax,
                                  compareExchange.Construct(flag.Type),
                                  Nothing, Nothing,
                                  ImmutableArray.Create(Of BoundExpression)(flag, newFlagInstance, New BoundLiteral(syntax, ConstantValue.Nothing, flag.Type)),
                                  Nothing,
                                  flag.Type)

            Dim conditionalFlagInit = RewriteIfStatement(syntax, flagIsNothing, interlockedCompareExchangeFlagWithNewInstance.ToStatement(), Nothing, instrumentationTargetOpt:=Nothing)

            statements.Add(conditionalFlagInit)

            ' Initialization of a static local occurs the first time
            ' control reaches the declaration. To guarantee this,
            ' some code to guard the execution of the initializer
            ' is necessary. For a static local named "Var", this
            ' guard code utilizes a flag named Var$Init of type
            '
            ' Class Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag
            '     Public State As Short
            ' End Class
            '
            ' and is of the form:
            '
            ' SyncLock Var$Init
            '     Try
            '         If (Var$Init.State = 0) Then
            '             Var$Init.State = 2
            '             Var = Initialization
            '         Else If (Var$Init.State = 2) Then
            '             ' Recursion in initialization
            '             Throw New Microsoft.VisualBasic.CompilerServices.IncompleteInitialization()
            '         End If
            '     Finally
            '         Var$Init.State = 1
            '     End Try
            ' End SyncLock

            Dim boundLockTakenLocal As BoundLocal = Nothing
            Dim tempLockTakenAssignment As BoundStatement = Nothing

            Dim boundMonitorEnterCall As BoundStatement = GenerateMonitorEnter(syntax, flagAsObject, boundLockTakenLocal, tempLockTakenAssignment)
            Dim flagState = New BoundFieldAccess(syntax, flag, state, isLValue:=True, type:=state.Type)

            Dim two = New BoundLiteral(syntax, ConstantValue.Create(2S), flagState.Type)
            Dim flagStateIsZero = New BoundBinaryOperator(syntax,
                                                          BinaryOperatorKind.Equals,
                                                          flagState.MakeRValue(),
                                                          New BoundLiteral(syntax, ConstantValue.Default(ConstantValueTypeDiscriminator.Int16), flagState.Type),
                                                          False,
                                                          booleanType)

            Dim flagStateAssignTwo = New BoundAssignmentOperator(syntax, flagState, two, suppressObjectClone:=True).ToStatement()

            Dim flagStateIsTwo = New BoundBinaryOperator(syntax,
                                                         BinaryOperatorKind.Equals,
                                                         flagState.MakeRValue(),
                                                         two,
                                                         False,
                                                         booleanType)

            Dim throwIncompleteInitialization = New BoundThrowStatement(syntax,
                                                                        New BoundObjectCreationExpression(syntax,
                                                                                                          ctorIncompleteInitialization,
                                                                                                          ImmutableArray(Of BoundExpression).Empty,
                                                                                                          Nothing,
                                                                                                          ctorIncompleteInitialization.ContainingType))

            Dim conditionalValueInit =
                RewriteIfStatement(syntax,
                                   flagStateIsZero,
                                   New BoundStatementList(syntax, ImmutableArray.Create(flagStateAssignTwo, rewrittenInitialization)),
                                   RewriteIfStatement(syntax,
                                                      flagStateIsTwo,
                                                      throwIncompleteInitialization,
                                                      Nothing,
                                                      instrumentationTargetOpt:=Nothing),
                                   instrumentationTargetOpt:=Nothing)

            Dim locals As ImmutableArray(Of LocalSymbol)
            Dim statementsInTry As ImmutableArray(Of BoundStatement)

            If boundLockTakenLocal IsNot Nothing Then
                locals = ImmutableArray.Create(boundLockTakenLocal.LocalSymbol)
                statements.Add(tempLockTakenAssignment)
                statementsInTry = ImmutableArray.Create(boundMonitorEnterCall, conditionalValueInit)
            Else
                locals = ImmutableArray(Of LocalSymbol).Empty
                statements.Add(boundMonitorEnterCall)
                statementsInTry = ImmutableArray.Create(Of BoundStatement)(conditionalValueInit)
            End If

            Dim tryBody As BoundBlock = New BoundBlock(syntax,
                                                       Nothing,
                                                       ImmutableArray(Of LocalSymbol).Empty,
                                                       statementsInTry)

            Dim flagStateAssignOne = New BoundAssignmentOperator(syntax, flagState,
                                                                 New BoundLiteral(syntax, ConstantValue.Create(1S), flagState.Type),
                                                                 suppressObjectClone:=True).ToStatement()

            Dim monitorExit As BoundStatement = GenerateMonitorExit(syntax, flagAsObject, boundLockTakenLocal)

            Dim finallyBody As BoundBlock = New BoundBlock(syntax,
                                                           Nothing,
                                                           ImmutableArray(Of LocalSymbol).Empty,
                                                           ImmutableArray.Create(flagStateAssignOne, monitorExit))

            Dim tryFinally = New BoundTryStatement(syntax, tryBody, ImmutableArray(Of BoundCatchBlock).Empty, finallyBody, Nothing)

            statements.Add(tryFinally)

            Return New BoundBlock(syntax,
                                  Nothing,
                                  locals,
                                  statements.ToImmutableAndFree)
        End Function

    End Class
End Namespace
