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

    [WorkItem("https://github.com/dotnet/roslyn/issues/79954")]
    public override Task InsertSnippetInReadonlyStructTest()
        => VerifyPropertyAsync("""
            readonly struct MyStruct
            {
                $$
            }
            """, "public required {|0:int|} {|1:MyProperty|} { get; init; }");

    [WorkItem("https://github.com/dotnet/roslyn/issues/79954")]
    public override Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration()
        => VerifyPropertyAsync("""
            partial struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public required {|0:int|} {|1:MyProperty|} { get; init; }");

    [WorkItem("https://github.com/dotnet/roslyn/issues/79954")]
    public override Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier()
        => VerifyPropertyAsync("""
            struct MyStruct
            {
                $$
            }

            readonly partial struct MyStruct
            {
            }
            """, "public required {|0:int|} {|1:MyProperty|} { get; init; }");

    public override Task VerifySnippetInInterfaceTest()
        => VerifySnippetIsAbsentAsync("""
            interface MyInterface
            {
                $$
            }
            """);

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    public override Task InsertSnippetAfterAllowedAccessibilityModifierTest(string modifier)
        => VerifyPropertyAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """, $$"""required {|0:int|} {|1:MyProperty|} {{DefaultPropertyBlockText}}""");

    [Theory]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("private protected")]
    public Task NoSnippetAfterWrongAccessibilityModifierTest(string modifier)
        => VerifySnippetIsAbsentAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """);

    protected override Task VerifyDefaultPropertyAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string propertyName = "MyProperty")
        => VerifyPropertyAsync(markup, $$"""public required {|0:int|} {|1:{{propertyName}}|} {{DefaultPropertyBlockText}}""");
}
