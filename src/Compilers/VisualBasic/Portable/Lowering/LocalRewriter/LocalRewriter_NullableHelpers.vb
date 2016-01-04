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

        Private Function WrapInNullable(expr As BoundExpression, nullableType As TypeSymbol) As BoundExpression
            Debug.Assert(nullableType.GetNullableUnderlyingType.IsSameTypeIgnoringCustomModifiers(expr.Type))

            Dim ctor = GetNullableMethod(expr.Syntax, nullableType, SpecialMember.System_Nullable_T__ctor)

            If ctor IsNot Nothing Then
                Return New BoundObjectCreationExpression(expr.Syntax,
                                                     ctor,
                                                     ImmutableArray.Create(expr),
                                                     Nothing,
                                                     nullableType)
            End If

            Return New BoundBadExpression(expr.Syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(Of BoundNode)(expr), nullableType, hasErrors:=True)
        End Function

        ''' <summary>
        ''' Splits nullable operand into a hasValueExpression and an expression that represents underlying value (returned).
        ''' 
        ''' Underlying value can be called after calling hasValueExpr without duplicated side-effects.
        ''' Note that hasValueExpr is guaranteed to have NO SIDE-EFFECTS, while result value is 
        ''' expected to be called exactly ONCE. That is the normal pattern in operator lifting.
        ''' 
        ''' All necessary temps and side-effecting initializations are appended to temps and inits
        ''' </summary>
        Private Function ProcessNullableOperand(operand As BoundExpression,
                                                <Out> ByRef hasValueExpr As BoundExpression,
                                                ByRef temps As ArrayBuilder(Of LocalSymbol),
                                                ByRef inits As ArrayBuilder(Of BoundExpression),
                                                doNotCaptureLocals As Boolean) As BoundExpression

            Return ProcessNullableOperand(operand, hasValueExpr, temps, inits, doNotCaptureLocals, HasValue(operand))
        End Function

        Private Function ProcessNullableOperand(operand As BoundExpression,
                                        <Out> ByRef hasValueExpr As BoundExpression,
                                        ByRef temps As ArrayBuilder(Of LocalSymbol),
                                        ByRef inits As ArrayBuilder(Of BoundExpression),
                                        doNotCaptureLocals As Boolean,
                                        operandHasValue As Boolean) As BoundExpression

            Debug.Assert(Not HasNoValue(operand), "processing nullable operand when it is known to be null")

            If operandHasValue Then
                operand = NullableValueOrDefault(operand)
            End If

            Dim captured = CaptureNullableIfNeeded(operand, temps, inits, doNotCaptureLocals)

            If operandHasValue Then
                hasValueExpr = New BoundLiteral(operand.Syntax, ConstantValue.True, Me.GetSpecialType(SpecialType.System_Boolean))
                Return captured
            End If

            hasValueExpr = NullableHasValue(captured)
            Return NullableValueOrDefault(captured)
        End Function

        ' Right operand could be a method that takes Left operand byref. Ex: " local And TakesArgByref(local) "
        ' So in general we must capture Left even if it is a local.
        ' however in many case we do not need that.
        Private Function RightCanChangeLeftLocal(left As BoundExpression, right As BoundExpression) As Boolean
            ' TODO: in most cases right operand does not change value of the left one
            '       we could be smarter than this.
            Return right.Kind = BoundKind.Local OrElse
                   right.Kind = BoundKind.Parameter

        End Function

        ''' <summary>
        ''' Returns a NOT-SIDE-EFFECTING expression that represents results of the operand
        ''' If such transformation requires a temp, the temp and its initializing expression
        ''' are returned in temp/init
        ''' </summary>
        Private Function CaptureNullableIfNeeded(operand As BoundExpression,
                                                        <Out> ByRef temp As SynthesizedLocal,
                                                        <Out> ByRef init As BoundExpression,
                                                        doNotCaptureLocals As Boolean) As BoundExpression

            temp = Nothing
            init = Nothing

            If operand.IsConstant Then
                Return operand
            End If

            If doNotCaptureLocals Then
                If operand.Kind = BoundKind.Local AndAlso Not DirectCast(operand, BoundLocal).LocalSymbol.IsByRef Then
                    Return operand
                End If

                If operand.Kind = BoundKind.Parameter AndAlso Not DirectCast(operand, BoundParameter).ParameterSymbol.IsByRef Then
                    Return operand
                End If
            End If

            ' capture into local.
            temp = New SynthesizedLocal(Me._currentMethodOrLambda, operand.Type, SynthesizedLocalKind.LoweringTemp)
            Dim localAccess = New BoundLocal(operand.Syntax, temp, True, temp.Type)
            init = New BoundAssignmentOperator(operand.Syntax, localAccess, operand, True, operand.Type)
            Return localAccess.MakeRValue
        End Function

        Private Function CaptureNullableIfNeeded(
            operand As BoundExpression,
            <[In], Out> ByRef temps As ArrayBuilder(Of LocalSymbol),
            <[In], Out> ByRef inits As ArrayBuilder(Of BoundExpression),
            doNotCaptureLocals As Boolean
        ) As BoundExpression

            Dim temp As SynthesizedLocal = Nothing
            Dim init As BoundExpression = Nothing
            Dim captured = CaptureNullableIfNeeded(operand, temp, init, doNotCaptureLocals)

            If temp IsNot Nothing Then
                temps = If(temps, ArrayBuilder(Of LocalSymbol).GetInstance)
                temps.Add(temp)

                Debug.Assert(init IsNot Nothing)
                inits = If(inits, ArrayBuilder(Of BoundExpression).GetInstance)
                inits.Add(init)
            Else
                Debug.Assert(captured Is operand)
            End If

            Return captured
        End Function


        ''' <summary>
        ''' Returns expression that -
        ''' a) evaluates the operand if needed
        ''' b) produces it's ValueOrDefault.
        ''' The helper is familiar with wrapping expressions and will go directly after the value 
        ''' skipping wrap/unwrap steps.
        ''' </summary>
        Private Function NullableValueOrDefault(expr As BoundExpression) As BoundExpression
            Debug.Assert(expr.Type.IsNullableType)

            ' check if we are not getting value from freshly constructed nullable
            ' no need to wrap/unwrap it then.
            If expr.Kind = BoundKind.ObjectCreationExpression Then
                Dim objectCreation = DirectCast(expr, BoundObjectCreationExpression)

                ' passing one argument means we are calling New Nullable<T>(arg)
                If objectCreation.Arguments.Length = 1 Then
                    Return objectCreation.Arguments(0)
                End If
            End If

            Dim getValueOrDefaultMethod = GetNullableMethod(expr.Syntax, expr.Type, SpecialMember.System_Nullable_T_GetValueOrDefault)

            If getValueOrDefaultMethod IsNot Nothing Then
                Return New BoundCall(expr.Syntax,
                                 getValueOrDefaultMethod,
                                 Nothing,
                                 expr,
                                 ImmutableArray(Of BoundExpression).Empty,
                                 Nothing,
                                 True,
                                 getValueOrDefaultMethod.ReturnType)
            End If

            Return New BoundBadExpression(expr.Syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(Of BoundNode)(expr), expr.Type.GetNullableUnderlyingType(), hasErrors:=True)
        End Function

        Private Function NullableValue(expr As BoundExpression) As BoundExpression
            Debug.Assert(expr.Type.IsNullableType)

            If HasValue(expr) Then
                Return NullableValueOrDefault(expr)
            End If

            Dim getValueMethod As MethodSymbol = GetNullableMethod(expr.Syntax, expr.Type, SpecialMember.System_Nullable_T_get_Value)

            If getValueMethod IsNot Nothing Then
                Return New BoundCall(expr.Syntax,
                                 getValueMethod,
                                 Nothing,
                                 expr,
                                 ImmutableArray(Of BoundExpression).Empty,
                                 Nothing,
                                 True,
                                 getValueMethod.ReturnType)
            End If

            Return New BoundBadExpression(expr.Syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(Of BoundNode)(expr), expr.Type.GetNullableUnderlyingType(), hasErrors:=True)
        End Function

        ''' <summary>
        ''' Evaluates expr and calls HasValue on it.
        ''' </summary>
        Private Function NullableHasValue(expr As BoundExpression) As BoundExpression
            Debug.Assert(expr.Type.IsNullableType)

            ' when we statically know if expr HasValue we may skip 
            ' evaluation depending on context.
            Debug.Assert(Not HasValue(expr))
            Debug.Assert(Not HasNoValue(expr))

            Dim hasValueMethod As MethodSymbol = GetNullableMethod(expr.Syntax, expr.Type, SpecialMember.System_Nullable_T_get_HasValue)

            If hasValueMethod IsNot Nothing Then
                Return New BoundCall(expr.Syntax,
                                 hasValueMethod,
                                 Nothing,
                                 expr,
                                 ImmutableArray(Of BoundExpression).Empty,
                                 Nothing,
                                 True,
                                 hasValueMethod.ReturnType)
            End If

            Return New BoundBadExpression(expr.Syntax, LookupResultKind.NotReferencable, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(Of BoundNode)(expr),
                                          Me.Compilation.GetSpecialType(SpecialType.System_Boolean), hasErrors:=True)
        End Function

        Private Shared Function NullableNull(syntax As VisualBasicSyntaxNode, nullableType As TypeSymbol) As BoundExpression
            Debug.Assert(nullableType.IsNullableType)

            Return New BoundObjectCreationExpression(syntax,
                                Nothing,
                                ImmutableArray(Of BoundExpression).Empty,
                                Nothing,
                                nullableType)
        End Function

        ''' <summary>
        ''' Checks that candidate Null expression is a simple expression that produces Null of the desired type
        ''' (not a conversion or anything like that) and returns it.
        ''' Otherwise creates "New T?()" expression.
        ''' </summary>
        Private Shared Function NullableNull(candidateNullExpression As BoundExpression,
                                             type As TypeSymbol) As BoundExpression
            Debug.Assert(HasNoValue(candidateNullExpression))

            ' in case if the expression is any more complicated than just creating a Null
            ' simplify it. This may happen if HasNoValue gets smarter and can
            ' detect situations other than "New T?()"
            If (Not type.IsSameTypeIgnoringCustomModifiers(candidateNullExpression.Type)) OrElse
                candidateNullExpression.Kind <> BoundKind.ObjectCreationExpression Then

                Return NullableNull(candidateNullExpression.Syntax, type)
            End If

            Return candidateNullExpression
        End Function

        Private Function NullableFalse(syntax As VisualBasicSyntaxNode, nullableOfBoolean As TypeSymbol) As BoundExpression
            Debug.Assert(nullableOfBoolean.IsNullableOfBoolean)
            Dim booleanType = nullableOfBoolean.GetNullableUnderlyingType
            Return WrapInNullable(New BoundLiteral(syntax, ConstantValue.False, booleanType), nullableOfBoolean)
        End Function

        Private Function NullableTrue(syntax As VisualBasicSyntaxNode, nullableOfBoolean As TypeSymbol) As BoundExpression
            Debug.Assert(nullableOfBoolean.IsNullableOfBoolean)
            Dim booleanType = nullableOfBoolean.GetNullableUnderlyingType
            Return WrapInNullable(New BoundLiteral(syntax, ConstantValue.True, booleanType), nullableOfBoolean)
        End Function

        Private Function GetNullableMethod(syntax As VisualBasicSyntaxNode, nullableType As TypeSymbol, member As SpecialMember) As MethodSymbol
            Dim method As MethodSymbol = Nothing

            If TryGetSpecialMember(method, member, syntax) Then
                Dim substitutedType = DirectCast(nullableType, SubstitutedNamedType)
                Return DirectCast(substitutedType.GetMemberForDefinition(method), MethodSymbol)
            End If

            Return Nothing
        End Function

        Private Function NullableOfBooleanValue(syntax As VisualBasicSyntaxNode, isTrue As Boolean, nullableOfBoolean As TypeSymbol) As BoundExpression
            If isTrue Then
                Return NullableTrue(syntax, nullableOfBoolean)
            Else
                Return NullableFalse(syntax, nullableOfBoolean)
            End If
        End Function

        ''' <summary>
        ''' returns true when expression has NO SIDE-EFFECTS and is known to produce nullable NULL
        ''' </summary>
        Private Shared Function HasNoValue(expr As BoundExpression) As Boolean
            Debug.Assert(expr.Type.IsNullableType)

            If expr.Kind = BoundKind.ObjectCreationExpression Then
                Dim objCreation = DirectCast(expr, BoundObjectCreationExpression)
                ' Nullable<T> has only one ctor with parameters and only that one sets hasValue = true
                Return objCreation.Arguments.Length = 0
            End If

            ' by default we do not know
            Return False
        End Function

        ''' <summary>
        ''' Returns true when expression is known to produce nullable NOT-NULL
        ''' NOTE: unlike HasNoValue case, HasValue expressions may have side-effects.
        ''' </summary>
        Private Shared Function HasValue(expr As BoundExpression) As Boolean
            Debug.Assert(expr.Type.IsNullableType)

            If expr.Kind = BoundKind.ObjectCreationExpression Then
                Dim objCreation = DirectCast(expr, BoundObjectCreationExpression)
                ' Nullable<T> has only one ctor with parameters and only that one sets hasValue = true
                Return objCreation.Arguments.Length <> 0
            End If

            ' by default we do not know
            Return False
        End Function

        ''' <summary>
        ''' Helper to generate binary expressions.
        ''' Performs some trivial constant folding.
        ''' TODO: Perhaps belong to a different file
        ''' </summary>
        Private Function MakeBinaryExpression(syntax As VisualBasicSyntaxNode,
                                            binaryOpKind As BinaryOperatorKind,
                                            left As BoundExpression,
                                            right As BoundExpression,
                                            isChecked As Boolean,
                                            resultType As TypeSymbol) As BoundExpression

            Debug.Assert(Not left.Type.IsNullableType)
            Debug.Assert(Not right.Type.IsNullableType)

            Dim intOverflow As Boolean = False
            Dim divideByZero As Boolean = False
            Dim compoundLengthOutOfLimit As Boolean = False

            Dim constant = OverloadResolution.TryFoldConstantBinaryOperator(binaryOpKind, left, right, resultType, intOverflow, divideByZero, compoundLengthOutOfLimit)
            If constant IsNot Nothing AndAlso
                Not divideByZero AndAlso
                Not (intOverflow And isChecked) AndAlso
                Not compoundLengthOutOfLimit Then

                Debug.Assert(Not constant.IsBad)
                Return New BoundLiteral(syntax, constant, resultType)
            End If

            Select Case binaryOpKind
                Case BinaryOperatorKind.Subtract
                    If right.IsDefaultValueConstant Then
                        Return left
                    End If

                Case BinaryOperatorKind.Add,
                     BinaryOperatorKind.Or,
                     BinaryOperatorKind.OrElse

                    ' if one of operands is trivial, return the other one
                    If left.IsDefaultValueConstant Then
                        Return right
                    End If

                    If right.IsDefaultValueConstant Then
                        Return left
                    End If

                    ' if one of operands is True, evaluate the other and return the True one
                    If left.IsTrueConstant Then
                        Return MakeSequence(right, left)
                    End If

                    If right.IsTrueConstant Then
                        Return MakeSequence(left, right)
                    End If

                Case BinaryOperatorKind.And,
                    BinaryOperatorKind.AndAlso,
                    BinaryOperatorKind.Multiply

                    ' if one of operands is trivial, evaluate the other and return the trivial one
                    If left.IsDefaultValueConstant Then
                        Return MakeSequence(right, left)
                    End If

                    If right.IsDefaultValueConstant Then
                        Return MakeSequence(left, right)
                    End If

                    ' if one of operands is True, return the other one
                    If left.IsTrueConstant Then
                        Return right
                    End If

                    If right.IsTrueConstant Then
                        Return left
                    End If

                Case BinaryOperatorKind.Equals
                    If left.IsTrueConstant Then
                        Return right
                    End If

                    If right.IsTrueConstant Then
                        Return left
                    End If

                Case BinaryOperatorKind.NotEquals
                    If left.IsFalseConstant Then
                        Return right
                    End If

                    If right.IsFalseConstant Then
                        Return left
                    End If
            End Select

            Return TransformRewrittenBinaryOperator(New BoundBinaryOperator(syntax, binaryOpKind, left, right, isChecked, resultType))
        End Function

        ''' <summary>
        ''' Simpler helper for binary expressions.
        ''' When operand are boolean, the result type is same as operand's and is never checked 
        ''' so do not need to pass that in.
        ''' </summary>
        Private Function MakeBooleanBinaryExpression(syntax As VisualBasicSyntaxNode,
                                    binaryOpKind As BinaryOperatorKind,
                                    left As BoundExpression,
                                    right As BoundExpression) As BoundExpression

            Debug.Assert(left.Type = right.Type)
            Debug.Assert(left.Type.IsBooleanType)

            Return MakeBinaryExpression(syntax, binaryOpKind, left, right, False, left.Type)
        End Function

        Private Function MakeNullLiteral(syntax As VisualBasicSyntaxNode, type As TypeSymbol) As BoundLiteral
            Return New BoundLiteral(syntax, ConstantValue.Nothing, type)
        End Function

        ''' <summary>
        ''' Takes two expressions and makes sequence.
        ''' </summary>
        Private Shared Function MakeSequence(first As BoundExpression, second As BoundExpression) As BoundExpression
            Return MakeSequence(second.Syntax, first, second)
        End Function

        ''' <summary>
        ''' Takes two expressions and makes sequence.
        ''' </summary>
        Private Shared Function MakeSequence(syntax As VisualBasicSyntaxNode,
                                             first As BoundExpression,
                                             second As BoundExpression) As BoundExpression

            Dim sideeffects = GetSideeffects(first)
            If sideeffects Is Nothing Then
                Return second
            End If

            Return New BoundSequence(syntax,
                                     ImmutableArray(Of LocalSymbol).Empty,
                                     ImmutableArray.Create(sideeffects),
                                     second,
                                     second.Type)
        End Function

        ''' <summary>
        ''' Takes two expressions and makes sequence.
        ''' </summary>
        Private Function MakeTernaryConditionalExpression(syntax As VisualBasicSyntaxNode,
                                                          condition As BoundExpression,
                                                          whenTrue As BoundExpression,
                                                          whenFalse As BoundExpression) As BoundExpression

            Debug.Assert(condition.Type.IsBooleanType, "ternary condition must be boolean")
            Debug.Assert(whenTrue.Type.IsSameTypeIgnoringCustomModifiers(whenFalse.Type), "ternary branches must have same types")

            Dim ifConditionConst = condition.ConstantValueOpt
            If ifConditionConst IsNot Nothing Then
                Return MakeSequence(syntax, condition, If(ifConditionConst Is ConstantValue.True, whenTrue, whenFalse))
            End If

            Return TransformRewrittenTernaryConditionalExpression(New BoundTernaryConditionalExpression(syntax, condition, whenTrue, whenFalse, Nothing, whenTrue.Type))
        End Function

        ''' <summary>
        ''' Returns an expression that can be used instead of the original one when
        ''' we want to run the expression for side-effects only (i.e. we intend to ignore result).
        ''' </summary>
        Private Shared Function GetSideeffects(operand As BoundExpression) As BoundExpression
            If operand.IsConstant Then
                Return Nothing
            End If

            Select Case operand.Kind
                Case BoundKind.Local,
                    BoundKind.Parameter

                    Return Nothing

                Case BoundKind.ObjectCreationExpression
                    If operand.Type.IsNullableType Then
                        Dim objCreation = DirectCast(operand, BoundObjectCreationExpression)
                        Dim args = objCreation.Arguments
                        If args.Length = 0 Then
                            Return Nothing
                        Else
                            Return GetSideeffects(args(0))
                        End If
                    End If
            End Select

            Return operand
        End Function
    End Class
End Namespace
