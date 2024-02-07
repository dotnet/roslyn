' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Partial Friend Class AsyncMethodToClassRewriter
            Inherits StateMachineMethodToClassRewriter

            Private Shared Function NeedsSpill(node As BoundExpression) As Boolean
                If node Is Nothing Then
                    Return False
                End If

                Select Case node.Kind
                    Case BoundKind.SpillSequence
                        Return True

                    Case BoundKind.ArrayInitialization
                        Debug.Assert(False, "How BoundArrayInitialization got here? ArrayInitializerNeedsSpill(...) should be used instead")
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)

                    Case Else
                        Return False
                End Select
            End Function

            Private Shared Function NeedsSpill(nodes As ImmutableArray(Of BoundExpression)) As Boolean
                If Not nodes.IsEmpty Then
                    For Each node In nodes
                        If NeedsSpill(node) Then
                            Return True
                        End If
                    Next
                End If
                Return False
            End Function

            Private Shared Function ArrayInitializerNeedsSpill(node As BoundArrayInitialization) As Boolean
                If node Is Nothing Then
                    Return False
                End If

                For Each initializer In node.Initializers
                    If initializer.Kind = BoundKind.ArrayInitialization Then
                        If ArrayInitializerNeedsSpill(DirectCast(initializer, BoundArrayInitialization)) Then
                            Return True
                        End If
                    Else
                        If NeedsSpill(initializer) Then
                            Return True
                        End If
                    End If
                Next

                Return False
            End Function

            Private Structure ExpressionsWithReceiver
                Public ReadOnly ReceiverOpt As BoundExpression
                Public ReadOnly Arguments As ImmutableArray(Of BoundExpression)

                Public Sub New(receiverOpt As BoundExpression, arguments As ImmutableArray(Of BoundExpression))
                    Me.ReceiverOpt = receiverOpt
                    Me.Arguments = arguments
                End Sub
            End Structure

            ''' <summary>
            ''' Spill a list of expressions (e.g. the arguments of a method call).
            ''' 
            ''' The expressions are processed right-to-left. Once an expression has been found that contains an await
            ''' expression, all subsequent expressions are spilled.
            ''' 
            ''' Example:
            ''' 
            '''     (1 + 2, await t1, Goo(), await t2, 3 + 4)
            ''' 
            '''     becomes:
            ''' 
            '''     Spill(
            '''         spill1 = 1 + 2,
            '''         spill2 = await t1,
            '''         spill3 = Goo(),
            '''         (spill1, spill2, spill3, await t2, 3 + 4))
            ''' 
            ''' NOTE: Consider nested array initializers:
            ''' 
            '''     new int[] {
            '''         { 1, await t1 },
            '''         { 3, await t2 }
            '''     }
            ''' 
            ''' If the arguments of the top-level initializer had already been spilled, we would end up trying to spill
            ''' something like this:
            ''' 
            '''     new int[] {
            '''         Spill(
            '''             spill1 = 1,
            '''             { spill1, await t1 }),
            '''         Spill(
            '''             spill2 = 3,
            '''             { spill2, await t2 })
            '''     }
            ''' 
            ''' The normal rewriting would produce:
            ''' 
            '''     Spill(
            '''         spill1 = 1,
            '''         spill3 = { spill1, await t1 },
            '''         spill2 = 3,
            '''         int[] a = new int[] {
            '''             spill3,
            '''             { spill2, await t2 }))
            ''' 
            ''' Which is invalid, because spill3 does not have a type.
            ''' 
            ''' To solve this problem the expression list spilled descends into nested array initializers.
            ''' 
            ''' </summary>
            Private Function SpillExpressionList(<[In], Out> ByRef builder As SpillBuilder,
                                                 expressions As ImmutableArray(Of BoundExpression)
            ) As ImmutableArray(Of BoundExpression)
                Dim spillBuilders = ArrayBuilder(Of SpillBuilder).GetInstance()

                Dim newArgs As ImmutableArray(Of BoundExpression) = SpillArgumentListInner(expressions, spillBuilders, False)

                For index = spillBuilders.Count - 1 To 0 Step -1
                    builder.AddSpill(spillBuilders(index))
                    spillBuilders(index).Free()
                Next
                spillBuilders.Free()

                Debug.Assert(expressions.Length = newArgs.Length)
                Return newArgs
            End Function

            Private Function SpillExpressionList(<[In], Out> ByRef builder As SpillBuilder,
                                                 ParamArray expressions() As BoundExpression) As ImmutableArray(Of BoundExpression)
                Return SpillExpressionList(builder, expressions.AsImmutableOrNull)
            End Function

            Private Function SpillArgumentListInner(arguments As ImmutableArray(Of BoundExpression),
                                                    spillBuilders As ArrayBuilder(Of SpillBuilder),
                                                    <[In], Out> ByRef spilledFirstArg As Boolean) As ImmutableArray(Of BoundExpression)

                Dim newArgs(arguments.Length - 1) As BoundExpression
                For index = arguments.Length - 1 To 0 Step -1
                    Dim arg As BoundExpression = arguments(index)

                    If arg.Kind = BoundKind.ArrayInitialization Then
                        ' Descend into a nested array initializer:
                        Dim nestedInitializer = DirectCast(arg, BoundArrayInitialization)
                        Dim newInitializers As ImmutableArray(Of BoundExpression) =
                            SpillArgumentListInner(nestedInitializer.Initializers, spillBuilders, spilledFirstArg)
                        newArgs(index) = nestedInitializer.Update(newInitializers, nestedInitializer.Type)
                        Continue For
                    End If

                    Dim builder As New SpillBuilder()

                    Dim newExpression As BoundExpression

                    If Not spilledFirstArg Then

                        If arg.Kind = BoundKind.SpillSequence Then

                            ' We have found the right-most expression containing an await expression.
                            ' Save the await result to a temp local
                            spilledFirstArg = True
                            Dim spill = DirectCast(arg, BoundSpillSequence)
                            builder.AddSpill(spill)
                            newExpression = spill.ValueOpt
                            Debug.Assert(newExpression IsNot Nothing)

                        Else
                            ' We are to the right of any await-containing expressions. 
                            ' The args do not yet need to be spilled.
                            newExpression = arg
                        End If
                    Else
                        ' We are to the left of an await-containing expression. Spill the arg.
                        newExpression = SpillValue(arg,
                                                   isReceiver:=False,
                                                   evaluateSideEffects:=True,
                                                   builder:=builder)
                    End If

                    newArgs(index) = newExpression

                    If Not builder.IsEmpty Then
                        spillBuilders.Add(builder)
                    End If
                Next

                Return newArgs.AsImmutableOrNull
            End Function

            Private Function SpillValue(expr As BoundExpression, <[In], Out> ByRef builder As SpillBuilder) As BoundExpression
                Return SpillValue(expr, isReceiver:=False, evaluateSideEffects:=True, builder:=builder)
            End Function

            Private Function SpillValue(expr As BoundExpression, isReceiver As Boolean, evaluateSideEffects As Boolean, <[In], Out> ByRef builder As SpillBuilder) As BoundExpression
                If Unspillable(expr) Then
                    Return expr

                ElseIf isReceiver OrElse expr.IsLValue Then
                    Return SpillLValue(expr, isReceiver, evaluateSideEffects, builder)

                Else
                    Return SpillRValue(expr, builder)
                End If
            End Function

            Private Function SpillLValue(expr As BoundExpression, isReceiver As Boolean, evaluateSideEffects As Boolean, <[In], Out> ByRef builder As SpillBuilder, Optional isAssignmentTarget As Boolean = False) As BoundExpression
                Debug.Assert(expr IsNot Nothing)
                Debug.Assert(isReceiver OrElse expr.IsLValue)

                If isReceiver AndAlso expr.Type.IsReferenceType Then
                    Return SpillRValue(expr.MakeRValue(), builder)
                End If

                Select Case expr.Kind

                    Case BoundKind.Sequence
                        Dim sequence = DirectCast(expr, BoundSequence)

                        builder.AddLocals(sequence.Locals)

                        Dim sideEffects As ImmutableArray(Of BoundExpression) = sequence.SideEffects
                        If Not sideEffects.IsEmpty Then
                            For Each sideEffect In sideEffects
                                If NeedsSpill(sideEffect) Then
                                    Debug.Assert(sideEffect.Kind = BoundKind.SpillSequence)
                                    Dim spill = DirectCast(sideEffect, BoundSpillSequence)
                                    builder.AssumeFieldsIfNeeded(spill)
                                    builder.AddStatement(Me.RewriteSpillSequenceIntoBlock(spill, True))
                                Else
                                    builder.AddStatement(Me.F.ExpressionStatement(sideEffect))
                                End If
                            Next
                        End If

                        Return SpillLValue(sequence.ValueOpt, evaluateSideEffects, isReceiver, builder)

                    Case BoundKind.SpillSequence
                        Dim spill = DirectCast(expr, BoundSpillSequence)
                        builder.AddSpill(spill)
                        Debug.Assert(spill.ValueOpt IsNot Nothing)
                        Return SpillLValue(spill.ValueOpt, isReceiver, evaluateSideEffects, builder)

                    Case BoundKind.ArrayAccess
                        Dim array = DirectCast(expr, BoundArrayAccess)

                        Dim spilledExpression As BoundExpression = SpillRValue(array.Expression, builder)

                        Dim indices As ImmutableArray(Of BoundExpression) = array.Indices
                        Dim spilledIndices(indices.Length - 1) As BoundExpression
                        For i = 0 To indices.Length - 1
                            spilledIndices(i) = SpillRValue(indices(i), builder)
                        Next

                        array = array.Update(spilledExpression, spilledIndices.AsImmutableOrNull, array.IsLValue, array.Type)

                        ' An assignment target is only evaluated on write, so don't evaluate it's side effects
                        If evaluateSideEffects And Not isAssignmentTarget Then
                            builder.AddStatement(Me.F.ExpressionStatement(array))
                        End If

                        Return array

                    Case BoundKind.FieldAccess
                        Dim fieldAccess = DirectCast(expr, BoundFieldAccess)

                        If Unspillable(fieldAccess.ReceiverOpt) Then
                            Return fieldAccess
                        End If

                        ' An assignment target is only evaluated on write, so don't evaluate it's side effects, but do evaluate side effects of the receiver expression
                        ' Evaluating a field of a struct has no side effects, so only evaluate side effects of the receiver expression
                        Dim evaluateSideEffectsHere = evaluateSideEffects And Not isAssignmentTarget And fieldAccess.FieldSymbol.ContainingType.IsReferenceType

                        Dim newReceiver As BoundExpression = SpillValue(fieldAccess.ReceiverOpt,
                                                                        isReceiver:=True,
                                                                        evaluateSideEffects:=evaluateSideEffects And Not evaluateSideEffectsHere,
                                                                        builder:=builder)

                        fieldAccess = fieldAccess.Update(newReceiver,
                                                         fieldAccess.FieldSymbol,
                                                         fieldAccess.IsLValue,
                                                         fieldAccess.SuppressVirtualCalls,
                                                         constantsInProgressOpt:=Nothing,
                                                         fieldAccess.Type)

                        If evaluateSideEffectsHere Then
                            builder.AddStatement(Me.F.ExpressionStatement(fieldAccess))
                        End If

                        Return fieldAccess

                    Case BoundKind.ComplexConditionalAccessReceiver
                        Debug.Assert(isReceiver)
                        Debug.Assert(Not isAssignmentTarget)

                        Dim complexReceiver = DirectCast(expr, BoundComplexConditionalAccessReceiver)

                        Dim valueReceiverBuilder As New SpillBuilder()
                        Dim spilledValueReceiver As BoundExpression = SpillLValue(complexReceiver.ValueTypeReceiver, isReceiver, evaluateSideEffects, valueReceiverBuilder, isAssignmentTarget)
                        spilledValueReceiver = valueReceiverBuilder.BuildSequenceAndFree(Me.F, spilledValueReceiver)
                        Dim valueReceiverSpillSequence = TryCast(spilledValueReceiver, BoundSpillSequence)

                        Dim referenceReceiverBuilder As New SpillBuilder()
                        Dim spilledReferenceReceiver As BoundExpression = SpillLValue(complexReceiver.ReferenceTypeReceiver, isReceiver, evaluateSideEffects, referenceReceiverBuilder, isAssignmentTarget)
                        spilledReferenceReceiver = referenceReceiverBuilder.BuildSequenceAndFree(Me.F, spilledReferenceReceiver)
                        Dim referenceReceiverSpillSequence = TryCast(spilledReferenceReceiver, BoundSpillSequence)

                        If valueReceiverSpillSequence Is Nothing Then
                            If referenceReceiverSpillSequence Is Nothing Then
                                Return complexReceiver.Update(spilledValueReceiver, spilledReferenceReceiver, complexReceiver.Type)
                            Else
                                ' If condition `(object)default(T) != null` is true at execution time,
                                ' the T is a value type. And it is a reference type otherwise.
                                Dim isValueTypeCheck = Me.F.ReferenceIsNotNothing(Me.F.DirectCast(Me.F.DirectCast(Me.F.Null(), complexReceiver.Type),
                                                                                          Me.F.SpecialType(SpecialType.System_Object)))
                                builder.AssumeFieldsIfNeeded(referenceReceiverSpillSequence)
                                builder.AddLocals(referenceReceiverSpillSequence.Locals)
                                builder.AddStatement(Me.F.If(Me.F.Not(isValueTypeCheck), Me.F.StatementList(referenceReceiverSpillSequence.Statements)))

                                Return complexReceiver.Update(spilledValueReceiver, referenceReceiverSpillSequence.ValueOpt, complexReceiver.Type)
                            End If

                        ElseIf referenceReceiverSpillSequence Is Nothing Then
                            ' If condition `(object)default(T) != null` is true at execution time,
                            ' the T is a value type. And it is a reference type otherwise.
                            Dim isValueTypeCheck = Me.F.ReferenceIsNotNothing(Me.F.DirectCast(Me.F.DirectCast(Me.F.Null(), complexReceiver.Type),
                                                                                          Me.F.SpecialType(SpecialType.System_Object)))
                            builder.AssumeFieldsIfNeeded(valueReceiverSpillSequence)
                            builder.AddLocals(valueReceiverSpillSequence.Locals)
                            builder.AddStatement(Me.F.If(isValueTypeCheck, Me.F.StatementList(valueReceiverSpillSequence.Statements)))

                            Return complexReceiver.Update(valueReceiverSpillSequence.ValueOpt, spilledReferenceReceiver, complexReceiver.Type)
                        Else

                            ' If condition `(object)default(T) != null` is true at execution time,
                            ' the T is a value type. And it is a reference type otherwise.
                            Dim isValueTypeCheck = Me.F.ReferenceIsNotNothing(Me.F.DirectCast(Me.F.DirectCast(Me.F.Null(), complexReceiver.Type),
                                                                                          Me.F.SpecialType(SpecialType.System_Object)))
                            builder.AssumeFieldsIfNeeded(valueReceiverSpillSequence)
                            builder.AddLocals(valueReceiverSpillSequence.Locals)
                            builder.AssumeFieldsIfNeeded(referenceReceiverSpillSequence)
                            builder.AddLocals(referenceReceiverSpillSequence.Locals)
                            builder.AddStatement(Me.F.If(isValueTypeCheck, Me.F.StatementList(valueReceiverSpillSequence.Statements), Me.F.StatementList(referenceReceiverSpillSequence.Statements)))

                            Return complexReceiver.Update(valueReceiverSpillSequence.ValueOpt, referenceReceiverSpillSequence.ValueOpt, complexReceiver.Type)
                        End If

                    Case BoundKind.Local
                        ' Ref locals that appear as l-values in await-containing expressions get hoisted
                        Debug.Assert(Not DirectCast(expr, BoundLocal).LocalSymbol.IsByRef)
                        Return expr

                    Case BoundKind.Parameter
                        Debug.Assert(Me.Proxies.ContainsKey(DirectCast(expr, BoundParameter).ParameterSymbol))
                        Return expr

                    Case Else
                        Debug.Assert(Not expr.IsLValue, "stack spilling for lvalue: " + expr.Kind.ToString())
                        Return SpillRValue(expr, builder)
                End Select
            End Function

            Private Function SpillRValue(expr As BoundExpression, <[In], Out> ByRef builder As SpillBuilder) As BoundExpression
                Debug.Assert(Not expr.IsLValue)

                Select Case expr.Kind
                    Case BoundKind.Literal
                        ' TODO: do we want to do that for all/some other nodes with const values?
                        Return expr

                    Case BoundKind.SpillSequence
                        Dim spill = DirectCast(expr, BoundSpillSequence)
                        builder.AddSpill(spill)
                        Debug.Assert(spill.ValueOpt IsNot Nothing)
                        Return SpillRValue(spill.ValueOpt, builder)

                    Case BoundKind.ArrayInitialization
                        Dim arrayInit = DirectCast(expr, BoundArrayInitialization)
                        Return arrayInit.Update(SpillExpressionList(builder, arrayInit.Initializers), arrayInit.Type)

                    Case BoundKind.ConditionalAccessReceiverPlaceholder
                        If _conditionalAccessReceiverPlaceholderReplacementInfo Is Nothing OrElse
                           _conditionalAccessReceiverPlaceholderReplacementInfo.PlaceholderId <> DirectCast(expr, BoundConditionalAccessReceiverPlaceholder).PlaceholderId Then
                            Throw ExceptionUtilities.Unreachable
                        End If

                        _conditionalAccessReceiverPlaceholderReplacementInfo.IsSpilled = True
                        Return expr

                    Case BoundKind.ComplexConditionalAccessReceiver
                        Throw ExceptionUtilities.Unreachable

                    Case Else
                        ' Create a field for a spill
                        Dim spillField As FieldSymbol = Me._spillFieldAllocator.AllocateField(expr.Type)
                        Dim initialization As BoundStatement = Me.F.Assignment(Me.F.Field(Me.F.Me(), spillField, True), expr)

                        If expr.Kind = BoundKind.SpillSequence Then
                            initialization = Me.RewriteSpillSequenceIntoBlock(DirectCast(expr, BoundSpillSequence), True, initialization)
                        End If

                        builder.AddFieldWithInitialization(spillField, initialization)

                        Return Me.F.Field(Me.F.Me(), spillField, False)
                End Select

                Throw ExceptionUtilities.UnexpectedValue(expr.Kind)
            End Function

            Private Function RewriteSpillSequenceIntoBlock(spill As BoundSpillSequence,
                                                           addValueAsExpression As Boolean) As BoundBlock
                Return RewriteSpillSequenceIntoBlock(spill, addValueAsExpression, Array.Empty(Of BoundStatement))
            End Function

            Private Function RewriteSpillSequenceIntoBlock(spill As BoundSpillSequence,
                                                           addValueAsExpression As Boolean,
                                                           ParamArray additional() As BoundStatement) As BoundBlock

                Dim newStatements = ArrayBuilder(Of BoundStatement).GetInstance()
                newStatements.AddRange(spill.Statements)

                If addValueAsExpression AndAlso spill.ValueOpt IsNot Nothing Then
                    newStatements.Add(Me.F.ExpressionStatement(spill.ValueOpt))
                End If

                newStatements.AddRange(additional)

                ' Release references held by the spill temps:
                Dim fields As ImmutableArray(Of FieldSymbol) = spill.SpillFields
                For i = 0 To fields.Length - 1
                    Dim field As FieldSymbol = fields(i)

                    If TypeNeedsClearing(field.Type) Then
                        newStatements.Add(F.Assignment(F.Field(F.Me(), field, True), F.Null(field.Type)))
                    End If

                    Me._spillFieldAllocator.FreeField(field)
                Next

                Return Me.F.Block(spill.Locals, newStatements.ToImmutableAndFree())
            End Function

            Private Function TypeNeedsClearing(type As TypeSymbol) As Boolean
                Dim result As Boolean = False
                If Me._typesNeedingClearingCache.TryGetValue(type, result) Then
                    Return result
                End If

                If type.IsArrayType OrElse type.IsTypeParameter Then
                    Me._typesNeedingClearingCache.Add(type, True)
                    Return True
                End If

                If type.IsErrorType OrElse type.IsEnumType Then
                    Me._typesNeedingClearingCache.Add(type, False)
                    Return False
                End If

                ' Short-circuit common cases.
                Select Case type.SpecialType
                    Case SpecialType.System_Void,
                         SpecialType.System_Boolean,
                         SpecialType.System_Char,
                         SpecialType.System_SByte,
                         SpecialType.System_Byte,
                         SpecialType.System_Int16,
                         SpecialType.System_UInt16,
                         SpecialType.System_Int32,
                         SpecialType.System_UInt32,
                         SpecialType.System_Int64,
                         SpecialType.System_UInt64,
                         SpecialType.System_Decimal,
                         SpecialType.System_Single,
                         SpecialType.System_Double,
                         SpecialType.System_IntPtr,
                         SpecialType.System_UIntPtr,
                         SpecialType.System_TypedReference,
                         SpecialType.System_ArgIterator,
                         SpecialType.System_RuntimeArgumentHandle
                        result = False

                    Case SpecialType.System_Object,
                         SpecialType.System_String
                        result = True

                    Case Else
                        Dim namedType = TryCast(type, NamedTypeSymbol)
                        If namedType IsNot Nothing AndAlso namedType.IsGenericType Then
                            result = True
                            Exit Select
                        End If

                        Debug.Assert(Not type.IsTypeParameter)
                        Debug.Assert(Not type.IsEnumType)

                        If type.TypeKind <> TypeKind.Structure Then
                            result = True
                            Exit Select
                        End If

                        Debug.Assert(namedType IsNot Nothing, "Structure which is not a NamedTypeSymbol??")

                        ' Prevent cycles
                        Me._typesNeedingClearingCache.Add(type, True)
                        result = False

                        ' For structures, go through the fields
                        For Each member In type.GetMembersUnordered
                            If Not member.IsShared Then
                                Select Case member.Kind
                                    Case SymbolKind.Event
                                        If TypeNeedsClearing(DirectCast(member, EventSymbol).AssociatedField.Type) Then
                                            result = True
                                            Exit Select
                                        End If

                                    Case SymbolKind.Field
                                        If TypeNeedsClearing(DirectCast(member, FieldSymbol).Type) Then
                                            result = True
                                            Exit Select
                                        End If
                                End Select
                            End If
                        Next
                        Me._typesNeedingClearingCache.Remove(type)

                        ' Note: structures with cycles will *NOT* be cleared
                End Select

                Me._typesNeedingClearingCache.Add(type, result)
                Return result
            End Function

            Private Shared Function Unspillable(node As BoundExpression) As Boolean
                If node Is Nothing Then
                    Return True
                End If

                Select Case node.Kind
                    Case BoundKind.Literal
                        Return True

                    Case BoundKind.MeReference
                        Return True

                    Case BoundKind.MyBaseReference,
                         BoundKind.MyClassReference
                        Throw ExceptionUtilities.UnexpectedValue(node.Kind)

                    Case BoundKind.TypeExpression
                        Return True

                    Case Else
                        Return False
                End Select
            End Function

            Private Shared Function SpillSequenceWithNewValue(spill As BoundSpillSequence, newValue As BoundExpression) As BoundSpillSequence
                Return spill.Update(spill.Locals, spill.SpillFields, spill.Statements, newValue, newValue.Type)
            End Function

        End Class
    End Class

End Namespace
