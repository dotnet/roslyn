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

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_CharChar() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$+ ""B""c",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("""B""c", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""AB""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_CharChar() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$& ""B""c",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("""B""c", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""AB""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_CharString() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$+ ""B""",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("""B""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""AB""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_CharString() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$& ""B""",
                ConstantValueContent(
                    ("""A""c", StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("""B""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""AB""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_StringString() As Task
            Await TestInMethodAsync("
const v as string = ""World""
dim f = ""Hello "" $$+ v",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""Hello World""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_StringString() As Task
            Await TestInMethodAsync("
const v as string = ""World""
dim f = ""Hello "" $$& v",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""Hello World""", StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_StringNothing() As Task
            Await TestInMethodAsync("
dim f = ""Hello "" $$+ nothing",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    ("+", [Operator]),
                    (" ", Space),
                    ("Nothing", Keyword),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""Hello """, StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_StringNothing() As Task
            Await TestInMethodAsync("
dim f = ""Hello "" $$& nothing",
                ConstantValueContent(
                    ("""Hello """, StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
                    (" ", Space),
                    ("Nothing", Keyword),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("""Hello """, StringLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_MultiLineString() As Task
            Await TestInMethodAsync("
const v as string = ""Hello
World""
dim f = v $$+ ""!""",
                ConstantValueContent(
                    ("""Hello""", StringLiteral),
                    ("&", [Operator]),
                    ("vbCrLf", Field),
                    ("&", [Operator]),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    ("+", [Operator]),
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

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_MultiLineString() As Task
            Await TestInMethodAsync("
const v as string = ""Hello
World""
dim f = v $$& ""!""",
                ConstantValueContent(
                    ("""Hello""", StringLiteral),
                    ("&", [Operator]),
                    ("vbCrLf", Field),
                    ("&", [Operator]),
                    ("""World""", StringLiteral),
                    (" ", Space),
                    ("&", [Operator]),
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

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMultiplyExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$* 2",
                ConstantValueContent(
                    ("1.2R", NumericLiteral),
                    (" ", Space),
                    ("*", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("2.4R", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestExponentiateExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$^ 2",
                ConstantValueContent(
                    ("1.2R", NumericLiteral),
                    (" ", Space),
                    ("^", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("1.44R", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDivideExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$/ 2",
                ConstantValueContent(
                    ("1.2R", NumericLiteral),
                    (" ", Space),
                    ("/", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("0.6R", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestIntegerDivideExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$\ 2",
                ConstantValueContent(
                    ("1.2R", NumericLiteral),
                    (" ", Space),
                    ("\", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("0L", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestModuloExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 12 $$mod 5",
                ConstantValueContent(
                    ("12", NumericLiteral),
                    (" ", Space),
                    ("mod", Keyword),
                    (" ", Space),
                    ("5", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLeftShiftExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 8 $$<< 2",
                ConstantValueContent(
                    ("8", NumericLiteral),
                    (" ", Space),
                    ("<<", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("32", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestRightShiftExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 8 $$>> 2",
                ConstantValueContent(
                    ("8", NumericLiteral),
                    (" ", Space),
                    (">>", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAndExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 2 $$and 3",
                ConstantValueContent(
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("and", Keyword),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOrExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 2 $$or 3",
                ConstantValueContent(
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("or", Keyword),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("3", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestExclusiveOrExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 2 $$xor 3",
                ConstantValueContent(
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("xor", Keyword),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("1", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLessThanExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$< 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("True", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLessThanOrEqualExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$<= 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<=", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("True", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGreaterThanExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$> 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    (">", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("False", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGreaterThanOrEqualExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$>= 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    (">=", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("False", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEqualsExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$= 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("False", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNotEqualsExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$<> 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<>", [Operator]),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("True", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAndAlsoExpression_Boolean() As Task
            Await TestInMethodAsync("
dim f = true $$andalso not true",
                ConstantValueContent(
                    ("True", Keyword),
                    (" ", Space),
                    ("andalso", Keyword),
                    (" ", Space),
                    ("False", Keyword),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("False", Keyword)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOrElseExpression_Boolean() As Task
            Await TestInMethodAsync("
dim f = true $$orelse not true",
                ConstantValueContent(
                    ("True", Keyword),
                    (" ", Space),
                    ("orelse", Keyword),
                    (" ", Space),
                    ("False", Keyword),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("True", Keyword)
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
        Public Async Function TestLeftShiftExpressionInEnum() As Task
            Await TestInMethodAsync("
enum Foo
    Bar = 1 <<$$ 3
end enum",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<<", [Operator]),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", [Operator]),
                    (" ", Space),
                    ("8", NumericLiteral)
                ))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLeftShiftExpressionInEnum_IgnoreTrivia() As Task
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
