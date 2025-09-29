// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReplaceDefaultLiteral;

[Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDefaultLiteral)]
public sealed class ReplaceDefaultLiteralTests(ITestOutputHelper logger) : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpReplaceDefaultLiteralCodeFixProvider());

    private static readonly ImmutableArray<LanguageVersion> s_csharp7_1above =
        [LanguageVersion.CSharp7_1, LanguageVersion.Latest];

    private static readonly ImmutableArray<LanguageVersion> s_csharp7below =
        [
            LanguageVersion.CSharp7,
            LanguageVersion.CSharp6,
            LanguageVersion.CSharp5,
            LanguageVersion.CSharp4,
            LanguageVersion.CSharp3,
            LanguageVersion.CSharp2,
            LanguageVersion.CSharp1,
        ];

    private async Task TestWithLanguageVersionsAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        ImmutableArray<LanguageVersion> versions)
    {
        foreach (var version in versions)
        {
            await TestInRegularAndScriptAsync(initialMarkup, expectedMarkup,
                new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(version)));
        }
    }

    private async Task TestMissingWithLanguageVersionsAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup, ImmutableArray<LanguageVersion> versions)
    {
        foreach (var version in versions)
        {
            await TestMissingInRegularAndScriptAsync(initialMarkup,
                new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(version)));
        }
    }

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_Int()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case [||]default: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (1) { case 0: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_Int_InParentheses()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case ([||]default): }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (1) { case (0): }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_Int_NotInsideCast()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case (int)[||]default: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnDefaultExpression()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case [||]default(int): }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnNumericLiteral()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case [||]0: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_DateTime()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (System.DateTime.Now) { case [||]default: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (System.DateTime.Now) { case default(System.DateTime): }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCaseSwitchLabel_TupleType()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch ((0, true)) { case [||]default: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch ((0, true)) { case default((int, bool)): }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("value")]
    [InlineData("null")]
    [InlineData("default")]
    [InlineData("() => { }")]
    [InlineData("")]
    public Task TestCSharp7_1_InCaseSwitchLabel_NotForInvalidType(string expression)
        => TestMissingWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                {
                    switch ({{expression}}) { case [||]default: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_Int()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case [||]default when true: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (1) { case 0 when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_Int_InParentheses()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case ([||]default) when true: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (1) { case (0) when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotInsideCast()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case (int)[||]default when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnDefaultExpression()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case [||]default(int) when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnNumericLiteral()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (1) { case [||]0 when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_DateTime()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch (System.DateTime.Now) { case [||]default when true: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (System.DateTime.Now) { case default(System.DateTime) when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_TupleType()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    switch ((0, true)) { case [||]default when true: }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch ((0, true)) { case default((int, bool)) when true: }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("value")]
    [InlineData("null")]
    [InlineData("default")]
    [InlineData("() => { }")]
    [InlineData("")]
    public Task TestCSharp7_1_InCasePatternSwitchLabel_NotForInvalidType(string expression)
        => TestMissingWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                {
                    switch ({{expression}}) { case [||]default when true: }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Bool()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (true is [||]default) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (true is false) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Bool_InParentheses()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (true is ([||]default)) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (true is (false)) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Bool_NotInsideCast()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (true is (bool)[||]default) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Bool_NotOnDefaultExpression()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (true is [||]default(bool)) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Bool_NotOnFalseLiteral()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (true is [||]false) { }
                }
            }
            """, s_csharp7_1above);

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
    [InlineData("char", "'\\0'")]
    [InlineData("string", "null")]
    [InlineData("object", "null")]
    public Task TestCSharp7_1_InIsPattern_BuiltInType(string type, string expectedLiteral)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M({{type}} value)
                {
                    if (value is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                void M({{type}} value)
                {
                    if (value is {{expectedLiteral}}) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_DateTime()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (System.DateTime.Now is [||]default) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (System.DateTime.Now is default(System.DateTime)) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_TupleType()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if ((0, true) is [||]default) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if ((0, true) is default((int, bool))) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("class Type { }")]
    [InlineData("interface Type { }")]
    [InlineData("delegate void Type();")]
    public Task TestCSharp7_1_InIsPattern_CustomReferenceType(string typeDeclaration)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                {{typeDeclaration}}
                void M()
                {
                    if (new Type() is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                {{typeDeclaration}}
                void M()
                {
                    if (new Type() is null) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("enum Enum { }")]
    [InlineData("enum Enum { None = 0 }")]
    [InlineData("[Flags] enum Enum { None = 0 }")]
    [InlineData("[System.Flags] enum Enum { None = 1 }")]
    [InlineData("[System.Flags] enum Enum { None = 1, None = 0 }")]
    [InlineData("[System.Flags] enum Enum { Some = 0 }")]
    public Task TestCSharp7_1_InIsPattern_CustomEnum_WithoutSpecialMember(string enumDeclaration)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                {{enumDeclaration}}
                void M()
                {
                    if (new Enum() is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                {{enumDeclaration}}
                void M()
                {
                    if (new Enum() is 0) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("[System.Flags] enum Enum : int { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : uint { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : byte { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : sbyte { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : short { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : ushort { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : long { None = 0 }")]
    [InlineData("[System.Flags] enum Enum : ulong { None = 0 }")]
    [InlineData("[System.Flags] enum Enum { None = default }")]
    [InlineData("[System.Flags] enum Enum { Some = 1, None = 0 }")]
    [InlineData("[System.FlagsAttribute] enum Enum { None = 0, Some = 1 }")]
    public Task TestCSharp7_1_InIsPattern_CustomEnum_WithSpecialMember(string enumDeclaration)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                {{enumDeclaration}}
                void M()
                {
                    if (new Enum() is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                {{enumDeclaration}}
                void M()
                {
                    if (new Enum() is Enum.None) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_CustomStruct()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                struct Struct { }
                void M()
                {
                    if (new Struct() is [||]default) { }
                }
            }
            """,
            """
            class C
            {
                struct Struct { }
                void M()
                {
                    if (new Struct() is default(Struct)) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_AnonymousType()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (new { a = 0 } is [||]default) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (new { a = 0 } is null) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("class Container<T> { }")]
    [InlineData("interface Container<T> { }")]
    [InlineData("delegate void Container<T>();")]
    public Task TestCSharp7_1_InIsPattern_CustomReferenceTypeOfAnonymousType(string typeDeclaration)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                {{typeDeclaration}}
                Container<T> ToContainer<T>(T value) => new Container<T>();
                void M()
                {
                    if (ToContainer(new { x = 0 }) is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                {{typeDeclaration}}
                Container<T> ToContainer<T>(T value) => new Container<T>();
                void M()
                {
                    if (ToContainer(new { x = 0 }) is null) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_NotForCustomStructOfAnonymousType()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                struct Container<T> { }
                Container<T> ToContainer<T>(T value) => new Container<T>();
                void M()
                {
                    if (ToContainer(new { x = 0 }) is [||]default) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("System.Threading", "CancellationToken", "None")]
    public Task TestCSharp7_1_InIsPattern_SpecialTypeQualified1(string @namespace, string type, string member)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                {
                    if (default({{@namespace}}.{{type}}) is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                void M()
                {
                    if (default({{@namespace}}.{{type}}) is {{@namespace}}.{{type}}.{{member}}) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("System", "IntPtr", "Zero")]
    [InlineData("System", "UIntPtr", "Zero")]
    public Task TestCSharp7_1_InIsPattern_SpecialTypeQualified2(string @namespace, string type, string member)
        => TestWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                {
                    if (default({{@namespace}}.{{type}}) is [||]default) { }
                }
            }
            """,
            $$"""
            class C
            {
                void M()
                {
                    if (default({{@namespace}}.{{type}}) is {{@namespace}}.{{type}}.{{member}}) { }
                }
            }
            """, [LanguageVersion.CSharp7_1, LanguageVersion.CSharp10, LanguageVersion.CSharp11, LanguageVersion.Latest]);

    [Theory]
    [InlineData("System.Threading", "CancellationToken", "None")]
    public Task TestCSharp7_1_InIsPattern_SpecialTypeUnqualifiedWithUsing1(string @namespace, string type, string member)
        => TestWithLanguageVersionsAsync(
            $$"""
            using {{@namespace}};
            class C
            {
                void M()
                {
                    if (default({{type}}) is [||]default) { }
                }
            }
            """,
            $$"""
            using {{@namespace}};
            class C
            {
                void M()
                {
                    if (default({{type}}) is {{type}}.{{member}}) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("System", "IntPtr", "Zero")]
    [InlineData("System", "UIntPtr", "Zero")]
    public Task TestCSharp7_1_InIsPattern_SpecialTypeUnqualifiedWithUsing2(string @namespace, string type, string member)
        => TestWithLanguageVersionsAsync(
            $$"""
            using {{@namespace}};
            class C
            {
                void M()
                {
                    if (default({{type}}) is [||]default) { }
                }
            }
            """,
            $$"""
            using {{@namespace}};
            class C
            {
                void M()
                {
                    if (default({{type}}) is {{type}}.{{member}}) { }
                }
            }
            """, [LanguageVersion.CSharp7_1, LanguageVersion.CSharp10, LanguageVersion.CSharp11, LanguageVersion.Latest]);

    [Theory]
    [InlineData("CancellationToken")]
    [InlineData("IntPtr")]
    [InlineData("UIntPtr")]
    public Task TestCSharp7_1_InIsPattern_NotForSpecialTypeUnqualifiedWithoutUsing(string type)
        => TestMissingWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                {
                    if (default({{type}}) is [||]default) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_NotForInvalidType1()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                { 
                    var value;
                    if (value is [||]default) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("value")]
    [InlineData("null")]
    [InlineData("default")]
    [InlineData("")]
    public Task TestCSharp7_1_InIsPattern_NotForInvalidType2(string expression)
        => TestMissingWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                { 
                    var value = {{expression}};
                    if (value is [||]default) { }
                }
            }
            """, s_csharp7_1above);

    [Theory]
    [InlineData("value")]
    [InlineData("null")]
    [InlineData("default")]
    [InlineData("() => { }")]
    [InlineData("")]
    public Task TestCSharp7_1_InIsPattern_NotForInvalidType3(string expression)
        => TestMissingWithLanguageVersionsAsync(
            $$"""
            class C
            {
                void M()
                {
                    if ({{expression}} is [||]default) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Lambda()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                { 
                    var value = () => { };
                    if (value is [||]default) { }
                }
            }
            """, [LanguageVersion.CSharp7_1]);

    [Fact]
    public Task TestCSharpLatest_InIsPattern_Lambda()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                { 
                    var value = () => { };
                    if (value is [||]default) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                { 
                    var value = () => { };
                    if (value is null) { }
                }
            }
            """, [LanguageVersion.Latest]);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_Bool_Trivia()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (true is
                        /*a*/ [||]default /*b*/) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (true is
                        /*a*/ false /*b*/) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_InIsPattern_DateTime_Trivia()
        => TestWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    if (System.DateTime.Now is
                        /*a*/ [||]default /*b*/) { }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    if (System.DateTime.Now is
                        /*a*/ default(System.DateTime) /*b*/) { }
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_NotInsideExpression()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    int i = [||]default;
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7_1_NotInsideExpression_InvalidType()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    var v = [||]default;
                }
            }
            """, s_csharp7_1above);

    [Fact]
    public Task TestCSharp7Lower_NotInsideExpression()
        => TestMissingWithLanguageVersionsAsync(
            """
            class C
            {
                void M()
                {
                    int i = [||]default;
                }
            }
            """, s_csharp7below);
}
