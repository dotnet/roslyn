' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter

        ' returns receiver, or Nothing literal otherwise
        Private Function LateMakeReceiverArgument(node As VisualBasicSyntaxNode,
                                                rewrittenReceiver As BoundExpression,
                                                objectType As TypeSymbol) As BoundExpression
            Debug.Assert(objectType.IsObjectType)

            If rewrittenReceiver Is Nothing Then
                Return MakeNullLiteral(node, objectType)
            Else
                If Not rewrittenReceiver.Type.IsObjectType Then
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim convKind = Conversions.ClassifyDirectCastConversion(rewrittenReceiver.Type, objectType, useSiteDiagnostics)
                    _diagnostics.Add(node, useSiteDiagnostics)
                    rewrittenReceiver = New BoundDirectCast(node, rewrittenReceiver, convKind, objectType)
                End If

                Return rewrittenReceiver
            End If
        End Function

        ' returns GetType(Type) if receiver is nothing, or Nothing literal otherwise
        Private Function LateMakeContainerArgument(node As VisualBasicSyntaxNode,
                                                       receiver As BoundExpression,
                                                       containerType As TypeSymbol,
                                                       typeType As TypeSymbol) As BoundExpression

            If receiver IsNot Nothing Then
                Return MakeNullLiteral(node, typeType)
            Else
                Return MakeGetTypeExpression(node, containerType, typeType)
            End If
        End Function


        ' returns "New Type(){GetType(Type1), GetType(Type2) ...} or Nothing literal
        Private Function LateMakeTypeArgumentArrayArgument(node As VisualBasicSyntaxNode, arguments As BoundTypeArguments, typeArrayType As TypeSymbol) As BoundExpression
            If arguments Is Nothing Then
                Return MakeNullLiteral(node, typeArrayType)
            Else
                Return MakeArrayOfGetTypeExpressions(node, arguments.Arguments, typeArrayType)
            End If
        End Function

        ' returns "New Boolean(length){}
        Private Function LateMakeCopyBackArray(node As VisualBasicSyntaxNode,
                                               flags As ImmutableArray(Of Boolean),
                                               booleanArrayType As TypeSymbol) As BoundExpression

            Dim arrayType = DirectCast(booleanArrayType, ArrayTypeSymbol)
            Dim booleanType = arrayType.ElementType

            Debug.Assert(arrayType.IsSZArray)
            Debug.Assert(booleanType.IsBooleanType)

            If flags.IsDefaultOrEmpty Then
                Return MakeNullLiteral(node, booleanArrayType)

            Else
                Dim intType = Me.GetSpecialType(SpecialType.System_Int32)
                Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Create(flags.Length), intType)

                Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance
                For Each f In flags
                    initializers.Add(MakeBooleanLiteral(node, f, booleanType))
                Next

                Dim initializer = New BoundArrayInitialization(node, initializers.ToImmutableAndFree, Nothing)

                Return New BoundArrayCreation(node, ImmutableArray.Create(bounds), initializer, booleanArrayType)
            End If
        End Function

        Private Function LateMakeArgumentArrayArgument(node As VisualBasicSyntaxNode,
                                               rewrittenArguments As ImmutableArray(Of BoundExpression),
                                               argumentNames As ImmutableArray(Of String),
                                               objectArrayType As TypeSymbol) As BoundExpression

            If argumentNames.IsDefaultOrEmpty Then
                Return LateMakeArgumentArrayArgumentNoNamed(node, rewrittenArguments, objectArrayType)
            End If

            Dim namedArgNum As Integer = 0
            Dim regularArgNum As Integer

            For Each name In argumentNames
                If name IsNot Nothing Then
                    namedArgNum += 1
                End If
            Next
            regularArgNum = rewrittenArguments.Length - namedArgNum

            Dim arrayType = DirectCast(objectArrayType, ArrayTypeSymbol)
            Dim objectType = arrayType.ElementType

            Debug.Assert(arrayType.IsSZArray)
            Debug.Assert(objectType.IsObjectType)
            Debug.Assert(Not rewrittenArguments.IsDefaultOrEmpty)

            Dim intType = Me.GetSpecialType(SpecialType.System_Int32)
            Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Create(rewrittenArguments.Length), intType)

            Dim arrayCreation = New BoundArrayCreation(node, ImmutableArray.Create(bounds), Nothing, objectArrayType)
            Dim arrayTemp As LocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, arrayCreation.Type, SynthesizedLocalKind.LoweringTemp)
            Dim arrayTempRef = New BoundLocal(node, arrayTemp, arrayTemp.Type)

            Dim arrayInit = New BoundAssignmentOperator(node, arrayTempRef, arrayCreation, suppressObjectClone:=True)

            Dim sideeffects = ArrayBuilder(Of BoundExpression).GetInstance
            sideeffects.Add(arrayInit)

            arrayTempRef = arrayTempRef.MakeRValue

            For i As Integer = 0 To rewrittenArguments.Length - 1
                Dim argument = rewrittenArguments(i)
                argument = argument.MakeRValue
                If Not argument.Type.IsObjectType Then
                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                    Dim convKind = Conversions.ClassifyDirectCastConversion(argument.Type, objectType, useSiteDiagnostics)
                    _diagnostics.Add(node, useSiteDiagnostics)
                    argument = New BoundDirectCast(node, argument, convKind, objectType)
                End If

                ' named arguments are actually passed first in the array
                Dim indexVal = If(i < regularArgNum,
                               namedArgNum + i,
                               i - regularArgNum)

                Dim indexExpr As BoundExpression = New BoundLiteral(node, ConstantValue.Create(indexVal), intType)
                Dim indices = ImmutableArray.Create(indexExpr)

                Dim arrayElement As BoundExpression = New BoundArrayAccess(node,
                                                                    arrayTempRef,
                                                                    indices,
                                                                    objectType)

                Dim elementAssignment = New BoundAssignmentOperator(node, arrayElement, argument, suppressObjectClone:=True)
                sideeffects.Add(elementAssignment)
            Next

            Return New BoundSequence(node, ImmutableArray.Create(arrayTemp), sideeffects.ToImmutableAndFree, arrayTempRef, arrayTempRef.Type)
        End Function

        ' returns "New object(){Arg1, Arg2 ..., value}
        Private Function LateMakeSetArgumentArrayArgument(node As VisualBasicSyntaxNode,
                                            rewrittenValue As BoundExpression,
                                            rewrittenArguments As ImmutableArray(Of BoundExpression),
                                            argumentNames As ImmutableArray(Of String),
                                            objectArrayType As TypeSymbol) As BoundExpression

            Dim arrayType = DirectCast(objectArrayType, ArrayTypeSymbol)
            Dim objectType = arrayType.ElementType

            Debug.Assert(arrayType.IsSZArray)
            Debug.Assert(objectType.IsObjectType)

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            If Not rewrittenValue.Type.IsObjectType Then
                Dim convKind = Conversions.ClassifyDirectCastConversion(rewrittenValue.Type, objectType, useSiteDiagnostics)
                _diagnostics.Add(node, useSiteDiagnostics)
                rewrittenValue = New BoundDirectCast(node, rewrittenValue, convKind, objectType)
            End If

            If argumentNames.IsDefaultOrEmpty Then
                If rewrittenArguments.IsDefaultOrEmpty Then
                    rewrittenArguments = ImmutableArray.Create(rewrittenValue)
                ElseIf argumentNames.IsDefaultOrEmpty Then
                    rewrittenArguments = rewrittenArguments.Add(rewrittenValue)
                End If

                Return LateMakeArgumentArrayArgumentNoNamed(node, rewrittenArguments, objectArrayType)
            End If

            ' have named arguments, need to reshuffle {named}{regular}{value}

            Dim namedArgNum As Integer = 0
            Dim regularArgNum As Integer

            For Each name In argumentNames
                If name IsNot Nothing Then
                    namedArgNum += 1
                End If
            Next
            regularArgNum = rewrittenArguments.Length - namedArgNum

            Debug.Assert(Not rewrittenArguments.IsDefaultOrEmpty)

            Dim intType = Me.GetSpecialType(SpecialType.System_Int32)
            Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Create(rewrittenArguments.Length + 1), intType)

            Dim arrayCreation = New BoundArrayCreation(node, ImmutableArray.Create(bounds), Nothing, objectArrayType)
            Dim arrayTemp As LocalSymbol = New SynthesizedLocal(Me._currentMethodOrLambda, arrayCreation.Type, SynthesizedLocalKind.LoweringTemp)
            Dim arrayTempRef = New BoundLocal(node, arrayTemp, arrayTemp.Type)

            Dim arrayInit = New BoundAssignmentOperator(node, arrayTempRef, arrayCreation, suppressObjectClone:=True)

            Dim sideeffects = ArrayBuilder(Of BoundExpression).GetInstance
            sideeffects.Add(arrayInit)

            arrayTempRef = arrayTempRef.MakeRValue

            For i As Integer = 0 To rewrittenArguments.Length - 1
                Dim argument = rewrittenArguments(i)
                argument = argument.MakeRValue
                If Not argument.Type.IsObjectType Then
                    Dim convKind = Conversions.ClassifyDirectCastConversion(argument.Type, objectType, useSiteDiagnostics)
                    _diagnostics.Add(argument, useSiteDiagnostics)
                    argument = New BoundDirectCast(node, argument, convKind, objectType)
                End If

                ' named arguments are actually passed first in the array, then regular, then assignment value
                Dim indexVal = If(i < regularArgNum,
                               namedArgNum + i,
                               i - regularArgNum)

                Dim elementAssignment = LateAssignToArrayElement(node, arrayTempRef, indexVal, argument, intType)
                sideeffects.Add(elementAssignment)
            Next

            ' value goes last
            If Not rewrittenValue.Type.IsObjectType Then
                Dim convKind = Conversions.ClassifyDirectCastConversion(rewrittenValue.Type, objectType, useSiteDiagnostics)
                _diagnostics.Add(rewrittenValue, useSiteDiagnostics)
                rewrittenValue = New BoundDirectCast(node, rewrittenValue, convKind, objectType)
            End If

            Dim valueElementAssignment = LateAssignToArrayElement(node, arrayTempRef, rewrittenArguments.Length, rewrittenValue, intType)
            sideeffects.Add(valueElementAssignment)

            Return New BoundSequence(node, ImmutableArray.Create(arrayTemp), sideeffects.ToImmutableAndFree, arrayTempRef, arrayTempRef.Type)
        End Function

        Private Function LateAssignToArrayElement(node As VisualBasicSyntaxNode,
                                                  arrayRef As BoundExpression,
                                                  index As Integer,
                                                  value As BoundExpression,
                                                  intType As TypeSymbol) As BoundExpression

            Dim indexExpr As BoundExpression = New BoundLiteral(node, ConstantValue.Create(index), intType)
            Dim arrayElement As BoundExpression = New BoundArrayAccess(node,
                                                                arrayRef,
                                                                ImmutableArray.Create(indexExpr),
                                                                value.Type)

            Return New BoundAssignmentOperator(node, arrayElement, value, suppressObjectClone:=True)
        End Function

        ' returns "New object(){Arg1, Arg2 ...}
        Private Function LateMakeArgumentArrayArgumentNoNamed(node As VisualBasicSyntaxNode,
                                                       rewrittenArguments As ImmutableArray(Of BoundExpression),
                                                       objectArrayType As TypeSymbol) As BoundExpression

            Dim arrayType = DirectCast(objectArrayType, ArrayTypeSymbol)
            Dim objectType = arrayType.ElementType
            Dim intType = Me.GetSpecialType(SpecialType.System_Int32)

            Debug.Assert(arrayType.IsSZArray)
            Debug.Assert(objectType.IsObjectType)

            If rewrittenArguments.IsDefaultOrEmpty Then
                Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Default(ConstantValueTypeDiscriminator.Int32), intType)
                Return New BoundArrayCreation(node, ImmutableArray.Create(bounds), Nothing, objectArrayType)

            Else
                Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Create(rewrittenArguments.Length), intType)

                Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance
                For Each argument In rewrittenArguments
                    argument = argument.MakeRValue
                    If Not argument.Type.IsObjectType Then
                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                        Dim convKind = Conversions.ClassifyDirectCastConversion(argument.Type, objectType, useSiteDiagnostics)
                        _diagnostics.Add(argument, useSiteDiagnostics)
                        argument = New BoundDirectCast(node, argument, convKind, objectType)
                    End If

                    initializers.Add(argument)
                Next

                'TODO: Dev11 does GetobjectValue here is it needed? Should array initialization do it in general?
                Dim initializer = New BoundArrayInitialization(node, initializers.ToImmutableAndFree, Nothing)

                Return New BoundArrayCreation(node, ImmutableArray.Create(bounds), initializer, objectArrayType)
            End If
        End Function

        ' returns "New object(){name1, name2 ...} or Nothing literal
        Private Function LateMakeArgumentNameArrayArgument(node As VisualBasicSyntaxNode,
                                                       argumentNames As ImmutableArray(Of String),
                                                       stringArrayType As TypeSymbol) As BoundExpression

            Dim arrayType = DirectCast(stringArrayType, ArrayTypeSymbol)
            Dim stringType = arrayType.ElementType

            Debug.Assert(arrayType.IsSZArray)
            Debug.Assert(stringType.IsStringType)

            If argumentNames.IsDefaultOrEmpty Then
                Return MakeNullLiteral(node, stringArrayType)

            Else
                Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance
                For Each name In argumentNames
                    If name IsNot Nothing Then
                        initializers.Add(MakeStringLiteral(node, name, stringType))
                    Else
                        Debug.Assert(initializers.Count = 0, "once we have named argument all following arguments must be named")
                    End If
                Next

                Dim initializer = New BoundArrayInitialization(node, initializers.ToImmutableAndFree, Nothing)

                Dim intType = Me.GetSpecialType(SpecialType.System_Int32)
                Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Create(initializer.Initializers.Length), intType)

                Return New BoundArrayCreation(node, ImmutableArray.Create(bounds), initializer, stringArrayType)
            End If
        End Function


        ' Makes expressions like
        '  if(copyBackArrayRef[argNum], assignmentTarget := valueArrayRef[argNum] , Nothing)
        '
        ' NOTE: assignmentTarget comes in not yet lowered.
        Private Function LateMakeConditionalCopyback(assignmentTarget As BoundExpression,
                                     valueArrayRef As BoundExpression,
                                     copyBackArrayRef As BoundExpression,
                                     argNum As Integer) As BoundExpression

            Dim syntax = assignmentTarget.Syntax
            Dim intType = Me.GetSpecialType(SpecialType.System_Int32)

            Dim index As BoundExpression = New BoundLiteral(syntax, ConstantValue.Create(argNum), intType)
            Dim indices = ImmutableArray.Create(index)

            Dim booleanType = DirectCast(copyBackArrayRef.Type, ArrayTypeSymbol).ElementType
            Dim condition As BoundExpression = New BoundArrayAccess(syntax,
                                                                    copyBackArrayRef,
                                                                    ImmutableArray.Create(index),
                                                                    booleanType).MakeRValue

            Dim objectType = DirectCast(valueArrayRef.Type, ArrayTypeSymbol).ElementType
            Dim value As BoundExpression = New BoundArrayAccess(syntax,
                                            valueArrayRef,
                                            indices,
                                            objectType).MakeRValue

            Dim targetType = assignmentTarget.Type

            If Not targetType.IsSameTypeIgnoringCustomModifiers(objectType) Then
                ' // Call ChangeType to perform a latebound conversion
                Dim changeTypeMethod As MethodSymbol = Nothing
                If TryGetWellknownMember(changeTypeMethod,
                                         WellKnownMember.Microsoft_VisualBasic_CompilerServices_Conversions__ChangeType, syntax) Then

                    ' value = ChangeType(value, GetType(targetType))

                    Dim getTypeExpr = New BoundGetType(syntax, New BoundTypeExpression(syntax, targetType), changeTypeMethod.Parameters(1).Type)

                    'TODO: should we suppress object clone here? Dev11 does not.
                    value = New BoundCall(syntax,
                                          changeTypeMethod,
                                          Nothing,
                                          Nothing,
                                          ImmutableArray.Create(Of BoundExpression)(value, getTypeExpr),
                                          Nothing,
                                          False,
                                          objectType)

                End If

                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                Dim conversionKind = Conversions.ClassifyDirectCastConversion(objectType, targetType, useSiteDiagnostics)
                Debug.Assert(useSiteDiagnostics.IsNullOrEmpty)
                value = New BoundDirectCast(syntax, value, conversionKind, targetType)
            End If

            Dim voidNoOp As BoundExpression = New BoundSequence(syntax,
                                                            ImmutableArray(Of LocalSymbol).Empty,
                                                            ImmutableArray(Of BoundExpression).Empty,
                                                            Nothing,
                                                            Me.GetSpecialType(SpecialType.System_Void))

            Dim voidAssignment As BoundExpression = LateMakeCopyback(syntax,
                                                                assignmentTarget,
                                                                value)

            Dim result = MakeTernaryConditionalExpression(syntax,
                                                          condition,
                                                          voidAssignment,
                                                          voidNoOp)

            Return VisitExpressionNode(result)
        End Function

        Private Function LateMakeCopyback(syntax As VisualBasicSyntaxNode,
                                          assignmentTarget As BoundExpression,
                                          convertedValue As BoundExpression) As BoundExpression

            If assignmentTarget.Kind = BoundKind.LateMemberAccess Then
                ' objExpr.foo = bar
                Dim memberAccess = DirectCast(assignmentTarget, BoundLateMemberAccess)
                Return LateSet(syntax,
                               memberAccess,
                               convertedValue,
                               argExpressions:=Nothing,
                               argNames:=Nothing,
                               isCopyBack:=True)

            ElseIf assignmentTarget.Kind = BoundKind.LateInvocation Then
                Dim invocation = DirectCast(assignmentTarget, BoundLateInvocation)

                If invocation.Member.Kind = BoundKind.LateMemberAccess Then
                    Dim memberAccess = DirectCast(invocation.Member, BoundLateMemberAccess)
                    ' objExpr.foo(args) = bar
                    Return LateSet(syntax,
                               memberAccess,
                               convertedValue,
                               argExpressions:=invocation.ArgumentsOpt,
                               argNames:=invocation.ArgumentNamesOpt,
                               isCopyBack:=True)
                Else
                    ' objExpr(args) = bar
                    Return LateIndexSet(syntax,
                                        invocation,
                                        convertedValue,
                                        isCopyBack:=True)
                End If
            End If

            ' TODO: should we suppress object clone here? Dev11 does not suppress.
            Dim assignment As BoundExpression = New BoundAssignmentOperator(syntax,
                                                                            assignmentTarget,
                                                                            GenerateObjectCloneIfNeeded(convertedValue),
                                                                            suppressObjectClone:=True)

            Dim voidAssignment = New BoundSequence(syntax,
                                                    ImmutableArray(Of LocalSymbol).Empty,
                                                    ImmutableArray.Create(assignment),
                                                    Nothing,
                                                    Me.GetSpecialType(SpecialType.System_Void))

            Return voidAssignment
        End Function

        Private Function LateIndexGet(node As BoundLateInvocation,
                            receiverExpr As BoundExpression,
                            argExpressions As ImmutableArray(Of BoundExpression)) As BoundExpression

            Debug.Assert(node.Member.Kind <> BoundKind.LateMemberAccess)

            ' We have 
            '   objExpr(arg0, ..., argN) 

            Dim syntax = node.Syntax

            ' expr = o.Foo<TypeParam>
            ' emit as:
            '     LateGet(invocation.Member, 
            '         invocation.Arguments, 
            '         invocation.ArgumentNames)
            '
            '
            Dim lateIndexGetMethod As MethodSymbol = Nothing
            If Not Me.TryGetWellknownMember(lateIndexGetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexGet, syntax) Then
                Return node
            End If

            ' arg0  "object Instance"
            Dim receiver As BoundExpression = LateMakeReceiverArgument(syntax, receiverExpr.MakeRValue, lateIndexGetMethod.Parameters(0).Type)
            ' arg1  "object[] Arguments"
            Dim arguments As BoundExpression = LateMakeArgumentArrayArgument(node.Syntax, argExpressions, node.ArgumentNamesOpt, lateIndexGetMethod.Parameters(1).Type)
            ' arg2  "string[] ArgumentNames"
            Dim argumentNames As BoundExpression = LateMakeArgumentNameArrayArgument(syntax, node.ArgumentNamesOpt, lateIndexGetMethod.Parameters(2).Type)

            Dim callArgs = ImmutableArray.Create(Of BoundExpression)(receiver,
                                                                    arguments,
                                                                    argumentNames)


            Dim callerInvocation As BoundExpression = New BoundCall(syntax, lateIndexGetMethod, Nothing, Nothing, callArgs, Nothing, True, lateIndexGetMethod.ReturnType)

            Return callerInvocation
        End Function

        Private Function LateSet(syntax As VisualBasicSyntaxNode,
                                memberAccess As BoundLateMemberAccess,
                                assignmentValue As BoundExpression,
                                argExpressions As ImmutableArray(Of BoundExpression),
                                argNames As ImmutableArray(Of String),
                                isCopyBack As Boolean) As BoundExpression

            Debug.Assert(memberAccess.AccessKind = LateBoundAccessKind.Set)

            ' TODO: May need a special case for parameters. 
            '       A readonly parameter (in queries) may be not IsLValue, but Dev11 may treat it as an LValue still.

            ' NOTE: Dev11 passes "false" when access is static. We will do the same. 
            '       It makes no difference at runtime since there is no base.
            Dim baseIsNotLValue As Boolean = memberAccess.ReceiverOpt IsNot Nothing AndAlso Not memberAccess.ReceiverOpt.IsLValue

            Dim lateSetMethod As MethodSymbol = Nothing
            Dim isComplex As Boolean = isCopyBack OrElse baseIsNotLValue

            If isComplex Then
                If Not Me.TryGetWellknownMember(lateSetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSetComplex, syntax) Then
                    ' need to return something void
                    Return New BoundSequence(syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(memberAccess), Nothing, Me.GetSpecialType(SpecialType.System_Void))
                End If
            Else
                If Not Me.TryGetWellknownMember(lateSetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateSet, syntax) Then
                    ' need to return something void
                    Return New BoundSequence(syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(memberAccess), Nothing, Me.GetSpecialType(SpecialType.System_Void))
                End If
            End If

            ' arg0  "object Instance"
            Dim receiver As BoundExpression = LateMakeReceiverArgument(syntax,
                                                                       If(memberAccess.ReceiverOpt IsNot Nothing, memberAccess.ReceiverOpt.MakeRValue, Nothing),
                                                                       lateSetMethod.Parameters(0).Type)
            ' arg1  "Type Type"
            Dim containerType As BoundExpression = LateMakeContainerArgument(syntax, memberAccess.ReceiverOpt, memberAccess.ContainerTypeOpt, lateSetMethod.Parameters(1).Type)
            ' arg2  "string MemberName"
            Dim name As BoundLiteral = MakeStringLiteral(syntax, memberAccess.NameOpt, lateSetMethod.Parameters(2).Type)
            ' arg3  "object[] Arguments"
            Dim arguments As BoundExpression = LateMakeSetArgumentArrayArgument(syntax, assignmentValue, argExpressions, argNames, lateSetMethod.Parameters(3).Type)
            ' arg4  "string[] ArgumentNames"
            Dim argumentNames As BoundExpression = LateMakeArgumentNameArrayArgument(syntax, argNames, lateSetMethod.Parameters(4).Type)
            ' arg5  "Type[] TypeArguments"
            Dim typeArguments As BoundExpression = LateMakeTypeArgumentArrayArgument(syntax, memberAccess.TypeArgumentsOpt, lateSetMethod.Parameters(5).Type)

            Dim callArgs As ImmutableArray(Of BoundExpression)
            If Not isComplex Then
                callArgs = ImmutableArray.Create(Of BoundExpression)(receiver,
                                                                    containerType,
                                                                    name,
                                                                    arguments,
                                                                    argumentNames,
                                                                    typeArguments)
            Else
                ' arg6  "bool OptimisticSet"
                Dim optimisticSet As BoundExpression = MakeBooleanLiteral(syntax, isCopyBack, lateSetMethod.Parameters(6).Type)
                ' arg7  "bool RValueBase"
                Dim rValueBase As BoundExpression = MakeBooleanLiteral(syntax, baseIsNotLValue, lateSetMethod.Parameters(7).Type)

                callArgs = ImmutableArray.Create(Of BoundExpression)(receiver,
                                                                    containerType,
                                                                    name,
                                                                    arguments,
                                                                    argumentNames,
                                                                    typeArguments,
                                                                    optimisticSet,
                                                                    rValueBase)
            End If

            Return New BoundCall(syntax, lateSetMethod, Nothing, Nothing, callArgs, Nothing, True, lateSetMethod.ReturnType)
        End Function

        Private Function LateIndexSet(syntax As VisualBasicSyntaxNode,
                                      invocation As BoundLateInvocation,
                                      assignmentValue As BoundExpression,
                                      isCopyBack As Boolean) As BoundExpression

            Debug.Assert(invocation.AccessKind = LateBoundAccessKind.Set)

            ' TODO: May need a special case for parameters. 
            '       A readonly parameter (in queries) may be not IsLValue, but Dev11 may treat it as an LValue still.

            ' NOTE: Dev11 passes "false" when access is static. We will do the same. 
            '       It makes no difference at runtime since there is no base.
            Dim baseIsNotLValue As Boolean = invocation.Member IsNot Nothing AndAlso Not invocation.Member.IsLValue

            Dim lateIndexSetMethod As MethodSymbol = Nothing
            Dim isComplex As Boolean = isCopyBack OrElse baseIsNotLValue

            If isComplex Then
                If Not Me.TryGetWellknownMember(lateIndexSetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSetComplex, syntax) Then
                    ' need to return something void
                    Return New BoundSequence(syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(invocation), Nothing, Me.GetSpecialType(SpecialType.System_Void))
                End If
            Else
                If Not Me.TryGetWellknownMember(lateIndexSetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateIndexSet, syntax) Then
                    ' need to return something void
                    Return New BoundSequence(syntax, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(Of BoundExpression)(invocation), Nothing, Me.GetSpecialType(SpecialType.System_Void))
                End If
            End If


            ' arg0  "object Instance"
            Dim receiver As BoundExpression = LateMakeReceiverArgument(syntax, invocation.Member.MakeRValue, lateIndexSetMethod.Parameters(0).Type)
            ' arg1  "object[] Arguments"
            Dim arguments As BoundExpression = LateMakeSetArgumentArrayArgument(syntax, assignmentValue.MakeRValue, invocation.ArgumentsOpt, invocation.ArgumentNamesOpt, lateIndexSetMethod.Parameters(1).Type)
            ' arg2  "string[] ArgumentNames"
            Dim argumentNames As BoundExpression = LateMakeArgumentNameArrayArgument(syntax, invocation.ArgumentNamesOpt, lateIndexSetMethod.Parameters(2).Type)

            Dim callArgs As ImmutableArray(Of BoundExpression)
            If Not isComplex Then
                callArgs = ImmutableArray.Create(Of BoundExpression)(receiver,
                                                                    arguments,
                                                                    argumentNames)
            Else
                ' arg3  "bool OptimisticSet"
                Dim optimisticSet As BoundExpression = MakeBooleanLiteral(syntax, isCopyBack, lateIndexSetMethod.Parameters(3).Type)
                ' arg4  "bool RValueBase"
                Dim rValueBase As BoundExpression = MakeBooleanLiteral(syntax, baseIsNotLValue, lateIndexSetMethod.Parameters(4).Type)

                callArgs = ImmutableArray.Create(Of BoundExpression)(receiver,
                                             arguments,
                                             argumentNames,
                                             optimisticSet,
                                             rValueBase)
            End If


            Return New BoundCall(syntax, lateIndexSetMethod, Nothing, Nothing, callArgs, Nothing, True, lateIndexSetMethod.ReturnType)
        End Function

        ' NOTE: assignmentArguments are no-side-effects expressions representing
        '       corresponding arguments if those need to be used as target of assignments.
        Private Function LateCallOrGet(memberAccess As BoundLateMemberAccess,
                                    receiverExpression As BoundExpression,
                                    argExpressions As ImmutableArray(Of BoundExpression),
                                    assignmentArguments As ImmutableArray(Of BoundExpression),
                                    argNames As ImmutableArray(Of String),
                                    useLateCall As Boolean) As BoundExpression

            Debug.Assert(memberAccess.AccessKind = LateBoundAccessKind.Call OrElse memberAccess.AccessKind = LateBoundAccessKind.Get)

            Debug.Assert((assignmentArguments.IsDefaultOrEmpty AndAlso argExpressions.IsDefaultOrEmpty) OrElse
              (assignmentArguments.Length = argExpressions.Length),
              "number of readable and writable arguments must match")

            Debug.Assert(argNames.IsDefaultOrEmpty OrElse argNames.Length = argExpressions.Length,
                         "should not have argument names or should have name for every argument")

            Dim syntax = memberAccess.Syntax

            ' We have 
            '   objExpr.Member()
            ' emit as:
            '     LateGet(memberAccess.ReceiverOpt, 
            '         memberAccess.MemberContainerOpt, 
            '         memberAccess.MemberNameOpt, 
            '         Arguments:=Nothing, 
            '         ArgumentNames:= Nothing, 
            '         TypeArguments = memberAccess.TypeArguments)
            '
            '
            Dim lateCallOrGetMethod As MethodSymbol = Nothing

            If useLateCall Then
                If Not Me.TryGetWellknownMember(lateCallOrGetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateCall, syntax) Then
                    Return memberAccess
                End If
            Else
                If Not Me.TryGetWellknownMember(lateCallOrGetMethod, WellKnownMember.Microsoft_VisualBasic_CompilerServices_NewLateBinding__LateGet, syntax) Then
                    Return memberAccess
                End If
            End If

            ' temp will be used only if we have copybacks
            Dim valueArrayTemp As SynthesizedLocal = Nothing
            Dim valueArrayRef As BoundLocal = Nothing

            Dim copyBackFlagArrayTemp As SynthesizedLocal = Nothing
            Dim copyBackFlagArrayRef As BoundLocal = Nothing

            ' passed as an array that represents "arguments of the call" 
            ' initially just rewrittenArgExpressions
            Dim argumentsArray As BoundExpression = LateMakeArgumentArrayArgument(syntax, argExpressions, argNames, lateCallOrGetMethod.Parameters(3).Type)

            ' passed as an array of flags indicating copyback status of corresponding elements of arguments array.
            ' initially Nothing
            Dim copyBackFlagArray As BoundExpression = LateMakeCopyBackArray(syntax, Nothing, lateCallOrGetMethod.Parameters(6).Type)

            '== process copybacks if needed
            Dim copyBackBuilder As ArrayBuilder(Of BoundExpression) = Nothing
            If Not assignmentArguments.IsDefaultOrEmpty Then
                Dim namedArgNum As Integer = 0
                Dim regularArgNum As Integer

                If Not argNames.IsDefaultOrEmpty Then
                    For Each n In argNames
                        If n IsNot Nothing Then
                            namedArgNum += 1
                        End If
                    Next
                End If

                regularArgNum = assignmentArguments.Length - namedArgNum

                ' This is a Late Call/Get with arguments. 
                ' All LValue arguments are presumed to be passed ByRef until rewriter assures us 
                ' the other way (we do not know the refness of parameters at the callsite).

                ' if have any copybacks, need an array of bool the size of assignmentArguments.Count
                ' "True" conveys to the latebinder that the argument can accept copyback
                ' "False" means to not bother.
                ' During the actual call latebinder will erase True for arguments that
                ' happened to be passed to a ByVal parameter (so that we do not need to do copyback assignment on our side).
                Dim copyBackFlagValues As Boolean() = Nothing

                For i As Integer = 0 To assignmentArguments.Length - 1
                    Dim assignmentTarget As BoundExpression = assignmentArguments(i)

                    If Not IsSupportingAssignment(assignmentTarget) Then
                        Continue For
                    End If

                    If copyBackFlagArrayTemp Is Nothing Then
                        ' since we may have copybacks, we need a temp for the flags array to examine its content after the call
                        copyBackFlagArrayTemp = New SynthesizedLocal(Me._currentMethodOrLambda, copyBackFlagArray.Type, SynthesizedLocalKind.LoweringTemp)
                        copyBackFlagArrayRef = (New BoundLocal(syntax, copyBackFlagArrayTemp, copyBackFlagArrayTemp.Type)).MakeRValue

                        ' since we may have copybacks, we need a temp for the arguments array to access it after the call
                        valueArrayTemp = New SynthesizedLocal(Me._currentMethodOrLambda, argumentsArray.Type, SynthesizedLocalKind.LoweringTemp)
                        valueArrayRef = New BoundLocal(syntax, valueArrayTemp, valueArrayTemp.Type)
                        argumentsArray = (New BoundAssignmentOperator(syntax, valueArrayRef, argumentsArray, suppressObjectClone:=True)).MakeRValue
                        valueArrayRef = valueArrayRef.MakeRValue

                        copyBackBuilder = ArrayBuilder(Of BoundExpression).GetInstance(assignmentArguments.Length)
                        copyBackFlagValues = (New Boolean(assignmentArguments.Length - 1) {})
                    End If

                    ' named arguments are actually passed first in the array
                    Dim indexVal = If(i < regularArgNum,
                                   namedArgNum + i,
                                   i - regularArgNum)

                    copyBackFlagValues(indexVal) = True
                    copyBackBuilder.Add(LateMakeConditionalCopyback(assignmentTarget, valueArrayRef, copyBackFlagArrayRef, indexVal))
                Next

                If copyBackFlagArrayTemp IsNot Nothing Then
                    copyBackFlagArray = (New BoundAssignmentOperator(syntax,
                                                                    New BoundLocal(syntax, copyBackFlagArrayTemp, copyBackFlagArrayTemp.Type),
                                                                    LateMakeCopyBackArray(syntax,
                                                                                          copyBackFlagValues.AsImmutableOrNull,
                                                                                          copyBackFlagArrayTemp.Type),
                                                                    suppressObjectClone:=True)).MakeRValue
                End If
            End If

            Dim receiverValue As BoundExpression = If(receiverExpression Is Nothing, Nothing, receiverExpression.MakeRValue)

            ' arg0  "object Instance"
            Dim receiver As BoundExpression = LateMakeReceiverArgument(syntax, receiverValue, lateCallOrGetMethod.Parameters(0).Type)
            ' arg1  "Type Type"
            Dim containerType As BoundExpression = LateMakeContainerArgument(syntax, receiverExpression, memberAccess.ContainerTypeOpt, lateCallOrGetMethod.Parameters(1).Type)
            ' arg2  "string MemberName"
            Dim name As BoundLiteral = MakeStringLiteral(syntax, memberAccess.NameOpt, lateCallOrGetMethod.Parameters(2).Type)
            ' arg3  "object[] Arguments"
            Dim arguments As BoundExpression = argumentsArray
            ' arg4  "string[] ArgumentNames"
            Dim argumentNames As BoundExpression = LateMakeArgumentNameArrayArgument(syntax, argNames, lateCallOrGetMethod.Parameters(4).Type)
            ' arg5  "Type[] TypeArguments"
            Dim typeArguments As BoundExpression = LateMakeTypeArgumentArrayArgument(syntax, memberAccess.TypeArgumentsOpt, lateCallOrGetMethod.Parameters(5).Type)
            ' arg6  "bool[] CopyBack"
            Dim copyBack As BoundExpression = copyBackFlagArray

            Dim callArgs = ImmutableArray.Create(Of BoundExpression)(receiver,
                                                                    containerType,
                                                                    name,
                                                                    arguments,
                                                                    argumentNames,
                                                                    typeArguments,
                                                                    copyBack)


            ' 
            ' It appears that IgnoreReturn is always set when LateCall is called from compiled code.
            '
            '           @ Expressions.cpp/CodeGenerator::GenerateLate [line:3314]
            '           ...          
            '           if (rtHelper == LateCallMember)
            '           {
            '               GenerateLiteralInt(COMPLUS_TRUE);
            '           }
            '           ...
            '
            If useLateCall Then
                ' arg7  "bool IgnoreReturn"
                Dim ignoreReturn As BoundExpression = MakeBooleanLiteral(syntax, True, lateCallOrGetMethod.Parameters(7).Type)
                callArgs = callArgs.Add(ignoreReturn)
            End If

            Dim callerInvocation As BoundExpression = New BoundCall(syntax, lateCallOrGetMethod, Nothing, Nothing, callArgs, Nothing, True, lateCallOrGetMethod.ReturnType)

            ' process copybacks
            If copyBackFlagArrayTemp IsNot Nothing Then
                Dim valueTemp = New SynthesizedLocal(Me._currentMethodOrLambda, callerInvocation.Type, SynthesizedLocalKind.LoweringTemp)
                Dim valueRef = New BoundLocal(syntax, valueTemp, valueTemp.Type)
                Dim store = New BoundAssignmentOperator(syntax, valueRef, callerInvocation, suppressObjectClone:=True)

                ' Seq{all temps; valueTemp = callerInvocation; copyBacks, valueTemp}
                callerInvocation = New BoundSequence(syntax,
                                                     ImmutableArray.Create(Of LocalSymbol)(valueArrayTemp, copyBackFlagArrayTemp, valueTemp),
                                                     ImmutableArray.Create(Of BoundExpression)(store).Concat(copyBackBuilder.ToImmutableAndFree),
                                                     valueRef.MakeRValue,
                                                     valueRef.Type)
            End If

            Return callerInvocation
        End Function

        ' same as LateCaptureReceiverAndArgsComplex, but without a receiver
        ' and does not produce reReadable arguments - just 
        ' argument (that includes initialization of captures if needed) and a no-side-effect writable.
        ' NOTE: writables are not rewritten. They will be rewritten when they are combined with values into assignments.
        Private Sub LateCaptureArgsComplex(ByRef temps As ArrayBuilder(Of SynthesizedLocal),
                           ByRef arguments As ImmutableArray(Of BoundExpression),
                           <Out> ByRef writeTargets As ImmutableArray(Of BoundExpression))

            Dim container = Me._currentMethodOrLambda

            If temps Is Nothing Then
                temps = ArrayBuilder(Of SynthesizedLocal).GetInstance
            End If

            If Not arguments.IsDefaultOrEmpty Then
                Dim argumentBuilder = ArrayBuilder(Of BoundExpression).GetInstance
                Dim writeTargetsBuilder = ArrayBuilder(Of BoundExpression).GetInstance
                For Each argument In arguments
                    Dim writeTarget As BoundExpression

                    If Not argument.IsSupportingAssignment() Then
                        ' in this case writeTarget will not be used for assignment
                        writeTarget = Nothing

                    Else
                        Dim argumentWithCapture As BoundLateBoundArgumentSupportingAssignmentWithCapture = Nothing

                        If argument.Kind = BoundKind.LateBoundArgumentSupportingAssignmentWithCapture Then
                            argumentWithCapture = DirectCast(argument, BoundLateBoundArgumentSupportingAssignmentWithCapture)
                            argument = argumentWithCapture.OriginalArgument
                        End If

                        Dim useTwice = UseTwiceRewriter.UseTwice(container, argument, temps)

                        If argument.IsPropertyOrXmlPropertyAccess Then
                            argument = useTwice.First.SetAccessKind(PropertyAccessKind.Get)
                            writeTarget = useTwice.Second.SetAccessKind(PropertyAccessKind.Set)
                        ElseIf argument.IsLateBound() Then
                            argument = useTwice.First.SetLateBoundAccessKind(LateBoundAccessKind.Get)
                            writeTarget = useTwice.Second.SetLateBoundAccessKind(LateBoundAccessKind.Set)
                        Else
                            argument = useTwice.First.MakeRValue()
                            writeTarget = useTwice.Second
                        End If

                        If argumentWithCapture IsNot Nothing Then
                            argument = New BoundAssignmentOperator(argumentWithCapture.Syntax,
                                                                   New BoundLocal(argumentWithCapture.Syntax,
                                                                                  argumentWithCapture.LocalSymbol,
                                                                                  argumentWithCapture.LocalSymbol.Type),
                                                                   argument,
                                                                   suppressObjectClone:=True,
                                                                   type:=argumentWithCapture.Type)
                        End If
                    End If

                    argumentBuilder.Add(VisitExpressionNode(argument))
                    writeTargetsBuilder.Add(writeTarget)
                Next

                arguments = argumentBuilder.ToImmutableAndFree
                writeTargets = writeTargetsBuilder.ToImmutableAndFree
            End If
        End Sub

        ' TODO: 
        ' ================= GENERAL PURPOSE, MOVE TO COMMON FILE

        Private Function MakeStringLiteral(node As VisualBasicSyntaxNode,
                                           value As String,
                                           stringType As TypeSymbol) As BoundLiteral

            If value Is Nothing Then
                Return MakeNullLiteral(node, stringType)
            Else
                Return New BoundLiteral(node, ConstantValue.Create(value), stringType)
            End If
        End Function

        Private Function MakeBooleanLiteral(node As VisualBasicSyntaxNode,
                                   value As Boolean,
                                   booleanType As TypeSymbol) As BoundLiteral

            Return New BoundLiteral(node, ConstantValue.Create(value), booleanType)
        End Function

        Private Function MakeGetTypeExpression(node As VisualBasicSyntaxNode,
                                               type As TypeSymbol,
                                               typeType As TypeSymbol) As BoundGetType

            Dim typeExpr = New BoundTypeExpression(node, type)
            Return New BoundGetType(node, typeExpr, typeType)
        End Function

        Private Function MakeArrayOfGetTypeExpressions(node As VisualBasicSyntaxNode,
                                       types As ImmutableArray(Of TypeSymbol),
                                       typeArrayType As TypeSymbol) As BoundArrayCreation

            Dim intType = Me.GetSpecialType(SpecialType.System_Int32)
            Dim bounds As BoundExpression = New BoundLiteral(node, ConstantValue.Create(types.Length), intType)

            Dim typeType = DirectCast(typeArrayType, ArrayTypeSymbol).ElementType
            Dim initializers = ArrayBuilder(Of BoundExpression).GetInstance
            For Each t In types
                initializers.Add(MakeGetTypeExpression(node, t, typeType))
            Next

            Dim initializer = New BoundArrayInitialization(node, initializers.ToImmutableAndFree, Nothing)

            Return New BoundArrayCreation(node, ImmutableArray.Create(bounds), initializer, typeArrayType)
        End Function

        Private Function TryGetWellknownMember(Of T As Symbol)(<Out> ByRef result As T,
                                                               memberId As WellKnownMember,
                                                               syntax As VisualBasicSyntaxNode,
                                                               Optional isOptional As Boolean = False) As Boolean

            Dim diagInfo As DiagnosticInfo = Nothing
            Dim memberSymbol = Binder.GetWellKnownTypeMember(Me.Compilation, memberId, diagInfo)

            If diagInfo IsNot Nothing Then
                If Not isOptional Then
                    Binder.ReportDiagnostic(_diagnostics, New VBDiagnostic(diagInfo, syntax.GetLocation()))
                End If

                Return False
            End If

            result = DirectCast(memberSymbol, T)
            Return True
        End Function

        ''' <summary>
        ''' Attempt to retrieve the specified special member, reporting a use-site diagnostic if the member is not found.
        ''' </summary>
        Private Function TryGetSpecialMember(Of T As Symbol)(<Out> ByRef result As T,
                                                       memberId As SpecialMember,
                                                       syntax As VisualBasicSyntaxNode) As Boolean

            Dim diagInfo As DiagnosticInfo = Nothing
            Dim memberSymbol = Binder.GetSpecialTypeMember(Me._topMethod.ContainingAssembly, memberId, diagInfo)

            If diagInfo IsNot Nothing Then
                Binder.ReportDiagnostic(_diagnostics, New VBDiagnostic(diagInfo, syntax.GetLocation()))
                result = Nothing
                Return False
            End If

            result = DirectCast(memberSymbol, T)
            Return True
        End Function

    End Class
End Namespace
