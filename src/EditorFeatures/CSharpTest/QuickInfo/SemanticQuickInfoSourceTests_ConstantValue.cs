// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public partial class SemanticQuickInfoSourceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Int()
        {
            await TestInMethodAsync(@"
const int v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 + 2 = 3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_UInt()
        {
            await TestInMethodAsync(@"
const uint v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1U + 2 = 3U"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Byte()
        {
            await TestInMethodAsync(@"
const byte v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 + 2 = 3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_SByte()
        {
            await TestInMethodAsync(@"
const sbyte v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 + 2 = 3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Short()
        {
            await TestInMethodAsync(@"
const short v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 + 2 = 3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_UShort()
        {
            await TestInMethodAsync(@"
const ushort v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 + 2 = 3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Long()
        {
            await TestInMethodAsync(@"
const long v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1L + 2 = 3L"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_ULong()
        {
            await TestInMethodAsync(@"
const ulong v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1UL + 2 = 3UL"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Float()
        {
            await TestInMethodAsync(@"
const float v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1F + 2 = 3F"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Double()
        {
            await TestInMethodAsync(@"
const double v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1D + 2 = 3D"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_Decimal()
        {
            await TestInMethodAsync(@"
const decimal v = 1;
var f = v $$+ 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1M + 2 = 3M"));
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

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_Int()
        {
            await TestInMethodAsync(@"
const int v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 - 2 = -1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_SByte()
        {
            await TestInMethodAsync(@"
const sbyte v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 - 2 = -1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_Short()
        {
            await TestInMethodAsync(@"
const short v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1 - 2 = -1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_Long()
        {
            await TestInMethodAsync(@"
const long v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1L - 2 = -1L"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_Float()
        {
            await TestInMethodAsync(@"
const float v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1F - 2 = -1F"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_Double()
        {
            await TestInMethodAsync(@"
const double v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1D - 2 = -1D"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_Decimal()
        {
            await TestInMethodAsync(@"
const decimal v = 1;
var f = v $$- 2",
                ConstantValue($"\r\n{FeaturesResources.Constant_value_colon} 1M - 2 = -1M"));
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
