' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class LocalRewriter

        Public Overrides Function VisitInterpolatedStringExpression(node As BoundInterpolatedStringExpression) As BoundNode
            node = DirectCast(MyBase.VisitInterpolatedStringExpression(node), BoundInterpolatedStringExpression)

            ' We lower an interpolated string into an invocation of String.Format.
            ' For example, we translate the expression:
            '
            '     $"Jenny, don't change your number: {phoneNumber:###-####}."
            '
            ' into
            '
            '     String.Format("Jenny, don't change your number: {0:###-####}.", phoneNumber)
            ' 

            '
            ' TODO: A number of optimizations would be beneficial in the generated code.
            ' 
            ' (1) Avoid the object array allocation by calling an overload of Format that has a fixed
            '     number of arguments. Check what is available in the platform and make the best choice,
            '     so that we benefit from any additional overloads that may be added in the future
            '
            ' (2) If there is no width or format, and the argument is a value type, call .ToString()
            '     on it directly so that we avoid the boxing overhead.
            '
            ' (3) For the built-in types, we can use .ToString(string format) for some format strings.
            '     Detect those cases that can be handled that way and take advantage of them.
            Dim factory As New SyntheticBoundNodeFactory(topMethod, currentMethodOrLambda, node.Syntax, compilationState, diagnostics)

            If node.Contents.IsEmpty Then
                Return factory.StringLiteral(ConstantValue.Create(String.Empty))
            End If

            If node.Contents.Length = 1 AndAlso node.Contents(0).Kind = BoundKind.Literal Then
                ' We have to process all of the escape sequences in the string.
                Dim valueWithEscapes = DirectCast(node.Contents(0), BoundLiteral).Value.StringValue

                Return factory.StringLiteral(ConstantValue.Create(valueWithEscapes.Replace("{{", "{").Replace("}}", "}")))
            End If

            Dim formatStringBuilderHandle = PooledStringBuilder.GetInstance()
            Dim interpolations = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim interpolationOrdinal = -1

            For Each item In node.Contents

                Select Case item.Kind

                    Case BoundKind.Literal

                        formatStringBuilderHandle.Builder.Append(DirectCast(item, BoundLiteral).Value.StringValue)

                    Case BoundKind.Interpolation

                        interpolationOrdinal += 1

                        Dim interpolation = DirectCast(item, BoundInterpolation)

                        With formatStringBuilderHandle.Builder

                            .Append("{"c)

                            .Append(interpolationOrdinal)

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

                        interpolations.Add(Convert(factory, GetSpecialType(SpecialType.System_Object), interpolation.Expression))

                    Case Else
                        Throw ExceptionUtilities.Unreachable()
                End Select

            Next

            Dim formatString = factory.StringLiteral(ConstantValue.Create(formatStringBuilderHandle.ToStringAndFree()))

            Dim stringFormatMethod As MethodSymbol = Nothing
            If Not TryGetSpecialMember(stringFormatMethod, SpecialMember.System_String__Format, node.Syntax) Then
                interpolations.Free()
                Return node
            End If

            Return factory.Call(Nothing, stringFormatMethod, {formatString, factory.Array(Compilation.ObjectType, interpolations.ToImmutableAndFree())})

        End Function

    End Class

End Namespace