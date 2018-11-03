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
        public async Task TestAddExpression_NumericType(string type, string value, string result)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$+ 2",
                ConstantValueContent(
                    (value, NumericLiteral),
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

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData(NumericLiteral, "2", "67")]
        [InlineData(StringLiteral, "'B'", "131")]
        public async Task TestAddExpression_Char(string operandTag, string operand, string result)
        {
            await TestInMethodAsync($@"
var f = 'A' $$+ {operand}",
                ConstantValueContent(
                    ("'A'", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    (operand, operandTag),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, NumericLiteral)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData(StringLiteral, "\"World\"", "\"Hello World\"")]
        [InlineData(Keyword, "null", "\"Hello \"")]
        public async Task TestAddExpression_String(string operandTag, string operand, string result)
        {
            await TestInMethodAsync($@"
var f = ""Hello "" $$+ {operand}",
                ConstantValueContent(
                    ("\"Hello \"", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    (operand, operandTag),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, StringLiteral)
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
        public async Task TestSubtractExpression_NumericType(string type, string value, string result)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$- 2",
                ConstantValueContent(
                    (value, NumericLiteral),
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

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("*", "5D")]
        [InlineData("/", "1.25D")]
        [InlineData("%", "0.5D")]
        public async Task TestMultiplicativeExpression_Double(string op, string result)
        {
            await TestInMethodAsync($@"
var f = 2.5 $${op} 2",
                ConstantValueContent(
                    ("2.5D", NumericLiteral),
                    (" ", Space),
                    (op, Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, NumericLiteral)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("<<", "6")]
        [InlineData(">>", "1")]
        [InlineData("&", "1")]
        [InlineData("|", "3")]
        [InlineData("^", "2")]
        public async Task TestBitwiseExpression_Int(string op, string result)
        {
            await TestInMethodAsync($@"
var f = 3 $${op} 1",
                ConstantValueContent(
                    ("3", NumericLiteral),
                    (" ", Space),
                    (op, Operator),
                    (" ", Space),
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, NumericLiteral)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("<", "true")]
        [InlineData("<=", "true")]
        [InlineData(">", "false")]
        [InlineData(">=", "false")]
        [InlineData("==", "false")]
        [InlineData("!=", "true")]
        public async Task TestComparisonExpression_Int(string op, string result)
        {
            await TestInMethodAsync($@"
var f = 1 $${op} 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    (op, Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, Keyword)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("&&", "false")]
        [InlineData("||", "true")]
        public async Task TestLogicalExpression_Bool(string op, string result)
        {
            await TestInMethodAsync($@"
var f = true $${op} !true",
                ConstantValueContent(
                    ("true", Keyword),
                    (" ", Space),
                    (op, Operator),
                    (" ", Space),
                    ("false", Keyword),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (result, Keyword)
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
        public async Task TestIgnoreTrivia()
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
