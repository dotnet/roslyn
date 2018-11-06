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
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        public async Task TestAddExpression_NumericType(string type)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$+ 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("3", NumericLiteral)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("int.MaxValue", "2147483647")]
        [InlineData("float.NaN", "NaN")]
        [InlineData("double.NaN", "NaN")]
        [InlineData("double.PositiveInfinity", "Infinity")]
        [InlineData("double.NegativeInfinity", "-Infinity")]
        public async Task TestAddExpression_NumericTypeSpecialValue(string value, string displayValue)
        {
            await TestInMethodAsync($@"
var f = {value} $$+ 0",
                ConstantValueContent(
                    (displayValue, NumericLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("0", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    (displayValue, NumericLiteral)
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
        public async Task TestAddExpression_EscapedChar()
        {
            await TestInMethodAsync(@"
var f = '\uFFFF' $$+ 0",
                ConstantValueContent(
                    ("'\\uffff'", StringLiteral),
                    (" ", Space),
                    ("+", Operator),
                    (" ", Space),
                    ("0", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("65535", NumericLiteral)
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
        [InlineData("int")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("long")]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        public async Task TestSubtractExpression_NumericType(string type)
        {
            await TestInMethodAsync($@"
const {type} v = 1;
var f = v $$- 2",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("-", Operator),
                    (" ", Space),
                    ("2", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("-1", NumericLiteral)
                ));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        [InlineData("*", "5")]
        [InlineData("/", "1.25")]
        [InlineData("%", "0.5")]
        public async Task TestMultiplicativeExpression_Double(string op, string result)
        {
            await TestInMethodAsync($@"
var f = 2.5 $${op} 2",
                ConstantValueContent(
                    ("2.5", NumericLiteral),
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
