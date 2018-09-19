// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseInterpolatedVerbatimString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseInterpolatedVerbatimString
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseInterpolatedVerbatimString)]
    public class CSharpUseInterpolatedVerbatimStringCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpUseInterpolatedVerbatimStringCodeFixProvider());

        [Fact]
        public async Task Simple()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var s = @[||]$""hello"";
    }
}",
@"class C
{
    void M()
    {
        var s = $@""hello"";
    }
}", parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task AfterString()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var s = @$""hello""[||];
    }
}", parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task InCall()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string x)
    {
        var s = M(@[||]$""hello"");
    }
}",
@"class C
{
    void M(string x)
    {
        var s = M($@""hello"");
    }
}", parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task FixAllInDocument()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var s = {|FixAllInDocument:@$""|}hello"";
        var s2 = @$""hello"";
    }
}",
@"class C
{
    void M()
    {
        var s = $@""hello"";
        var s2 = $@""hello"";
    }
}", parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task MissingOnInterpolatedVerbatimString()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var s = $[||]@""hello"";
    }
}", parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp7_3)));
        }

        [Fact]
        public async Task MissingInCSharp8()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var s = @[||]$""hello"";
    }
}", parameters: new TestParameters().WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp8)));
        }
    }
}
