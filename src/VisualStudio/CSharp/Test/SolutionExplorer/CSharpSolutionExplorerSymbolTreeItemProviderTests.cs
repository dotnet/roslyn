// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.SolutionExplorer;

[UseExportProvider, Trait(Traits.Feature, Traits.Features.SolutionExplorer)]
public sealed class CSharpSolutionExplorerSymbolTreeItemProviderTests
{
    private static readonly TestComposition s_testComposition = VisualStudioTestCompositions.LanguageServices;

    private static async Task TestCompilationUnit(
        string code, string expected)
    {
        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_testComposition);

        var testDocument = workspace.Documents.Single();
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var root = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);

        var service = document.GetRequiredLanguageService<ISolutionExplorerSymbolTreeItemProvider>();
        var items = service.GetItems(root, CancellationToken.None);

        var actual = string.Join("\r\n", items);
        AssertEx.Equal(expected, actual);

        AssertEx.SequenceEqual(
            testDocument.SelectedSpans,
            items.Select(i => i.ItemSyntax.NavigationToken.Span));
    }

    [Fact]
    public async Task TestEmptyFile()
    {
        await TestCompilationUnit("", "");
    }

    [Fact]
    public async Task TestTopLevelClass()
    {
        await TestCompilationUnit("""
            class [|C|]
            {
            }
            """, """
            Name=C Glyph=ClassInternal HasItems=False
            """);
    }

    [Fact]
    public async Task TestTwoTopLevelTypes()
    {
        await TestCompilationUnit("""
            class [|C|]
            {
            }

            class [|D|]
            {
            }
            """, """
            Name=C Glyph=ClassInternal HasItems=False
            Name=D Glyph=ClassInternal HasItems=False
            """);
    }

    [Fact]
    public async Task TestDelegatesAndEnums()
    {
        await TestCompilationUnit("""
            delegate string [|D|](int x);

            enum [|E|]
            {
            }
            """, """
            Name=D(int) : string Glyph=DelegateInternal HasItems=False
            Name=E Glyph=EnumInternal HasItems=False
            """);
    }

    [Fact]
    public async Task TestTypesInBlockNamespace()
    {
        await TestCompilationUnit("""
            namespace N
            {
                class [|C|]
                {
                }

                class [|D|]
                {
                }
            }
            """, """
            Name=C Glyph=ClassInternal HasItems=False
            Name=D Glyph=ClassInternal HasItems=False
            """);
    }

    [Fact]
    public async Task TestTypesInFileScopedNamespace()
    {
        await TestCompilationUnit("""
            namespace N;

            class [|C|]
            {
            }

            class [|D|]
            {
            }
            """, """
            Name=C Glyph=ClassInternal HasItems=False
            Name=D Glyph=ClassInternal HasItems=False
            """);
    }

    [Fact]
    public async Task TestTypesAcrossNamespaces()
    {
        await TestCompilationUnit("""
            class [|C|]
            {
            }

            namespace N
            {
                class [|D|]
                {
                }
            }
            """, """
            Name=C Glyph=ClassInternal HasItems=False
            Name=D Glyph=ClassInternal HasItems=False
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestTypePermutations(
        [CombinatorialValues("Public", "Private", "Protected", "Internal")] string accessibility,
        [CombinatorialValues("Record", "Class", "Interface", "Struct")] string type)
    {
        await TestCompilationUnit($$"""
            {{accessibility.ToLowerInvariant()}} {{type.ToLowerInvariant()}} [|C|]
            {
            }
            """, $$"""
            Name=C Glyph={{type switch { "Record" => "Class", "Struct" => "Structure", _ => type }}}{{accessibility}} HasItems=False
            """);
    }

    [Theory, CombinatorialData]
    public async Task TestTypeHasItems(
        [CombinatorialValues("Record", "Class", "Interface", "Struct")] string type)
    {
        await TestCompilationUnit($$"""
            {{type.ToLowerInvariant()}} [|C|]
            {
                int i;
            }
            """, $$"""
            Name=C Glyph={{type switch { "Record" => "Class", "Struct" => "Structure", _ => type }}}Internal HasItems=True
            """);
    }

    [Fact]
    public async Task TestEnumHasItems()
    {
        await TestCompilationUnit("""
            enum [|E|]
            {
                A,
                B,
                C
            }
            """, """
            Name=E Glyph=EnumInternal HasItems=True
            """);
    }

    [Theory]
    [InlineData("int", "int")]
    [InlineData("int[]", "int[]")]
    [InlineData("int[][]", "int[][]")]
    [InlineData("int[,][,,]", "int[,][,,]")]
    [InlineData("int*", "int*")]
    [InlineData("int?", "int?")]
    [InlineData("(int, string)", "(int, string)")]
    [InlineData("(int a, string b)", "(int a, string b)")]
    [InlineData("delegate*unmanaged[a]<int, string>", "delegate*<int, string>")]
    [InlineData("A.B", "B")]
    [InlineData("A::B", "B")]
    [InlineData("A::B.C", "C")]
    [InlineData("A", "A")]
    [InlineData("A.B<C::D, E::F.G<int>>", "B<D, G<int>>")]
    public async Task TestTypes(
        string parameterType, string resultType)
    {
        await TestCompilationUnit($$"""
            delegate void [|D|]({{parameterType}} x);
            """, $$"""
            Name=D({{resultType}}) : void Glyph=DelegateInternal HasItems=False
            """);
    }
}
