// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

public sealed class CSharpProprSnippetProviderTests : AbstractCSharpAutoPropertySnippetProviderTests
{
    protected override string SnippetIdentifier => "propr";

    protected override string DefaultPropertyBlockText => "{ get; set; }";

    [WorkItem("https://github.com/dotnet/roslyn/issues/75128")]
    public override async Task InsertSnippetInReadonlyStructTest()
    {
        // Ensure we don't generate redundant `set` accessor when executed in readonly struct
        await VerifyPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """, "public required {|0:int|} {|1:MyProperty|} { get; }");
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/75128")]
    public override async Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration()
    {
        // Ensure we don't generate redundant `set` accessor when executed in readonly struct
        await VerifyPropertyAsync("""
            partial struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public required {|0:int|} {|1:MyProperty|} { get; }");
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/75128")]
    public override async Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
    {
        // Even though there is no `partial` modifier on the first declaration
        // compiler still treats the whole type as partial since it is more likely that
        // the user's intent was to have a partial type and they just forgot the modifier.
        // Thus we still recognize that as `readonly` context and don't generate a setter
        await VerifyPropertyAsync("""
            struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public required {|0:int|} {|1:MyProperty|} { get; }");
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/75128")]
    public override async Task InsertSnippetInInterfaceTest()
    {
        await VerifyDefaultPropertyAsync("""
            interface MyInterface
            {
                $$
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/75128")]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    public async Task InsertSnippetAfterAccessibilityModifierTest(string modifier)
    {
        await VerifyPropertyAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """,
            $$"""required {|0:int|} {|1:MyProperty|} {{DefaultPropertyBlockText}}""");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/75128")]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("private protected")]
    public async Task DoNotInsertSnippetAfterAccessibilityModifierTest(string modifier)
    {
        await VerifySnippetIsAbsentAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """);
    }

    protected override Task VerifyDefaultPropertyAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string propertyName = "MyProperty")
        => VerifyPropertyAsync(markup, $$"""public required {|0:int|} {|1:{{propertyName}}|} {{DefaultPropertyBlockText}}""");
}
