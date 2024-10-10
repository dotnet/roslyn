' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Binder

        Private Function BindInterpolatedStringExpression(syntax As InterpolatedStringExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression

            Dim contentBuilder = ArrayBuilder(Of BoundNode).GetInstance()

            For Each item In syntax.Contents

                Select Case item.Kind
                    Case SyntaxKind.InterpolatedStringText
                        contentBuilder.Add(BindInterpolatedStringText(DirectCast(item, InterpolatedStringTextSyntax), diagnostics))
                    Case SyntaxKind.Interpolation
                        contentBuilder.Add(BindInterpolation(DirectCast(item, InterpolationSyntax), diagnostics))
                    Case Else
                        Throw ExceptionUtilities.Unreachable
                End Select
            Next

            Return New BoundInterpolatedStringExpression(syntax, contentBuilder.ToImmutableAndFree(), constructionOpt:=Nothing, type:=GetSpecialType(SpecialType.System_String, syntax, diagnostics))

        End Function

        Private Function BindInterpolatedStringText(syntax As InterpolatedStringTextSyntax, diagnostics As BindingDiagnosticBag) As BoundLiteral
            Return CreateStringLiteral(syntax, syntax.TextToken.ValueText, compilerGenerated:=False, diagnostics:=diagnostics)
        End Function

        Private Function BindInterpolation(syntax As InterpolationSyntax, diagnostics As BindingDiagnosticBag) As BoundInterpolation

            Dim expression = BindRValue(syntax.Expression, diagnostics)

            Dim alignmentOpt As BoundExpression = Nothing

            If syntax.AlignmentClause IsNot Nothing Then
                alignmentOpt = BindRValue(syntax.AlignmentClause.Value, diagnostics)

                If alignmentOpt.IsConstant AndAlso alignmentOpt.ConstantValueOpt.IsIntegral Then

                    Dim constantValue = alignmentOpt.ConstantValueOpt

                    If constantValue.IsNegativeNumeric Then
                        If constantValue.Int64Value < -Short.MaxValue Then
                            ReportDiagnostic(diagnostics, syntax.AlignmentClause.Value, ERRID.ERR_InterpolationAlignmentOutOfRange)
                        End If
                    Else
                        If constantValue.UInt64Value > Short.MaxValue Then
                            ReportDiagnostic(diagnostics, syntax.AlignmentClause.Value, ERRID.ERR_InterpolationAlignmentOutOfRange)
                        End If
                    End If
                Else
                    ReportDiagnostic(diagnostics, syntax.AlignmentClause.Value, ERRID.ERR_ExpectedIntLiteral)
                End If
            End If

            Dim formatStringOpt = If(syntax.FormatClause IsNot Nothing,
                                  CreateStringLiteral(syntax.FormatClause, syntax.FormatClause.FormatStringToken.ValueText, compilerGenerated:=False, diagnostics:=diagnostics),
                                  Nothing)

            Return New BoundInterpolation(syntax, expression, alignmentOpt, formatStringOpt)

        End Function

        Private Function BindUnconvertedInterpolatedStringToString(node As BoundInterpolatedStringExpression, diagnostics As BindingDiagnosticBag) As BoundExpression

            Debug.Assert(node.Type.SpecialType = SpecialType.System_String)

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
            If node.IsEmpty OrElse Not node.HasInterpolations Then
                Return node
            End If

            Return node.Update(node.Contents,
                               constructionOpt:=TryInvokeInterpolatedStringFactory(node,
                                                                                   factoryType:=node.Type,
                                                                                   factoryMethodName:="Format",
                                                                                   targetType:=node.Type,
                                                                                   diagnostics),
                               node.Type)
        End Function

        Private Function BindUnconvertedInterpolatedStringToFormattable(syntax As SyntaxNode, node As BoundInterpolatedStringExpression, targetType As TypeSymbol, diagnostics As BindingDiagnosticBag) As BoundExpression

            Debug.Assert(targetType.Equals(Compilation.GetWellKnownType(WellKnownType.System_FormattableString)) OrElse
                         targetType.Equals(Compilation.GetWellKnownType(WellKnownType.System_IFormattable)))

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

            Return node.Update(node.Contents,
                               constructionOpt:=TryInvokeInterpolatedStringFactory(node,
                                                                                   factoryType:=GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_FormattableStringFactory, syntax, diagnostics),
                                                                                   factoryMethodName:="Create",
                                                                                   targetType,
                                                                                   diagnostics),
                               node.Type)
        End Function

        Private Function TryInvokeInterpolatedStringFactory(node As BoundInterpolatedStringExpression, factoryType As TypeSymbol, factoryMethodName As String, targetType As TypeSymbol, diagnostics As BindingDiagnosticBag) As BoundExpression

            Dim hasErrors As Boolean = False

            If factoryType.IsErrorType() OrElse
               node.HasErrors OrElse
               node.Contents.OfType(Of BoundInterpolation)().Any(
                    Function(interpolation) interpolation.Expression.HasErrors OrElse
                                            (interpolation.AlignmentOpt IsNot Nothing AndAlso
                                             Not (interpolation.AlignmentOpt.IsConstant AndAlso interpolation.AlignmentOpt.ConstantValueOpt.IsIntegral))) Then
                Return Nothing
            End If

            Dim lookup = LookupResult.GetInstance()
            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

            LookupMember(lookup, factoryType, factoryMethodName, 0, LookupOptions.MustNotBeInstance Or LookupOptions.MethodsOnly Or LookupOptions.AllMethodsOfAnyArity, useSiteInfo)

            diagnostics.Add(node, useSiteInfo)

            If lookup.Kind = LookupResultKind.Inaccessible Then
                hasErrors = True
            ElseIf Not lookup.IsGood Then
                lookup.Free()
                GoTo Report_ERR_InterpolatedStringFactoryError
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

                        interpolationOrdinal += 1

                        Dim interpolation = DirectCast(item, BoundInterpolation)

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

            arguments(0) = CreateStringLiteral(node.Syntax, formatStringBuilderHandle.ToStringAndFree(), compilerGenerated:=True, diagnostics)

            Dim result As BoundExpression = MakeRValue(BindInvocationExpression(node.Syntax,
                                                                                node.Syntax,
                                                                                TypeCharacter.None,
                                                                                methodGroup,
                                                                                arguments.ToImmutableAndFree(),
                                                                                Nothing,
                                                                                diagnostics,
                                                                                callerInfoOpt:=Nothing,
                                                                                forceExpandedForm:=True), diagnostics).MakeCompilerGenerated()

            If Not result.Type.Equals(targetType) Then
                result = ApplyImplicitConversion(node.Syntax, targetType, result, diagnostics).MakeCompilerGenerated()
            End If

            If hasErrors OrElse result.HasErrors Then
                GoTo Report_ERR_InterpolatedStringFactoryError
            End If

            Return result

Report_ERR_InterpolatedStringFactoryError:
            ReportDiagnostic(diagnostics, node.Syntax, ErrorFactory.ErrorInfo(ERRID.ERR_InterpolatedStringFactoryError, factoryType.Name, factoryMethodName))
            Return Nothing
        End Function

    End Class
End Namespace
