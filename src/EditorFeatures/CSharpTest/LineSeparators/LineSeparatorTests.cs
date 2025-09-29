// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.LineSeparators;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.LineSeparators;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.LineSeparators)]
public sealed class LineSeparatorTests
{
    [Fact]
    public async Task TestEmptyFile()
        => await AssertTagsOnBracesOrSemicolonsAsync(contents: string.Empty);

    [Fact]
    public Task TestEmptyClass()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
            }
            """, 0);

    [Fact]
    public Task TestClassWithOneMethod()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                void M()
                {
                }
            }
            """, 1);

    [Fact]
    public Task TestClassWithTwoMethods()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                void M()
                {
                }

                void N()
                {
                }
            }
            """, 0, 2);

    [Fact]
    public Task TestClassWithTwoNonEmptyMethods()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                void M()
                {
                    N();
                }

                void N()
                {
                    M();
                }
            }
            """, 1, 4);

    [Fact]
    public Task TestClassWithMethodAndField()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                void M()
                {
                }

                int field;
            }
            """, 0, 2);

    [Fact]
    public Task TestEmptyNamespace()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace N
            {
            }
            """, 0);

    [Fact]
    public Task TestNamespaceAndClass()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace N
            {
                class C
                {
                }
            }
            """, 1);

    [Fact]
    public Task TestNamespaceAndTwoClasses()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace N
            {
                class C
                {
                }

                class D
                {
                }
            }
            """, 0, 2);

    [Fact]
    public Task TestNamespaceAndTwoClassesAndDelegate()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace N
            {
                class C
                {
                }

                class D
                {
                }

                delegate void Del();
            }
            """, 0, 1, 3);

    [Fact]
    public Task TestNestedClass()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                class N
                {
                }
            }
            """, 1);

    [Fact]
    public Task TestTwoNestedClasses()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                class N
                {
                }

                class N2
                {
                }
            }
            """, 0, 2);

    [Fact]
    public Task TestStruct()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            struct S
            {
            }
            """, 0);

    [Fact]
    public Task TestInterface()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            interface I
            {
            }
            """, 0);

    [Fact]
    public Task TestEnum()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            enum E
            {
            }
            """, 0);

    [Fact]
    public Task TestProperty()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                int Prop
                {
                    get
                    {
                        return 0;
                    }
                    set
                    {
                    }
                }
            }
            """, 4);

    [Fact]
    public Task TestPropertyAndField()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                int Prop
                {
                    get
                    {
                        return 0;
                    }
                    set
                    {
                    }
                }

                int field;
            }
            """, 3, 5);

    [Fact]
    public Task TestClassWithFieldAndMethod()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                int field;

                void M()
                {
                }
            }
            """, 0, 2);

    [Fact]
    public Task UsingDirective()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            using System;

            class C
            {
            }
            """, 0, 1);

    [Fact]
    public Task UsingDirectiveInNamespace()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace N
            {
                using System;

                class C
                {
                }
            }
            """, 0, 2);

    [Fact]
    public Task UsingDirectiveInFileScopedNamespace()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace N;

            using System;

            class C
            {
            }
            """, 1);

    [Fact]
    public Task PropertyStyleEventDeclaration()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                event EventHandler E
                {
                    add { }
                    remove { }
                }

                int i;
            }
            """, 2, 4);

    [Fact]
    public Task IndexerDeclaration()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                int this[int i]
                {
                    get { return i; }
                    set { }
                }

                int i;
            }
            """, 3, 5);

    [Fact]
    public Task Constructor()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                C()
                {
                }

                int i;
            }
            """, 0, 2);

    [Fact]
    public Task Destructor()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                ~C()
                {
                }

                int i;
            }
            """, 0, 2);

    [Fact]
    public Task Operator()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                static C operator +(C lhs, C rhs)
                {
                }

                int i;
            }
            """, 0, 2);

    [Fact]
    public Task ConversionOperator()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                static implicit operator C(int i)
                {
                }

                int i;
            }
            """, 0, 2);

    [Fact]
    public Task Bug930292()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class Program
            {
            void A() { }
            void B() { }
            void C() { }
            void D() { }
            }
            """, 4);

    [Fact]
    public Task Bug930289()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            namespace Roslyn.Compilers.CSharp
            {
            internal struct ArrayElement<T>
            {
            internal T Value;
            internal ArrayElement(T value) { this.Value = value; }
            public static implicit operator ArrayElement<T>(T value) { return new ArrayElement<T>(value); }
            }
            }
            """, 6);

    [Fact]
    public Task TestConsoleApp()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """, 2, 4);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1297")]
    public Task ExpressionBodiedProperty()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                int Prop => 3;

                void M()
                {
                }
            }
            """, 0, 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1297")]
    public Task ExpressionBodiedIndexer()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                int this[int i] => 3;

                void M()
                {
                }
            }
            """, 0, 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1297")]
    public Task ExpressionBodiedEvent()
        => AssertTagsOnBracesOrSemicolonsAsync("""
            class C
            {
                event EventHandler MyEvent => 3;

                void M()
                {
                }
            }
            """, 3);

    #region Negative (incomplete) tests

    [Fact]
    public async Task IncompleteClass()
    {
        await AssertTagsOnBracesOrSemicolonsAsync(@"class C");
        await AssertTagsOnBracesOrSemicolonsAsync(@"class C {");
    }

    [Fact]
    public async Task IncompleteEnum()
    {
        await AssertTagsOnBracesOrSemicolonsAsync(@"enum E");
        await AssertTagsOnBracesOrSemicolonsAsync(@"enum E {");
    }

    [Fact]
    public async Task IncompleteMethod()
        => await AssertTagsOnBracesOrSemicolonsAsync(@"void goo() {");

    [Fact]
    public async Task IncompleteProperty()
        => await AssertTagsOnBracesOrSemicolonsAsync(@"class C { int P { get; set; void");

    [Fact]
    public async Task IncompleteEvent()
    {
        await AssertTagsOnBracesOrSemicolonsAsync(@"public event EventHandler");
        await AssertTagsOnBracesOrSemicolonsAsync(@"public event EventHandler {");
    }

    [Fact]
    public async Task IncompleteIndexer()
    {
        await AssertTagsOnBracesOrSemicolonsAsync(@"int this[int i]");
        await AssertTagsOnBracesOrSemicolonsAsync(@"int this[int i] {");
    }

    [Fact]
    public Task IncompleteOperator()
        => AssertTagsOnBracesOrSemicolonsTokensAsync(@"C operator +(C lhs, C rhs) {", [], Options.Regular);

    [Fact]
    public async Task IncompleteConversionOperator()
        => await AssertTagsOnBracesOrSemicolonsAsync(@"implicit operator C(int i) {");

    [Fact]
    public async Task IncompleteMember()
        => await AssertTagsOnBracesOrSemicolonsAsync(@"class C { private !C(");

    #endregion

    private static async Task AssertTagsOnBracesOrSemicolonsAsync(string contents, params int[] tokenIndices)
    {
        await AssertTagsOnBracesOrSemicolonsTokensAsync(contents, tokenIndices);
        await AssertTagsOnBracesOrSemicolonsTokensAsync(contents, tokenIndices, Options.Script);
    }

    private static async Task AssertTagsOnBracesOrSemicolonsTokensAsync(string contents, int[] tokenIndices, CSharpParseOptions? options = null)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(contents, options);
        var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
        var root = await document.GetRequiredSyntaxRootAsync(default);

        var lineSeparatorService = Assert.IsType<CSharpLineSeparatorService>(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<ILineSeparatorService>());
        var spans = await lineSeparatorService.GetLineSeparatorsAsync(document, root.FullSpan, CancellationToken.None);
        var tokens = root.DescendantTokens().Where(t => t.Kind() is SyntaxKind.CloseBraceToken or SyntaxKind.SemicolonToken);

        Assert.Equal(tokenIndices.Length, spans.Length);

        var i = 0;
        foreach (var span in spans.OrderBy(t => t.Start))
        {
            var expectedToken = tokens.ElementAt(tokenIndices[i]);

            var expectedSpan = expectedToken.Span;

            var message = string.Format("Expected to match curly {0} at span {1}.  Actual span {2}",
                                        tokenIndices[i],
                                        expectedSpan,
                                        span);
            Assert.True(expectedSpan == span, message);
            ++i;
        }
    }
}
