// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseImplicitObjectCreationTests;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseImplicitObjectCreationDiagnosticAnalyzer,
    CSharpUseImplicitObjectCreationCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseImplicitObjectCreation)]
public sealed class UseImplicitObjectCreationTests
{
    [Fact]
    public Task TestMissingBeforeCSharp9()
        => new VerifyCS.Test
        {
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp8,
            TestCode = """
                class C
                {
                    C c = new C();
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestAfterCSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C c = [|new|] C();
                }
                """,
            FixedCode = """
                class C
                {
                    C c = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithObjectInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C c = [|new|] C() { };
                }
                """,
            FixedCode = """
                class C
                {
                    C c = new() { };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithObjectInitializerWithoutArguments()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C c = [|new|] C { };
                }
                """,
            FixedCode = """
                class C
                {
                    C c = new() { };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithTriviaAfterNew()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C c = [|new|] /*x*/ C();
                }
                """,
            FixedCode = """
                class C
                {
                    C c = new /*x*/ ();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotWithDifferentTypes()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object c = new C();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotWithErrorTypes()
        => new VerifyCS.Test
        {
            TestState = {
                Sources =
                {
                    """
                        class C
                        {
                            {|#0:E|} c = new {|#1:E|}();
                        }
                        """
                },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(4,5): error CS0246: The type or namespace name 'E' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("E"),
                    // /0/Test0.cs(4,15): error CS0246: The type or namespace name 'E' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithLocation(1).WithArguments("E"),
                }
            },
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotWithDynamic()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    dynamic c = new C();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotWithArrayTypes()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] c = new int[0];
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithTypeParameter()
        => new VerifyCS.Test
        {
            TestCode = """
                class C<T> where T : new()
                {
                    T t = [|new|] T();
                }
                """,
            FixedCode = """
                class C<T> where T : new()
                {
                    T t = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithLocalWhenUserDoesNotPreferVar()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        C c = [|new|] C();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        C c = new();
                    }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.FalseWithSuggestionEnforcement },
            }
        }.RunAsync();

    [Fact]
    public Task TestNotWithLocalWhenUserDoesPreferVar()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        C c = new C();
                    }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSuggestionEnforcement },
            }
        }.RunAsync();

    [Fact]
    public Task TestWithForVariable()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        for (C c = [|new|] C();;)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        for (C c = new();;)
                        {
                        }
                    }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithLocalFunctionExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        C Func() => [|new|] C();
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        C Func() => new();
                    }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithMethodExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C Func() => [|new|] C();
                }
                """,
            FixedCode = """
                class C
                {
                    C Func() => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithConversionExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public static implicit operator C(int i) => [|new|] C();
                }
                """,
            FixedCode = """
                class C
                {
                    public static implicit operator C(int i) => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithOperatorExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public static C operator +(C c1, C c2) => [|new|] C();
                }
                """,
            FixedCode = """
                class C
                {
                    public static C operator +(C c1, C c2) => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithPropertyExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C P => [|new|] C();
                }
                """,
            FixedCode = """
                class C
                {
                    C P => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithPropertyAccessorExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C P { get => [|new|] C(); }
                }
                """,
            FixedCode = """
                class C
                {
                    C P { get => new(); }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotWithPropertySetAccessorExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C P { set => new C(); }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithIndexerExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C this[int i] => [|new|] C();
                }
                """,
            FixedCode = """
                class C
                {
                    C this[int i] => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestWithIndexerAccessorExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C this[int i] { get => [|new|] C(); }
                }
                """,
            FixedCode = """
                class C
                {
                    C this[int i] { get => new(); }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotWithMethodBlockBody()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    C Func() { return new C(); }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotInNonApparentCode1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void X() => Bar(new C());
                    void Bar(C c) { }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestNotInNonApparentCode2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void X()
                    {
                        C c;
                        c = new C();
                    }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestQualifiedUnqualified1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    List<int> list = [|new|] System.Collections.Generic.List<int>();
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                class C
                {
                    List<int> list = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestQualifiedUnqualified2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    System.Collections.Generic.List<int> list = [|new|] List<int>();
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                class C
                {
                    System.Collections.Generic.List<int> list = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestAlias()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                using X = System.Collections.Generic.List<int>;
                class C
                {
                    System.Collections.Generic.List<int> list = [|new|] X();
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                using X = System.Collections.Generic.List<int>;
                class C
                {
                    System.Collections.Generic.List<int> list = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestFixAll1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    public C() { }
                    public C(Action action) { }

                    C c1 = [|new|] C(() =>
                    {
                        C c2 = [|new|] C();
                    });
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    public C() { }
                    public C(Action action) { }

                    C c1 = new(() =>
                    {
                        C c2 = new();
                    });
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49291")]
    public Task TestListOfTuplesWithLabels()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    List<(int SomeName, int SomeOtherName, int YetAnotherName)> list = [|new|] List<(int SomeName, int SomeOtherName, int YetAnotherName)>();
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                class C
                {
                    List<(int SomeName, int SomeOtherName, int YetAnotherName)> list = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49291")]
    public Task TestListOfTuplesWithoutLabels()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    List<(int SomeName, int SomeOtherName, int YetAnotherName)> list = [|new|] List<(int, int, int)>();
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                class C
                {
                    List<(int SomeName, int SomeOtherName, int YetAnotherName)> list = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49291")]
    public Task TestListOfTuplesWithoutLabelsAsLocal()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    void M()
                    {
                        List<(int SomeName, int SomeOtherName, int YetAnotherName)> list = [|new|] List<(int, int, int)>();
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                class C
                {
                    void M()
                    {
                        List<(int SomeName, int SomeOtherName, int YetAnotherName)> list = new();
                    }
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57777")]
    public Task TestMissingOnNullableStruct()
        => new VerifyCS.Test
        {
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            TestCode = """
                class C
                {
                    int? i = new int?();
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57777")]
    public Task TestOnNullableReferenceType()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable
                class C
                {
                    C? c = [|new|] C();
                }
                """,
            FixedCode = """
                #nullable enable
                class C
                {
                    C? c = new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestOnSingleDimensionalCollectionConstruction1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    IList<C> list = new List<C> { [|new|] C() };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    IList<C> list = new List<C> { new() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestOnSingleDimensionalCollectionConstruction2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    IList<C> list = new List<C>() { [|new|] C() };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    IList<C> list = new List<C>() { new() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestOnSingleDimensionalCollectionConstruction3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    List<C> list = [|new|] List<C>() { [|new|] C() };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    List<C> list = new() { new C() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestNotOnSingleDimensionalImplicitCollectionConstruction2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    List<C> list = new() { new C() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestNotOnMultiDimensionalImplicitCollectionConstruction2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    IDictionary<C, C> d = new Dictionary<C, C>() { { new C(), new C() } };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestOnSingleDimensionalArrayConstruction1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    C[] list = new C[] { [|new|] C() };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    C[] list = new C[] { new() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestOnSingleDimensionalArrayConstruction2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    C[] list = { [|new|] C() };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    C[] list = { new() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestNotOnSingleDimensionalImplicitArrayConstruction2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    C[] list = new[] { new C() };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestNotOnTwoDimensionalArrayConstruction1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    C[,] list = new C[,] { { new C(), new C() } };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57789")]
    public Task TestOnInnerJaggedArray1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    C[][] list = new C[][] { new C[] { [|new|] C() } };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    C[][] list = new C[][] { new C[] { new() } };
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77670")]
    public Task TestWithTask1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Threading.Tasks;

                class C
                {
                    async Task<C> Func() => [|new|] C();
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;
                
                class C
                {
                    async Task<C> Func() => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77670")]
    public Task TestWithValueTask1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Threading.Tasks;

                class C
                {
                    async ValueTask<C> Func() => [|new|] C();
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;
                
                class C
                {
                    async ValueTask<C> Func() => new();
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77670")]
    public Task TestWithValueTask2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Threading.Tasks;

                class C
                {
                    ValueTask<C> Func() => [|new|] ValueTask<C>(new C());
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;
                
                class C
                {
                    ValueTask<C> Func() => new(new C());
                }
                """,
            LanguageVersion = CodeAnalysis.CSharp.LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();
}
