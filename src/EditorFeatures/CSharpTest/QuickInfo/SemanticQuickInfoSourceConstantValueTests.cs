// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.TextTags;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public sealed class SemanticQuickInfoSourceConstantValueTests : SemanticQuickInfoSourceTestsBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestConstantVariable()
        {
            await TestInClassAsync($@"
const int a = 1;
const int b = 2;
const int c = a + b;
const int f = $$c;",
                ConstantValueContent(
                    ("3", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNumericLiteral()
        {
            await TestInMethodAsync($@"
var v = $$1;",
                ConstantValueContent(
                    ("1", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestTrueLiteral()
        {
            await TestInMethodAsync($@"
var v = $$true;",
                ConstantValueContent(
                    ("true", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCharLiteral()
        {
            await TestInMethodAsync($@"
var v = $$'a';",
                ConstantValueContent(
                    ("'a'", StringLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestStringLiteral()
        {
            await TestInMethodAsync($@"
var v = $$""Hello World"";",
                ConstantValueContent(
                    ("\"Hello World\"", StringLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultLiteral_Int()
        {
            await TestInMethodAsync($@"
int v = $$default;",
                ConstantValueContent(
                    ("0", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultLiteral_Bool()
        {
            await TestInMethodAsync($@"
bool v = $$default;",
                ConstantValueContent(
                    ("false", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultLiteral_Char()
        {
            await TestInMethodAsync($@"
char v = $$default;",
                ConstantValueContent(
                    ("'\\0'", StringLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultLiteral_String()
        {
            await TestInMethodAsync($@"
string v = $$default;",
                ConstantValueContent(
                    ("null", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultExpression_String()
        {
            await TestInMethodAsync($@"
var v = $$default(string);",
                ConstantValueContent(
                    ("null", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultExpression_String2()
        {
            await TestInMethodAsync($@"
var v = default$$(string);",
            ConstantValueContent(
                ("null", Keyword)
            ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultExpression_String_NotOnType()
        {
            await TestInMethodAsync($@"
var v = default($$string);",
                ConstantValue());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultExpression_String_NotOnParenthesisToken1()
        {
            await TestInMethodAsync($@"
var v = default(string$$);",
                ConstantValue());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestDefaultExpression_String_NotOnParenthesisToken2()
        {
            await TestInMethodAsync($@"
var v = default(string)$$;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestCheckedExpression_Bool()
        {
            await TestInMethodAsync($@"
var v = $$checked(true);",
                ConstantValueContent(
                    ("true", Keyword)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUncheckedExpression_UInt()
        {
            await TestInMethodAsync($@"
var v = $$unchecked((uint)0 - 1);",
                ConstantValueContent(
                    ("4294967295", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNotOnCastExpression()
        {
            await TestInMethodAsync($@"
var v = $$(int)0;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestNotOnParenthesizedExpression()
        {
            await TestInMethodAsync($@"
var v = $$(0);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUnaryPlusExpression_Int()
        {
            await TestInMethodAsync($@"
var v = $$+1",
                ConstantValueContent(
                    ("1", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestUnaryMinusExpression_Int()
        {
            await TestInMethodAsync($@"
var v = $$-1",
                ConstantValueContent(
                    ("-1", NumericLiteral)
                ));
        }

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
        [InlineData("(byte)1", "1")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBitwiseExpression_Enum1()
        {
            await TestInMethodAsync($@"
var f = System.Text.RegularExpressions.RegexOptions.Compiled $$| System.Text.RegularExpressions.RegexOptions.ExplicitCapture",
                ConstantValueContent(
                    ("8", NumericLiteral),
                    (" ", Space),
                    ("|", Operator),
                    (" ", Space),
                    ("4", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("12", NumericLiteral)
                ));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestBitwiseExpression_Enum2()
        {
            await TestInMethodAsync($@"
var f = System.AttributeTargets.Assembly $$| System.AttributeTargets.Class",
                ConstantValueContent(
                    ("1", NumericLiteral),
                    (" ", Space),
                    ("|", Operator),
                    (" ", Space),
                    ("4", NumericLiteral),
                    (" ", Space),
                    ("=", Operator),
                    (" ", Space),
                    ("5", NumericLiteral)
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
        public async Task TestConditionalExpression_Int()
        {
            await TestInMethodAsync($@"
var f = false $$? 0 : 1",
                ConstantValueContentNoLineBreak(
                    ("1", NumericLiteral)
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
        public async Task TestNonConstantVariable()
        {
            await TestInMethodAsync(@"
int v = 1;
var f = v $$+ 2",
                ConstantValue());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task TestInvalidConstant()
        {
            await TestInMethodAsync(@"
const int v = int.Parse(""1"");
const int f = v $$+ 2",
                ConstantValue());
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
