// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private async Task TestAsync(string markup, string expectedText = null)
        {
            await TestSpanGetterAsync(markup, async (document, position, expectedSpan) =>
            {
                var result = await DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None);

                Assert.Equal(expectedSpan, result.Span);
                Assert.Equal(expectedText, result.Text);
            });
        }

        private async Task TestNoDataTipAsync(string markup)
        {
            await TestSpanGetterAsync(markup, async (document, position, expectedSpan) =>
            {
                var result = await DataTipInfoGetter.GetInfoAsync(document, position, CancellationToken.None);
                Assert.True(result.IsDefault);
            });
        }

        private async Task TestSpanGetterAsync(string markup, Func<Document, int, TextSpan?, Task> continuation)
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync(markup))
            {
                var testHostDocument = workspace.Documents.Single();
                var position = testHostDocument.CursorPosition.Value;
                var expectedSpan = testHostDocument.SelectedSpans.Any()
                    ? testHostDocument.SelectedSpans.Single()
                    : (TextSpan?)null;

                await continuation(
                    workspace.CurrentSolution.Projects.First().Documents.First(),
                    position,
                    expectedSpan);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestCSharpLanguageDebugInfoGetDataTipSpanAndText()
        {
            await TestAsync("class [|C$$|] { }");
            await TestAsync("struct [|C$$|] { }");
            await TestAsync("interface [|C$$|] { }");
            await TestAsync("enum [|C$$|] { }");
            await TestAsync("delegate void [|C$$|] ();"); // Without the space, that position is actually on the open paren.
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test1()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|Sys$$tem|].Console.WriteLine(args);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test2()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|System$$.Console|].WriteLine(args);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test3()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|System.$$Console|].WriteLine(args);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test4()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|System.Con$$sole|].WriteLine(args);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test5()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|System.Console.Wri$$teLine|](args);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test6()
        {
            await TestNoDataTipAsync(
@"class C
{
  void Foo()
  {
    [|System.Console.WriteLine|]$$(args);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test7()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    System.Console.WriteLine($$[|args|]);
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task Test8()
        {
            await TestNoDataTipAsync(
@"class C
{
  void Foo()
  {
    [|System.Console.WriteLine|](args$$);
  }
}");
        }

        [Fact]
        public async Task TestVar()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|va$$r|] v = 0;
  }
}", "int");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestVariableType()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    [|in$$t|] i = 0;
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestVariableIdentifier()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    int [|$$i|] = 0;
  }
}");
        }

        [WorkItem(539910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539910")]
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestLiterals()
        {
            await TestAsync(
@"class C
{
  void Foo()
  {
    int i = [|4$$2|];
  }
}", "int");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestNonExpressions()
        {
            await TestNoDataTipAsync(
@"class C
{
  void Foo()
  {
    int i = 42;
  }$$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestParameterIdentifier()
        {
            await TestAsync(
@"class C
{
  void Foo(int [|$$i|])
  {
  }
}");
        }

        [WorkItem(942699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942699")]
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestCatchIdentifier()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestEvent()
        {
            await TestAsync(
@"class C
{
    event System.Action [|$$E|];
}");

            await TestAsync(
@"class C
{
    event System.Action [|$$E|]
    {
        add { }
        remove { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestMethod()
        {
            await TestAsync(
@"class C
{
    int [|$$M|]() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestTypeParameter()
        {
            await TestAsync("class C<T, [|$$U|], V> { }");
            await TestAsync(
@"class C
{
    void M<T, [|$$U|]>() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task UsingAlias()
        {
            await TestAsync(
@"using [|$$S|] = Static;

static class Static
{
}");
        }

        [WorkItem(540921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540921")]
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestForEachIdentifier()
        {
            await TestAsync(
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

        [WorkItem(546328, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546328")]
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestProperty()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
        public async Task TestQueryIdentifier()
        {
            await TestAsync( // From
@"class C
{
    object Foo(string[] args)
    {
        return from [|$$a|] in args select a;
    }
}");
            await TestAsync( // Let
@"class C
{
    object Foo(string[] args)
    {
        return from a in args let [|$$b|] = ""END"" select a + b;
    }
}");
            await TestAsync( // Join
@"class C
{
    object Foo(string[] args)
    {
        return from a in args join [|$$b|] in args on a equals b;
    }
}");
            await TestAsync( // Join Into
@"class C
{
    object Foo(string[] args)
    {
        return from a in args join b in args on a equals b into [|$$c|];
    }
}");
            await TestAsync( // Continuation
@"class C
{
    object Foo(string[] args)
    {
        return from a in args select a into [|$$b|] from c in b select c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077843")]
        public async Task TestConditionalAccessExpression()
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
            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|]"));

            // Two levels.
            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|].C"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B.$$C|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me.$$B|]?.C"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B?.$$C|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|]?.C"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B?.$$C|]"));

            // Three levels.
            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|].C.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B.$$C|].D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B.C.$$D|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me.$$B|]?.C.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B?.$$C|].D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B?.C.$$D|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me.$$B|].C?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B.$$C|]?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B.C?.$$D|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|]?.C.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B?.$$C|].D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B?.C.$$D|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|].C?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B.$$C|]?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B.C?.$$D|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me.$$B|]?.C?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B?.$$C|]?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me.B?.C?.$$D|]"));

            await TestAsync(string.Format(sourceTemplate, "[|Me?.$$B|]?.C?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B?.$$C|]?.D"));
            await TestAsync(string.Format(sourceTemplate, "[|Me?.B?.C?.$$D|]"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingDataTips), WorkItem(1077843, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077843")]
        public async Task TestConditionalAccessExpression_Trivia()
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

            await TestAsync(string.Format(sourceTemplate, "/*1*/[|$$Me|]/*2*/?./*3*/B/*4*/?./*5*/C/*6*/"));
            await TestAsync(string.Format(sourceTemplate, "/*1*/[|Me/*2*/?./*3*/$$B|]/*4*/?./*5*/C/*6*/"));
            await TestAsync(string.Format(sourceTemplate, "/*1*/[|Me/*2*/?./*3*/B/*4*/?./*5*/$$C|]/*6*/"));
        }
    }
}
