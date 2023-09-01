// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.LineSeparators;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LineSeparators;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.LineSeparators
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.LineSeparators)]
    public class LineSeparatorTests
    {
        [Fact]
        public async Task TestEmptyFile()
            => await AssertTagsOnBracesOrSemicolonsAsync(contents: string.Empty);

        [Fact]
        public async Task TestEmptyClass()
        {
            var file = """
                class C
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact]
        public async Task TestClassWithOneMethod()
        {
            var file = """
                class C
                {
                    void M()
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact]
        public async Task TestClassWithTwoMethods()
        {
            var file = """
                class C
                {
                    void M()
                    {
                    }

                    void N()
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task TestClassWithTwoNonEmptyMethods()
        {
            var file = """
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
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1, 4);
        }

        [Fact]
        public async Task TestClassWithMethodAndField()
        {
            var file = """
                class C
                {
                    void M()
                    {
                    }

                    int field;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task TestEmptyNamespace()
        {
            var file = """
                namespace N
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact]
        public async Task TestNamespaceAndClass()
        {
            var file = """
                namespace N
                {
                    class C
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact]
        public async Task TestNamespaceAndTwoClasses()
        {
            var file = """
                namespace N
                {
                    class C
                    {
                    }

                    class D
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task TestNamespaceAndTwoClassesAndDelegate()
        {
            var file = """
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
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 1, 3);
        }

        [Fact]
        public async Task TestNestedClass()
        {
            var file = """
                class C
                {
                    class N
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact]
        public async Task TestTwoNestedClasses()
        {
            var file = """
                class C
                {
                    class N
                    {
                    }

                    class N2
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task TestStruct()
        {
            var file = """
                struct S
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact]
        public async Task TestInterface()
        {
            var file = """
                interface I
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact]
        public async Task TestEnum()
        {
            var file = """
                enum E
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact]
        public async Task TestProperty()
        {
            var file = """
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
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 4);
        }

        [Fact]
        public async Task TestPropertyAndField()
        {
            var file = """
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
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 3, 5);
        }

        [Fact]
        public async Task TestClassWithFieldAndMethod()
        {
            var file = """
                class C
                {
                    int field;

                    void M()
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task UsingDirective()
        {
            var file = """
                using System;

                class C
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 1);
        }

        [Fact]
        public async Task UsingDirectiveInNamespace()
        {
            var file = """
                namespace N
                {
                    using System;

                    class C
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task UsingDirectiveInFileScopedNamespace()
        {
            var file = """
                namespace N;

                using System;

                class C
                {
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact]
        public async Task PropertyStyleEventDeclaration()
        {
            var file = """
                class C
                {
                    event EventHandler E
                    {
                        add { }
                        remove { }
                    }

                    int i;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 2, 4);
        }

        [Fact]
        public async Task IndexerDeclaration()
        {
            var file = """
                class C
                {
                    int this[int i]
                    {
                        get { return i; }
                        set { }
                    }

                    int i;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 3, 5);
        }

        [Fact]
        public async Task Constructor()
        {
            var file = """
                class C
                {
                    C()
                    {
                    }

                    int i;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task Destructor()
        {
            var file = """
                class C
                {
                    ~C()
                    {
                    }

                    int i;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task Operator()
        {
            var file = """
                class C
                {
                    static C operator +(C lhs, C rhs)
                    {
                    }

                    int i;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task ConversionOperator()
        {
            var file = """
                class C
                {
                    static implicit operator C(int i)
                    {
                    }

                    int i;
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact]
        public async Task Bug930292()
        {
            var file = """
                class Program
                {
                void A() { }
                void B() { }
                void C() { }
                void D() { }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 4);
        }

        [Fact]
        public async Task Bug930289()
        {
            var file = """
                namespace Roslyn.Compilers.CSharp
                {
                internal struct ArrayElement<T>
                {
                internal T Value;
                internal ArrayElement(T value) { this.Value = value; }
                public static implicit operator ArrayElement<T>(T value) { return new ArrayElement<T>(value); }
                }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 6);
        }

        [Fact]
        public async Task TestConsoleApp()
        {
            var file = """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
                """;
            await AssertTagsOnBracesOrSemicolonsAsync(file, 2, 4);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1297")]
        public async Task ExpressionBodiedProperty()
        {
            await AssertTagsOnBracesOrSemicolonsAsync("""
                class C
                {
                    int Prop => 3;

                    void M()
                    {
                    }
                }
                """, 0, 2);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1297")]
        public async Task ExpressionBodiedIndexer()
        {
            await AssertTagsOnBracesOrSemicolonsAsync("""
                class C
                {
                    int this[int i] => 3;

                    void M()
                    {
                    }
                }
                """, 0, 2);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1297")]
        public async Task ExpressionBodiedEvent()
        {
            // This is not valid code, and parses all wrong, but just in case a user writes it.  Note
            // the 3 is because there is a skipped } in the event declaration.
            await AssertTagsOnBracesOrSemicolonsAsync("""
                class C
                {
                    event EventHandler MyEvent => 3;

                    void M()
                    {
                    }
                }
                """, 3);
        }

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
        public async Task IncompleteOperator()
        {
            // top level operators not supported in script code
            await AssertTagsOnBracesOrSemicolonsTokensAsync(@"C operator +(C lhs, C rhs) {", Array.Empty<int>(), Options.Regular);
        }

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
            using var workspace = TestWorkspace.CreateCSharp(contents, options);
            var document = workspace.CurrentSolution.GetRequiredDocument(workspace.Documents.First().Id);
            var root = await document.GetRequiredSyntaxRootAsync(default);

            var lineSeparatorService = Assert.IsType<CSharpLineSeparatorService>(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService<ILineSeparatorService>());
            var spans = await lineSeparatorService.GetLineSeparatorsAsync(document, root.FullSpan, CancellationToken.None);
            var tokens = root.DescendantTokens().Where(t => t.Kind() is SyntaxKind.CloseBraceToken or SyntaxKind.SemicolonToken);

            Assert.Equal(tokenIndices.Length, spans.Count());

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
}
