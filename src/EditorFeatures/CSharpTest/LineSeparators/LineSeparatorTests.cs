// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.LineSeparator;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.LineSeparators
{
    public class LineSeparatorTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestEmptyFile()
        {
            AssertTagsOnBracesOrSemicolons(contents: string.Empty);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestEmptyClass()
        {
            var file = @"class C
{
}";
            AssertTagsOnBracesOrSemicolons(file, 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestClassWithOneMethod()
        {
            var file = @"class C
{
    void M()
    {
    }
}";
            AssertTagsOnBracesOrSemicolons(file, 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestClassWithTwoMethods()
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
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestClassWithTwoNonEmptyMethods()
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
            AssertTagsOnBracesOrSemicolons(file, 1, 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestClassWithMethodAndField()
        {
            var file = @"class C
{
    void M()
    {
    }

    int field;
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestEmptyNamespace()
        {
            var file = @"namespace N
{
}";
            AssertTagsOnBracesOrSemicolons(file, 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestNamespaceAndClass()
        {
            var file = @"namespace N
{
    class C
    {
    }
}";
            AssertTagsOnBracesOrSemicolons(file, 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestNamespaceAndTwoClasses()
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
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestNamespaceAndTwoClassesAndDelegate()
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
            AssertTagsOnBracesOrSemicolons(file, 0, 1, 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestNestedClass()
        {
            var file = @"class C
{
    class N
    {
    }
}";
            AssertTagsOnBracesOrSemicolons(file, 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestTwoNestedClasses()
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
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestStruct()
        {
            var file = @"struct S
{
}";
            AssertTagsOnBracesOrSemicolons(file, 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestInterface()
        {
            var file = @"interface I
{
}";
            AssertTagsOnBracesOrSemicolons(file, 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestEnum()
        {
            var file = @"enum E
{
}";
            AssertTagsOnBracesOrSemicolons(file, 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestProperty()
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
            AssertTagsOnBracesOrSemicolons(file, 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestPropertyAndField()
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
            AssertTagsOnBracesOrSemicolons(file, 3, 5);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestClassWithFieldAndMethod()
        {
            var file = @"class C
{
    int field;

    void M()
    {
    }
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void UsingDirective()
        {
            var file = @"using System;

class C
{
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void UsingDirectiveInNamespace()
        {
            var file = @"namespace N
{
    using System;

    class C
    {
    }
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void PropertyStyleEventDeclaration()
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
            AssertTagsOnBracesOrSemicolons(file, 2, 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IndexerDeclaration()
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
            AssertTagsOnBracesOrSemicolons(file, 3, 5);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void Constructor()
        {
            var file = @"class C
{
    C()
    {
    }

    int i;
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void Destructor()
        {
            var file = @"class C
{
    ~C()
    {
    }

    int i;
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void Operator()
        {
            var file = @"class C
{
    static C operator +(C lhs, C rhs)
    {
    }

    int i;
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void ConversionOperator()
        {
            var file = @"class C
{
    static implicit operator C(int i)
    {
    }

    int i;
}";
            AssertTagsOnBracesOrSemicolons(file, 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void Bug930292()
        {
            var file = @"class Program
{
void A() { }
void B() { }
void C() { }
void D() { }
}
";
            AssertTagsOnBracesOrSemicolons(file, 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void Bug930289()
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
            AssertTagsOnBracesOrSemicolons(file, 6);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void TestConsoleApp()
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
            AssertTagsOnBracesOrSemicolons(file, 2, 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        [WorkItem(1297, "https://github.com/dotnet/roslyn/issues/1297")]
        public void ExpressionBodiedProperty()
        {
            AssertTagsOnBracesOrSemicolons(@"class C
{
    int Prop => 3;

    void M()
    {
    }
}", 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        [WorkItem(1297, "https://github.com/dotnet/roslyn/issues/1297")]
        public void ExpressionBodiedIndexer()
        {
            AssertTagsOnBracesOrSemicolons(@"class C
{
    int this[int i] => 3;

    void M()
    {
    }
}", 0, 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        [WorkItem(1297, "https://github.com/dotnet/roslyn/issues/1297")]
        public void ExpressionBodiedEvent()
        {
            // This is not valid code, and parses all wrong, but just in case a user writes it.  Note
            // the 3 is because there is a skipped } in the event declaration.
            AssertTagsOnBracesOrSemicolons(@"class C
{
    event EventHandler MyEvent => 3;

    void M()
    {
    }
}", 3);
        }

        #region Negative (incomplete) tests

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteClass()
        {
            AssertTagsOnBracesOrSemicolons(@"class C");
            AssertTagsOnBracesOrSemicolons(@"class C {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteEnum()
        {
            AssertTagsOnBracesOrSemicolons(@"enum E");
            AssertTagsOnBracesOrSemicolons(@"enum E {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteMethod()
        {
            AssertTagsOnBracesOrSemicolons(@"void foo() {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteProperty()
        {
            AssertTagsOnBracesOrSemicolons(@"class C { int P { get; set; void");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteEvent()
        {
            AssertTagsOnBracesOrSemicolons(@"public event EventHandler");
            AssertTagsOnBracesOrSemicolons(@"public event EventHandler {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteIndexer()
        {
            AssertTagsOnBracesOrSemicolons(@"int this[int i]");
            AssertTagsOnBracesOrSemicolons(@"int this[int i] {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteOperator()
        {
            // top level operators not supported in script code
            AssertTagsOnBracesOrSemicolonsTokens(@"C operator +(C lhs, C rhs) {", Array.Empty<int>(), Options.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteConversionOperator()
        {
            AssertTagsOnBracesOrSemicolons(@"implicit operator C(int i) {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.LineSeparators)]
        public void IncompleteMember()
        {
            AssertTagsOnBracesOrSemicolons(@"class C { private !C(");
        }

        #endregion

        private void AssertTagsOnBracesOrSemicolons(string contents, params int[] tokenIndices)
        {
            AssertTagsOnBracesOrSemicolonsTokens(contents, tokenIndices);
            AssertTagsOnBracesOrSemicolonsTokens(contents, tokenIndices, Options.Script);
        }

        private void AssertTagsOnBracesOrSemicolonsTokens(string contents, int[] tokenIndices, CSharpParseOptions options = null)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(contents, options))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var spans = new CSharpLineSeparatorService().GetLineSeparatorsAsync(document, document.GetSyntaxTreeAsync().Result.GetRoot().FullSpan, CancellationToken.None).Result;
                var tokens = document.GetSyntaxRootAsync(CancellationToken.None).Result.DescendantTokens().Where(t => t.Kind() == SyntaxKind.CloseBraceToken || t.Kind() == SyntaxKind.SemicolonToken);

                Assert.Equal(tokenIndices.Length, spans.Count());

                int i = 0;
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
