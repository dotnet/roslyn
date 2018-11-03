' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.TextTags

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QuickInfo
    Partial Public Class SemanticQuickInfoSourceTests
        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer", "1", "3")>
        <InlineData("uinteger", "1UI", "3L")>
        <InlineData("short", "1S", "3")>
        <InlineData("ushort", "1US", "3")>
        <InlineData("long", "1L", "3L")>
        <InlineData("ulong", "1UL", "3D")>
        <InlineData("single", "1F", "3F")>
        <InlineData("double", "1R", "3R")>
        <InlineData("decimal", "1D", "3D")>
        Public Async Function TestAddExpression_NumericType1(type As String, value As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$+ 2",
                ConstantValueContent(
                    (value, NumericLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("byte", "CByte", "1", "3")>
        <InlineData("sbyte", "CSByte", "1", "3")>
        Public Async Function TestAddExpression_NumericType2(type As String, castKeyword As String, value As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$+ 2",
                ConstantValueContent(
                    (castKeyword, Keyword),
                    ("(", Punctuation),
                    (value, NumericLiteral),
                    (")", Punctuation),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    (result, NumericLiteral)
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
        Public Async Function TestConcatenateExpression_MultiLineString(op As String) As Task
            Await TestInMethodAsync($"
const v as string = ""Hello
World""
dim f = v $${op} ""!""",
                ConstantValueContent(
                    ("""Hello""", StringLiteral),
                    ("&", [Operator]),
                    ("vbCrLf", Field),
                    ("&", [Operator]),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    (op, [Operator]),
                    (" ", Space),
                    ("""!""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""Hello""", StringLiteral),
                    ("&", [Operator]),
                    ("vbCrLf", Field),
                    ("&", [Operator]),
                    ("""World!""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer", "1", "1")>
        <InlineData("short", "1S", "1")>
        <InlineData("long", "1L", "1L")>
        <InlineData("single", "1F", "1F")>
        <InlineData("double", "1R", "1R")>
        Public Async Function TestSubtractExpression_NumericType1(type As String, value As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$- 2",
                ConstantValueContent(
                    (value, NumericLiteral),
                    (" ", Space),
                    ("-", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("-", [Operator]),
                    (result, NumericLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("sbyte", "CSByte", "1", "1")>
        Public Async Function TestSubtractExpression_NumericType2(type As String, castKeyword As String, value As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$- 2",
                ConstantValueContent(
                    (castKeyword, Keyword),
                    ("(", Punctuation),
                    (value, NumericLiteral),
                    (")", Punctuation),
                    (" ", Space),
                    ("-", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("-", [Operator]),
                    (result, NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestSubtractExpression_NumericType3() As Task
            Await TestInMethodAsync("
const v as decimal = 1
dim f = v $$- 2",
                ConstantValueContent(
                    ("1D", NumericLiteral),
                    (" ", Space),
                    ("-", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("-1D", NumericLiteral)
            ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData([Operator], "*", "5R")>
        <InlineData([Operator], "^", "6.25R")>
        <InlineData([Operator], "/", "1.25R")>
        <InlineData([Operator], "\", "1L")>
        <InlineData(Keyword, "mod", "0.5R")>
        Public Async Function TestMultiplicativeExpression_Double(opTag As String, op As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 2.5 $${op} 2",
                ConstantValueContent(
                    ("2.5R", NumericLiteral),
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
