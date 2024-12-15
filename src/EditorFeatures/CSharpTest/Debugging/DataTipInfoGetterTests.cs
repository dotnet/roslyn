// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.Debugging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DebuggingDataTips)]
public sealed class DataTipInfoGetterTests : AbstractDataTipInfoGetterTests
{
    protected override EditorTestWorkspace CreateWorkspace(string markup)
        => EditorTestWorkspace.CreateCSharp(markup);

    [Fact]
    public async Task TestCSharpLanguageDebugInfoGetDataTipSpanAndText()
    {
        await TestAsync("class [|C$$|] { }");
        await TestAsync("struct [|C$$|] { }");
        await TestAsync("interface [|C$$|] { }");
        await TestAsync("enum [|C$$|] { }");
        await TestAsync("delegate void [|C$$|] ();"); // Without the space, that position is actually on the open paren.
    }

    [Fact]
    public async Task Test1()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|Sys$$tem|].Console.WriteLine(args);
              }
            }
            """);
    }

    [Fact]
    public async Task Test2()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|System$$.Console|].WriteLine(args);
              }
            }
            """);
    }

    [Fact]
    public async Task Test3()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|System.$$Console|].WriteLine(args);
              }
            }
            """);
    }

    [Fact]
    public async Task Test4()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|System.Con$$sole|].WriteLine(args);
              }
            }
            """);
    }

    [Fact]
    public async Task Test5()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|System.Console.Wri$$teLine|](args);
              }
            }
            """);
    }

    [Fact]
    public async Task Test6()
    {
        await TestNoDataTipAsync(
            """
            class C
            {
              void Goo()
              {
                [|System.Console.WriteLine|]$$(args);
              }
            }
            """);
    }

    [Fact]
    public async Task Test7()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                System.Console.WriteLine($$[|args|]);
              }
            }
            """);
    }

    [Fact]
    public async Task Test8()
    {
        await TestNoDataTipAsync(
            """
            class C
            {
              void Goo()
              {
                [|System.Console.WriteLine|](args$$);
              }
            }
            """);
    }

    [Fact]
    public async Task TestVar()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|va$$r|] v = 0;
              }
            }
            """, "int");
    }

    [Fact]
    public async Task TestVariableType()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                [|in$$t|] i = 0;
              }
            }
            """);
    }

    [Fact]
    public async Task TestVariableIdentifier()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                int [|$$i|] = 0;
              }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539910")]
    public async Task TestLiterals()
    {
        await TestAsync(
            """
            class C
            {
              void Goo()
              {
                int i = [|4$$2|];
              }
            }
            """, "int");
    }

    [Fact]
    public async Task TestNonExpressions()
    {
        await TestNoDataTipAsync(
            """
            class C
            {
              void Goo()
              {
                int i = 42;
              }$$
            }
            """);
    }

    [Fact]
    public async Task TestParameterIdentifier()
    {
        await TestAsync(
            """
            class C
            {
              void Goo(int [|$$i|])
              {
              }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942699")]
    public async Task TestCatchIdentifier()
    {
        await TestAsync(
            """
            class C
            {
                void Goo()
                {
                    try
                    {
                    }
                    catch (System.Exception [|$$e|])
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestEvent()
    {
        await TestAsync(
            """
            class C
            {
                event System.Action [|$$E|];
            }
            """);

        await TestAsync(
            """
            class C
            {
                event System.Action [|$$E|]
                {
                    add { }
                    remove { }
                }
            }
            """);
    }

    [Fact]
    public async Task TestMethod()
    {
        await TestAsync(
            """
            class C
            {
                int [|$$M|]() { }
            }
            """);
    }

    [Fact]
    public async Task TestTypeParameter()
    {
        await TestAsync("class C<T, [|$$U|], V> { }");
        await TestAsync(
            """
            class C
            {
                void M<T, [|$$U|]>() { }
            }
            """);
    }

    [Fact]
    public async Task UsingAlias()
    {
        await TestAsync(
            """
            using [|$$S|] = Static;

            static class Static
            {
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540921")]
    public async Task TestForEachIdentifier()
    {
        await TestAsync(
            """
            class C
            {
              void Goo(string[] args)
              {
                foreach (string [|$$s|] in args)
                {
                }
              }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546328")]
    public async Task TestProperty()
    {
        await TestAsync(
            """
            namespace ConsoleApplication16
            {
                class C
                {
                    public int [|$$goo|] { get; private set; } // hover over me
                    public C()
                    {
                        this.goo = 1;
                    }
                    public int Goo()
                    {
                        return 2; // breakpoint here
                    }
                }
                class Program
                {
                    static void Main(string[] args)
                    {
                        new C().Goo();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestQueryIdentifier()
    {
        await TestAsync( // From
            """
            class C
            {
                object Goo(string[] args)
                {
                    return from [|$$a|] in args select a;
                }
            }
            """);
        await TestAsync( // Let
            """
            class C
            {
                object Goo(string[] args)
                {
                    return from a in args let [|$$b|] = "END" select a + b;
                }
            }
            """);
        await TestAsync( // Join
            """
            class C
            {
                object Goo(string[] args)
                {
                    return from a in args join [|$$b|] in args on a equals b;
                }
            }
            """);
        await TestAsync( // Join Into
            """
            class C
            {
                object Goo(string[] args)
                {
                    return from a in args join b in args on a equals b into [|$$c|];
                }
            }
            """);
        await TestAsync( // Continuation
            """
            class C
            {
                object Goo(string[] args)
                {
                    return from a in args select a into [|$$b|] from c in b select c;
                }
            }
            """);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077843")]
    // One level.
    [InlineData("[|Me?.$$B|]")]
    // Two levels.
    [InlineData("[|Me?.$$B|].C")]
    [InlineData("[|Me?.B.$$C|]")]
    [InlineData("[|Me.$$B|]?.C")]
    [InlineData("[|Me.B?.$$C|]")]
    [InlineData("[|Me?.$$B|]?.C")]
    [InlineData("[|Me?.B?.$$C|]")]
    // Three levels.
    [InlineData("[|Me?.$$B|].C.D")]
    [InlineData("[|Me?.B.$$C|].D")]
    [InlineData("[|Me?.B.C.$$D|]")]
    [InlineData("[|Me.$$B|]?.C.D")]
    [InlineData("[|Me.B?.$$C|].D")]
    [InlineData("[|Me.B?.C.$$D|]")]
    [InlineData("[|Me.$$B|].C?.D")]
    [InlineData("[|Me.B.$$C|]?.D")]
    [InlineData("[|Me.B.C?.$$D|]")]
    [InlineData("[|Me?.$$B|]?.C.D")]
    [InlineData("[|Me?.B?.$$C|].D")]
    [InlineData("[|Me?.B?.C.$$D|]")]
    [InlineData("[|Me?.$$B|].C?.D")]
    [InlineData("[|Me?.B.$$C|]?.D")]
    [InlineData("[|Me?.B.C?.$$D|]")]
    [InlineData("[|Me.$$B|]?.C?.D")]
    [InlineData("[|Me.B?.$$C|]?.D")]
    [InlineData("[|Me.B?.C?.$$D|]")]
    [InlineData("[|Me?.$$B|]?.C?.D")]
    [InlineData("[|Me?.B?.$$C|]?.D")]
    [InlineData("[|Me?.B?.C?.$$D|]")]
    public async Task TestConditionalAccessExpression(string data)
    {
        await TestAsync($$"""
            class A
            {
                B B;

                object M()
                {
                    return {{data}};
                }
            }

            class B
            {
                C C;
            }

            class C
            {
                D D;
            }

            class D
            {
            }
            """);
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077843")]
    [InlineData("/*1*/[|$$Me|]/*2*/?./*3*/B/*4*/?./*5*/C/*6*/")]
    [InlineData("/*1*/[|Me/*2*/?./*3*/$$B|]/*4*/?./*5*/C/*6*/")]
    [InlineData("/*1*/[|Me/*2*/?./*3*/B/*4*/?./*5*/$$C|]/*6*/")]
    public async Task TestConditionalAccessExpression_Trivia(string data)
    {
        await TestAsync($$"""
            class A
            {
                B B;

                object M()
                {
                    return {{data}};
                }
            }

            class B
            {
                C C;
            }

            class C
            {
            }
            """);
    }

    [Fact]
    public async Task TestLinq1()
    {
        await TestAsync("""
            using System.Linq;

            int[] args;
            var v = $$[|args|].Select(a => a.ToString());
            """);
    }

    [Fact]
    public async Task TestLinq2()
    {
        await TestAsync("""
            using System.Linq;

            int[] args;
            var v = {|LinqExpression:[|args.$$Select|](a => a.ToString())|};
            """);
    }

    [Fact]
    public async Task TestLinq3()
    {
        await TestAsync("""
            using System.Linq;

            int[] args;
            var v = $$[|args|].Select(a => a.ToString()).Where(a => a.Length >= 0);
            """);
    }

    [Fact]
    public async Task TestLinq4()
    {
        await TestAsync("""
            using System.Linq;

            int[] args;
            var v = {|LinqExpression:[|args.$$Select|](a => a.ToString())|}.Where(a => a.Length >= 0);
            """);
    }

    [Fact]
    public async Task TestLinq5()
    {
        await TestAsync("""
            using System.Linq;

            int[] args;
            var v = {|LinqExpression:[|args.Select(a => a.ToString()).$$Where|](a => a.Length >= 0)|};
            """);
    }
}
