' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class ExpressionLambdaRewriter

        Private Const s_coalesceLambdaParameterName = "CoalesceLHS"

        Private Function VisitTernaryConditionalExpression(node As BoundTernaryConditionalExpression) As BoundExpression
            Dim condition As BoundExpression = Visit(node.Condition)
            Dim whenTrue As BoundExpression = Visit(node.WhenTrue)
            Dim whenFalse As BoundExpression = Visit(node.WhenFalse)
            Return ConvertRuntimeHelperToExpressionTree("Condition", condition, whenTrue, whenFalse)
        End Function

        Private Function VisitBinaryConditionalExpression(node As BoundBinaryConditionalExpression) As BoundExpression
            Dim testExpression As BoundExpression = node.TestExpression
            Dim convTestExpr As BoundExpression = node.ConvertedTestExpression

            Dim resultType As TypeSymbol = node.Type
            Dim testExpressionType As TypeSymbol = testExpression.Type

            Dim rewrittenTestExpression As BoundExpression = Visit(testExpression)
            Dim rewrittenElseExpression As BoundExpression = Visit(node.ElseExpression)

            ' NOTE: it is possible that testExpressionType is the same as resultType in which 
            '       case conversion is not needed

            ' NOTE: if testExpressionType is a nullable and the resultType of the 'Coalesce'
            '       is its underlying type, runtime will perform conversion itself

            If convTestExpr Is Nothing OrElse resultType.IsSameTypeIgnoringAll(testExpressionType) OrElse
                    (testExpressionType.IsNullableType AndAlso resultType.IsSameTypeIgnoringAll(testExpressionType.GetNullableUnderlyingType)) Then
                Return ConvertRuntimeHelperToExpressionTree("Coalesce", rewrittenTestExpression, rewrittenElseExpression)
            End If

            Select Case convTestExpr.Kind
                Case BoundKind.Conversion
                    Dim conversion = DirectCast(convTestExpr, BoundConversion)
                    Dim paramSymbol As ParameterSymbol = CreateCoalesceLambdaParameterSymbol(testExpressionType)
                    Dim lambdaBody As BoundExpression = BuildLambdaBodyForCoalesce(conversion, resultType, paramSymbol, conversion.Checked)
                    Dim coalesceLambda As BoundExpression = BuildLambdaForCoalesceCall(resultType, paramSymbol, lambdaBody)
                    Return ConvertRuntimeHelperToExpressionTree("Coalesce", rewrittenTestExpression, rewrittenElseExpression, coalesceLambda)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(convTestExpr.Kind)
            End Select
        End Function

        Private Function CreateCoalesceLambdaParameterSymbol(paramType As TypeSymbol) As ParameterSymbol
            Return Me._factory.SynthesizedParameter(paramType, s_coalesceLambdaParameterName)
        End Function

        Private Function CreateCoalesceLambdaParameter(paramSymbol As ParameterSymbol) As BoundExpression
            Return Me._factory.Parameter(paramSymbol).MakeRValue()
        End Function

        Private Function BuildLambdaForCoalesceCall(toType As TypeSymbol, lambdaParameter As ParameterSymbol, body As BoundExpression) As BoundExpression
            Dim parameterExpressionType As TypeSymbol = _factory.WellKnownType(WellKnownType.System_Linq_Expressions_ParameterExpression)

            Dim paramLocalSymbol As LocalSymbol = Me._factory.SynthesizedLocal(parameterExpressionType)
            Dim parameterReference As BoundLocal = Me._factory.Local(paramLocalSymbol, True)
            Dim parameter As BoundExpression = ConvertRuntimeHelperToExpressionTree("Parameter", _factory.[Typeof](lambdaParameter.Type), _factory.Literal(s_coalesceLambdaParameterName))

            Me._parameterMap(lambdaParameter) = parameterReference.MakeRValue
            Dim convertedValue As BoundExpression = Visit(body)
            Me._parameterMap.Remove(lambdaParameter)

            Dim result As BoundExpression =
                Me._factory.Sequence(ImmutableArray.Create(Of LocalSymbol)(
                                        paramLocalSymbol),
                                     ImmutableArray.Create(Of BoundExpression)(
                                         Me._factory.AssignmentExpression(parameterReference, parameter)),
                                     ConvertRuntimeHelperToExpressionTree(
                                         "Lambda",
                                         convertedValue,
                                         Me._factory.Array(
                                             parameterExpressionType,
                                             ImmutableArray.Create(Of BoundExpression)(
                                                 parameterReference.MakeRValue))))
            Return result
        End Function

        Private Function BuildLambdaBodyForCoalesce(conversion As BoundConversion, toType As TypeSymbol, lambdaParameter As ParameterSymbol, isChecked As Boolean) As BoundExpression
            Dim parameter As BoundExpression = CreateCoalesceLambdaParameter(lambdaParameter)

            If (conversion.ConversionKind And ConversionKind.UserDefined) = 0 Then
                ' This is a predefined conversion, but the type of the parameter may be different from 
                ' the type of the argument of 'conversion' in case 'parameter' is a nullable and the real 
                ' conversion argument is not
                Dim parameterType As TypeSymbol = parameter.Type
                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                Dim convKind As ConversionKind = Conversions.ClassifyPredefinedConversion(parameterType, conversion.Operand.Type, useSiteDiagnostics)
                Diagnostics.Add(conversion, useSiteDiagnostics)

                If (convKind And ConversionKind.NarrowingNullable) = ConversionKind.NarrowingNullable AndAlso Not toType.IsNullableType Then
                    ' Convert to non-nullable type first to mimic Dev11
                    Return Me._factory.Convert(toType, CreateUserDefinedNullableToUnderlyingConversion(parameter, parameterType, isChecked), isChecked)
                Else
                    Return Me._factory.Convert(toType, parameter, isChecked)
                End If
            End If

            ' For user defined conversion we need to keep the proper method call 
            Return ReplaceArgWithParameterInUserDefinedConversion(conversion, toType, parameter, isChecked)
        End Function

        Private Function CreateUserDefinedNullableToUnderlyingConversion(expression As BoundExpression, nullableType As TypeSymbol, isChecked As Boolean) As BoundExpression
            Debug.Assert(nullableType.IsNullableType)
            Dim underlyingType As TypeSymbol = nullableType.GetNullableUnderlyingType

            Dim helper As MethodSymbol = DirectCast(Me._factory.SpecialMember(
                        SpecialMember.System_Nullable_T__op_Explicit_ToT), MethodSymbol)

            If helper Is Nothing Then
                ' Method not found, fall back on default conversion
                Return Me._factory.Convert(underlyingType, expression, isChecked)
            End If

            ' Get real method
            helper = DirectCast(DirectCast(nullableType, SubstitutedNamedType).GetMemberForDefinition(helper), MethodSymbol)

            Dim syntax As SyntaxNode = expression.Syntax
            Return New BoundConversion(
                            syntax,
                            New BoundUserDefinedConversion(
                                syntax,
                                New BoundCall(
                                    syntax,
                                    method:=helper,
                                    methodGroupOpt:=Nothing,
                                    receiverOpt:=Nothing,
                                    arguments:=ImmutableArray.Create(Of BoundExpression)(expression),
                                    constantValueOpt:=Nothing,
                                    suppressObjectClone:=True,
                                    type:=underlyingType),
                                inOutConversionFlags:=CByte(0),
                                type:=nullableType),
                            conversionKind:=ConversionKind.Narrowing Or ConversionKind.UserDefined,
                            checked:=isChecked,
                            explicitCastInCode:=False,
                            constantValueOpt:=Nothing,
                            type:=underlyingType)
        End Function

        ''' <summary>
        ''' Given user defined conversion node replace the operand with the coalesce lambda parameter. 
        ''' 
        ''' The input bound conversion node must have the following form:
        '''     --> BoundConversion [UserDefined]
        '''         --> [optional] BoundConversion (OutConversion)
        '''             --> BoundCall [shared method, no receiver, one argument]
        ''' 
        ''' The OUTPUT bound conversion node will have the following form:
        '''     --> BoundConversion *updated*
        '''         --> [optional] BoundConversion *updated*
        '''             --> BoundCall [shared method, no receiver, * updated argument *]
        '''                 --> [optional] BoundConversion (parameter from nullable to value)
        '''                     --> *parameter*
        ''' 
        ''' </summary>
        Private Function ReplaceArgWithParameterInUserDefinedConversion(conversion As BoundConversion,
                                                                        toType As TypeSymbol,
                                                                        parameter As BoundExpression,
                                                                        isChecked As Boolean) As BoundExpression

            ' User defined conversion
            Debug.Assert((conversion.ConversionKind And ConversionKind.UserDefined) <> 0)
            Dim userDefinedConv = DirectCast(conversion.Operand, BoundUserDefinedConversion)
            Dim [call] As BoundCall = userDefinedConv.Call
            Dim callType As TypeSymbol = [call].Type
            Dim method As MethodSymbol = [call].Method
            Dim outConv As BoundConversion = userDefinedConv.OutConversionOpt

            Debug.Assert(outConv IsNot Nothing AndAlso
                         toType.IsSameTypeIgnoringAll(outConv.Type) OrElse
                         toType.IsSameTypeIgnoringAll([call].Type))
            Debug.Assert(method.ReturnType = callType)
            Debug.Assert(toType = conversion.Type)

            Dim expectedParameterType As TypeSymbol = method.Parameters(0).Type
            Dim realParameterType As TypeSymbol = parameter.Type
            Debug.Assert(expectedParameterType.GetNullableUnderlyingTypeOrSelf = realParameterType.GetNullableUnderlyingTypeOrSelf)

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim innerConversion As ConversionKind = Conversions.ClassifyConversion(realParameterType, expectedParameterType, useSiteDiagnostics).Key
            Diagnostics.Add(conversion, useSiteDiagnostics)

            Dim innerConversionApplied As Boolean = Not Conversions.IsIdentityConversion(innerConversion)
            If innerConversionApplied Then
                Debug.Assert((innerConversion And ConversionKind.NarrowingNullable) = ConversionKind.NarrowingNullable)

                'If outConv Is Nothing OrElse outConv.ConversionKind = ConversionKind.WideningNullable Then
                ' NOTE: in simple cases where inner conversion is (T? -> T) and outer conversion is (S -> S?),
                '       Dev11 does generate simplified lifted conversion

                parameter = Me._factory.Convert(expectedParameterType, parameter, isChecked)

                'Else
                '    ' NOTE: Otherwise Dev11 emits conversion explicitly referencing 
                '    '           [T System.Nullable`1[T]::op_Explicit(System.Nullable`1[T])]
                '    parameter = CreateUserDefinedNullableToUnderlyingConversion(parameter, realParameterType, isChecked)
                '    innerConversionApplied = False
                'End If
            End If

            [call] = [call].Update(
                method,
                Nothing,
                Nothing,
                ImmutableArray.Create(Of BoundExpression)(parameter),
                Nothing,
                Nothing,
                isLValue:=False,
                suppressObjectClone:=True,
                type:=callType)

            If outConv IsNot Nothing Then
                outConv = outConv.Update([call], outConv.ConversionKind, outConv.Checked, outConv.ExplicitCastInCode, outConv.ConstantValueOpt,
                                         outConv.ExtendedInfoOpt, outConv.Type)
            End If

            Dim newInOutConversionFlags As Byte = CByte(If(outConv IsNot Nothing, 2, 0) + If(innerConversionApplied, 1, 0))
            userDefinedConv = userDefinedConv.Update(If(outConv, DirectCast([call], BoundExpression)), newInOutConversionFlags, realParameterType)

            Dim newConversionKind As ConversionKind = conversion.ConversionKind And Not ConversionKind.Nullable
            If realParameterType.IsNullableType AndAlso Not method.Parameters(0).Type.IsNullableType AndAlso
                    toType.IsNullableType AndAlso Not method.ReturnType.IsNullableType Then
                newConversionKind = newConversionKind Or ConversionKind.Nullable
            End If

            Return conversion.Update(userDefinedConv, newConversionKind,
                                     conversion.Checked, conversion.ExplicitCastInCode, conversion.ConstantValueOpt,
                                     conversion.ExtendedInfoOpt, toType)
        End Function

    End Class
End Namespace
