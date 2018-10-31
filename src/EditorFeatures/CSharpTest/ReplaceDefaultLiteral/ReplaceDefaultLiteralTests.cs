// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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

        private static readonly ImmutableArray<LanguageVersion> s_csharp7_1above =
            ImmutableArray.Create(
                LanguageVersion.CSharp7_1,
                LanguageVersion.Latest);

        private static readonly ImmutableArray<LanguageVersion> s_csharp7below =
            ImmutableArray.Create(
                LanguageVersion.CSharp7,
                LanguageVersion.CSharp6,
                LanguageVersion.CSharp5,
                LanguageVersion.CSharp4,
                LanguageVersion.CSharp3,
                LanguageVersion.CSharp2,
                LanguageVersion.CSharp1);

        private async Task TestWithLanguageVersionsAsync(string initialMarkup, string expectedMarkup, ImmutableArray<LanguageVersion> versions)
        {
            foreach (var version in versions)
            {
                await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup,
                    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(version));
            }
        }

        private async Task TestMissingWithLanguageVersionsAsync(string initialMarkup, ImmutableArray<LanguageVersion> versions)
        {
            foreach (var version in versions)
            {
                await TestMissingInRegularAndScriptAsync(initialMarkup,
                    new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(version)));
            }
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_InParentheses()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotInsideCast()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        switch (1) { case (int)[||]default: }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnDefaultExpression()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]default(int): }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnNumericLiteral()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]0: }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotForInvalidType(string expression)
        {
            await TestMissingWithLanguageVersionsAsync(
$@"class C
{{
    void M()
    {{
        switch ({expression}) {{ case [||]default: }}
    }}
}}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_InParentheses()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotInsideCast()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        switch (1) { case (int)[||]default when true: }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnDefaultExpression()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]default(int) when true: }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnNumericLiteral()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]0 when true: }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotForInvalidType(string expression)
        {
            await TestMissingWithLanguageVersionsAsync(
$@"class C
{{
    void M()
    {{
        switch ({expression}) {{ case [||]default when true: }}
    }}
}}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_InParentheses()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotInsideCast()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        if (true is (bool)[||]default) { }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotOnDefaultExpression()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        if (true is [||]default(bool)) { }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotOnFalseLiteral()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        if (true is [||]false) { }
    }
}", s_csharp7_1above);
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
            await TestWithLanguageVersionsAsync(
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
}}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Char()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_String()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Object()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Theory]
        [InlineData("class Type { }")]
        [InlineData("interface Type { }")]
        [InlineData("delegate void Type();")]
        public async Task TestCSharp7_1_InIsPattern_CustomReferenceType(string typeDeclaration)
        {
            await TestWithLanguageVersionsAsync(
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
}}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomEnum()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomStruct()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_AnonymousType()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Theory]
        [InlineData("class Container<T> { }")]
        [InlineData("interface Container<T> { }")]
        [InlineData("delegate void Container<T>();")]
        public async Task TestCSharp7_1_InIsPattern_CustomReferenceTypeOfAnonymousType(string typeDeclaration)
        {
            await TestWithLanguageVersionsAsync(
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
}}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForCustomStructOfAnonymousType()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    struct Container<T> { }
    Container<T> ToContainer<T>(T value) => new Container<T>();
    void M()
    {
        if (ToContainer(new { x = 0 }) is [||]default) { }
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType1()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    { 
        var value;
        if (value is [||]default) { }
    }
}", s_csharp7_1above);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType2(string expression)
        {
            await TestMissingWithLanguageVersionsAsync(
$@"class C
{{
    void M()
    {{ 
        var value = {expression};
        if (value is [||]default) {{ }}
    }}
}}", s_csharp7_1above);
        }

        [Theory]
        [InlineData("value")]
        [InlineData("null")]
        [InlineData("default")]
        [InlineData("() => { }")]
        [InlineData("")]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType3(string expression)
        {
            await TestMissingWithLanguageVersionsAsync(
$@"class C
{{
    void M()
    {{
        if ({expression} is [||]default) {{ }}
    }}
}}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_Trivia()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_DateTime_Trivia()
        {
            await TestWithLanguageVersionsAsync(
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
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression_InvalidType()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        var v = [||]default;
    }
}", s_csharp7_1above);
        }

        [Fact]
        public async Task TestCSharp7Lower_NotInsideExpression()
        {
            await TestMissingWithLanguageVersionsAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", s_csharp7below);
        }
    }
}
