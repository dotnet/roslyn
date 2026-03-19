' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Collections
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

                Return factory.StringLiteral(ConstantValue.Create(valueWithEscapes.Replace("{{", "{").Replace("}}", "}")))

            Else
                Debug.Assert(node.ConstructionOpt IsNot Nothing)
                Return VisitExpression(node.ConstructionOpt)
            End If

        End Function

        Private Function RewriteInterpolatedStringConversion(conversion As BoundConversion) As BoundExpression

            Debug.Assert((conversion.ConversionKind And ConversionKind.InterpolatedString) = ConversionKind.InterpolatedString)

            Dim targetType = conversion.Type
            Dim node = DirectCast(conversion.Operand, BoundInterpolatedStringExpression)
            Debug.Assert(node.ConstructionOpt IsNot Nothing)

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
            Return VisitExpression(node.ConstructionOpt)
        End Function

    End Class

End Namespace
