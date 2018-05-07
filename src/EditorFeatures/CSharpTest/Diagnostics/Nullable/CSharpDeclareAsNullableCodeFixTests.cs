// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.DeclareAsNullable
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsDeclareAsNullable)]
    public class CSharpDeclareAsNullableCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpDeclareAsNullableCodeFixProvider());

        private static readonly TestParameters s_nullableFeature = new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));

        [Fact]
        public async Task FixAll()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    static string M()
    {
        return {|FixAllInDocument:null|};
    }
    static string M2(bool b)
    {
        if (b)
            return null;
        else
            return null;
    }
}",
@"class Program
{
    static string? M()
    {
        return null;
    }
    static string? M2(bool b)
    {
        if (b)
            return null;
        else
            return null;
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    static string M()
    {
        return [|null|];
    }
}",
@"class Program
{
    static string? M()
    {
        return null;
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_WithTrivia()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    static /*before*/ string /*after*/ M()
    {
        return [|null|];
    }
}",
@"class Program
{
    static /*before*/ string? /*after*/ M()
    {
        return null;
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixReturnType_ArrowBody()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    static string M() => [|null|];
}",
@"class Program
{
    static string? M() => null;
}", parameters: s_nullableFeature);
        }

        [Fact]
        [WorkItem(26639, "https://github.com/dotnet/roslyn/issues/26639")]
        public async Task FixReturnType_LocalFunction_ArrowBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void M()
    {
        string local() => [|null|];
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        [WorkItem(26639, "https://github.com/dotnet/roslyn/issues/26639")]
        public async Task FixLocalFunctionReturnType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    void M()
    {
        string local()
        {
            return [|null|];
        }
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task NoFixAlreadyNullableReturnType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static string? M()
    {
        return [|null|];
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        [WorkItem(26628, "https://github.com/dotnet/roslyn/issues/26628")]
        public async Task FixField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    string x = [|null|];
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixLocalDeclaration()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    static void M()
    {
        string x = [|null|];
    }
}",
@"class Program
{
    static void M()
    {
        string? x = null;
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixLocalDeclaration_WithVar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void M()
    {
        var x = [|null|];
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task NoFixMultiDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void M()
    {
        string x = [|null|], y = null;
    }
}", parameters: s_nullableFeature);
        }

        [Fact]
        [WorkItem(26628, "https://github.com/dotnet/roslyn/issues/26628")]
        public async Task FixPropertyDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    string x { get; set; } = [|null|];
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixPropertyDeclaration_WithReturnNull()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    string x { get { return [|null|]; } }
}",
@"class Program
{
    string? x { get { return null; } }
}", parameters: s_nullableFeature);
        }

        [Fact]
        public async Task FixPropertyDeclaration_ArrowBody()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    string x => [|null|];
}",
@"class Program
{
    string? x => null;
}", parameters: s_nullableFeature);
        }

        [Fact]
        [WorkItem(26626, "https://github.com/dotnet/roslyn/issues/26626")]
        public async Task FixOptionalParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class Program
{
    static void M(string x = [|null|]) { }
}", parameters: s_nullableFeature);
        }
    }
}
