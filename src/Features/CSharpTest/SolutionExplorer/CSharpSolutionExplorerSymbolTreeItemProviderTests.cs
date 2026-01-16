// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.SolutionExplorer;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SolutionExplorer;

[UseExportProvider, Trait(Traits.Feature, Traits.Features.SolutionExplorer)]
public sealed class CSharpSolutionExplorerSymbolTreeItemProviderTests
    : AbstractSolutionExplorerSymbolTreeItemProviderTests
{
    protected override TestWorkspace CreateWorkspace(string code)
    {
        return TestWorkspace.CreateCSharp(
            code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
    }

    private Task TestCompilationUnit(
        string code, string expected)
    {
        return TestNode<CompilationUnitSyntax>(code, expected);
    }

    [Fact]
    public Task TestEmptyFile()
        => TestCompilationUnit("", "");

    [Fact]
    public Task TestTopLevelClass()
        => TestCompilationUnit("""
            class [|C|]
            {
            }
            """, """
            Name="C" Glyph=ClassInternal HasItems=False
            """);

    [Fact]
    public Task TestTwoTopLevelTypes()
        => TestCompilationUnit("""
            class [|C|]
            {
            }

            class [|D|]
            {
            }
            """, """
            Name="C" Glyph=ClassInternal HasItems=False
            Name="D" Glyph=ClassInternal HasItems=False
            """);

    [Fact]
    public Task TestDelegatesAndEnums()
        => TestCompilationUnit("""
            delegate string [|D|](int x);

            enum [|E|]
            {
            }
            """, """
            Name="D(int) : string" Glyph=DelegateInternal HasItems=False
            Name="E" Glyph=EnumInternal HasItems=False
            """);

    [Fact]
    public Task TestTypesInBlockNamespace()
        => TestCompilationUnit("""
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
            Name="C" Glyph=ClassInternal HasItems=False
            Name="D" Glyph=ClassInternal HasItems=False
            """);

    [Fact]
    public Task TestTypesInFileScopedNamespace()
        => TestCompilationUnit("""
            namespace N;

            class [|C|]
            {
            }

            class [|D|]
            {
            }
            """, """
            Name="C" Glyph=ClassInternal HasItems=False
            Name="D" Glyph=ClassInternal HasItems=False
            """);

    [Fact]
    public Task TestTypesAcrossNamespaces()
        => TestCompilationUnit("""
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
            Name="C" Glyph=ClassInternal HasItems=False
            Name="D" Glyph=ClassInternal HasItems=False
            """);

    [Theory, CombinatorialData]
    public Task TestTypePermutations(
        [CombinatorialValues("Public", "Private", "Protected", "Internal")] string accessibility,
        [CombinatorialValues("Record", "Class", "Interface", "Struct")] string type)
        => TestCompilationUnit($$"""
            {{accessibility.ToLowerInvariant()}} {{type.ToLowerInvariant()}} [|C|]
            {
            }
            """, $$"""
            Name="C" Glyph={{type switch { "Record" => "Class", "Struct" => "Structure", _ => type }}}{{accessibility}} HasItems=False
            """);

    [Theory, CombinatorialData]
    public Task TestTypeHasItems(
        [CombinatorialValues("Record", "Class", "Interface", "Struct")] string type)
        => TestCompilationUnit($$"""
            {{type.ToLowerInvariant()}} [|C|]
            {
                int i;
            }
            """, $$"""
            Name="C" Glyph={{type switch { "Record" => "Class", "Struct" => "Structure", _ => type }}}Internal HasItems=True
            """);

    [Fact]
    public Task TestEnumHasItems()
        => TestCompilationUnit("""
            enum [|E|]
            {
                A,
                B,
                C
            }
            """, """
            Name="E" Glyph=EnumInternal HasItems=True
            """);

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
    public Task TestTypes(
        string parameterType, string resultType)
        => TestCompilationUnit($$"""
            delegate void [|D|]({{parameterType}} x);
            """, $$"""
            Name="D({{resultType}}) : void" Glyph=DelegateInternal HasItems=False
            """);

    [Fact]
    public Task TestGenericClass()
        => TestCompilationUnit("""
            class [|C|]<T>
            {
            }
            """, """
            Name="C<T>" Glyph=ClassInternal HasItems=False
            """);

    [Fact]
    public Task TestGenericDelegate()
        => TestCompilationUnit("""
            delegate void [|D|]<T>();
            """, """
            Name="D<T>() : void" Glyph=DelegateInternal HasItems=False
            """);

    [Fact]
    public Task TestEnumMembers()
        => TestNode<EnumDeclarationSyntax>("""
            enum E
            {
                [|A|], [|B|], [|C|]
            }
            """, """
            Name="A" Glyph=EnumMemberPublic HasItems=False
            Name="B" Glyph=EnumMemberPublic HasItems=False
            Name="C" Glyph=EnumMemberPublic HasItems=False
            """);

    [Fact]
    public Task TestClassMembers()
        => TestNode<ClassDeclarationSyntax>("""
            class C
            {
                private int [|a|], [|b|];
                public P [|Prop|] => default;
                internal [|C|]() { }
                ~[|C|]() { }

                protected R [|this|][string s] => default;
                private event Action [|A|] { }
                public event Action [|B|], [|C|];

                void [|M|]<T>(int a) { }
                public void IInterface.[|O|]() { }

                public static C operator [|+|](C c1, int a) => default;

                internal static implicit operator [|int|](C c1) => default;
            }
            """, """
            Name="a : int" Glyph=FieldPrivate HasItems=False
            Name="b : int" Glyph=FieldPrivate HasItems=False
            Name="Prop : P" Glyph=PropertyPublic HasItems=False
            Name="C()" Glyph=MethodInternal HasItems=False
            Name="~C()" Glyph=MethodPrivate HasItems=False
            Name="this[string] : R" Glyph=PropertyProtected HasItems=False
            Name="A : Action" Glyph=EventPrivate HasItems=False
            Name="B : Action" Glyph=EventPublic HasItems=False
            Name="C : Action" Glyph=EventPublic HasItems=False
            Name="M<T>(int) : void" Glyph=MethodPrivate HasItems=False
            Name="O() : void" Glyph=MethodPublic HasItems=False
            Name="operator +(C, int) : C" Glyph=OperatorPublic HasItems=False
            Name="implicit operator int(C)" Glyph=OperatorInternal HasItems=False
            """);

    [Fact]
    public Task TestExtension1()
        => TestNode<ClassDeclarationSyntax>("""
            static class C
            {
                [|extension|]<T>(int i)
                {
                }

                public static void [|M|](this int i) {}
            }
            """, """
            Name="extension<T>(int)" Glyph=ClassPublic HasItems=False
            Name="M(int) : void" Glyph=ExtensionMethodPublic HasItems=False
            """);

    [Fact]
    public Task TestExtension2()
        => TestNode<ExtensionBlockDeclarationSyntax>("""
            static class C
            {
                extension<T>(int i)
                {
                    public void [|M|]() { }
                }
            }
            """, """
            Name="M() : void" Glyph=ExtensionMethodPublic HasItems=False
            """);

    [Fact]
    public Task TestMemberSetsHasItemsForLocalFunction()
        => TestNode<ClassDeclarationSyntax>("""
            class C
            {
                private void [|M|]()
                {
                    void MethodLocal() { }
                }
            }
            """, """
            Name="M() : void" Glyph=MethodPrivate HasItems=True
            """);

    [Fact]
    public Task TestLocalFunctionSetsHasItemsForNestedLocalFunction()
        => TestNode<MethodDeclarationSyntax>("""
            class C
            {
                private void M()
                {
                    void [|MethodLocal|]()
                    {
                        void NestedLocal() { }
                    }
                }
            }
            """, """
            Name="MethodLocal() : void" Glyph=MethodPrivate HasItems=True
            """);

    [Fact]
    public Task TestLocalFunctionReturnsNestedLocalFunction()
        => TestNode<LocalFunctionStatementSyntax>("""
            class C
            {
                void M()
                {
                    void MethodLocal()
                    {
                        void [|NestedLocal|]() { }
                    }
                }
            }
            """, """
            Name="NestedLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestPropertyReturnsLocalFunctions()
        => TestNode<PropertyDeclarationSyntax>("""
            class C
            {
                public P Prop
                {
                    get
                    {
                        void [|GetLocal|]() { }
                        return default;
                    }
                    set
                    {
                        void [|SetLocal|]() { }
                    }
                }
            }
            """, """
            Name="GetLocal() : void" Glyph=MethodPrivate HasItems=False
            Name="SetLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestConstructorReturnsLocalFunction()
        => TestNode<ConstructorDeclarationSyntax>("""
            class C
            {
                internal C()
                {
                    void [|CtorLocal|]() { }
                }
            }
            """, """
            Name="CtorLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestDestructorReturnsLocalFunction()
        => TestNode<DestructorDeclarationSyntax>("""
            class C
            {
                ~C()
                {
                    void [|DtorLocal|]() { }
                }
            }
            """, """
            Name="DtorLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestMethodReturnsLocalFunction()
        => TestNode<MethodDeclarationSyntax>("""
            class C
            {
                void M<T>(int a)
                {
                    void [|MethodLocal|]()
                    {
                    }
                }
            }
            """, """
            Name="MethodLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestOperatorReturnsLocalFunction()
        => TestNode<OperatorDeclarationSyntax>("""
            class C
            {
                public static C operator +(C c1, int a)
                {
                    void [|OperatorLocal|]() { }
                    return default;
                }
            }
            """, """
            Name="OperatorLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestConversionOperatorReturnsLocalFunction()
        => TestNode<ConversionOperatorDeclarationSyntax>("""
            class C
            {
                internal static implicit operator int(C c1)
                {
                    void [|ConversionLocal|]() { }
                    return default;
                }
            }
            """, """
            Name="ConversionLocal() : void" Glyph=MethodPrivate HasItems=False
            """);

    [Fact]
    public Task TestLocalFunctionWithParameters()
        => TestNode<MethodDeclarationSyntax>("""
            class C
            {
                void M<T>(int a)
                {
                    int [|MethodLocal|](string input)
                    {
                    }
                }
            }
            """, """
            Name="MethodLocal(string) : int" Glyph=MethodPrivate HasItems=False
            """);
}
