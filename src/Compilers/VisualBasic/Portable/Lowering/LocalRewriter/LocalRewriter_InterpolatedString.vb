' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class LocalRewriter

        Public Overrides Function VisitInterpolatedStringExpression(node As BoundInterpolatedStringExpression) As BoundNode

            Debug.Assert(node.Type.SpecialType = SpecialType.System_String)
            Dim factory = New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics)

            ' We lower an interpolated string into an invocation of String.Format or System.Runtime.CompilerServices.FormattableStringFactory.Create.
            ' For example, we translate the expression:
            '
            '     $"Jenny, don't change your number: {phoneNumber:###-####}."
            '
            ' into
            '
            '     String.Format("Jenny, don't change your number: {0:###-####}.", phoneNumber)
            '
            ' TODO: A number of optimizations would be beneficial in the generated code.
            ' 
            ' (1) If there is no width or format, and the argument is a value type, call .ToString()
            '     on it directly so that we avoid the boxing overhead.
            '
            ' (2) For the built-in types, we can use .ToString(string format) for some format strings.
            '     Detect those cases that can be handled that way and take advantage of them.
            If node.IsEmpty Then

                Return factory.StringLiteral(ConstantValue.Create(String.Empty))

            ElseIf Not node.HasInterpolations Then
                ' We have to process all of the escape sequences in the string.
                Dim valueWithEscapes = DirectCast(node.Contents(0), BoundLiteral).Value.StringValue

                Return factory.StringLiteral(ConstantValue.Create(UnescapeLiteralInterpolatedString(valueWithEscapes)))

            Else
                ' If all the interpolations can be interpreted as strings (including char constants),
                ' there is no alignment or format string,
                ' and we are not in an expression tree definition,
                ' We can use String.Concat by interpreting them as string concatenations.
                ' For example, we translate the expression:
                '     $"Jenny, don't change your number: {phoneNumberString}."
                ' into
                '     String.Concat("Jenny, don't change your number: ", phoneNumberString)
                ' via
                '     "Jenny, don't change your number: " & phoneNumberString
                ' This optimization has been adopted in C# for .NET Framework.
                Dim canHideOptimization = Not _inExpressionLambda
                If canHideOptimization AndAlso node.Contents.Where(
                    Function(item) item.Kind = BoundKind.Interpolation
                ).All(
                    Function(item) IsOneInterpolationLowerableToConcat(DirectCast(item, BoundInterpolation))
                ) Then
                    Return ConvertToConcatenationNode(node.Contents, node.Type, factory)
                End If

                Return InvokeInterpolatedStringFactory(node, node.Type, "Format", node.Type, factory, canHideOptimization)
            End If

        End Function

        Private Function UnescapeLiteralInterpolatedString(valueWithEscapes As String) As String
            Return valueWithEscapes.Replace("{{", "{").Replace("}}", "}")
        End Function

        Private Function EscapeStringConstantForLiteralInterpolatedString(value As String) As String
            Return value.Replace("{", "{{").Replace("}", "}}")
        End Function

        Private Function RewriteInterpolatedStringConversion(conversion As BoundConversion) As BoundExpression

            Debug.Assert((conversion.ConversionKind And ConversionKind.InterpolatedString) = ConversionKind.InterpolatedString)

            Dim targetType = conversion.Type
            Dim node = DirectCast(conversion.Operand, BoundInterpolatedStringExpression)
            Dim binder = node.Binder

            Debug.Assert(targetType.Equals(binder.Compilation.GetWellKnownType(WellKnownType.System_FormattableString)) OrElse
                         targetType.Equals(binder.Compilation.GetWellKnownType(WellKnownType.System_IFormattable)))

            ' We lower an interpolated string into an invocation of System.Runtime.CompilerServices.FormattableStringFactory.Create.
            ' For example, we translate the expression:
            '
            '     $"Jenny, don't change your number: {phoneNumber:###-####}."
            '
            ' into
            '
            '     FormattableStringFactory.Create("Jenny, don't change your number: {0:###-####}.", phoneNumber)
            '
            ' TODO: A number of optimizations would be beneficial in the generated code.
            ' 
            ' (1) If there is no width or format, and the argument is a value type, call .ToString()
            '     on it directly so that we avoid the boxing overhead.
            '
            ' (2) For the built-in types, we can use .ToString(string format) for some format strings.
            '     Detect those cases that can be handled that way and take advantage of them.
            Return InvokeInterpolatedStringFactory(node,
                                                   binder.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory, conversion.Syntax, _diagnostics),
                                                   "Create",
                                                   conversion.Type,
                                                   New SyntheticBoundNodeFactory(_topMethod, _currentMethodOrLambda, node.Syntax, _compilationState, _diagnostics),
                                                   False)

        End Function

        Private Function InvokeInterpolatedStringFactory(node As BoundInterpolatedStringExpression, factoryType As TypeSymbol, factoryMethodName As String, targetType As TypeSymbol, factory As SyntheticBoundNodeFactory, Optional trustsFormatProvider As Boolean = False) As BoundExpression

            Dim hasErrors As Boolean = False

            If factoryType.IsErrorType() Then
                GoTo ReturnBadExpression
            End If

            Dim binder = node.Binder

            Dim lookup = LookupResult.GetInstance()
            Dim useSiteInfo = GetNewCompoundUseSiteInfo()

            binder.LookupMember(lookup, factoryType, factoryMethodName, 0, LookupOptions.MustNotBeInstance Or LookupOptions.MethodsOnly Or LookupOptions.AllMethodsOfAnyArity, useSiteInfo)

            _diagnostics.Add(node, useSiteInfo)

            If lookup.Kind = LookupResultKind.Inaccessible Then
                hasErrors = True
            ElseIf Not lookup.IsGood Then
                lookup.Free()
                GoTo ReturnBadExpression
            End If

            Dim methodGroup = New BoundMethodGroup(node.Syntax, Nothing, lookup.Symbols.ToDowncastedImmutable(Of MethodSymbol), lookup.Kind, Nothing, QualificationKind.QualifiedViaTypeName).MakeCompilerGenerated()
            lookup.Free()

            Dim formatStringBuilderHandle = PooledStringBuilder.GetInstance()
            Dim arguments = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim interpolationOrdinal = -1

            arguments.Add(Nothing) ' Placeholder for format string.

            For Each item In node.Contents

                Select Case item.Kind

                    Case BoundKind.Literal

                        formatStringBuilderHandle.Builder.Append(DirectCast(item, BoundLiteral).Value.StringValue)

                    Case BoundKind.Interpolation

                        Dim interpolation = DirectCast(item, BoundInterpolation)

                        ' Don't trust format provider everywhen.
                        ' They MAY format literals and embedded strings differently.
                        ' (trustsFormatProvider = False)
                        If trustsFormatProvider AndAlso
                            interpolation.AlignmentOpt Is Nothing AndAlso
                            interpolation.FormatStringOpt Is Nothing AndAlso
                            TryInsertConstantEquivalentIntoLiteral(interpolation.Expression, formatStringBuilderHandle.Builder) Then
                            Continue For
                        End If

                        interpolationOrdinal += 1

                        With formatStringBuilderHandle.Builder

                            .Append("{"c)

                            .Append(interpolationOrdinal.ToString(Globalization.CultureInfo.InvariantCulture))

                            If interpolation.AlignmentOpt IsNot Nothing Then
                                Debug.Assert(interpolation.AlignmentOpt.IsConstant AndAlso interpolation.AlignmentOpt.ConstantValueOpt.IsIntegral)
                                .Append(","c)
                                .Append(interpolation.AlignmentOpt.ConstantValueOpt.Int64Value.ToString(Globalization.CultureInfo.InvariantCulture))
                            End If

                            If interpolation.FormatStringOpt IsNot Nothing Then
                                .Append(":")
                                .Append(interpolation.FormatStringOpt.Value.StringValue)
                            End If

                            .Append("}"c)
                        End With

                        arguments.Add(interpolation.Expression)

                    Case Else
                        Throw ExceptionUtilities.Unreachable()
                End Select

            Next

            arguments(0) = factory.StringLiteral(ConstantValue.Create(formatStringBuilderHandle.ToStringAndFree())).MakeCompilerGenerated()

            Dim result As BoundExpression = binder.MakeRValue(binder.BindInvocationExpression(node.Syntax,
                                                                                              node.Syntax,
                                                                                              TypeCharacter.None,
                                                                                              methodGroup,
                                                                                              arguments.ToImmutableAndFree(),
                                                                                              Nothing,
                                                                                              _diagnostics,
                                                                                              callerInfoOpt:=Nothing,
                                                                                              forceExpandedForm:=True), _diagnostics).MakeCompilerGenerated()

            If Not result.Type.Equals(targetType) Then
                result = binder.ApplyImplicitConversion(node.Syntax, targetType, result, _diagnostics).MakeCompilerGenerated()
            End If

            If hasErrors OrElse result.HasErrors Then
                GoTo ReturnBadExpression
            End If

            result = VisitExpression(result)

            Return result

ReturnBadExpression:
            ReportDiagnostic(node, ErrorFactory.ErrorInfo(ERRID.ERR_InterpolatedStringFactoryError, factoryType.Name, factoryMethodName), _diagnostics)
            Return factory.Convert(targetType, factory.BadExpression(DirectCast(MyBase.VisitInterpolatedStringExpression(node), BoundExpression)))

        End Function
        ''' <summary>
        ''' Returns true if the interpolation part can be lowered to a string concatenation.
        ''' </summary>
        ''' <param name="interpolation">interpolation node</param>
        ''' <returns><see langword="True"/> if the part can be accepted as a part of string concatenation.</returns>
        Private Function IsOneInterpolationLowerableToConcat(interpolation As BoundInterpolation) As Boolean
            If interpolation.AlignmentOpt IsNot Nothing OrElse interpolation.FormatStringOpt IsNot Nothing Then
                Return False
            End If
            If IsTreatedAsStringLiteralInStringConcatenation(interpolation.Expression) Then
                Return True
            End If
            Return interpolation.Expression.Type.IsStringType()
        End Function

        Private Function TryInsertConstantEquivalentIntoLiteral(expression As BoundExpression, builder As StringBuilder) As Boolean
            If expression.ConstantValueOpt IsNot Nothing Then
                With expression.ConstantValueOpt
                    If .IsString Then
                        builder?.Append(EscapeStringConstantForLiteralInterpolatedString(.StringValue))
                        Return True
                    End If
                    If .IsChar Then
                        ' Optimize instead of EscapeStringConstantForLiteralInterpolatedString(String)
                        If .CharValue = "{"c Then
                            builder?.Append("{{")
                        ElseIf .CharValue = "}"c Then
                            builder?.Append("}}")
                        Else
                            builder?.Append(.CharValue)
                        End If
                        Return True
                    End If
                End With
            End If

            Return False
        End Function

        Private Function IsTreatedAsStringLiteralInStringConcatenation(expression As BoundExpression) As Boolean
            If Not expression.IsConstant Then
                Return False
            End If
            Return expression.ConstantValueOpt.IsString OrElse expression.ConstantValueOpt.IsChar
        End Function

        Private Function ConvertElementToConcatenationOperand(item As BoundNode, factory As SyntheticBoundNodeFactory) As BoundExpression
            Select Case item.Kind
                Case BoundKind.Literal
                    Dim literal = DirectCast(item, BoundLiteral)
                    Debug.Assert(literal.Value.StringValue IsNot Nothing)
                    Dim literalValue = literal.Value.StringValue
                    Dim unescapedLiteralValue = UnescapeLiteralInterpolatedString(literalValue)
                    Return If(
                        String.Equals(literalValue, unescapedLiteralValue),
                        literal,
                        factory.StringLiteral(ConstantValue.Create(unescapedLiteralValue))
                    )
                Case BoundKind.Interpolation
                    ' We don't have to check the existences of alignment or format string options here because we have done
                    Dim interpolation = DirectCast(item, BoundInterpolation)
                    Debug.Assert(interpolation.FormatStringOpt Is Nothing)
                    Debug.Assert(interpolation.AlignmentOpt Is Nothing)
                    Dim expression = interpolation.Expression
                    If expression.ConstantValueOpt?.IsChar Then
                        Return factory.StringLiteral(ConstantValue.Create(expression.ConstantValueOpt.CharValue.ToString()))
                    End If
                    ' If we consider some pseudo string literal expressions (e.g. Char.ConvertFromUtf32(Integer) and String.Empty) in the future,
                    ' insert here:
                    ' (1) let string constants be passed directly first (2) Convert such expressions to string constants after (1)
                    Return VisitExpression(expression)
                Case Else
                    Throw ExceptionUtilities.Unreachable()
            End Select
        End Function

        Private Function ConvertToConcatenationNode(contents As ImmutableArray(Of BoundNode), factoryType As TypeSymbol, factory As SyntheticBoundNodeFactory) As BoundExpression
            Return contents.AsEnumerable().Aggregate(Of BoundExpression)(
                    Nothing,
                    Function(left, right)
                        Dim modifiedRight = ConvertElementToConcatenationOperand(right, factory)
                        If left Is Nothing Then
                            Return modifiedRight
                        End If
                        Return RewriteConcatenateOperator(
                            factory.Binary(
                                BinaryOperatorKind.Concatenate,
                                factoryType, left, modifiedRight
                            )
                        )
                    End Function
                )
        End Function

    End Class

End Namespace
