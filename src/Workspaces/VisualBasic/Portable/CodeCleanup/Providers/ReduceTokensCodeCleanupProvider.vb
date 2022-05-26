' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.ReduceTokens, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeCleanupProviderNames.AddMissingTokens, Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class ReduceTokensCodeCleanupProvider
        Inherits AbstractTokensCodeCleanupProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="https://github.com/dotnet/roslyn/issues/42820")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return PredefinedCodeCleanupProviderNames.ReduceTokens
            End Get
        End Property

        Protected Overrides Function GetRewriterAsync(document As Document, root As SyntaxNode, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken) As Task(Of Rewriter)
            Return Task.FromResult(Of Rewriter)(New ReduceTokensRewriter(spans, cancellationToken))
        End Function

        Private Class ReduceTokensRewriter
            Inherits AbstractTokensCodeCleanupProvider.Rewriter

            Public Sub New(spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken)
                MyBase.New(spans, cancellationToken)
            End Sub

            Public Overrides Function VisitLiteralExpression(node As LiteralExpressionSyntax) As SyntaxNode
                Dim newNode = DirectCast(MyBase.VisitLiteralExpression(node), LiteralExpressionSyntax)
                Dim literal As SyntaxToken = newNode.Token
                Const digitSeparator = "_"c

                ' Pretty list floating and decimal literals.
                Select Case literal.Kind
                    Case SyntaxKind.FloatingLiteralToken
                        ' Get the literal identifier text which needs to be pretty listed.
                        Dim idText = literal.GetIdentifierText()

                        ' Compiler has parsed the literal text as single/double value, fetch the string representation of this value.
                        Dim value As Double = 0
                        Dim valueText As String = GetFloatLiteralValueString(literal, value) + GetTypeCharString(literal.GetTypeCharacter())

                        If value = 0 OrElse idText.Contains(digitSeparator) Then
                            ' Overflow/underflow case or zero literal, skip pretty listing.
                            Return newNode
                        End If

                        ' If the string representation of the value differs from the identifier text, create a new literal token with same value but pretty listed "valueText".
                        If Not CaseInsensitiveComparison.Equals(valueText, idText) Then
                            Return newNode.ReplaceToken(literal, CreateLiteralToken(literal, valueText, value))
                        End If

                    Case SyntaxKind.DecimalLiteralToken
                        ' Get the literal identifier text which needs to be pretty listed.
                        Dim idText = literal.GetIdentifierText()
                        Dim value = DirectCast(literal.Value, Decimal)

                        If value = 0 OrElse idText.Contains(digitSeparator) Then
                            ' Overflow/underflow case or zero literal, skip pretty listing.
                            Return newNode
                        End If

                        ' Compiler has parsed the literal text as a decimal value, fetch the string representation of this value.
                        Dim valueText As String = GetDecimalLiteralValueString(value) + GetTypeCharString(literal.GetTypeCharacter())

                        If Not CaseInsensitiveComparison.Equals(valueText, idText) Then
                            Return newNode.ReplaceToken(literal, CreateLiteralToken(literal, valueText, value))
                        End If
                    Case SyntaxKind.IntegerLiteralToken
                        ' Get the literal identifier text which needs to be pretty listed.
                        Dim idText = literal.GetIdentifierText()

                        'The value will only ever be negative when we have a hex or oct value
                        'it's safe to cast to ULong as we check for negative values later
                        Dim value As ULong = CType(literal.Value, ULong)

                        If value = 0 AndAlso HasOverflow(literal.GetDiagnostics()) Then
                            'Overflow/underflow, skip pretty listing.
                            Return newNode
                        End If

                        Dim base = literal.GetBase()

                        If Not base.HasValue OrElse idText.Contains(digitSeparator) Then
                            Return newNode
                        End If

                        'fetch the string representation of this value in the correct base.
                        Dim valueText As String = GetIntegerLiteralValueString(literal.Value, base.Value) + GetTypeCharString(literal.GetTypeCharacter())

                        If Not CaseInsensitiveComparison.Equals(valueText, idText) Then
                            Return newNode.ReplaceToken(literal, CreateLiteralToken(literal, valueText, value))
                        End If
                End Select

                Return newNode
            End Function

            Private Shared Function GetTypeCharString(typeChar As TypeCharacter) As String
                Select Case typeChar
                    Case TypeCharacter.Single
                        Return "!"
                    Case TypeCharacter.SingleLiteral
                        Return "F"
                    Case TypeCharacter.Double
                        Return "#"
                    Case TypeCharacter.DoubleLiteral
                        Return "R"
                    Case TypeCharacter.Decimal
                        Return "@"
                    Case TypeCharacter.DecimalLiteral
                        Return "D"
                    Case TypeCharacter.Integer
                        Return "%"
                    Case TypeCharacter.IntegerLiteral
                        Return "I"
                    Case TypeCharacter.ShortLiteral
                        Return "S"
                    Case TypeCharacter.Long
                        Return "&"
                    Case TypeCharacter.LongLiteral
                        Return "L"
                    Case TypeCharacter.UIntegerLiteral
                        Return "UI"
                    Case TypeCharacter.UShortLiteral
                        Return "US"
                    Case TypeCharacter.ULongLiteral
                        Return "UL"
                    Case Else
                        Return ""
                End Select
            End Function

            Private Shared Function GetFloatLiteralValueString(literal As SyntaxToken, <Out> ByRef value As Double) As String
                Dim isSingle As Boolean = literal.GetTypeCharacter() = TypeCharacter.Single OrElse literal.GetTypeCharacter() = TypeCharacter.SingleLiteral

                ' Get the string representation of the value using The Round-trip ("R") Format Specifier.
                ' MSDN comments about "R" format specifier:
                '   The round-trip ("R") format specifier guarantees that a numeric value that is converted to a string will be parsed back into the same numeric value.
                '   This format is supported only for the Single, Double, and BigInteger types. 
                '   When a Single or Double value is formatted using this specifier, it is first tested using the general format, with 15 digits of precision for a Double and
                '   7 digits of precision for a Single. If the value is successfully parsed back to the same numeric value, it is formatted using the general format specifier.
                '   If the value is not successfully parsed back to the same numeric value, it is formatted using 17 digits of precision for a Double and 9 digits of precision for a Single. 

                ' Hence the possible actual precision values are:
                '   (a) Single: 7 or 9 and
                '   (b) Double: 15 or 17
                Dim valueText As String = GetValueStringCore(literal, isSingle, "R", value)

                ' Floating point values might be represented either in fixed point notation or scientific/exponent notation.
                ' MSDN comment for Standard Numeric Format Strings used in Single.ToString(String) API (or Double.ToString(String)):
                '   Fixed-point notation is used if the exponent that would result from expressing the number in scientific notation is greater than -5 and
                '   less than the precision specifier; otherwise, scientific notation is used.
                '
                ' However, Dev11 pretty lister differs from this for floating point values with exponent < 0.
                ' Instead of "greater than -5" mentioned above, it uses fixed point notation as long as exponent is greater than "-(precision + 2)".
                ' For example, consider pretty listing for Single literals:
                '     (i) Precision = 7
                '           0.0000001234567F        =>  0.0000001234567F            (exponent = -7: fixed point notation)
                '           0.00000001234567F       =>  0.00000001234567F           (exponent = -8: fixed point notation)
                '           0.000000001234567F      =>  1.234567E-9F                (exponent = -9: exponent notation)
                '           0.0000000001234567F     =>  1.234567E-10F               (exponent = -10: exponent notation)
                '     (ii) Precision = 9
                '           0.0000000012345678F     =>  0.00000000123456778F        (exponent = -9: fixed point notation)
                '           0.00000000012345678F    =>  0.000000000123456786F       (exponent = -10: fixed point notation)
                '           0.000000000012345678F   =>  1.23456783E-11F             (exponent = -11: exponent notation)
                '           0.0000000000012345678F  =>  1.23456779E-12F             (exponent = -12: exponent notation)
                '
                ' We replicate the same behavior below

                Dim exponentIndex As Integer = valueText.IndexOf("E"c)
                If exponentIndex > 0 Then
                    Dim exponent = Integer.Parse(valueText.Substring(exponentIndex + 1), CultureInfo.InvariantCulture)
                    If exponent < 0 Then
                        Dim defaultPrecision As Integer = If(isSingle, 7, 15)
                        Dim numSignificantDigits = exponentIndex - 1 ' subtract 1 for the decimal point
                        Dim actualPrecision As Integer = If(numSignificantDigits > defaultPrecision, defaultPrecision + 2, defaultPrecision)

                        If exponent > -(actualPrecision + 2) Then
                            ' Convert valueText to floating point notation.

                            ' Prepend "0.00000.."
                            Dim prefix = "0." + New String("0"c, -exponent - 1)

                            ' Get the significant digits string.
                            Dim significantDigitsStr = valueText.Substring(0, exponentIndex)

                            ' Remove the existing decimal point, if any, from valueText.
                            If significantDigitsStr.Length > 1 AndAlso significantDigitsStr(1) = "."c Then
                                significantDigitsStr = significantDigitsStr.Remove(1, 1)
                            End If

                            Return prefix + significantDigitsStr
                        End If
                    End If
                End If

                ' Single.ToString(String) might return result in exponential notation, where the exponent is formatted to at least 2 digits.
                ' Dev11 pretty lister is identical in all cases except when the exponent is exactly 2 digit with a leading zero, e.g. "2.3E+08F" or "2.3E-08F".
                ' Dev11 pretty lists these cases to "2.3E+8F" or "2.3E-8F" respectively; we do the same here.
                If isSingle Then
                    ' Check if valueText ends with "E+XX" or "E-XX"
                    If valueText.Length > 4 Then
                        If valueText.Length = exponentIndex + 4 Then
                            ' Trim zero for these two cases: "E+0X" or "E-0X"
                            If valueText(exponentIndex + 2) = "0"c Then
                                valueText = valueText.Remove(exponentIndex + 2, 1)
                            End If
                        End If
                    End If
                End If

                ' If the value is integral, then append ".0" to the valueText.
                If Not valueText.Contains("."c) Then
                    Return If(exponentIndex > 0, valueText.Insert(exponentIndex, ".0"), valueText + ".0")
                End If

                Return valueText
            End Function

            Private Shared Function GetValueStringCore(literal As SyntaxToken, isSingle As Boolean, formatSpecifier As String, <Out> ByRef value As Double) As String
                If isSingle Then
                    Dim singleValue = DirectCast(literal.Value, Single)
                    value = singleValue
                    Return singleValue.ToString(formatSpecifier, CultureInfo.InvariantCulture)
                Else
                    value = DirectCast(literal.Value, Double)
                    Return value.ToString(formatSpecifier, CultureInfo.InvariantCulture)
                End If
            End Function

            Private Shared Function GetDecimalLiteralValueString(value As Decimal) As String
                ' CONSIDER: If the parsed value is integral, i.e. has no decimal point, we should insert ".0" before "D" in the valueText (similar to the pretty listing for float literals).
                ' CONSIDER: However, native VB compiler doesn't do so for decimal literals, we will maintain compatibility.
                ' CONSIDER: We may want to consider taking a breaking change and make this consistent between float and decimal literals.

                Dim valueText = value.ToString(CultureInfo.InvariantCulture)

                ' Trim any redundant zeros after the decimal point.
                ' If all the digits after the decimal point are 0, then trim the decimal point as well.
                Dim parts As String() = valueText.Split("."c)
                If parts.Length() > 1 Then
                    ' We might have something like "1.000E+100". Ensure we only truncate the zeros before "E".
                    Dim partsAfterDot = parts(1).Split("E"c)

                    Dim stringToTruncate As String = partsAfterDot(0)
                    Dim truncatedString = stringToTruncate.TrimEnd("0"c)

                    If Not String.Equals(truncatedString, stringToTruncate, StringComparison.Ordinal) Then
                        Dim integralPart As String = parts(0)
                        Dim fractionPartOpt As String = If(truncatedString.Length > 0, "." + truncatedString, "")
                        Dim exponentPartOpt As String = If(partsAfterDot.Length > 1, "E" + partsAfterDot(1), "")
                        Return integralPart + fractionPartOpt + exponentPartOpt
                    End If
                End If

                Return valueText
            End Function

            Private Shared Function GetIntegerLiteralValueString(value As Object, base As LiteralBase) As String
                Select Case base
                    Case LiteralBase.Decimal
                        Return CType(value, ULong).ToString(CultureInfo.InvariantCulture)
                    Case LiteralBase.Hexadecimal
                        Return "&H" + ConvertToULong(value).ToString("X")
                    Case LiteralBase.Octal
                        Dim val1 As ULong = ConvertToULong(value)
                        Return "&O" + ConvertToOctalString(val1)
                    Case LiteralBase.Binary
                        Dim asLong = CType(ConvertToULong(value), Long)
                        Return "&B" + Convert.ToString(asLong, 2)
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(base)
                End Select
            End Function

            Private Shared Function CreateLiteralToken(token As SyntaxToken, newValueString As String, newValue As Object) As SyntaxToken
                ' create a new token with valid token text and carries over annotations attached to original token to be a good citizen 
                ' it might be replacing a token that has annotation injected by other code cleanups
                Dim leading = If(token.LeadingTrivia.Count > 0, token.LeadingTrivia, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker))
                Dim trailing = If(token.TrailingTrivia.Count > 0, token.TrailingTrivia, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker))

                Select Case token.Kind
                    Case SyntaxKind.FloatingLiteralToken
                        Return token.CopyAnnotationsTo(SyntaxFactory.FloatingLiteralToken(leading, newValueString, token.GetTypeCharacter(), DirectCast(newValue, Double), trailing))
                    Case SyntaxKind.DecimalLiteralToken
                        Return token.CopyAnnotationsTo(SyntaxFactory.DecimalLiteralToken(leading, newValueString, token.GetTypeCharacter(), DirectCast(newValue, Decimal), trailing))
                    Case SyntaxKind.IntegerLiteralToken
                        Return token.CopyAnnotationsTo(SyntaxFactory.IntegerLiteralToken(leading, newValueString, token.GetBase().Value, token.GetTypeCharacter(), DirectCast(newValue, ULong), trailing))
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(token.Kind)
                End Select
            End Function

            Private Shared Function ConvertToOctalString(value As ULong) As String
                Dim exponent As ULong = value
                Dim builder As New StringBuilder()

                If value = 0 Then
                    Return "0"
                End If

                While (exponent > 0)
                    Dim remainder = exponent Mod 8UL

                    builder.Insert(0, remainder)

                    exponent = exponent \ 8UL
                End While

                Return builder.ToString()
            End Function

            Private Shared Function HasOverflow(diagnostics As IEnumerable(Of Diagnostic)) As Boolean
                Return diagnostics.Any(Function(diagnostic As Diagnostic) diagnostic.Id = "BC30036")
            End Function

            Private Shared Function ConvertToULong(value As Object) As ULong
                'Cannot convert directly to ULong from Short or Integer as negative numbers
                'appear to have all bits above the current bit range set to 1
                'so short value -32768 or binary 1000000000000000 becomes
                'binary 1111111111111111111111111111111111111111111111111000000000000000
                'or in decimal 18446744073709518848
                'This will cause the subsequent conversion to a hex or octal string to output an incorrect value
                If TypeOf (value) Is Short Then
                    Return CType(value, UShort)
                ElseIf TypeOf (value) Is Integer Then
                    Return CType(value, UInteger)
                Else
                    Return CType(value, ULong)
                End If
            End Function
        End Class
    End Class
End Namespace
