// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.LineSeparator;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.LineSeparators
{
    [UseExportProvider]
    public class LineSeparatorTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestEmptyFile()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(contents: string.Empty);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestEmptyClass()
        {
            var file = @"class C
{
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestClassWithOneMethod()
        {
            var file = @"class C
{
    void M()
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestClassWithTwoMethods()
        {
            var file = @"class C
{
    void M()
    {
    }

    void N()
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestClassWithTwoNonEmptyMethods()
        {
            var file = @"class C
{
    void M()
    {
        N();
    }

    void N()
    {
        M();
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1, 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestClassWithMethodAndField()
        {
            var file = @"class C
{
    void M()
    {
    }

    int field;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestEmptyNamespace()
        {
            var file = @"namespace N
{
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestNamespaceAndClass()
        {
            var file = @"namespace N
{
    class C
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestNamespaceAndTwoClasses()
        {
            var file = @"namespace N
{
    class C
    {
    }

    class D
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestNamespaceAndTwoClassesAndDelegate()
        {
            var file = @"namespace N
{
    class C
    {
    }

    class D
    {
    }

    delegate void Del();
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 1, 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestNestedClass()
        {
            var file = @"class C
{
    class N
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestTwoNestedClasses()
        {
            var file = @"class C
{
    class N
    {
    }

    class N2
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestStruct()
        {
            var file = @"struct S
{
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestInterface()
        {
            var file = @"interface I
{
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestEnum()
        {
            var file = @"enum E
{
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestProperty()
        {
            var file = @"class C
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
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestPropertyAndField()
        {
            var file = @"class C
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
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 3, 5);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestClassWithFieldAndMethod()
        {
            var file = @"class C
{
    int field;

    void M()
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task UsingDirective()
        {
            var file = @"using System;

class C
{
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task UsingDirectiveInNamespace()
        {
            var file = @"namespace N
{
    using System;

    class C
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task PropertyStyleEventDeclaration()
        {
            var file = @"class C
{
    event EventHandler E
    {
        add { }
        remove { }
    }

    int i;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 2, 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IndexerDeclaration()
        {
            var file = @"class C
{
    int this[int i]
    {
        get { return i; }
        set { }
    }

    int i;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 3, 5);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task Constructor()
        {
            var file = @"class C
{
    C()
    {
    }

    int i;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task Destructor()
        {
            var file = @"class C
{
    ~C()
    {
    }

    int i;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task Operator()
        {
            var file = @"class C
{
    static C operator +(C lhs, C rhs)
    {
    }

    int i;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task ConversionOperator()
        {
            var file = @"class C
{
    static implicit operator C(int i)
    {
    }

    int i;
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task Bug930292()
        {
            var file = @"class Program
{
void A() { }
void B() { }
void C() { }
void D() { }
}
";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task Bug930289()
        {
            var file = @"namespace Roslyn.Compilers.CSharp
{
internal struct ArrayElement<T>
{
internal T Value;
internal ArrayElement(T value) { this.Value = value; }
public static implicit operator ArrayElement<T>(T value) { return new ArrayElement<T>(value); }
}
}
";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 6);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task TestConsoleApp()
        {
            var file = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
    }
}";
            await AssertTagsOnBracesOrSemicolonsAsync(file, 2, 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        [WorkItem(1297, "https://github.com/dotnet/roslyn/issues/1297")]
        public async Task ExpressionBodiedProperty()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C
{
    int Prop => 3;

    void M()
    {
    }
}", 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        [WorkItem(1297, "https://github.com/dotnet/roslyn/issues/1297")]
        public async Task ExpressionBodiedIndexer()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C
{
    int this[int i] => 3;

    void M()
    {
    }
}", 0, 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        [WorkItem(1297, "https://github.com/dotnet/roslyn/issues/1297")]
        public async Task ExpressionBodiedEvent()
        {
            // This is not valid code, and parses all wrong, but just in case a user writes it.  Note
            // the 3 is because there is a skipped } in the event declaration.
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C
{
    event EventHandler MyEvent => 3;

    void M()
    {
    }
}", 3);
        }

        #region Negative (incomplete) tests

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteClass()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C");
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteEnum()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"enum E");
            await AssertTagsOnBracesOrSemicolonsAsync(@"enum E {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteMethod()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"void goo() {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteProperty()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C { int P { get; set; void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteEvent()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"public event EventHandler");
            await AssertTagsOnBracesOrSemicolonsAsync(@"public event EventHandler {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteIndexer()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"int this[int i]");
            await AssertTagsOnBracesOrSemicolonsAsync(@"int this[int i] {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteOperator()
        {
            // top level operators not supported in script code
            await AssertTagsOnBracesOrSemicolonsTokensAsync(@"C operator +(C lhs, C rhs) {", Array.Empty<int>(), Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteConversionOperator()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"implicit operator C(int i) {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public async Task IncompleteMember()
        {
            await AssertTagsOnBracesOrSemicolonsAsync(@"class C { private !C(");
        }

        #endregion

        private async Task AssertTagsOnBracesOrSemicolonsAsync(string contents, params int[] tokenIndices)
        {
            await AssertTagsOnBracesOrSemicolonsTokensAsync(contents, tokenIndices);
            await AssertTagsOnBracesOrSemicolonsTokensAsync(contents, tokenIndices, Options.Script);
        }

        private async Task AssertTagsOnBracesOrSemicolonsTokensAsync(string contents, int[] tokenIndices, CSharpParseOptions options = null)
        {
            using (var workspace = TestWorkspace.CreateCSharp(contents, options))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var spans = await new CSharpLineSeparatorService().GetLineSeparatorsAsync(document, (await document.GetSyntaxRootAsync()).FullSpan, CancellationToken.None);
                var tokens = (await document.GetSyntaxRootAsync(CancellationToken.None)).DescendantTokens().Where(t => t.Kind() == SyntaxKind.CloseBraceToken || t.Kind() == SyntaxKind.SemicolonToken);

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

        private static SyntaxToken GetOpenBrace(SyntaxTree syntaxTree, SyntaxToken token)
        {
            return token.Parent.ChildTokens().Where(n => n.Kind() == SyntaxKind.OpenBraceToken).Single();
        }
    }
}
