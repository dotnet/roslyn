// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.TextTags;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public partial class SemanticQuickInfoSourceTests
    {
        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("int", "1", "3")]
        [InlineData("uint", "1U", "3U")]
        [InlineData("byte", "1", "3")]
        [InlineData("sbyte", "1", "3")]
        [InlineData("short", "1", "3")]
        [InlineData("ushort", "1", "3")]
        [InlineData("long", "1L", "3L")]
        [InlineData("ulong", "1UL", "3UL")]
        [InlineData("float", "1F", "3F")]
        [InlineData("double", "1D", "3D")]
        [InlineData("decimal", "1M", "3M")]
        public async Task TestAddExpression_NumericType(string type, string left, string result)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$+ 2",
                ConstantValueContent(
                    (left, NumericLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_CharInt()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$+ 2",
                ConstantValueContent(
                    ("'A'", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("67", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_CharChar()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$+ 'B'",
                ConstantValueContent(
                    ("'A'", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("'B'", StringLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("131", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_StringString()
        {
            await TestInMethodAsync(@"
const string v = ""World"";
var f = ""Hello "" $$+ v",
                ConstantValueContent(
                    ("\"Hello \"", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("\"World\"", StringLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("\"Hello World\"", StringLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_StringNull()
        {
            await TestInMethodAsync(@"
var f = ""Hello "" $$+ default(string)",
                ConstantValueContent(
                    ("\"Hello \"", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("null", Keyword),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("\"Hello \"", StringLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestAddExpression_MultiLineString()
        {
            await TestInMethodAsync(@"
const string v = @""Hello
World"";
var f = v $$+ ""!""",
                ConstantValueContent(
                    ("\"Hello\\r\\nWorld\"", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("\"!\"", StringLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("\"Hello\\r\\nWorld!\"", StringLiteral)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("int", "1", "1")]
        [InlineData("sbyte", "1", "1")]
        [InlineData("short", "1", "1")]
        [InlineData("long", "1L", "1L")]
        [InlineData("float", "1F", "1F")]
        [InlineData("double", "1D", "1D")]
        [InlineData("decimal", "1M", "1M")]
        public async Task TestSubtractExpression_NumericType(string type, string left, string result)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$- 2",
                ConstantValueContent(
                    (left, NumericLiteral),
                    (" ", Space),
                    ("-", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("-", Operator),
                    (result, NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_CharInt()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$- 2",
                ConstantValueContent(
                    ("'A'", StringLiteral),
                    (" ", Space),
                    ("-", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("63", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestSubtractExpression_CharChar()
        {
            await TestInMethodAsync(@"
const char v = 'A';
var f = v $$- 'B'",
                ConstantValueContent(
                    ("'A'", StringLiteral),
                    (" ", Space),
                    ("-", Operator),
                    (" ", Space),
                    ("'B'", StringLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("-", Operator),
                    ("1", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestMultiplyExpression_Double()
        {
            await TestInMethodAsync(@"
var f = 1.2 $$* 2",
                ConstantValueContent(
                    ("1.2D", NumericLiteral),
                    (" ", Space),
                    ("*", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("2.4D", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDivideExpression_Double()
        {
            await TestInMethodAsync(@"
var f = 1.2 $$/ 2",
                ConstantValueContent(
                    ("1.2D", NumericLiteral),
                    (" ", Space),
                    ("/", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("0.6D", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestModuloExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 12 $$% 5",
                ConstantValueContent(
                    ("12", NumericLiteral),
                    (" ", Space),
                    ("%", Operator),
                    (" ", Space),
                    ("5", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("2", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLeftShiftExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 8 $$<< 2",
                ConstantValueContent(
                    ("8", NumericLiteral),
                    (" ", Space),
                    ("<<", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("32", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestRightShiftExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 8 $$>> 2",
                ConstantValueContent(
                    ("8", NumericLiteral),
                    (" ", Space),
                    (">>", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("2", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBitwiseAndExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 2 $$& 3",
                ConstantValueContent(
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("&", Operator),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("2", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBitwiseOrExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 2 $$| 3",
                ConstantValueContent(
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("|", Operator),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("3", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestExclusiveOrExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 2 $$^ 3",
                ConstantValueContent(
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("^", Operator),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("1", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLessThanExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$< 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("true", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLessThanOrEqualExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$<= 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<=", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("true", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGreaterThanExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$> 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    (">", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("false", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestGreaterThanOrEqualExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$>= 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    (">=", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("false", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestEqualsExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$== 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("==", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("false", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNotEqualsExpression_Int()
        {
            await TestInMethodAsync(@"
var f = 1 $$!= 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("!=", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("true", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLogicalAndExpression_Bool()
        {
            await TestInMethodAsync(@"
var f = true $$&& !true",
                ConstantValueContent(
                    ("true", Keyword),
                    (" ", Space),
                    ("&&", Operator),
                    (" ", Space),
                    ("false", Keyword),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("false", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLogicalOrExpression_Bool()
        {
            await TestInMethodAsync(@"
var f = true $$|| !true",
                ConstantValueContent(
                    ("true", Keyword),
                    (" ", Space),
                    ("||", Operator),
                    (" ", Space),
                    ("false", Keyword),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("true", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLongString()
        {
            const string UnicodeEllipsis = "\u2026";

            await TestInMethodAsync(@"
var f = ""abcdefghijklmnopqrstuvwxyzabcdefghijklmn"" $$+ ""o""",
                ConstantValueContent(
                    ("\"abcdefghijklmnopqrstuvwxyzabcdefghijklmn\"", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("\"o\"", StringLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("\"abcdefghijklmnopqrstuvwxyzabcdefghijklmno", StringLiteral),
                    (UnicodeEllipsis, TextTags.Text)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestLeftShiftExpressionInEnum()
        {
            await TestAsync(@"
enum Foo
{
    Bar = 1 <<$$ 3,
}",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<<", Operator),
                    (" ", Space),
                    ("3", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("8", NumericLiteral)
                ));
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
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("<<", Operator),
                    (" ", Space),
                    ("4", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("16", NumericLiteral)
                ));
        }
    }
}
