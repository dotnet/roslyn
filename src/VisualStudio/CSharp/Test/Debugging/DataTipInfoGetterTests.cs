// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging
{
    public class DataTipInfoGetterTests
    {
        private void Test(string markup, string expectedText = null)
        {
            TestSpanGetter(markup, (document, position, expectedSpan) =>
            {
                var result = DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None).WaitAndGetResult(CancellationToken.None);

                Assert.Equal(expectedSpan, result.Span);
                Assert.Equal(expectedText, result.Text);
            });
        }

        private void TestNoDataTip(string markup)
        {
            TestSpanGetter(markup, (document, position, expectedSpan) =>
            {
                var result = DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                Assert.True(result.IsDefault);
            });
        }

        private void TestSpanGetter(string markup, Action<Document, int, TextSpan?> continuation)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(markup))
            {
                var testHostDocument = workspace.Documents.Single();
                var position = testHostDocument.CursorPosition.Value;
                var expectedSpan = testHostDocument.SelectedSpans.Any()
                    ? testHostDocument.SelectedSpans.Single()
                    : (TextSpan?)null;

                continuation(
                    workspace.CurrentSolution.Projects.First().Documents.First(),
                    position,
                    expectedSpan);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestCSharpLanguageDebugInfoGetDataTipSpanAndText()
        {
            Test("class [|C$$|] { }");
            Test("struct [|C$$|] { }");
            Test("interface [|C$$|] { }");
            Test("enum [|C$$|] { }");
            Test("delegate void [|C$$|] ();"); // Without the space, that position is actually on the open paren.
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test1()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|Sys$$tem|].Console.WriteLine(args);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test2()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|System$$.Console|].WriteLine(args);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test3()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|System.$$Console|].WriteLine(args);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test4()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|System.Con$$sole|].WriteLine(args);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test5()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|System.Console.Wri$$teLine|](args);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test6()
        {
            TestNoDataTip(
@"class C
{
  void Foo()
  {
    [|System.Console.WriteLine|]$$(args);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test7()
        {
            Test(
@"class C
{
  void Foo()
  {
    System.Console.WriteLine($$[|args|]);
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void Test8()
        {
            TestNoDataTip(
@"class C
{
  void Foo()
  {
    [|System.Console.WriteLine|](args$$);
  }
}");
        }

        [WpfFact]
        public void TestVar()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|va$$r|] v = 0;
  }
}", "int");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestVariableType()
        {
            Test(
@"class C
{
  void Foo()
  {
    [|in$$t|] i = 0;
  }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestVariableIdentifier()
        {
            Test(
@"class C
{
  void Foo()
  {
    int [|$$i|] = 0;
  }
}");
        }

        [WorkItem(539910)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestLiterals()
        {
            Test(
@"class C
{
  void Foo()
  {
    int i = [|4$$2|];
  }
}", "int");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestNonExpressions()
        {
            TestNoDataTip(
@"class C
{
  void Foo()
  {
    int i = 42;
  }$$
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestParameterIdentifier()
        {
            Test(
@"class C
{
  void Foo(int [|$$i|])
  {
  }
}");
        }

        [WorkItem(942699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestCatchIdentifier()
        {
            Test(
@"class C
{
    void Foo()
    {
        try
        {
        }
        catch (System.Exception [|$$e|])
        {
        }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestEvent()
        {
            Test(
@"class C
{
    event System.Action [|$$E|];
}");

            Test(
@"class C
{
    event System.Action [|$$E|]
    {
        add { }
        remove { }
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestMethod()
        {
            Test(
@"class C
{
    int [|$$M|]() { }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestTypeParameter()
        {
            Test("class C<T, [|$$U|], V> { }");
            Test(
@"class C
{
    void M<T, [|$$U|]>() { }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void UsingAlias()
        {
            Test(
@"using [|$$S|] = Static;

static class Static
{
}");
        }

        [WorkItem(540921)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestForEachIdentifier()
        {
            Test(
@"class C
{
  void Foo(string[] args)
  {
    foreach (string [|$$s|] in args)
    {
    }
  }
}");
        }

        [WorkItem(546328)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestProperty()
        {
            Test(
@"namespace ConsoleApplication16
{
    class C
    {
        public int [|$$foo|] { get; private set; } // hover over me
        public C()
        {
            this.foo = 1;
        }
        public int Foo()
        {
            return 2; // breakpoint here
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            new C().Foo();
        }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public void TestQueryIdentifier()
        {
            Test( // From
@"class C
{
    object Foo(string[] args)
    {
        return from [|$$a|] in args select a;
    }
}");
            Test( // Let
@"class C
{
    object Foo(string[] args)
    {
        return from a in args let [|$$b|] = ""END"" select a + b;
    }
}");
            Test( // Join
@"class C
{
    object Foo(string[] args)
    {
        return from a in args join [|$$b|] in args on a equals b;
    }
}");
            Test( // Join Into
@"class C
{
    object Foo(string[] args)
    {
        return from a in args join b in args on a equals b into [|$$c|];
    }
}");
            Test( // Continuation
@"class C
{
    object Foo(string[] args)
    {
        return from a in args select a into [|$$b|] from c in b select c;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843)]
        public void TestConditionalAccessExpression()
        {
            var sourceTemplate = @"
class A
{{
    B B;

    object M()
    {{
        return {0};
    }}
}}

class B
{{
    C C;
}}

class C
{{
    D D;
}}

class D
{{
}}
";

            // One level.
            Test(string.Format(sourceTemplate, "[|Me?.$$B|]"));

            // Two levels.
            Test(string.Format(sourceTemplate, "[|Me?.$$B|].C"));
            Test(string.Format(sourceTemplate, "[|Me?.B.$$C|]"));

            Test(string.Format(sourceTemplate, "[|Me.$$B|]?.C"));
            Test(string.Format(sourceTemplate, "[|Me.B?.$$C|]"));

            Test(string.Format(sourceTemplate, "[|Me?.$$B|]?.C"));
            Test(string.Format(sourceTemplate, "[|Me?.B?.$$C|]"));

            // Three levels.
            Test(string.Format(sourceTemplate, "[|Me?.$$B|].C.D"));
            Test(string.Format(sourceTemplate, "[|Me?.B.$$C|].D"));
            Test(string.Format(sourceTemplate, "[|Me?.B.C.$$D|]"));

            Test(string.Format(sourceTemplate, "[|Me.$$B|]?.C.D"));
            Test(string.Format(sourceTemplate, "[|Me.B?.$$C|].D"));
            Test(string.Format(sourceTemplate, "[|Me.B?.C.$$D|]"));

            Test(string.Format(sourceTemplate, "[|Me.$$B|].C?.D"));
            Test(string.Format(sourceTemplate, "[|Me.B.$$C|]?.D"));
            Test(string.Format(sourceTemplate, "[|Me.B.C?.$$D|]"));

            Test(string.Format(sourceTemplate, "[|Me?.$$B|]?.C.D"));
            Test(string.Format(sourceTemplate, "[|Me?.B?.$$C|].D"));
            Test(string.Format(sourceTemplate, "[|Me?.B?.C.$$D|]"));

            Test(string.Format(sourceTemplate, "[|Me?.$$B|].C?.D"));
            Test(string.Format(sourceTemplate, "[|Me?.B.$$C|]?.D"));
            Test(string.Format(sourceTemplate, "[|Me?.B.C?.$$D|]"));

            Test(string.Format(sourceTemplate, "[|Me.$$B|]?.C?.D"));
            Test(string.Format(sourceTemplate, "[|Me.B?.$$C|]?.D"));
            Test(string.Format(sourceTemplate, "[|Me.B?.C?.$$D|]"));

            Test(string.Format(sourceTemplate, "[|Me?.$$B|]?.C?.D"));
            Test(string.Format(sourceTemplate, "[|Me?.B?.$$C|]?.D"));
            Test(string.Format(sourceTemplate, "[|Me?.B?.C?.$$D|]"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843)]
        public void TestConditionalAccessExpression_Trivia()
        {
            var sourceTemplate = @"
class A
{{
    B B;

    object M()
    {{
        return {0};
    }}
}}

class B
{{
    C C;
}}

class C
{{
}}
";

            Test(string.Format(sourceTemplate, "/*1*/[|$$Me|]/*2*/?./*3*/B/*4*/?./*5*/C/*6*/"));
            Test(string.Format(sourceTemplate, "/*1*/[|Me/*2*/?./*3*/$$B|]/*4*/?./*5*/C/*6*/"));
            Test(string.Format(sourceTemplate, "/*1*/[|Me/*2*/?./*3*/B/*4*/?./*5*/$$C|]/*6*/"));
        }
    }
}
