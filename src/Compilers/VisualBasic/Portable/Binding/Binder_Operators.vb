' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' Binding of binary and unary operators is implemented in this part.

    Partial Friend Class Binder

        Private Function BindIsExpression(
             node As BinaryExpressionSyntax,
             diagnostics As DiagnosticBag
        ) As BoundExpression

            Debug.Assert(node.Kind = SyntaxKind.IsExpression OrElse node.Kind = SyntaxKind.IsNotExpression)
            Dim [isNot] As Boolean = (node.Kind = SyntaxKind.IsNotExpression)

            ' The function below will make sure they are RValues.
            Dim left As BoundExpression = BindExpression(node.Left, diagnostics)
            Dim right As BoundExpression = BindExpression(node.Right, diagnostics)

            Return BindIsExpression(left, right, node, [isNot], diagnostics)
        End Function

        Private Function BindIsExpression(
             left As BoundExpression,
             right As BoundExpression,
             node As VisualBasicSyntaxNode,
             [isNot] As Boolean,
             diagnostics As DiagnosticBag
        ) As BoundExpression
            left = MakeRValue(left, diagnostics)
            right = MakeRValue(right, diagnostics)

            left = ValidateAndConvertIsExpressionArgument(left, right, [isNot], diagnostics)
            right = ValidateAndConvertIsExpressionArgument(right, left, [isNot], diagnostics)

            Dim result As BoundExpression
            Dim booleanType = GetSpecialType(SpecialType.System_Boolean, node, diagnostics)

            result = New BoundBinaryOperator(node,
                                             If([isNot], BinaryOperatorKind.IsNot, BinaryOperatorKind.Is),
                                             left,
                                             right,
                                             checked:=False,
                                             Type:=booleanType,
                                             hasErrors:=booleanType.IsErrorType())

            ' TODO: Add rewrite for Nullable.

            Return result
        End Function

        ''' <summary>
        ''' Validate and apply appropriate conversion for the target argument of Is/IsNot expression.
        ''' </summary>
        Private Function ValidateAndConvertIsExpressionArgument(
            targetArgument As BoundExpression,
            otherArgument As BoundExpression,
            [isNot] As Boolean,
            diagnostics As DiagnosticBag
        ) As BoundExpression

            Dim targetArgumentType As TypeSymbol = targetArgument.Type
            Dim result As BoundExpression

            If targetArgument.IsNothingLiteral() Then
                result = targetArgument

            ElseIf targetArgumentType.IsErrorType() Then
                result = targetArgument

            ElseIf targetArgumentType.IsReferenceType Then
                result = ApplyImplicitConversion(targetArgument.Syntax,
                                                 GetSpecialType(SpecialType.System_Object, targetArgument.Syntax, diagnostics),
                                                 targetArgument,
                                                 diagnostics)

            ElseIf targetArgumentType.IsNullableType() Then
                If Not otherArgument.HasErrors AndAlso Not otherArgument.IsNothingLiteral() Then
                    ReportDiagnostic(diagnostics, targetArgument.Syntax,
                                     If([isNot], ERRID.ERR_IsNotOperatorNullable1, ERRID.ERR_IsOperatorNullable1),
                                     targetArgumentType)
                End If

                result = targetArgument

            ElseIf targetArgumentType.IsTypeParameter() AndAlso Not targetArgumentType.IsValueType Then
                If Not otherArgument.HasErrors AndAlso Not otherArgument.IsNothingLiteral() Then
                    ReportDiagnostic(diagnostics, targetArgument.Syntax,
                                     If([isNot], ERRID.ERR_IsNotOperatorGenericParam1, ERRID.ERR_IsOperatorGenericParam1),
                                     targetArgumentType)
                End If

                ' If any of the left or right operands of the Is or IsNot operands
                ' are entities of type parameters types, then they need to be boxed.
                result = ApplyImplicitConversion(targetArgument.Syntax,
                                                 GetSpecialType(SpecialType.System_Object, targetArgument.Syntax, diagnostics),
                                                 targetArgument,
                                                 diagnostics)

            Else
                ReportDiagnostic(diagnostics, targetArgument.Syntax,
                                 If([isNot], ERRID.ERR_IsNotOpRequiresReferenceTypes1, ERRID.ERR_IsOperatorRequiresReferenceTypes1),
                                 targetArgumentType)

                result = targetArgument
            End If

            Return result
        End Function

        Private Function BindBinaryOperator(
            node As BinaryExpressionSyntax,
            isOperandOfConditionalBranch As Boolean,
            diagnostics As DiagnosticBag
        ) As BoundExpression
            ' Some tools, such as ASP .NET, generate expressions containing thousands
            ' of string concatenations. For this reason, for string concatenations,
            ' avoid the usual recursion along the left side of the parse. Also, attempt
            ' to flatten whole sequences of string literal concatenations to avoid
            ' allocating space for intermediate results.

            Dim preliminaryOperatorKind As BinaryOperatorKind = OverloadResolution.MapBinaryOperatorKind(node.Kind)
            Dim propagateIsOperandOfConditionalBranch = isOperandOfConditionalBranch AndAlso
                                                            (preliminaryOperatorKind = BinaryOperatorKind.AndAlso OrElse
                                                                preliminaryOperatorKind = BinaryOperatorKind.OrElse)

            Dim binary As BinaryExpressionSyntax = node
            Dim child As ExpressionSyntax

            Do
                child = binary.Left

                Select Case child.Kind
                    Case SyntaxKind.AddExpression,
                         SyntaxKind.ConcatenateExpression,
                         SyntaxKind.LikeExpression,
                         SyntaxKind.EqualsExpression,
                         SyntaxKind.NotEqualsExpression,
                         SyntaxKind.LessThanOrEqualExpression,
                         SyntaxKind.GreaterThanOrEqualExpression,
                         SyntaxKind.LessThanExpression,
                         SyntaxKind.GreaterThanExpression,
                         SyntaxKind.SubtractExpression,
                         SyntaxKind.MultiplyExpression,
                         SyntaxKind.ExponentiateExpression,
                         SyntaxKind.DivideExpression,
                         SyntaxKind.ModuloExpression,
                         SyntaxKind.IntegerDivideExpression,
                         SyntaxKind.LeftShiftExpression,
                         SyntaxKind.RightShiftExpression,
                         SyntaxKind.ExclusiveOrExpression,
                         SyntaxKind.OrExpression,
                         SyntaxKind.AndExpression

                        If propagateIsOperandOfConditionalBranch Then
                            Exit Do
                        End If

                    Case SyntaxKind.OrElseExpression,
                         SyntaxKind.AndAlsoExpression
                        Exit Select

                    Case Else
                        Exit Do
                End Select

                binary = DirectCast(child, BinaryExpressionSyntax)
            Loop

            Dim compoundStringLength As Integer = 0
            Dim left As BoundExpression = BindValue(child, diagnostics, propagateIsOperandOfConditionalBranch)

            Do
                binary = DirectCast(child.Parent, BinaryExpressionSyntax)

                Dim right As BoundExpression = BindValue(binary.Right, diagnostics, propagateIsOperandOfConditionalBranch)

                left = BindBinaryOperator(binary, left, right, binary.OperatorToken.Kind,
                                          OverloadResolution.MapBinaryOperatorKind(binary.Kind),
                                          If(binary Is node, isOperandOfConditionalBranch, propagateIsOperandOfConditionalBranch),
                                          diagnostics,
                                          compoundStringLength:=compoundStringLength)

                child = binary
            Loop While child IsNot node

            Return left
        End Function

        Private Function BindBinaryOperator(
            node As VisualBasicSyntaxNode,
            left As BoundExpression,
            right As BoundExpression,
            operatorTokenKind As SyntaxKind,
            preliminaryOperatorKind As BinaryOperatorKind,
            isOperandOfConditionalBranch As Boolean,
            diagnostics As DiagnosticBag,
            Optional isSelectCase As Boolean = False,
            <[In], Out> Optional ByRef compoundStringLength As Integer = 0
        ) As BoundExpression

            Debug.Assert(left.IsValue)
            Debug.Assert(right.IsValue)

            Dim originalDiagnostics = diagnostics

            If (left.HasErrors OrElse right.HasErrors) Then
                ' Suppress any additional diagnostics by overriding DiagnosticBag.
                diagnostics = New DiagnosticBag()
            End If

            ' Deal with NOTHING literal as an input.
            ConvertNothingLiterals(preliminaryOperatorKind, left, right, diagnostics)

            left = MakeRValue(left, diagnostics)
            right = MakeRValue(right, diagnostics)

            If (left.HasErrors OrElse right.HasErrors) Then
                ' Suppress any additional diagnostics by overriding DiagnosticBag.
                If diagnostics Is originalDiagnostics Then
                    diagnostics = New DiagnosticBag()
                End If
            End If

            Dim leftType As TypeSymbol = left.Type
            Dim rightType As TypeSymbol = right.Type

            Dim leftIsDBNull As Boolean = leftType.IsDBNullType()
            Dim rightIsDBNull As Boolean = rightType.IsDBNullType()

            '§11.16 Concatenation Operator
            'A System.DBNull value is converted to the literal Nothing typed as String. 
            If (preliminaryOperatorKind = BinaryOperatorKind.Concatenate AndAlso leftIsDBNull <> rightIsDBNull) OrElse
               (preliminaryOperatorKind = BinaryOperatorKind.Add AndAlso
                  ((leftType.IsStringType() AndAlso rightIsDBNull) OrElse (leftIsDBNull AndAlso rightType.IsStringType))) Then

                Debug.Assert(leftIsDBNull Xor rightIsDBNull)

                If leftIsDBNull Then
                    leftType = SubstituteDBNullWithNothingString(left, rightType, diagnostics)
                Else
                    rightType = SubstituteDBNullWithNothingString(right, leftType, diagnostics)
                End If
            End If

            ' For comparison operators, the result type computed here is not
            ' the result type of the comparison (which is typically boolean),
            ' but is the type to which the operands are to be converted. For
            ' other operators, the type computed here is both the result type
            ' and the common operand type.
            Dim intrinsicOperatorType As SpecialType = SpecialType.None
            Dim userDefinedOperator As OverloadResolution.OverloadResolutionResult = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim operatorKind As BinaryOperatorKind = OverloadResolution.ResolveBinaryOperator(preliminaryOperatorKind, left, right, Me,
                                                                                              True,
                                                                                              intrinsicOperatorType,
                                                                                              userDefinedOperator,
                                                                                              useSiteDiagnostics)

            If diagnostics.Add(node, useSiteDiagnostics) Then
                ' Suppress additional diagnostics
                diagnostics = New DiagnosticBag()
            End If

            If operatorKind = BinaryOperatorKind.UserDefined Then
                Dim bestCandidate As OverloadResolution.Candidate = If(userDefinedOperator.BestResult.HasValue,
                                                                       userDefinedOperator.BestResult.Value.Candidate,
                                                                       Nothing)
                If bestCandidate Is Nothing OrElse
                   Not bestCandidate.IsLifted OrElse
                   (OverloadResolution.IsValidInLiftedSignature(bestCandidate.Parameters(0).Type) AndAlso
                    OverloadResolution.IsValidInLiftedSignature(bestCandidate.Parameters(1).Type) AndAlso
                    OverloadResolution.IsValidInLiftedSignature(bestCandidate.ReturnType)) Then

                    If preliminaryOperatorKind = BinaryOperatorKind.AndAlso OrElse preliminaryOperatorKind = BinaryOperatorKind.OrElse Then
                        Return BindUserDefinedShortCircuitingOperator(node, preliminaryOperatorKind, left, right,
                                                                      userDefinedOperator, diagnostics)
                    Else
                        Return BindUserDefinedNonShortCircuitingBinaryOperator(node, preliminaryOperatorKind, left, right,
                                                                               userDefinedOperator, diagnostics)
                    End If
                End If

                operatorKind = BinaryOperatorKind.Error
            End If

            If operatorKind = BinaryOperatorKind.Error Then
                ReportUndefinedOperatorError(node, left, right, operatorTokenKind, preliminaryOperatorKind, diagnostics)

                Return New BoundBinaryOperator(node, preliminaryOperatorKind Or BinaryOperatorKind.Error, left, right, CheckOverflow, ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
            End If

            ' We are dealing with intrinsic operator 

            ' Get the symbol for operand type
            Dim operandType As TypeSymbol

            If intrinsicOperatorType = SpecialType.None Then
                ' Must be a bitwise operation with enum type.
                Debug.Assert(leftType.GetNullableUnderlyingTypeOrSelf().IsEnumType() AndAlso
                             leftType.GetNullableUnderlyingTypeOrSelf().IsSameTypeIgnoringCustomModifiers(rightType.GetNullableUnderlyingTypeOrSelf()))

                If (operatorKind And BinaryOperatorKind.Lifted) = 0 OrElse leftType.IsNullableType() Then
                    operandType = leftType
                Else
                    Debug.Assert(rightType.IsNullableType())
                    operandType = rightType
                End If

            Else
                operandType = GetSpecialTypeForBinaryOperator(node, leftType, rightType, intrinsicOperatorType,
                                                              (operatorKind And BinaryOperatorKind.Lifted) <> 0, diagnostics)
            End If

            ' Get the symbol for result type
            Dim operatorResultType As TypeSymbol = operandType

            Dim forceToBooleanType As TypeSymbol = Nothing
            Dim applyIsTrue As Boolean = False

            Select Case preliminaryOperatorKind

                Case BinaryOperatorKind.Equals,
                     BinaryOperatorKind.NotEquals,
                     BinaryOperatorKind.LessThanOrEqual,
                     BinaryOperatorKind.GreaterThanOrEqual,
                     BinaryOperatorKind.LessThan,
                     BinaryOperatorKind.GreaterThan,
                     BinaryOperatorKind.Like

                    If OptionCompareText AndAlso (operandType.IsObjectType() OrElse operandType.IsStringType()) Then
                        operatorKind = operatorKind Or BinaryOperatorKind.CompareText
                    End If

                    If Not operatorResultType.IsObjectType() OrElse
                        (isOperandOfConditionalBranch AndAlso preliminaryOperatorKind <> BinaryOperatorKind.Like) Then

                        Dim booleanType As TypeSymbol = GetSpecialTypeForBinaryOperator(node, leftType, rightType, SpecialType.System_Boolean,
                                                              False, diagnostics)

                        If (operatorKind And BinaryOperatorKind.Lifted) <> 0 Then

                            operatorResultType = GetNullableTypeForBinaryOperator(leftType, rightType, booleanType)

                            If (preliminaryOperatorKind = BinaryOperatorKind.Equals OrElse preliminaryOperatorKind = BinaryOperatorKind.NotEquals) AndAlso
                                (IsKnownToBeNullableNothing(left) OrElse IsKnownToBeNullableNothing(right)) Then

                                ReportDiagnostic(diagnostics, node,
                                                 ErrorFactory.ErrorInfo(
                                                     If(preliminaryOperatorKind = BinaryOperatorKind.Equals,
                                                        ERRID.WRN_EqualToLiteralNothing, ERRID.WRN_NotEqualToLiteralNothing)))
                            End If

                            If isOperandOfConditionalBranch Then
                                ' TODO: I believe the IsTrue is just an optimization to prevent Nullable from unnecessary bubbling up the tree.
                                ' Perhaps we can do this optimization as a rewrite.
                                applyIsTrue = True
                                forceToBooleanType = booleanType
                            End If
                        Else
                            If Not operatorResultType.IsObjectType() Then
                                operatorResultType = booleanType
                            Else
                                ' I believe this is just an optimization to prevent Object from bubbling up the tree.
                                Debug.Assert(isOperandOfConditionalBranch AndAlso preliminaryOperatorKind <> BinaryOperatorKind.Like)
                                forceToBooleanType = booleanType
                            End If
                        End If
                    End If
            End Select

            If operandType.GetNullableUnderlyingTypeOrSelf().IsErrorType() OrElse
               operatorResultType.GetNullableUnderlyingTypeOrSelf().IsErrorType() OrElse
               (forceToBooleanType IsNot Nothing AndAlso forceToBooleanType.GetNullableUnderlyingTypeOrSelf().IsErrorType()) Then
                ' Suppress any additional diagnostics by overriding DiagnosticBag.
                If diagnostics Is originalDiagnostics Then
                    diagnostics = New DiagnosticBag()
                End If
            End If

            Dim hasError As Boolean = False

            ' Option Strict disallows all operations on Object operands. Or, at least, warn.
            If OptionStrict = VisualBasic.OptionStrict.On Then
                Dim reportedAnEror As Boolean = False

                If leftType.IsObjectType Then
                    ReportBinaryOperatorOnObject(operatorTokenKind, left, preliminaryOperatorKind, diagnostics)
                    reportedAnEror = True
                End If

                If rightType.IsObjectType() Then
                    ReportBinaryOperatorOnObject(operatorTokenKind, right, preliminaryOperatorKind, diagnostics)
                    reportedAnEror = True
                End If

                If reportedAnEror Then
                    hasError = True

                    ' Suppress any additional diagnostics by overriding DiagnosticBag.
                    If diagnostics Is originalDiagnostics Then
                        diagnostics = New DiagnosticBag()
                    End If
                End If
            ElseIf OptionStrict = VisualBasic.OptionStrict.Custom Then 'warn if option strict is off
                If Not isSelectCase OrElse preliminaryOperatorKind <> BinaryOperatorKind.OrElse Then
                    Dim errorId = If(isSelectCase, ERRID.WRN_ObjectMathSelectCase,
                                    If(preliminaryOperatorKind = BinaryOperatorKind.Equals, ERRID.WRN_ObjectMath1,
                                        If(preliminaryOperatorKind = BinaryOperatorKind.NotEquals, ERRID.WRN_ObjectMath1Not, ERRID.WRN_ObjectMath2)))

                    If leftType.IsObjectType Then
                        ReportDiagnostic(diagnostics, left.Syntax, ErrorFactory.ErrorInfo(errorId, operatorTokenKind))
                    End If

                    If rightType.IsObjectType Then
                        ReportDiagnostic(diagnostics, right.Syntax, ErrorFactory.ErrorInfo(errorId, operatorTokenKind))
                    End If
                End If
            End If

            ' Apply conversions to operands.
            Dim explicitSemanticForConcatArgument As Boolean = False

            ' Concatenation will apply conversions to its operands as if the
            ' conversions were explicit. Effectively, the use of the concatenation
            ' operator is treated as an explicit conversion to String.
            If preliminaryOperatorKind = BinaryOperatorKind.Concatenate Then
                explicitSemanticForConcatArgument = True

                Debug.Assert((operatorKind And BinaryOperatorKind.Lifted) = 0)

                If operandType.IsStringType() Then
                    If left.Type.IsNullableType Then
                        left = ForceLiftToEmptyString(left, operandType, diagnostics)
                    End If

                    If right.Type.IsNullableType Then
                        right = ForceLiftToEmptyString(right, operandType, diagnostics)
                    End If
                End If
            End If

            left = ApplyConversion(left.Syntax, operandType, left, explicitSemanticForConcatArgument, diagnostics,
                                   explicitSemanticForConcatArgument:=explicitSemanticForConcatArgument)

            If (preliminaryOperatorKind = BinaryOperatorKind.LeftShift OrElse preliminaryOperatorKind = BinaryOperatorKind.RightShift) AndAlso
                Not operandType.IsObjectType() Then

                Dim rightTargetType As TypeSymbol = GetSpecialTypeForBinaryOperator(node, leftType, rightType, SpecialType.System_Int32,
                                                                                False, diagnostics)

                '§11.18 Shift Operators
                'The type of the right operand must be implicitly convertible to Integer 

                ' If operator is lifted, convert right operand to Nullable(Of Integer)
                If (operatorKind And BinaryOperatorKind.Lifted) <> 0 Then
                    rightTargetType = GetNullableTypeForBinaryOperator(leftType, rightType, rightTargetType)
                End If

                right = ApplyImplicitConversion(right.Syntax, rightTargetType, right, diagnostics)
            Else
                right = ApplyConversion(right.Syntax, operandType, right, explicitSemanticForConcatArgument, diagnostics,
                                        explicitSemanticForConcatArgument:=explicitSemanticForConcatArgument)
            End If

            If (operatorKind And BinaryOperatorKind.OpMask) = BinaryOperatorKind.Add AndAlso operatorResultType.IsStringType() Then
                ' Transform the addition into a string concatenation.  This won't use a runtime helper - it will turn into System.String::Concat
                operatorKind = (operatorKind And (Not BinaryOperatorKind.OpMask))
                operatorKind = operatorKind Or BinaryOperatorKind.Concatenate
            End If

            ' Perform constant folding.
            Dim value As ConstantValue = Nothing

            If Not (left.HasErrors OrElse right.HasErrors) Then
                Dim integerOverflow As Boolean = False
                Dim divideByZero As Boolean = False
                Dim compoundLengthOutOfLimit As Boolean = False

                value = OverloadResolution.TryFoldConstantBinaryOperator(operatorKind,
                                                                         left,
                                                                         right,
                                                                         operatorResultType,
                                                                         integerOverflow,
                                                                         divideByZero,
                                                                         compoundLengthOutOfLimit,
                                                                         compoundStringLength)

                If value IsNot Nothing Then
                    If divideByZero Then
                        Debug.Assert(value.IsBad)
                        ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_ZeroDivide))
                    ElseIf compoundLengthOutOfLimit
                        Debug.Assert(value.IsBad)
                        ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_ConstantStringTooLong))
                    ElseIf (value.IsBad OrElse integerOverflow) Then
                        ' Overflows are reported regardless of the value of OptionRemoveIntegerOverflowChecks, Dev10 behavior.
                        ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_ExpressionOverflow1, operatorResultType))

                        ' there should be no constant value in case of overflows.
                        If Not value.IsBad Then
                            value = ConstantValue.Bad
                        End If
                    End If
                End If
            End If

            Dim result As BoundExpression = New BoundBinaryOperator(node, operatorKind, left, right, CheckOverflow, value, operatorResultType, hasError)

            Debug.Assert(Not applyIsTrue OrElse forceToBooleanType IsNot Nothing)

            If forceToBooleanType IsNot Nothing Then
                Debug.Assert(forceToBooleanType.IsBooleanType())

                If applyIsTrue Then
                    Return ApplyNullableIsTrueOperator(result, forceToBooleanType)
                End If

                result = ApplyConversion(node, forceToBooleanType, result, isExplicit:=True, diagnostics:=diagnostics)
            End If

            Return result
        End Function

        ''' <summary>
        ''' This helper is used to wrap nullable argument into something that would return null string if argument is null.
        '''
        ''' Unlike conversion to a string where nullable nulls result in an exception,         
        ''' concatenation requires that nullable nulls are treated as null strings. 
        ''' Note that conversion is treated as explicit conversion.
        ''' </summary>
        Private Function ForceLiftToEmptyString(left As BoundExpression, stringType As TypeSymbol, diagnostics As DiagnosticBag) As BoundExpression
            Debug.Assert(stringType.IsStringType)

            Dim nothingStr = New BoundLiteral(left.Syntax, ConstantValue.Nothing, stringType)

            Return AnalyzeConversionAndCreateBinaryConditionalExpression(left.Syntax,
                                                                         left,
                                                                         nothingStr,
                                                                         Nothing,
                                                                         stringType,
                                                                         False,
                                                                         diagnostics,
                                                                         explicitConversion:=True)
        End Function

        Private Function BindUserDefinedNonShortCircuitingBinaryOperator(
            node As VisualBasicSyntaxNode,
            opKind As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            <[In]> ByRef userDefinedOperator As OverloadResolution.OverloadResolutionResult,
            diagnostics As DiagnosticBag
        ) As BoundUserDefinedBinaryOperator
            Debug.Assert(userDefinedOperator.Candidates.Length > 0)

            opKind = opKind Or BinaryOperatorKind.UserDefined

            Dim result As BoundExpression

            If userDefinedOperator.BestResult.HasValue Then
                Dim bestCandidate As OverloadResolution.CandidateAnalysisResult = userDefinedOperator.BestResult.Value

                result = CreateBoundCallOrPropertyAccess(node, node, TypeCharacter.None,
                                                         New BoundMethodGroup(node, Nothing,
                                                                              ImmutableArray.Create(Of MethodSymbol)(
                                                                                  DirectCast(bestCandidate.Candidate.UnderlyingSymbol, MethodSymbol)),
                                                                              LookupResultKind.Good, Nothing,
                                                                              QualificationKind.Unqualified).MakeCompilerGenerated(),
                                                         ImmutableArray.Create(Of BoundExpression)(left, right),
                                                         bestCandidate,
                                                         userDefinedOperator.AsyncLambdaSubToFunctionMismatch,
                                                         diagnostics)

                If bestCandidate.Candidate.IsLifted Then
                    opKind = opKind Or BinaryOperatorKind.Lifted
                End If
            Else
                result = ReportOverloadResolutionFailureAndProduceBoundNode(node, LookupResultKind.Good,
                                                                            ImmutableArray.Create(Of BoundExpression)(left, right),
                                                                            Nothing, userDefinedOperator, diagnostics,
                                                                            callerInfoOpt:=Nothing)
            End If

            Return New BoundUserDefinedBinaryOperator(node, opKind, result, CheckOverflow, result.Type)
        End Function

        ''' <summary>
        ''' This function builds a bound tree representing an overloaded short circuiting expression
        ''' after determining that the necessary semantic conditions are met.
        ''' 
        ''' An expression of the form:
        ''' 
        '''     x AndAlso y  (where the type of x is X and the type of y is Y)
        ''' 
        ''' is an overloaded short circuit operation if X and Y are user-defined types and an
        ''' applicable operator And exists after applying normal operator resolution rules.
        ''' 
        ''' Given an applicable And operator declared in type T, the following must be true:
        ''' 
        '''     - The return type and parameter types must be T.
        '''     - T must contain a declaration of operator IsFalse.
        ''' 
        ''' If these conditions are met, the expression "x AndAlso y" is translated into:
        ''' 
        '''     !T.IsFalse(temp = x) ? T.And(temp, y) : temp
        ''' 
        ''' The temporary is necessary for evaluating x only once. Similarly, "x OrElse y" is
        ''' translated into:
        ''' 
        '''     !T.IsTrue(temp = x) ? T.Or(temp, y) : temp
        ''' </summary>
        Private Function BindUserDefinedShortCircuitingOperator(
            node As VisualBasicSyntaxNode,
            opKind As BinaryOperatorKind,
            left As BoundExpression,
            right As BoundExpression,
            <[In]> ByRef bitwiseOperator As OverloadResolution.OverloadResolutionResult,
            diagnostics As DiagnosticBag
        ) As BoundUserDefinedShortCircuitingOperator
            Debug.Assert(opKind = BinaryOperatorKind.AndAlso OrElse opKind = BinaryOperatorKind.OrElse)
            Debug.Assert(bitwiseOperator.Candidates.Length > 0)

            Dim bitwiseKind As BinaryOperatorKind = If(opKind = BinaryOperatorKind.AndAlso, BinaryOperatorKind.And, BinaryOperatorKind.Or) Or BinaryOperatorKind.UserDefined

            Dim operatorType As TypeSymbol
            Dim leftOperand As BoundExpression = Nothing
            Dim leftPlaceholder As BoundRValuePlaceholder = Nothing
            Dim test As BoundExpression = Nothing
            Dim bitwise As BoundUserDefinedBinaryOperator
            Dim hasErrors As Boolean = False

            If Not bitwiseOperator.BestResult.HasValue Then
                ' This will take care of the diagnostic.
                bitwise = BindUserDefinedNonShortCircuitingBinaryOperator(node, bitwiseKind, left, right, bitwiseOperator, diagnostics)
                operatorType = bitwise.Type
                hasErrors = True
                GoTo Done
            End If

            Dim bitwiseAnalysis As OverloadResolution.CandidateAnalysisResult = bitwiseOperator.BestResult.Value
            Dim bitwiseCandidate As OverloadResolution.Candidate = bitwiseAnalysis.Candidate
            operatorType = bitwiseCandidate.ReturnType

            If bitwiseCandidate.IsLifted Then
                bitwiseKind = bitwiseKind Or BinaryOperatorKind.Lifted
            End If

            If Not operatorType.IsSameTypeIgnoringCustomModifiers(bitwiseCandidate.Parameters(0).Type) OrElse
               Not operatorType.IsSameTypeIgnoringCustomModifiers(bitwiseCandidate.Parameters(1).Type) Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_UnacceptableLogicalOperator3,
                                 bitwiseCandidate.UnderlyingSymbol,
                                 bitwiseCandidate.UnderlyingSymbol.ContainingType,
                                 SyntaxFacts.GetText(If(opKind = BinaryOperatorKind.AndAlso,
                                                        SyntaxKind.AndAlsoKeyword, SyntaxKind.OrElseKeyword)))

                Dim discardedDiagnostics = DiagnosticBag.GetInstance()
                bitwise = BindUserDefinedNonShortCircuitingBinaryOperator(node, bitwiseKind, left, right, bitwiseOperator,
                                                                          discardedDiagnostics) ' Ignore any additional diagnostics.
                discardedDiagnostics.Free()
                hasErrors = True
                GoTo Done
            End If

            leftPlaceholder = New BoundRValuePlaceholder(left.Syntax, operatorType).MakeCompilerGenerated()

            ' Find IsTrue/IsFalse operator
            Dim leftCheckOperator As OverloadResolution.OverloadResolutionResult

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            If opKind = BinaryOperatorKind.AndAlso Then
                leftCheckOperator = OverloadResolution.ResolveIsFalseOperator(leftPlaceholder, Me, useSiteDiagnostics)
            Else
                leftCheckOperator = OverloadResolution.ResolveIsTrueOperator(leftPlaceholder, Me, useSiteDiagnostics)
            End If

            If diagnostics.Add(node, useSiteDiagnostics) Then
                ' Suppress additional diagnostics
                diagnostics = New DiagnosticBag()
            End If

            If Not leftCheckOperator.BestResult.HasValue Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_ConditionOperatorRequired3,
                                 operatorType,
                                 SyntaxFacts.GetText(If(opKind = BinaryOperatorKind.AndAlso, SyntaxKind.IsFalseKeyword, SyntaxKind.IsTrueKeyword)),
                                 SyntaxFacts.GetText(If(opKind = BinaryOperatorKind.AndAlso, SyntaxKind.AndAlsoKeyword, SyntaxKind.OrElseKeyword)))

                Dim discardedDiagnostics = DiagnosticBag.GetInstance()
                bitwise = BindUserDefinedNonShortCircuitingBinaryOperator(node, bitwiseKind, left, right, bitwiseOperator,
                                                                          discardedDiagnostics) ' Ignore any additional diagnostics.
                discardedDiagnostics.Free()
                leftPlaceholder = Nothing
                hasErrors = True
                GoTo Done
            End If

            Dim checkCandidate As OverloadResolution.Candidate = leftCheckOperator.BestResult.Value.Candidate
            Debug.Assert(checkCandidate.ReturnType.IsBooleanType() OrElse checkCandidate.ReturnType.IsNullableOfBoolean())

            If Not operatorType.IsSameTypeIgnoringCustomModifiers(checkCandidate.Parameters(0).Type) Then
                ReportDiagnostic(diagnostics, node, ERRID.ERR_BinaryOperands3,
                                 SyntaxFacts.GetText(If(opKind = BinaryOperatorKind.AndAlso, SyntaxKind.AndAlsoKeyword, SyntaxKind.OrElseKeyword)),
                                 left.Type, right.Type)

                hasErrors = True
                diagnostics = New DiagnosticBag() ' Ignore any additional diagnostics.
                bitwise = BindUserDefinedNonShortCircuitingBinaryOperator(node, bitwiseKind, left, right, bitwiseOperator, diagnostics)
            Else
                ' Convert the operands to the operator type.
                Dim operands As ImmutableArray(Of BoundExpression) = PassArguments(node, bitwiseAnalysis,
                                                                                  ImmutableArray.Create(Of BoundExpression)(left, right),
                                                                                  diagnostics)
                bitwiseAnalysis.ConversionsOpt = Nothing

                bitwise = New BoundUserDefinedBinaryOperator(node, bitwiseKind,
                                                             CreateBoundCallOrPropertyAccess(node, node, TypeCharacter.None,
                                                                 New BoundMethodGroup(node, Nothing,
                                                                                      ImmutableArray.Create(Of MethodSymbol)(
                                                                                          DirectCast(bitwiseCandidate.UnderlyingSymbol, MethodSymbol)),
                                                                                      LookupResultKind.Good, Nothing,
                                                                                      QualificationKind.Unqualified).MakeCompilerGenerated(),
                                                                 ImmutableArray.Create(Of BoundExpression)(leftPlaceholder, operands(1)),
                                                                 bitwiseAnalysis,
                                                                 bitwiseOperator.AsyncLambdaSubToFunctionMismatch,
                                                                 diagnostics),
                                                             CheckOverflow,
                                                             operatorType)

                leftOperand = operands(0)
            End If

            Dim testOp As BoundUserDefinedUnaryOperator = BindUserDefinedUnaryOperator(node,
                                                                                       If(opKind = BinaryOperatorKind.AndAlso,
                                                                                          UnaryOperatorKind.IsFalse,
                                                                                          UnaryOperatorKind.IsTrue),
                                                                                       leftPlaceholder,
                                                                                       leftCheckOperator,
                                                                                       diagnostics).MakeCompilerGenerated()
            testOp.UnderlyingExpression.SetWasCompilerGenerated()

            If hasErrors Then
                leftPlaceholder = Nothing
            End If

            If checkCandidate.IsLifted Then
                test = ApplyNullableIsTrueOperator(testOp, checkCandidate.ReturnType.GetNullableUnderlyingTypeOrSelf())
            Else
                test = testOp
            End If

Done:
            Debug.Assert(hasErrors OrElse (leftOperand IsNot Nothing AndAlso leftPlaceholder IsNot Nothing AndAlso test IsNot Nothing))
            Debug.Assert(Not hasErrors OrElse (leftOperand Is Nothing AndAlso leftPlaceholder Is Nothing))

            bitwise.UnderlyingExpression.SetWasCompilerGenerated()
            bitwise.SetWasCompilerGenerated()
            Return New BoundUserDefinedShortCircuitingOperator(node, leftOperand, leftPlaceholder, test, bitwise, operatorType, hasErrors)
        End Function


        Private Sub ReportBinaryOperatorOnObject(
            operatorTokenKind As SyntaxKind,
            operand As BoundExpression,
            preliminaryOperatorKind As BinaryOperatorKind,
            diagnostics As DiagnosticBag
        )
            ReportDiagnostic(diagnostics, operand.Syntax,
                             ErrorFactory.ErrorInfo(
                                 If(preliminaryOperatorKind = BinaryOperatorKind.Equals OrElse preliminaryOperatorKind = BinaryOperatorKind.NotEquals,
                                    ERRID.ERR_StrictDisallowsObjectComparison1, ERRID.ERR_StrictDisallowsObjectOperand1),
                                 operatorTokenKind))
        End Sub

        ''' <summary>
        ''' Returns Symbol for String type.
        ''' </summary>
        Private Function SubstituteDBNullWithNothingString(
            ByRef dbNullOperand As BoundExpression,
            otherOperandType As TypeSymbol,
            diagnostics As DiagnosticBag
        ) As TypeSymbol
            Dim stringType As TypeSymbol

            If otherOperandType.IsStringType() Then
                stringType = otherOperandType
            Else
                stringType = GetSpecialType(SpecialType.System_String, dbNullOperand.Syntax, diagnostics)
            End If

            dbNullOperand = New BoundConversion(dbNullOperand.Syntax, dbNullOperand, ConversionKind.Widening,
                                        checked:=False, explicitCastInCode:=False, type:=stringType,
                                        constantValueOpt:=ConstantValue.Nothing)

            Return stringType
        End Function

        ''' <summary>
        ''' Get symbol for a special type, reuse symbols for operand types to avoid type 
        ''' lookups and construction of new instances of symbols.
        ''' </summary>
        Private Function GetSpecialTypeForBinaryOperator(
            node As VisualBasicSyntaxNode,
            leftType As TypeSymbol,
            rightType As TypeSymbol,
            specialType As SpecialType,
            makeNullable As Boolean,
            diagnostics As DiagnosticBag
        ) As TypeSymbol
            Debug.Assert(specialType <> Microsoft.CodeAnalysis.SpecialType.None)
            Debug.Assert(Not makeNullable OrElse leftType.IsNullableType() OrElse rightType.IsNullableType())

            Dim resultType As TypeSymbol
            Dim leftNullableUnderlying = leftType.GetNullableUnderlyingTypeOrSelf()
            Dim leftSpecialType = leftNullableUnderlying.SpecialType
            Dim rightNullableUnderlying = rightType.GetNullableUnderlyingTypeOrSelf()
            Dim rightSpecialType = rightNullableUnderlying.SpecialType

            If leftSpecialType = specialType Then

                If Not makeNullable Then
                    resultType = leftNullableUnderlying
                ElseIf leftType.IsNullableType() Then
                    resultType = leftType
                ElseIf rightSpecialType = specialType Then
                    Debug.Assert(makeNullable AndAlso rightType.IsNullableType())
                    resultType = rightType
                Else
                    Debug.Assert(makeNullable AndAlso
                                 rightType.IsNullableType() AndAlso
                                 Not leftType.IsNullableType())
                    resultType = DirectCast(rightType.OriginalDefinition, NamedTypeSymbol).Construct(leftType)
                End If

            ElseIf rightSpecialType = specialType Then

                If Not makeNullable Then
                    resultType = rightNullableUnderlying
                ElseIf rightType.IsNullableType() Then
                    resultType = rightType
                Else
                    Debug.Assert(makeNullable AndAlso
                                 Not rightType.IsNullableType() AndAlso
                                 leftType.IsNullableType())

                    resultType = DirectCast(leftType.OriginalDefinition, NamedTypeSymbol).Construct(rightNullableUnderlying)
                End If
            Else
                resultType = GetSpecialType(specialType, node, diagnostics)

                If makeNullable Then
                    If leftType.IsNullableType() Then
                        resultType = DirectCast(leftType.OriginalDefinition, NamedTypeSymbol).Construct(resultType)
                    Else
                        Debug.Assert(rightType.IsNullableType())
                        resultType = DirectCast(rightType.OriginalDefinition, NamedTypeSymbol).Construct(resultType)
                    End If
                End If
            End If

            Return resultType
        End Function

        ''' <summary>
        ''' Get symbol for a Nullable type of particular type, reuse symbols for operand types to avoid type 
        ''' lookups and construction of new instances of symbols.
        ''' </summary>
        Private Function GetNullableTypeForBinaryOperator(
            leftType As TypeSymbol,
            rightType As TypeSymbol,
            ofType As TypeSymbol
        ) As TypeSymbol
            Dim leftIsNullable = leftType.IsNullableType()
            Dim rightIsNullable = rightType.IsNullableType()
            Dim ofSpecialType = ofType.SpecialType

            Debug.Assert(leftIsNullable OrElse rightIsNullable)

            If ofSpecialType <> SpecialType.None Then
                If leftIsNullable AndAlso leftType.GetNullableUnderlyingType().SpecialType = ofSpecialType Then
                    Return leftType
                ElseIf rightIsNullable AndAlso rightType.GetNullableUnderlyingType().SpecialType = ofSpecialType Then
                    Return rightType
                End If
            End If

            If leftIsNullable Then
                Return DirectCast(leftType.OriginalDefinition, NamedTypeSymbol).Construct(ofType)
            Else
                Return DirectCast(rightType.OriginalDefinition, NamedTypeSymbol).Construct(ofType)
            End If
        End Function

        Private Shared Function IsKnownToBeNullableNothing(expr As BoundExpression) As Boolean
            Dim cast = expr

            ' TODO: Add handling for TryCast, similar to DirectCast
            While cast.Kind = BoundKind.Conversion OrElse cast.Kind = BoundKind.DirectCast

                If cast.HasErrors Then
                    Return False
                End If

                Dim resultType As TypeSymbol = Nothing

                Select Case cast.Kind
                    Case BoundKind.Conversion
                        Dim conv = DirectCast(cast, BoundConversion)
                        resultType = conv.Type
                        cast = conv.Operand

                    Case BoundKind.DirectCast
                        Dim conv = DirectCast(cast, BoundDirectCast)
                        resultType = conv.Type
                        cast = conv.Operand
                End Select

                If resultType Is Nothing OrElse Not (resultType.IsNullableType() OrElse resultType.IsObjectType()) Then
                    Return False
                End If
            End While

            Return cast.IsNothingLiteral()
        End Function

        Private Sub ReportUndefinedOperatorError(
            syntax As VisualBasicSyntaxNode,
            left As BoundExpression,
            right As BoundExpression,
            operatorTokenKind As SyntaxKind,
            operatorKind As BinaryOperatorKind,
            diagnostics As DiagnosticBag
        )
            Dim leftType = left.Type
            Dim rightType = right.Type

            Debug.Assert(leftType IsNot Nothing)
            Debug.Assert(rightType IsNot Nothing)

            If leftType.IsErrorType() OrElse rightType.IsErrorType() Then
                Return ' Let's not report more errors.
            End If

            Dim operatorTokenText = SyntaxFacts.GetText(operatorTokenKind)

            If OverloadResolution.UseUserDefinedBinaryOperators(operatorKind, leftType, rightType) AndAlso
                Not leftType.CanContainUserDefinedOperators(useSiteDiagnostics:=Nothing) AndAlso Not rightType.CanContainUserDefinedOperators(useSiteDiagnostics:=Nothing) AndAlso
                (operatorKind = BinaryOperatorKind.Equals OrElse operatorKind = BinaryOperatorKind.NotEquals) AndAlso
                leftType.IsReferenceType() AndAlso rightType.IsReferenceType() Then
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_ReferenceComparison3, operatorTokenText, leftType, rightType)

            ElseIf IsIEnumerableOfXElement(leftType, Nothing) Then
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_BinaryOperandsForXml4, operatorTokenText, leftType, rightType, leftType)

            ElseIf IsIEnumerableOfXElement(rightType, Nothing) Then
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_BinaryOperandsForXml4, operatorTokenText, leftType, rightType, rightType)

            Else
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_BinaryOperands3, operatorTokenText, leftType, rightType)

            End If
        End Sub

        ''' <summary>
        ''' §11.12.2 Object Operands
        ''' The value Nothing is treated as the default value of the type of 
        ''' the other operand in a binary operator expression. In a unary operator expression, 
        ''' or if both operands are Nothing in a binary operator expression, 
        ''' the type of the operation is Integer or the only result type of the operator, 
        ''' if the operator does not result in Integer.
        ''' </summary>
        Private Sub ConvertNothingLiterals(
            operatorKind As BinaryOperatorKind,
            ByRef left As BoundExpression,
            ByRef right As BoundExpression,
            diagnostics As DiagnosticBag
        )
            Debug.Assert((operatorKind And BinaryOperatorKind.OpMask) = operatorKind AndAlso operatorKind <> 0)

            Dim rightType As TypeSymbol
            Dim leftType As TypeSymbol

            If left.IsNothingLiteral() Then

                If right.IsNothingLiteral() Then
                    ' Both are NOTHING
                    Dim defaultRightSpecialType As SpecialType

                    Select Case operatorKind
                        Case BinaryOperatorKind.Concatenate,
                             BinaryOperatorKind.Like
                            defaultRightSpecialType = SpecialType.System_String

                        Case BinaryOperatorKind.OrElse,
                             BinaryOperatorKind.AndAlso
                            defaultRightSpecialType = SpecialType.System_Boolean

                        Case BinaryOperatorKind.Add,
                             BinaryOperatorKind.Equals,
                             BinaryOperatorKind.NotEquals,
                             BinaryOperatorKind.LessThanOrEqual,
                             BinaryOperatorKind.GreaterThanOrEqual,
                             BinaryOperatorKind.LessThan,
                             BinaryOperatorKind.GreaterThan,
                             BinaryOperatorKind.Subtract,
                             BinaryOperatorKind.Multiply,
                             BinaryOperatorKind.Power,
                             BinaryOperatorKind.Divide,
                             BinaryOperatorKind.Modulo,
                             BinaryOperatorKind.IntegerDivide,
                             BinaryOperatorKind.LeftShift,
                             BinaryOperatorKind.RightShift,
                             BinaryOperatorKind.Xor,
                             BinaryOperatorKind.Or,
                             BinaryOperatorKind.And
                            defaultRightSpecialType = SpecialType.System_Int32

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(operatorKind)
                    End Select

                    rightType = GetSpecialType(defaultRightSpecialType, right.Syntax, diagnostics)
                    right = ApplyImplicitConversion(right.Syntax,
                                                    rightType,
                                                    right, diagnostics)
                Else
                    rightType = right.Type

                    If rightType Is Nothing Then
                        Return
                    End If
                End If

                Debug.Assert(rightType IsNot Nothing)
                Dim defaultLeftSpecialType As SpecialType = SpecialType.None

                Select Case operatorKind
                    Case BinaryOperatorKind.Concatenate,
                         BinaryOperatorKind.Like

                        If rightType.GetNullableUnderlyingTypeOrSelf().GetEnumUnderlyingTypeOrSelf().IsIntrinsicType() OrElse
                           rightType.IsCharSZArray() OrElse
                           rightType.IsDBNullType() Then

                            ' For & and Like, a Nothing operand is typed String unless the other operand
                            ' is non-intrinsic (VSW#240203).
                            ' The same goes for DBNull (VSW#278518)
                            ' The same goes for enum types (VSW#288077)
                            defaultLeftSpecialType = SpecialType.System_String
                        End If

                    Case BinaryOperatorKind.LeftShift,
                         BinaryOperatorKind.RightShift
                        ' Nothing should default to Integer for Shift operations.
                        defaultLeftSpecialType = SpecialType.System_Int32
                End Select

                If defaultLeftSpecialType = SpecialType.None OrElse defaultLeftSpecialType = rightType.SpecialType Then
                    leftType = rightType
                Else
                    leftType = GetSpecialType(defaultLeftSpecialType, left.Syntax, diagnostics)
                End If

                left = ApplyImplicitConversion(left.Syntax,
                                                leftType,
                                                left, diagnostics)

            ElseIf right.IsNothingLiteral() Then

                leftType = left.Type
                If leftType Is Nothing Then
                    Return
                End If

                rightType = leftType

                Select Case operatorKind
                    Case BinaryOperatorKind.Concatenate,
                         BinaryOperatorKind.Like

                        If leftType.GetNullableUnderlyingTypeOrSelf().GetEnumUnderlyingTypeOrSelf().IsIntrinsicType() OrElse
                           leftType.IsCharSZArray() OrElse
                           leftType.IsDBNullType() Then

                            ' For & and Like, a Nothing operand is typed String unless the other operand
                            ' is non-intrinsic (VSW#240203).
                            ' The same goes for DBNull (VSW#278518)
                            ' The same goes for enum types (VSW#288077)
                            If leftType.SpecialType <> SpecialType.System_String Then
                                rightType = GetSpecialType(SpecialType.System_String, right.Syntax, diagnostics)
                            End If
                        End If
                End Select

                right = ApplyImplicitConversion(right.Syntax,
                                                rightType,
                                                right, diagnostics)
            End If
        End Sub

        Private Function BindUnaryOperator(node As UnaryExpressionSyntax, diagnostics As DiagnosticBag) As BoundExpression

            Dim operand As BoundExpression = BindValue(node.Operand, diagnostics)
            Dim preliminaryOperatorKind As UnaryOperatorKind = OverloadResolution.MapUnaryOperatorKind(node.Kind)

            If Not operand.HasErrors AndAlso operand.IsNothingLiteral Then
                '§11.12.2 Object Operands
                'In a unary operator expression, or if both operands are Nothing in a 
                'binary operator expression, the type of the operation is Integer
                Dim int32Type = GetSpecialType(SpecialType.System_Int32, node.Operand, diagnostics)
                operand = ApplyImplicitConversion(node.Operand, int32Type, operand, diagnostics)
            Else
                operand = MakeRValue(operand, diagnostics)
            End If

            If operand.HasErrors Then
                ' Suppress any additional diagnostics by overriding DiagnosticBag.
                diagnostics = New DiagnosticBag()
            End If

            Dim intrinsicOperatorType As SpecialType = SpecialType.None
            Dim userDefinedOperator As OverloadResolution.OverloadResolutionResult = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim operatorKind As UnaryOperatorKind = OverloadResolution.ResolveUnaryOperator(preliminaryOperatorKind, operand, Me, intrinsicOperatorType, userDefinedOperator, useSiteDiagnostics)

            If diagnostics.Add(node, useSiteDiagnostics) Then
                ' Suppress additional diagnostics
                diagnostics = New DiagnosticBag()
            End If

            If operatorKind = UnaryOperatorKind.UserDefined Then
                Dim bestCandidate As OverloadResolution.Candidate = If(userDefinedOperator.BestResult.HasValue,
                                                                       userDefinedOperator.BestResult.Value.Candidate,
                                                                       Nothing)
                If bestCandidate Is Nothing OrElse
                   Not bestCandidate.IsLifted OrElse
                   (OverloadResolution.IsValidInLiftedSignature(bestCandidate.Parameters(0).Type) AndAlso
                    OverloadResolution.IsValidInLiftedSignature(bestCandidate.ReturnType)) Then
                    Return BindUserDefinedUnaryOperator(node, preliminaryOperatorKind, operand, userDefinedOperator, diagnostics)
                End If

                operatorKind = UnaryOperatorKind.Error
            End If

            If operatorKind = UnaryOperatorKind.Error Then
                ReportUndefinedOperatorError(node, operand, diagnostics)

                Return New BoundUnaryOperator(node, preliminaryOperatorKind Or UnaryOperatorKind.Error, operand, CheckOverflow, ErrorTypeSymbol.UnknownResultType, HasErrors:=True)
            End If

            ' We are dealing with intrinsic operator 
            Dim operandType As TypeSymbol = operand.Type
            Dim resultType As TypeSymbol = Nothing

            If intrinsicOperatorType = SpecialType.None Then
                Debug.Assert(operandType.GetNullableUnderlyingTypeOrSelf().IsEnumType())
                resultType = operandType

            Else
                If operandType.GetNullableUnderlyingTypeOrSelf().SpecialType = intrinsicOperatorType Then
                    resultType = operandType
                Else
                    resultType = GetSpecialType(intrinsicOperatorType, node.Operand, diagnostics)

                    If operandType.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T Then
                        resultType = DirectCast(operandType.OriginalDefinition, NamedTypeSymbol).Construct(resultType)
                    End If
                End If
            End If

            Debug.Assert(((operatorKind And UnaryOperatorKind.Lifted) <> 0) = (resultType.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T))

            ' Option Strict disallows all unary operations on Object operands. Otherwise just warn.
            If operandType.SpecialType = SpecialType.System_Object Then
                If OptionStrict = VisualBasic.OptionStrict.On Then
                    ReportDiagnostic(diagnostics, node.Operand, ErrorFactory.ErrorInfo(ERRID.ERR_StrictDisallowsObjectOperand1, node.OperatorToken))
                ElseIf OptionStrict = VisualBasic.OptionStrict.Custom Then
                    ReportDiagnostic(diagnostics, node.Operand, ErrorFactory.ErrorInfo(ERRID.WRN_ObjectMath2, node.OperatorToken))
                End If
            End If

            operand = ApplyImplicitConversion(node.Operand, resultType, operand, diagnostics)

            Dim constantValue As ConstantValue = Nothing

            If Not operand.HasErrors Then
                Dim integerOverflow As Boolean = False
                constantValue = OverloadResolution.TryFoldConstantUnaryOperator(operatorKind, operand, resultType, integerOverflow)

                ' Overflows are reported regardless of the value of OptionRemoveIntegerOverflowChecks, Dev10 behavior.
                If constantValue IsNot Nothing AndAlso (constantValue.IsBad OrElse integerOverflow) Then
                    ReportDiagnostic(diagnostics, node, ErrorFactory.ErrorInfo(ERRID.ERR_ExpressionOverflow1, resultType))

                    ' there should be no constant value in case of overflows.
                    If Not constantValue.IsBad Then
                        constantValue = constantValue.Bad
                    End If
                End If
            End If

            Return New BoundUnaryOperator(node, operatorKind, operand, CheckOverflow, constantValue, resultType)
        End Function

        Private Function BindUserDefinedUnaryOperator(
            node As VisualBasicSyntaxNode,
            opKind As UnaryOperatorKind,
            operand As BoundExpression,
            <[In]> ByRef userDefinedOperator As OverloadResolution.OverloadResolutionResult,
            diagnostics As DiagnosticBag
        ) As BoundUserDefinedUnaryOperator
            Debug.Assert(userDefinedOperator.Candidates.Length > 0)

            Dim result As BoundExpression

            opKind = opKind Or UnaryOperatorKind.UserDefined

            If userDefinedOperator.BestResult.HasValue Then
                Dim bestCandidate As OverloadResolution.CandidateAnalysisResult = userDefinedOperator.BestResult.Value

                result = CreateBoundCallOrPropertyAccess(node, node, TypeCharacter.None,
                                                         New BoundMethodGroup(node, Nothing,
                                                                              ImmutableArray.Create(Of MethodSymbol)(
                                                                                  DirectCast(bestCandidate.Candidate.UnderlyingSymbol, MethodSymbol)),
                                                                              LookupResultKind.Good, Nothing,
                                                                              QualificationKind.Unqualified).MakeCompilerGenerated(),
                                                         ImmutableArray.Create(Of BoundExpression)(operand),
                                                         bestCandidate,
                                                         userDefinedOperator.AsyncLambdaSubToFunctionMismatch,
                                                         diagnostics)

                If bestCandidate.Candidate.IsLifted Then
                    opKind = opKind Or UnaryOperatorKind.Lifted
                End If
            Else
                result = ReportOverloadResolutionFailureAndProduceBoundNode(node, LookupResultKind.Good,
                                                                            ImmutableArray.Create(Of BoundExpression)(operand),
                                                                            Nothing, userDefinedOperator, diagnostics,
                                                                            callerInfoOpt:=Nothing)
            End If

            Return New BoundUserDefinedUnaryOperator(node, opKind, result, result.Type)
        End Function

        Private Sub ReportUndefinedOperatorError(
            syntax As UnaryExpressionSyntax,
            operand As BoundExpression,
            diagnostics As DiagnosticBag
        )
            If operand.Type.IsErrorType() Then
                Return ' Let's not report more errors.
            End If

            ReportDiagnostic(diagnostics, syntax, ErrorFactory.ErrorInfo(ERRID.ERR_UnaryOperand2, syntax.OperatorToken, operand.Type))
        End Sub

    End Class

End Namespace
