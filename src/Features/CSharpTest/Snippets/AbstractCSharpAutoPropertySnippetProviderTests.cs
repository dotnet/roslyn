// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public abstract class AbstractCSharpAutoPropertySnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected virtual string AdditionalPropertyModifiers => string.Empty;

    protected abstract string DefaultPropertyBlockText { get; }

    [Fact]
    public async Task NoSnippetInBlockNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace

            $$
            """);
    }

    [Fact]
    public async Task NoSnippetInTopLevelContextTest()
    {
        await VerifySnippetIsAbsentAsync("""
            System.Console.WriteLine();
            $$
            """);
    }

    [Fact]
    public async Task InsertSnippetInClassTest()
    {
        await VerifyDefaultPropertyAsync("""
            class MyClass
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertSnippetInRecordTest()
    {
        await VerifyDefaultPropertyAsync("""
            record MyRecord
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task InsertSnippetInStructTest()
    {
        await VerifyDefaultPropertyAsync("""
            struct MyStruct
            {
                $$
            }
            """);
    }

    [Fact]
    public abstract Task InsertSnippetInReadonlyStructTest();

    [Fact]
    public abstract Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration();

    [Fact]
    public abstract Task InsertSnippetInReadonlyStructTest_ReadonlyModifierInOtherPartialDeclaration_MissingPartialModifier();

    // This case might produce non-default results for different snippets (e.g. no `set` accessor in 'propg' snippet),
    // so it is tested separately for all of them
    [Fact]
    public abstract Task InsertSnippetInInterfaceTest();

    [Fact]
    public async Task InsertSnippetNamingTest()
    {
        await VerifyDefaultPropertyAsync("""
            class MyClass
            {
                public int MyProperty { get; set; }
                $$
            }
            """, "MyProperty1");
    }

    [Fact]
    public async Task NoSnippetInEnumTest()
    {
        await VerifySnippetIsAbsentAsync("""
            enum MyEnum
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoSnippetInMethodTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """);
    }

    [Fact]
    public async Task NoSnippetInConstructorTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public Program()
                {
                    $$
                }
            }
            """);
    }

    [Theory]
    [MemberData(nameof(CommonSnippetTestData.AllAccessibilityModifiers), MemberType = typeof(CommonSnippetTestData))]
    public async Task InsertSnippetAfterAccessibilityModifierTest(string modifier)
    {
        await VerifyPropertyAsync($$"""
            class Program
            {
                {{modifier}} $$
            }
            """, $$"""{{AdditionalPropertyModifiers}}{|0:int|} {|1:MyProperty|} {{DefaultPropertyBlockText}}""");
    }

    protected async Task VerifyPropertyAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string propertyMarkup)
    {
        TestFileMarkupParser.GetPosition(markup, out var code, out var position);
        var expectedCode = code.Insert(position, propertyMarkup + "$$");
        await VerifySnippetAsync(markup, expectedCode);
    }

    protected Task VerifyDefaultPropertyAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string propertyName = "MyProperty")
        => VerifyPropertyAsync(markup, $$"""public {{AdditionalPropertyModifiers}}{|0:int|} {|1:{{propertyName}}|} {{DefaultPropertyBlockText}}""");
}
