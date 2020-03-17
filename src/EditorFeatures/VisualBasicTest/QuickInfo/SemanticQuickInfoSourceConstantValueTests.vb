' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.TextTags

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QuickInfo
    Public NotInheritable Class SemanticQuickInfoSourceConstantValueTests
        Inherits SemanticQuickInfoSourceTestsBase

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantVariable() As Task
            Await TestInClassAsync($"
const a as integer = 1
const b as integer = 2
const c as integer = a + b
const f as integer = $$c",
                ConstantValueContent(
                    ("3", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNumericLiteral() As Task
            Await TestInMethodAsync($"
dim v = $$1",
                ConstantValueContent(
                    ("1", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTrueLiteral() As Task
            Await TestInMethodAsync($"
dim v = $$true",
                ConstantValueContent(
                    ("True", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCharLiteral() As Task
            Await TestInMethodAsync($"
dim v = $$""a""c",
                ConstantValueContent(
                    ("""a""c", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestStringLiteral() As Task
            Await TestInMethodAsync($"
dim v = $$""Hello World""",
                ConstantValueContent(
                    ("""Hello World""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNotOnParenthesizedExpression() As Task
            Await TestInMethodAsync($"
dim v = $$(0)", Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestUnaryPlusExpression_Int() As Task
            Await TestInMethodAsync($"
dim v = $$+1",
                ConstantValueContent(
                    ("1", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestUnaryMinusExpression_Int() As Task
            Await TestInMethodAsync($"
dim v = $$-1",
                ConstantValueContent(
                    ("-1", NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer")>
        <InlineData("uinteger")>
        <InlineData("byte")>
        <InlineData("sbyte")>
        <InlineData("short")>
        <InlineData("ushort")>
        <InlineData("long")>
        <InlineData("ulong")>
        <InlineData("single")>
        <InlineData("double")>
        <InlineData("decimal")>
        Public Async Function TestAddExpression_NumericType(type As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$+ 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("3", NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer.MaxValue", "2147483647")>
        <InlineData("single.NaN", "NaN")>
        <InlineData("double.NaN", "NaN")>
        <InlineData("double.PositiveInfinity", "Infinity")>
        <InlineData("double.NegativeInfinity", "-Infinity")>
        <InlineData("cbyte(1)", "1")>
        <InlineData("ctype(1, byte)", "1")>
        Public Async Function TestAddExpression_NumericTypeSpecialValue(value As String, displayValue As String) As Task
            Await TestInMethodAsync($"
dim f = {value} $$+ 0",
                ConstantValueContent(
                    (displayValue, NumericLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("0", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (displayValue, NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+", StringLiteral, """B""c", """AB""")>
        <InlineData("&", StringLiteral, """B""c", """AB""")>
        <InlineData("+", StringLiteral, """B""", """AB""")>
        <InlineData("&", StringLiteral, """B""", """AB""")>
        Public Async Function TestConcatenateExpression_Char(op As String, operandTag As String, operand As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = ""A""c $${op} {operand}",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    (op, [Operator]),
                    (" ", Space),
                    (operand, operandTag),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+", StringLiteral, """World""", """Hello World""")>
        <InlineData("&", StringLiteral, """World""", """Hello World""")>
        <InlineData("+", Keyword, "Nothing", """Hello """)>
        <InlineData("&", Keyword, "Nothing", """Hello """)>
        Public Async Function TestConcatenateExpression_String(op As String, operandTag As String, operand As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = ""Hello "" $${op} {operand}",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    (op, [Operator]),
                    (" ", Space),
                    (operand, operandTag),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_EscapedChar(op As String) As Task
            Await TestInMethodAsync($"
dim f = Microsoft.VisualBasic.Strings.ChrW(&HFFFF) $${op} "".""c",
                ConstantValueContent(
                    ("ChrW", Method),
                    ("(", Punctuation),
                    ("&HFFFF", NumericLiteral),
                    (")", Punctuation),
                    (" ", Space),
                    (op, [Operator]),
                    (" ", Space),
                    (""".""c", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("ChrW", Method),
                    ("(", Punctuation),
                    ("&HFFFF", NumericLiteral),
                    (")", Punctuation),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    (""".""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_MultiLineString(op As String) As Task
            Await TestInMethodAsync($"
const v as string = ""Hello
World""
dim f = v $${op} ""!""",
                ConstantValueContent(
                    ("""Hello""", StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("vbCrLf", Constant),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    (op, [Operator]),
                    (" ", Space),
                    ("""!""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""Hello""", StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("vbCrLf", Constant),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("""World!""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer")>
        <InlineData("sbyte")>
        <InlineData("short")>
        <InlineData("long")>
        <InlineData("single")>
        <InlineData("double")>
        <InlineData("decimal")>
        Public Async Function TestSubtractExpression_NumericType(type As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$- 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("-", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("-1", NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData([Operator], "*", "5")>
        <InlineData([Operator], "^", "6.25")>
        <InlineData([Operator], "/", "1.25")>
        <InlineData([Operator], "\", "1")>
        <InlineData(Keyword, "mod", "0.5")>
        Public Async Function TestMultiplicativeExpression_Double(opTag As String, op As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 2.5 $${op} 2",
                ConstantValueContent(
                    ("2.5", NumericLiteral),
                    (" ", Space),
                    (op, opTag),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData([Operator], "<<", "6")>
        <InlineData([Operator], ">>", "1")>
        <InlineData(Keyword, "and", "1")>
        <InlineData(Keyword, "or", "3")>
        <InlineData(Keyword, "xor", "2")>
        Public Async Function TestBitwiseExpression_Integer(opTag As String, op As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 3 $${op} 1",
                ConstantValueContent(
                    ("3", NumericLiteral),
                    (" ", Space),
                    (op, opTag),
                    (" ", Space),
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("<", "True")>
        <InlineData("<=", "True")>
        <InlineData(">", "False")>
        <InlineData(">=", "False")>
        <InlineData("=", "False")>
        <InlineData("<>", "True")>
        Public Async Function TestComparisonExpression_Integer(op As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 1 $${op} 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    (op, [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, Keyword)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("andalso", "False")>
        <InlineData("orelse", "True")>
        Public Async Function TestLogicalExpression_Boolean(op As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = true $${op} not true",
                ConstantValueContent(
                    ("True", Keyword),
                    (" ", Space),
                    (op, Keyword),
                    (" ", Space),
                    ("False", Keyword),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLongString() As Task
            Const UnicodeEllipsis = ChrW(&H2026)

            Await TestInMethodAsync("
dim f = ""abcdefghijklmnopqrstuvwxyzabcdefghijklmn"" $$& ""o""",
                ConstantValueContent(
                    ("""abcdefghijklmnopqrstuvwxyzabcdefghijklmn""", StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("""o""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""abcdefghijklmnopqrstuvwxyzabcdefghijklmno", StringLiteral),
                    (UnicodeEllipsis, TextTags.Text)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNonConstantVariable() As Task
            Await TestInMethodAsync("
dim v as integer = 1
dim f = v $$+ 2",
                ConstantValue())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestInvalidConstant() As Task
            Await TestInMethodAsync("
const v as integer = integer.Parse(""1"")
const f = v $$+ 2",
                ConstantValue())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestIgnoreTrivia() As Task
            Await TestInMethodAsync("
enum Foo
    Bar = 1 _
        <<$$'a
     4 ' b
end enum",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<<", [Operator]),
                    (" ", Space),
                    ("4", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("16", NumericLiteral)
                ))
        End Function
    End Class
End Namespace
