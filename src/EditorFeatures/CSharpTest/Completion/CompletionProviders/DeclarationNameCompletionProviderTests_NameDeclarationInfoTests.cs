// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DeclarationInfoTests;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class DeclarationNameCompletion_ContextTests
{
    private readonly CSharpTestWorkspaceFixture _fixture = new();

    [Fact]
    public async Task AfterTypeInClass1()
    {
        var markup = """
            class C
            {
                int $$
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Field),
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));
        await VerifyNoModifiers(markup);
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task AfterTypeInClassWithAccessibility()
    {
        var markup = """
            class C
            {
                public int $$
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Field),
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));
        await VerifyNoModifiers(markup);
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, Accessibility.Public);
    }

    [Fact]
    public async Task AfterTypeInClassVirtual()
    {
        var markup = """
            class C
            {
                public virtual int $$
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));
        await VerifyModifiers(markup, new DeclarationModifiers(isVirtual: true));
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, Accessibility.Public);
    }

    [Fact]
    public async Task AfterTypeInClassStatic()
    {
        var markup = """
            class C
            {
                private static int $$
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Field),
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));
        await VerifyModifiers(markup, new DeclarationModifiers(isStatic: true));
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, Accessibility.Private);
    }

    [Fact]
    public async Task AfterTypeInClassConst()
    {
        var markup = """
            class C
            {
                private const int $$
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Field));
        await VerifyModifiers(markup, new DeclarationModifiers(isConst: true));
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, Accessibility.Private);
    }

    [Fact]
    public async Task VariableDeclaration1()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    int $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local),
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task VariableDeclaration2()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    int c1, $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ReadonlyVariableDeclaration1()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    readonly int $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local),
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));
        await VerifyModifiers(markup, new DeclarationModifiers(isReadOnly: true));
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ReadonlyVariableDeclaration2()
    {
        var markup = """
            class C
            {
                void goo()
                {
                    readonly int c1, $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local));
        await VerifyModifiers(markup, new DeclarationModifiers(isReadOnly: true));
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task UsingVariableDeclaration1()
    {
        var markup = """
            class C
            {
                void M()
                {
                    using (int i$$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task UsingVariableDeclaration2()
    {
        var markup = """
            class C
            {
                void M()
                {
                    using (int i1, $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ForVariableDeclaration1()
    {
        var markup = """
            class C
            {
                void M()
                {
                    for (int i$$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ForVariableDeclaration2()
    {
        var markup = """
            class C
            {
                void M()
                {
                    for (int i1, $$
                }
            }
            """;
        await VerifySymbolKinds(markup);
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, null);
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ForEachVariableDeclaration()
    {
        var markup = """
            class C
            {
                void M()
                {
                    foreach (int $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Local));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "int");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task Parameter1()
    {
        var markup = """
            class C
            {
                void goo(C $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Parameter));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "global::C");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task Parameter2()
    {
        var markup = """
            class C
            {
                void goo(C c1, C $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Parameter));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "global::C");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ParameterAfterPredefinedType1()
    {
        var markup = """
            class C
            {
                void goo(string $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Parameter));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "string");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ParameterAfterPredefinedType2()
    {
        var markup = """
            class C
            {
                void goo(C c1, string $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Parameter));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "string");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public async Task ParameterAfterGeneric()
    {
        var markup = """
            using System.Collections.Generic;
            class C
            {
                void goo(C c1, List<string> $$
                }
            }
            """;
        await VerifySymbolKinds(markup,
            new SymbolKindOrTypeKind(SymbolKind.Parameter));
        await VerifyModifiers(markup, new DeclarationModifiers());
        await VerifyTypeName(markup, "global::System.Collections.Generic.List<string>");
        await VerifyAccessibility(markup, null);
    }

    [Fact]
    public Task ClassTypeParameter1()
        => VerifySymbolKinds("""
            class C<$$
            {
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.TypeParameter));

    [Fact]
    public Task ClassTypeParameter2()
        => VerifySymbolKinds("""
            class C<T1, $$
            {
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.TypeParameter));

    [Fact]
    public Task ModifierExclusion1()
        => VerifySymbolKinds("""
            class C
            {
                readonly int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Field));

    [Fact]
    public Task ModifierExclusion2()
        => VerifySymbolKinds("""
            class C
            {
                const int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Field));

    [Fact]
    public Task ModifierExclusion3()
        => VerifySymbolKinds("""
            class C
            {
                abstract int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));

    [Fact]
    public Task ModifierExclusion4()
        => VerifySymbolKinds("""
            class C
            {
                virtual int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));

    [Fact]
    public Task ModifierExclusion5()
        => VerifySymbolKinds("""
            class C
            {
                sealed int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));

    [Fact]
    public Task ModifierExclusion6()
        => VerifySymbolKinds("""
            class C
            {
                override int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));

    [Fact]
    public Task ModifierExclusion7()
        => VerifySymbolKinds("""
            class C
            {
                async int $$
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.Ordinary));

    [Fact]
    public Task ModifierExclusion8()
        => VerifySymbolKinds("""
            class C
            {
                partial int $$
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Field),
            new SymbolKindOrTypeKind(SymbolKind.Property),
            new SymbolKindOrTypeKind(MethodKind.Ordinary));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_Const(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    const {{type}} $$
                }
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_ConstLocalDeclaration(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    const {{type}} v$$ = default;
                }
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_ConstLocalFunction(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    const {{type}} v$$()
                    {
                    }
                }
            }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_Async(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    async {{type}} v$$
                }
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_AsyncLocalDeclaration(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    async {{type}} v$$ = default;
                }
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_AsyncLocalFunction(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    async {{type}} v$$()
                    {
                    }
                }
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_Unsafe(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    unsafe {{type}} $$
                }
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_UnsafeLocalDeclaration(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    unsafe {{type}} v$$ = default;
                }
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Theory]
    [InlineData("int")]
    [InlineData("C")]
    [InlineData("List<string>")]
    public Task ModifierExclusionInsideMethod_UnsafeLocalFunction(string type)
        => VerifySymbolKinds($$"""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    unsafe {{type}} v$$()
                    {
                    }
                }
            }
            """,
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Fact]
    public Task LocalInsideMethod1()
        => VerifySymbolKinds("""
            namespace ConsoleApp1
            {
                class ReallyLongClassName { }
                class Program
                {
                    static void Main(string[] args)
                    {
                        ReallyLongClassName $$
                    }
                }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local),
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Fact]
    public Task LocalInsideMethod2()
        => VerifySymbolKinds("""
            namespace ConsoleApp1
            {
                class ReallyLongClassName<T> { }
                class Program
                {
                    static void Main(string[] args)
                    {
                        ReallyLongClassName<int> $$
                    }
                }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local),
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Fact]
    public Task LocalInsideMethodAfterPredefinedTypeKeyword()
        => VerifySymbolKinds("""
            namespace ConsoleApp1
            {
                class ReallyLongClassName { }
                class Program
                {
                    static void Main(string[] args)
                    {
                        string $$
                    }
                }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local),
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    [Fact]
    public Task LocalInsideMethodAfterArray()
        => VerifySymbolKinds("""
            namespace ConsoleApp1
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        string[] $$
                    }
                }
            """,
            new SymbolKindOrTypeKind(SymbolKind.Local),
            new SymbolKindOrTypeKind(MethodKind.LocalFunction));

    private async Task VerifyTypeName(string markup, string? typeName)
    {
        var result = await GetResultsAsync(markup);
        Assert.Equal(typeName, result.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private async Task VerifyNoModifiers(string markup)
    {
        var result = await GetResultsAsync(markup);
        Assert.Equal(default, result.Modifiers);
    }

    private async Task VerifySymbolKinds(string markup, params SymbolKindOrTypeKind[] expectedSymbolKinds)
    {
        var result = await GetResultsAsync(markup);
        Assert.True(expectedSymbolKinds.SequenceEqual(result.PossibleSymbolKinds));
    }

    private async Task VerifyModifiers(string markup, DeclarationModifiers modifiers)
    {
        var result = await GetResultsAsync(markup);
        Assert.Equal(modifiers, result.Modifiers);
    }

    private async Task VerifyAccessibility(string markup, Accessibility? accessibility)
    {
        var result = await GetResultsAsync(markup);
        Assert.Equal(accessibility, result.DeclaredAccessibility);
    }

    private async Task<NameDeclarationInfo> GetResultsAsync(string markup)
    {
        var (document, position) = ApplyChangesToFixture(markup);
        var result = await NameDeclarationInfo.GetDeclarationInfoAsync(document, position, CancellationToken.None);
        return result;
    }

    private (Document, int) ApplyChangesToFixture(string markup)
    {
        MarkupTestFile.GetPosition(markup, out var text, out int position);
        return (_fixture.UpdateDocument(text, SourceCodeKind.Regular), position);
    }
}
