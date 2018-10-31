// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReplaceDefaultLiteral
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDefaultLiteral)]
    public sealed class ReplaceDefaultLiteralTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpReplaceDefaultLiteralCodeFixProvider());

        private static readonly TestParameters s_csharpLatest =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        private static readonly TestParameters s_csharp7_1 =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1));

        [Fact]
        public async Task TestCSharpLatest_InCaseSwitchLabel_Bool()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (true) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (true) { case false: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCaseSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime): }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCasePatternSwitchLabel_Bool()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (true) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (true) { case false when true: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCasePatternSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime) when true: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InIsPattern_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (1 is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (1 is 0) { }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InIsPattern_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (System.DateTime.Now is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (System.DateTime.Now is default(System.DateTime)) { }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case 0: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case ([||]default): }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case (0): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case (int)[||]default: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]default(int): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnNumericLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]0: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch ((0, true)) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch ((0, true)) { case default((int, bool)): }
    }
}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotForInvalidType(string expression)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        switch ({expression}) {{ case [||]default: }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case 0 when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case ([||]default) when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case (0) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case (int)[||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]default(int) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnNumericLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]0 when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch ((0, true)) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch ((0, true)) { case default((int, bool)) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotForInvalidType(string expression)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        switch ({expression}) {{ case [||]default when true: }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is false) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is ([||]default)) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is (false)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is (bool)[||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is [||]default(bool)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotOnFalseLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is [||]false) { }
    }
}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("int", "0")]
        [InlineData("uint", "0U")]
        [InlineData("byte", "0")]
        [InlineData("sbyte", "0")]
        [InlineData("short", "0")]
        [InlineData("ushort", "0")]
        [InlineData("long", "0L")]
        [InlineData("ulong", "0UL")]
        [InlineData("float", "0F")]
        [InlineData("double", "0D")]
        [InlineData("decimal", "0M")]
        public async Task TestCSharp7_1_InIsPattern_NumericType(string type, string expectedLiteral)
        {
            await TestInRegularAndScript1Async(
$@"class C
{{
    void M()
    {{
        {type} value = 1;
        if (value is [||]default) {{ }}
    }}
}}",
$@"class C
{{
    void M()
    {{
        {type} value = 1;
        if (value is {expectedLiteral}) {{ }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Char()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        char value = '1';
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        char value = '1';
        if (value is '\0') { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_String()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        string value = "";
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        string value = "";
        if (value is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Object()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var value = new object();
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        var value = new object();
        if (value is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (System.DateTime.Now is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (System.DateTime.Now is default(System.DateTime)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if ((0, true) is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if ((0, true) is default((int, bool))) { }
    }
}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("class Type { }")]
        [InlineData("interface Type { }")]
        [InlineData("delegate void Type();")]
        public async Task TestCSharp7_1_InIsPattern_CustomReferenceType(string typeDeclaration)
        {
            await TestInRegularAndScript1Async(
$@"class C
{{
    {typeDeclaration}
    void M()
    {{
        if (new Type() is [||]default) {{ }}
    }}
}}",
$@"class C
{{
    {typeDeclaration}
    void M()
    {{
        if (new Type() is null) {{ }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomEnum()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    enum Enum { }
    void M()
    {
        if (new Enum() is [||]default) { }
    }
}",
@"class C
{
    enum Enum { }
    void M()
    {
        if (new Enum() is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomStruct()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    struct Struct { }
    void M()
    {
        if (new Struct() is [||]default) { }
    }
}",
@"class C
{
    struct Struct { }
    void M()
    {
        if (new Struct() is default(Struct)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_AnonymousType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (new { a = 0 } is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (new { a = 0 } is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("class Container<T> { }")]
        [InlineData("interface Container<T> { }")]
        [InlineData("delegate void Container<T>();")]
        public async Task TestCSharp7_1_InIsPattern_CustomReferenceTypeOfAnonymousType(string typeDeclaration)
        {
            await TestInRegularAndScript1Async(
$@"class C
{{
    {typeDeclaration}
    Container<T> ToContainer<T>(T value) => new Container<T>();
    void M()
    {{
        if (ToContainer(new {{ x = 0 }}) is [||]default) {{ }}
    }}
}}",
$@"class C
{{
    {typeDeclaration}
    Container<T> ToContainer<T>(T value) => new Container<T>();
    void M()
    {{
        if (ToContainer(new {{ x = 0 }}) is null) {{ }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForCustomStructOfAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    struct Container<T> { }
    Container<T> ToContainer<T>(T value) => new Container<T>();
    void M()
    {
        if (ToContainer(new { x = 0 }) is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    { 
        var value;
        if (value is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType2(string expression)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{ 
        var value = {expression};
        if (value is [||]default) {{ }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType3(string expression)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        if ({expression} is [||]default) {{ }}
    }}
}}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_Trivia()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is
            /*a*/ [||]default /*b*/) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is
            /*a*/ false /*b*/) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_DateTime_Trivia()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (System.DateTime.Now is
            /*a*/ [||]default /*b*/) { }
    }
}",
@"class C
{
    void M()
    {
        if (System.DateTime.Now is
            /*a*/ default(System.DateTime) /*b*/) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression_InvalidType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var v = [||]default;
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7Lower_NotInsideExpression()
        {
            foreach (var languageVersion in new[] {
                LanguageVersion.CSharp7,
                LanguageVersion.CSharp6,
                LanguageVersion.CSharp5,
                LanguageVersion.CSharp4,
                LanguageVersion.CSharp3,
                LanguageVersion.CSharp2,
                LanguageVersion.CSharp1,
                })
            {
                await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
            }
        }
    }
}
