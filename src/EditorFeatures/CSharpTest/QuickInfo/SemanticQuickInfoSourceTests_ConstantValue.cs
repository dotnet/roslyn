// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public partial class SemanticQuickInfoSourceTests
    {
        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("int", "1 + 2 = 3")]
        [InlineData("uint", "1U + 2 = 3U")]
        [InlineData("byte", "1 + 2 = 3")]
        [InlineData("sbyte", "1 + 2 = 3")]
        [InlineData("short", "1 + 2 = 3")]
        [InlineData("ushort", "1 + 2 = 3")]
        [InlineData("long", "1L + 2 = 3L")]
        [InlineData("ulong", "1UL + 2 = 3UL")]
        [InlineData("float", "1F + 2 = 3F")]
        [InlineData("double", "1D + 2 = 3D")]
        [InlineData("decimal", "1M + 2 = 3M")]
        public async Task TestAddExpression_NumericType(string type, string result)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} {result}"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_CharInt()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 'A' + 2 = 67"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_CharChar()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$+ 'B'",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 'A' + 'B' = 131"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_StringString()
        {
            await TestInMethodAsync(@"
const string v = ""World"";
var f = ""Hello "" $$+ v",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} \"Hello \" + \"World\" = \"Hello World\""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_StringNull()
        {
            await TestInMethodAsync(@"
var f = ""Hello "" $$+ default(string)",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} \"Hello \" + null = \"Hello \""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_MultiLineString()
        {
            await TestInMethodAsync(@"
const string v = @""Hello
World"";
var f = v $$+ ""!""",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} \"Hello\\r\\nWorld\" + \"!\" = \"Hello\\r\\nWorld!\""));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("int", "1 - 2 = -1")]
        [InlineData("sbyte", "1 - 2 = -1")]
        [InlineData("short", "1 - 2 = -1")]
        [InlineData("long", "1L - 2 = -1L")]
        [InlineData("float", "1F - 2 = -1F")]
        [InlineData("double", "1D - 2 = -1D")]
        [InlineData("decimal", "1M - 2 = -1M")]
        public async Task TestSubtractExpression_NumericType(string type, string result)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} {result}"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_CharInt()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 'A' - 2 = 63"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_CharChar()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$- 'B'",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 'A' - 'B' = -1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMultiplyExpression_Double()
        {
            await TestInMethodAsync(@"
var f = 1.2 $$* 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1.2D * 2 = 2.4D"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDivideExpression_Double()
        {
            await TestInMethodAsync(@"
var f = 1.2 $$/ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1.2D / 2 = 0.6D"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestModuloExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 12 $$% 5",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 12 % 5 = 2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLeftShiftExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 8 $$<< 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 8 << 2 = 32"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestRightShiftExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 8 $$>> 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 8 >> 2 = 2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBitwiseAndExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 2 $$& 3",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 2 & 3 = 2"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBitwiseOrExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 2 $$| 3",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 2 | 3 = 3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestExclusiveOrExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 2 $$^ 3",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 2 ^ 3 = 1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLessThanExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$< 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 < 2 = true"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLessThanOrEqualExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$<= 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 <= 2 = true"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGreaterThanExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$> 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 > 2 = false"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGreaterThanOrEqualExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$>= 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 >= 2 = false"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEqualsExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$== 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 == 2 = false"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNotEqualsExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$!= 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 != 2 = true"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLogicalAndExpression_Bool()
        {
            await TestInMethodAsync(@"
var f = true $$&& !true",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} true && false = false"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLogicalOrExpression_Bool()
        {
            await TestInMethodAsync(@"
var f = true $$|| !true",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} true || false = true"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLongString()
        {
            const string UnicodeEllipsis = "\u2026";

            await TestInMethodAsync(@"
var f = ""abcdefghijklmnopqrstuvwxyzabcdefghijklmn"" $$+ ""o""",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} \"abcdefghijklmnopqrstuvwxyzabcdefghijklmn\" + \"o\" = \"abcdefghijklmnopqrstuvwxyzabcdefghijklmno{UnicodeEllipsis}"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLeftShiftExpressionInEnum()
        {
            await TestAsync(@"
enum Foo
{
    Bar = 1 <<$$ 3,
}",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 << 3 = 8"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLeftShiftExpressionInEnum_IgnoreTrivia()
        {
            await TestAsync(@"
enum Foo
{
    Bar = /*a*/1/*b*/
       /*c*/<<$$//d
   /*e*/4 // f
}",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 << 4 = 16"));
        }
    }
}
