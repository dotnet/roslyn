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
        Public Async Function TestAddExpression_NumericType1(type As String, left As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$+ 2",
                ConstantValueContent(
                    (left, NumericLiteral),
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
        Public Async Function TestAddExpression_NumericType2(type As String, left1 As String, left2 As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$+ 2",
                ConstantValueContent(
                    (left1, Keyword),
                    ("(", Punctuation),
                    (left2, NumericLiteral),
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
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_CharChar([operator] As String) As Task
            Await TestInMethodAsync($"
const v as char = ""A""c
dim f = v $${[operator]} ""B""c",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    ([operator], TextTags.Operator),
                    (" ", Space),
                    ("""B""c", StringLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
                    (" ", Space),
                    ("""AB""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_CharString([operator] As String) As Task
            Await TestInMethodAsync($"
const v as char = ""A""c
dim f = v $${[operator]} ""B""",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    ([operator], TextTags.Operator),
                    (" ", Space),
                    ("""B""", StringLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
                    (" ", Space),
                    ("""AB""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_StringString([operator] As String) As Task
            Await TestInMethodAsync($"
const v as string = ""World""
dim f = ""Hello "" $${[operator]} v",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    ([operator], TextTags.Operator),
                    (" ", Space),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
                    (" ", Space),
                    ("""Hello World""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_StringNothing([operator] As String) As Task
            Await TestInMethodAsync($"
dim f = ""Hello "" $${[operator]} nothing",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    ([operator], TextTags.Operator),
                    (" ", Space),
                    ("Nothing", Keyword),
                    (" ", Space),
                    ("=", TextTags.Operator),
                    (" ", Space),
                    ("""Hello """, StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("+")>
        <InlineData("&")>
        Public Async Function TestConcatenateExpression_MultiLineString([operator] As String) As Task
            Await TestInMethodAsync($"
const v as string = ""Hello
World""
dim f = v $${[operator]} ""!""",
                ConstantValueContent(
                    ("""Hello""", StringLiteral),
                    ("&", TextTags.Operator),
                    ("vbCrLf", Field),
                    ("&", TextTags.Operator),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    ([operator], TextTags.Operator),
                    (" ", Space),
                    ("""!""", StringLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
                    (" ", Space),
                    ("""Hello""", StringLiteral),
                    ("&", TextTags.Operator),
                    ("vbCrLf", Field),
                    ("&", TextTags.Operator),
                    ("""World!""", StringLiteral)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer", "1", "1")>
        <InlineData("short", "1S", "1")>
        <InlineData("long", "1L", "1L")>
        <InlineData("single", "1F", "1F")>
        <InlineData("double", "1R", "1R")>
        Public Async Function TestSubtractExpression_NumericType1(type As String, left As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$- 2",
                ConstantValueContent(
                    (left, NumericLiteral),
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
        Public Async Function TestSubtractExpression_NumericType2(type As String, left1 As String, left2 As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$- 2",
                ConstantValueContent(
                    (left1, Keyword),
                    ("(", Punctuation),
                    (left2, NumericLiteral),
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
        Public Async Function TestMultiplicativeExpression_Double(operatorTag As String, [operator] As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 2.5 $${[operator]} 2",
                ConstantValueContent(
                    ("2.5R", NumericLiteral),
                    (" ", Space),
                    ([operator], operatorTag),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
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
        Public Async Function TestBitwiseExpression_Integer(operatorTag As String, [operator] As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 3 $${[operator]} 1",
                ConstantValueContent(
                    ("3", NumericLiteral),
                    (" ", Space),
                    ([operator], operatorTag),
                    (" ", Space),
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
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
        Public Async Function TestComparisonExpression_Integer([operator] As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = 1 $${[operator]} 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ([operator], TextTags.Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", TextTags.Operator),
                    (" ", Space),
                    (result, Keyword)
                ))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("andalso", "False")>
        <InlineData("orelse", "True")>
        Public Async Function TestLogicalExpression_Boolean([operator] As String, result As String) As Task
            Await TestInMethodAsync($"
dim f = true $${[operator]} not true",
                ConstantValueContent(
                    ("True", Keyword),
                    (" ", Space),
                    ([operator], Keyword),
                    (" ", Space),
                    ("False", Keyword),
                    (" ", Space),
                    ("=", TextTags.Operator),
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
