' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QuickInfo
    Partial Public Class SemanticQuickInfoSourceTests
        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer", "1 + 2 = 3")>
        <InlineData("uinteger", "1UI + 2 = 3L")>
        <InlineData("byte", "CByte(1) + 2 = 3")>
        <InlineData("sbyte", "CSByte(1) + 2 = 3")>
        <InlineData("short", "1S + 2 = 3")>
        <InlineData("ushort", "1US + 2 = 3")>
        <InlineData("long", "1L + 2 = 3L")>
        <InlineData("ulong", "1UL + 2 = 3D")>
        <InlineData("single", "1F + 2 = 3F")>
        <InlineData("double", "1R + 2 = 3R")>
        <InlineData("decimal", "1D + 2 = 3D")>
        Public Async Function TestAddExpression_NumericType(type As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$+ 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} {result}"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_CharChar() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$+ ""B""c",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""A""c + ""B""c = ""AB"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_CharChar() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$& ""B""c",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""A""c & ""B""c = ""AB"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_CharString() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$+ ""B""",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""A""c + ""B"" = ""AB"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_CharString() As Task
            Await TestInMethodAsync("
const v as char = ""A""c
dim f = v $$& ""B""",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""A""c & ""B"" = ""AB"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_StringString() As Task
            Await TestInMethodAsync("
const v as string = ""World""
dim f = ""Hello "" $$+ v",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""Hello "" + ""World"" = ""Hello World"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_StringString() As Task
            Await TestInMethodAsync("
const v as string = ""World""
dim f = ""Hello "" $$& v",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""Hello "" & ""World"" = ""Hello World"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_StringNothing() As Task
            Await TestInMethodAsync("
dim f = ""Hello "" $$+ nothing",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""Hello "" + Nothing = ""Hello """))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_StringNothing() As Task
            Await TestInMethodAsync("
dim f = ""Hello "" $$& nothing",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""Hello "" & Nothing = ""Hello """))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddExpression_MultiLineString() As Task
            Await TestInMethodAsync("
const v as string = ""Hello
World""
dim f = v $$+ ""!""",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""Hello""&vbCrLf&""World"" + ""!"" = ""Hello""&vbCrLf&""World!"""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConcatenateExpression_MultiLineString() As Task
            Await TestInMethodAsync("
const v as string = ""Hello
World""
dim f = v $$& ""!""",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""Hello""&vbCrLf&""World"" & ""!"" = ""Hello""&vbCrLf&""World!"""))
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("integer", "1 - 2 = -1")>
        <InlineData("sbyte", "CSByte(1) - 2 = -1")>
        <InlineData("short", "1S - 2 = -1")>
        <InlineData("long", "1L - 2 = -1L")>
        <InlineData("single", "1F - 2 = -1F")>
        <InlineData("double", "1R - 2 = -1R")>
        <InlineData("decimal", "1D - 2 = -1D")>
        Public Async Function TestSubtractExpression_NumericType(type As String, result As String) As Task
            Await TestInMethodAsync($"
const v as {type} = 1
dim f = v $$- 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} {result}"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMultiplyExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$* 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1.2R * 2 = 2.4R"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestExponentiateExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$^ 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1.2R ^ 2 = 1.44R"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDivideExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$/ 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1.2R / 2 = 0.6R"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestIntegerDivideExpression_Double() As Task
            Await TestInMethodAsync("
dim f = 1.2 $$\ 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1.2R \ 2 = 0L"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestModuloExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 12 $$mod 5",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 12 mod 5 = 2"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLeftShiftExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 8 $$<< 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 8 << 2 = 32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestRightShiftExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 8 $$>> 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 8 >> 2 = 2"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAndExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 2 $$and 3",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 2 and 3 = 2"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOrExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 2 $$or 3",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 2 or 3 = 3"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestExclusiveOrExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 2 $$xor 3",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 2 xor 3 = 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLessThanExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$< 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 < 2 = True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLessThanOrEqualExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$<= 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 <= 2 = True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGreaterThanExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$> 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 > 2 = False"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGreaterThanOrEqualExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$>= 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 >= 2 = False"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEqualsExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$= 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 = 2 = False"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNotEqualsExpression_Integer() As Task
            Await TestInMethodAsync("
dim f = 1 $$<> 2",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 <> 2 = True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAndAlsoExpression_Boolean() As Task
            Await TestInMethodAsync("
dim f = true $$andalso not true",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} True andalso False = False"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOrElseExpression_Boolean() As Task
            Await TestInMethodAsync("
dim f = true $$orelse not true",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} True orelse False = True"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLongString() As Task
            Const UnicodeEllipsis = ChrW(&H2026)

            Await TestInMethodAsync("
dim f = ""abcdefghijklmnopqrstuvwxyzabcdefghijklmn"" $$& ""o""",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} ""abcdefghijklmnopqrstuvwxyzabcdefghijklmn"" & ""o"" = ""abcdefghijklmnopqrstuvwxyzabcdefghijklmno{UnicodeEllipsis}"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLeftShiftExpressionInEnum() As Task
            Await TestInMethodAsync("
enum Foo
    Bar = 1 <<$$ 3
end enum",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 << 3 = 8"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLeftShiftExpressionInEnum_IgnoreTrivia() As Task
            Await TestInMethodAsync("
enum Foo
    Bar = 1 _
        <<$$'a
     4 ' b
end enum",
                ConstantValue($"{vbCrLf}{FeaturesResources.Constant_value_colon} 1 << 4 = 16"))
        End Function
    End Class
End Namespace
