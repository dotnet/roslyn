// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class SuggestionModeCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(CSharpSuggestionModeCompletionProvider);

    [Fact]
    public Task AfterFirstExplicitArgument()
        => VerifyBuilderAsync(AddInsideMethod(@"Func<int, int, int> f = (int x, i $$"));

    [Fact]
    public Task AfterFirstImplicitArgument()
        => VerifyBuilderAsync(AddInsideMethod(@"Func<int, int, int> f = (x, i $$"));

    [Fact]
    public Task AfterFirstImplicitArgumentInMethodCall()
        => VerifyBuilderAsync("""
            class c
            {
                private void bar(Func<int, int, bool> f) { }

                private void goo()
                {
                    bar((x, i $$
                }
            }
            """);

    [Fact]
    public Task AfterFirstExplicitArgumentInMethodCall()
        => VerifyBuilderAsync("""
            class c
            {
                private void bar(Func<int, int, bool> f) { }

                private void goo()
                {
                    bar((int x, i $$
                }
            }
            """);

    [Fact]
    public Task DelegateTypeExpected1()
        => VerifyBuilderAsync("""
            using System;

            class c
            {
                private void bar(Func<int, int, bool> f) { }

                private void goo()
                {
                    bar($$
                }
            }
            """);

    [Fact]
    public async Task DelegateTypeExpected2()
        => await VerifyBuilderAsync(AddUsingDirectives("using System;", AddInsideMethod(@"Func<int, int, int> f = $$")));

    [Fact]
    public Task ObjectInitializerDelegateType()
        => VerifyBuilderAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                public Func<int> myfunc { get; set; }
            }

            class a
            {
                void goo()
                {
                    var b = new Program() { myfunc = $$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817145")]
    public Task ExplicitArrayInitializer()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    Func<int, int>[] myfunc = new Func<int, int>[] { $$;
                }
            }
            """);

    [Fact]
    public Task ImplicitArrayInitializerUnknownType()
        => VerifyNotBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = new [] { $$;
                }
            }
            """);

    [Fact]
    public Task ImplicitArrayInitializerKnownDelegateType()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = new [] { x => 2 * x, $$
                }
            }
            """);

    [Fact]
    public Task TernaryOperatorUnknownType()
        => VerifyNotBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = true ? $$
                }
            }
            """);

    [Fact]
    public Task TernaryOperatorKnownDelegateType1()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = true ? x => x * 2 : $$
                }
            }
            """);

    [Fact]
    public Task TernaryOperatorKnownDelegateType2()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    Func<int, int> a = true ? $$
                }
            }
            """);

    [Fact]
    public Task OverloadTakesADelegate1()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo(int a) { }
                void goo(Func<int, int> a) { }

                void bar()
                {
                    this.goo($$
                }
            }
            """);

    [Fact]
    public Task OverloadTakesDelegate2()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo(int i, int a) { }
                void goo(int i, Func<int, int> a) { }

                void bar()
                {
                    this.goo(1, $$
                }
            }
            """);

    [Fact]
    public Task ExplicitCastToDelegate()
        => VerifyBuilderAsync("""
            using System;

            class a
            {

                void bar()
                {
                    (Func<int, int>) ($$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860580")]
    public Task ReturnStatement()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                Func<int, int> bar()
                {
                    return $$
                }
            }
            """);

    [Fact]
    public Task BuilderInAnonymousType1()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                int bar()
                {
                    var q = new {$$
                }
            }
            """);

    [Fact]
    public Task BuilderInAnonymousType2()
        => VerifyBuilderAsync("""
            using System;

            class a
            {
                int bar()
                {
                    var q = new {$$ 1, 2 };
                }
            }
            """);

    [Fact]
    public Task BuilderInAnonymousType3()
        => VerifyBuilderAsync("""
            using System;
            class a
            {
                int bar()
                {
                    var q = new {Name = 1, $$ };
                }
            }
            """);

    [Fact]
    public async Task BuilderInFromClause()
    {
        var markup = """
            using System;
            using System.Linq;

            class a
            {
                int bar()
                {
                    var q = from $$
                }
            }
            """;
        await VerifyBuilderAsync(markup.ToString());
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/823968")]
    public async Task BuilderInJoinClause()
    {
        var markup = """
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class a
            {
                int bar()
                {
                    var list = new List<int>();
                    var q = from a in list
                            join $$
                }
            }
            """;
        await VerifyBuilderAsync(markup.ToString());
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544290")]
    public Task ParenthesizedLambdaArgument()
        => VerifyBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$, e) => { });
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
    public Task IncompleteParenthesizedLambdaArgument()
        => VerifyBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
    public Task IncompleteNestedParenthesizedLambdaArgument()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler(((a$$
                }
            }
            """);

    [Fact]
    public Task ParenthesizedExpressionInVarDeclaration()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
    public Task TestInObjectCreation()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main()
                {
                    Program x = new P$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
    public Task TestInArrayCreation()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main()
                {
                    Program[] x = new $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
    public Task TestInArrayCreation2()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main()
                {
                    Program[] x = new Pr$$
                }
            }
            """);

    [Fact]
    public Task TupleExpressionInVarDeclaration()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a$$, b)
                }
            }
            """);

    [Fact]
    public Task TupleExpressionInVarDeclaration2()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a, b$$)
                }
            }
            """);

    [Fact]
    public Task IncompleteLambdaInActionDeclaration()
        => VerifyBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    System.Action x = (a$$, b)
                }
            }
            """);

    [Fact]
    public Task TupleWithNamesInActionDeclaration()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    System.Action x = (a$$, b: b)
                }
            }
            """);

    [Fact]
    public Task TupleWithNamesInActionDeclaration2()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    System.Action x = (a: a, b$$)
                }
            }
            """);

    [Fact]
    public Task TupleWithNamesInVarDeclaration()
        => VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a: a, b$$)
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
    public Task BuilderForLinqExpression()
        => VerifyBuilderAsync("""
            using System;
            using System.Linq.Expressions;

            public class Class
            {
                public void Goo(Expression<Action<int>> arg)
                {
                    Goo($$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
    public Task NotInTypeParameter()
        => VerifyNotBuilderAsync("""
            using System;
            using System.Linq.Expressions;

            public class Class
            {
                public void Goo(Expression<Action<int>> arg)
                {
                    Enumerable.Empty<$$
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611477")]
    public Task ExtensionMethodFaultTolerance()
        => VerifyBuilderAsync("""
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;

            namespace Outer
            {
                public struct ImmutableArray<T> : IEnumerable<T>
                {
                    public IEnumerator<T> GetEnumerator()
                    {
                        throw new NotImplementedException();
                    }

                    IEnumerator IEnumerable.GetEnumerator()
                    {
                        throw new NotImplementedException();
                    }
                }

                public static class ReadOnlyArrayExtensions
                {
                    public static ImmutableArray<TResult> Select<T, TResult>(this ImmutableArray<T> array, Func<T, TResult> selector)
                    {
                        throw new NotImplementedException();
                    }
                }

                namespace Inner
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {
                            args.Select($$
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834609")]
    public Task LambdaWithAutomaticBraceCompletion()
        => VerifyBuilderAsync("""
            using System;
            using System;

            public class Class
            {
                public void Goo()
                {
                    EventHandler h = (s$$)
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
    public Task ThisConstructorInitializer()
        => VerifyBuilderAsync("""
            using System;
            class X 
            { 
                X(Func<X> x) : this($$) { } 
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
    public Task BaseConstructorInitializer()
        => VerifyBuilderAsync("""
            using System;
            class B
            {
                public B(Func<B> x) {}
            }

            class D : B 
            { 
                D() : base($$) { } 
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/887842")]
    public Task PreprocessorExpression()
        => VerifyBuilderAsync("""
            class C
            {
            #if $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/967254")]
    public Task ImplicitArrayInitializerAfterNew()
        => VerifyNotBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    int[] a = new $$;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceDeclaration_Unqualified()
        => VerifyBuilderAsync(@"namespace $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task NamespaceDeclaration_Qualified()
        => VerifyBuilderAsync(@"namespace A.$$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task FileScopedNamespaceDeclaration_Unqualified()
        => VerifyBuilderAsync(@"namespace $$;");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task FileScopedNamespaceDeclaration_Qualified()
        => VerifyBuilderAsync(@"namespace A.$$;");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task PartialClassName()
        => VerifyBuilderAsync(@"partial class $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task PartialStructName()
        => VerifyBuilderAsync(@"partial struct $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public Task PartialInterfaceName()
        => VerifyBuilderAsync(@"partial interface $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12818")]
    public Task UnwrapParamsArray()
        => VerifyBuilderAsync("""
            using System;
            class C {
                C(params Action<int>[] a) {
                    new C($$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72225")]
    public Task UnwrapParamsCollection()
        => VerifyBuilderAsync("""
            using System;
            using System.Collections.Generic;

            class C
            {
                C(params IEnumerable<Action<int>> a)
                {
                    new C($$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12818")]
    public Task DoNotUnwrapRegularArray()
        => VerifyNotBuilderAsync("""
            using System;
            class C {
                C(Action<int>[] a) {
                    new C($$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47662")]
    public Task LambdaExpressionInImplicitObjectCreation()
        => VerifyBuilderAsync("""
            using System;
            class C {
                C(Action<int> a) {
                    C c = new($$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15443")]
    public Task NotBuilderWhenDelegateInferredRightOfDotInInvocation()
        => VerifyNotBuilderAsync("""
            class C {
            	Action a = Task.$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15443")]
    public Task NotBuilderInTypeArgument()
        => VerifyNotBuilderAsync("""
            namespace ConsoleApplication1
            {
                class Program
                {
                    class N { }
                    static void Main(string[] args)
                    {
                        Program.N n = Load<Program.$$
                    }

                    static T Load<T>() => default(T);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16176")]
    public Task NotBuilderForLambdaAfterNew()
        => VerifyNotBuilderAsync("""
            class C {
            	Action a = new $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20937")]
    public Task AsyncLambda()
        => VerifyBuilderAsync("""
            using System;
            using System.Threading.Tasks;
            class Program
            {
                public void B(Func<int, int, Task<int>> f) { }

                void A()
                {
                    B(async($$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20937")]
    public Task AsyncLambdaAfterComma()
        => VerifyBuilderAsync("""
            using System;
            using System.Threading.Tasks;
            class Program
            {
                public void B(Func<int, int, Task<int>> f) { }

                void A()
                {
                    B(async(p1, $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod1()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(a$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod2()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(a$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod3()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(($$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod4()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(($$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod5()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(($$))
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod6()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar((a, $$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithExtensionAndInstanceMethod7()
        => VerifyBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, Action<int> action)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(async (a$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public Task WithNonDelegateExtensionAndInstanceMethod1()
        => VerifyNotBuilderAsync("""
            using System;

            public sealed class Goo
            {
                public void Bar()
                {
                }
            }

            public static class GooExtensions
            {
                public static void Bar(this Goo goo, int val)
                {
                }
            }

            public static class Repro
            {
                public static void ReproMethod(Goo goo)
                {
                    goo.Bar(a$$
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInDeclarationPattern()
        => VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is int o$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInDeclarationPattern2()
        => VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is System.Collections.Generic.List<int> an$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInRecursivePattern()
        => VerifyBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is { P: 1 } o$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInPropertyPattern()
        => VerifyBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is { P: int o$$ })
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInAndPattern()
        => VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is 1 and int a$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInAndOrPattern()
        => VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is (int or 1) and int a$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInSwitchStatement()
        => VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    switch (e)
                    {
                        case int o$$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestInSwitchExpression()
        => VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    var result = e switch
                    {
                        int o$$
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestMissingInNotPattern_Declaration()
        => VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is not int o$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestMissingInNotPattern_Declaration2()
        => VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is not (1 and int o$$))
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestMissingInNotPattern_Recursive()
        => VerifyNotBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is not { P: 1 } o$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestMissingInOrPattern()
        => VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is 1 or int o$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestMissingInAndOrPattern()
        => VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is 1 or int and int o$$)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestMissingInRecursiveOrPattern()
        => VerifyNotBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is null or { P: 1 } o$$)
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/46927"), CombinatorialData]
    public Task FirstArgumentOfInvocation_NoParameter(bool hasTypedChar)
        => VerifyNotBuilderAsync($$"""
            using System;
            interface Foo
            {
                bool Bar() => true;
            }
            class P
            {
                void M(Foo f)
                {
                    f.Bar({{(hasTypedChar ? "s" : "")}}$$
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/46927"), CombinatorialData]
    public async Task FirstArgumentOfInvocation_PossibleLambdaExpression(bool isLambda, bool hasTypedChar)
    {
        var overload = isLambda
            ? "bool Bar(Func<int, bool> predicate) => true;"
            : "bool Bar(int x) => true;";

        var markup = $$"""
            using System;
            interface Foo
            {
                bool Bar() => true;
                {{overload}}
            }
            class P
            {
                void M(Foo f)
                {
                    f.Bar({{(hasTypedChar ? "s" : "")}}$$
                }
            }
            """;
        if (isLambda)
        {
            await VerifyBuilderAsync(markup);
        }
        else
        {
            await VerifyNotBuilderAsync(markup);
        }
    }

    [InlineData("params string[] x")]
    [InlineData("string x = null, string y = null")]
    [InlineData("string x = null, string y = null, params string[] z")]
    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/49656")]
    public Task FirstArgumentOfInvocation_WithOverloadAcceptEmptyArgumentList(string overloadParameterList)
        => VerifyBuilderAsync($$"""
            using System;
            interface Foo
            {
                bool Bar({{overloadParameterList}}) => true;
                bool Bar(Func<int, bool> predicate) => true;
            }
            class P
            {
                void M(Foo f)
                {
                    f.Bar($$)
                }
            }
            """);

    private async Task VerifyNotBuilderAsync(string markup)
        => await VerifyWorkerAsync(markup, isBuilder: false);

    private async Task VerifyBuilderAsync(string markup)
        => await VerifyWorkerAsync(markup, isBuilder: true);

    private async Task VerifyWorkerAsync(string markup, bool isBuilder)
    {
        MarkupTestFile.GetPosition(markup, out var code, out int position);

        using var workspaceFixture = new CSharpTestWorkspaceFixture();
        workspaceFixture.GetWorkspace(GetComposition());
        var document1 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular);
        await CheckResultsAsync(document1, position, isBuilder);

        if (await CanUseSpeculativeSemanticModelAsync(document1, position))
        {
            var document2 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular, cleanBeforeUpdate: false);
            await CheckResultsAsync(document2, position, isBuilder);
        }
    }

    private async Task CheckResultsAsync(Document document, int position, bool isBuilder)
    {
        var triggerInfos = new List<CompletionTrigger>
        {
            CompletionTrigger.CreateInsertionTrigger('a'),
            CompletionTrigger.Invoke,
            CompletionTrigger.CreateDeletionTrigger('z')
        };

        var service = GetCompletionService(document.Project);
        var provider = Assert.Single(service.GetTestAccessor().GetImportedAndBuiltInProviders([]));

        foreach (var triggerInfo in triggerInfos)
        {
            var completionList = await service.GetTestAccessor().GetContextAsync(
                provider, document, position, triggerInfo,
                options: CompletionOptions.Default, cancellationToken: CancellationToken.None);

            if (isBuilder)
            {
                Assert.NotNull(completionList);
                Assert.True(completionList.SuggestionModeItem != null, "Expecting a suggestion mode, but none was present");
            }
            else
            {
                if (completionList != null)
                {
                    Assert.True(completionList.SuggestionModeItem == null, "group.Builder == " + (completionList.SuggestionModeItem != null ? completionList.SuggestionModeItem.DisplayText : "null"));
                }
            }
        }
    }
}
