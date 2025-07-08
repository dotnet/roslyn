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
    public async Task AfterFirstImplicitArgumentInMethodCall()
    {
        // The right-hand-side parses like a possible deconstruction or tuple type
        await VerifyBuilderAsync("""
            class c
            {
                private void bar(Func<int, int, bool> f) { }

                private void goo()
                {
                    bar((x, i $$
                }
            }
            """);
    }

    [Fact]
    public async Task AfterFirstExplicitArgumentInMethodCall()
    {
        // Could be a deconstruction expression
        await VerifyBuilderAsync("""
            class c
            {
                private void bar(Func<int, int, bool> f) { }

                private void goo()
                {
                    bar((int x, i $$
                }
            }
            """);
    }

    [Fact]
    public async Task DelegateTypeExpected1()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact]
    public async Task DelegateTypeExpected2()
        => await VerifyBuilderAsync(AddUsingDirectives("using System;", AddInsideMethod(@"Func<int, int, int> f = $$")));

    [Fact]
    public async Task ObjectInitializerDelegateType()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817145")]
    public async Task ExplicitArrayInitializer()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    Func<int, int>[] myfunc = new Func<int, int>[] { $$;
                }
            }
            """);
    }

    [Fact]
    public async Task ImplicitArrayInitializerUnknownType()
    {
        await VerifyNotBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = new [] { $$;
                }
            }
            """);
    }

    [Fact]
    public async Task ImplicitArrayInitializerKnownDelegateType()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = new [] { x => 2 * x, $$
                }
            }
            """);
    }

    [Fact]
    public async Task TernaryOperatorUnknownType()
    {
        await VerifyNotBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = true ? $$
                }
            }
            """);
    }

    [Fact]
    public async Task TernaryOperatorKnownDelegateType1()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    var a = true ? x => x * 2 : $$
                }
            }
            """);
    }

    [Fact]
    public async Task TernaryOperatorKnownDelegateType2()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    Func<int, int> a = true ? $$
                }
            }
            """);
    }

    [Fact]
    public async Task OverloadTakesADelegate1()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact]
    public async Task OverloadTakesDelegate2()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact]
    public async Task ExplicitCastToDelegate()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {

                void bar()
                {
                    (Func<int, int>) ($$
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860580")]
    public async Task ReturnStatement()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                Func<int, int> bar()
                {
                    return $$
                }
            }
            """);
    }

    [Fact]
    public async Task BuilderInAnonymousType1()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                int bar()
                {
                    var q = new {$$
                }
            }
            """);
    }

    [Fact]
    public async Task BuilderInAnonymousType2()
    {
        await VerifyBuilderAsync("""
            using System;

            class a
            {
                int bar()
                {
                    var q = new {$$ 1, 2 };
                }
            }
            """);
    }

    [Fact]
    public async Task BuilderInAnonymousType3()
    {
        await VerifyBuilderAsync("""
            using System;
            class a
            {
                int bar()
                {
                    var q = new {Name = 1, $$ };
                }
            }
            """);
    }

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
    public async Task ParenthesizedLambdaArgument()
    {
        await VerifyBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$, e) => { });
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
    public async Task IncompleteParenthesizedLambdaArgument()
    {
        await VerifyBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
    public async Task IncompleteNestedParenthesizedLambdaArgument()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler(((a$$
                }
            }
            """);
    }

    [Fact]
    public async Task ParenthesizedExpressionInVarDeclaration()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a$$
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
    public async Task TestInObjectCreation()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main()
                {
                    Program x = new P$$
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
    public async Task TestInArrayCreation()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main()
                {
                    Program[] x = new $$
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
    public async Task TestInArrayCreation2()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main()
                {
                    Program[] x = new Pr$$
                }
            }
            """);
    }

    [Fact]
    public async Task TupleExpressionInVarDeclaration()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a$$, b)
                }
            }
            """);
    }

    [Fact]
    public async Task TupleExpressionInVarDeclaration2()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a, b$$)
                }
            }
            """);
    }

    [Fact]
    public async Task IncompleteLambdaInActionDeclaration()
    {
        await VerifyBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    System.Action x = (a$$, b)
                }
            }
            """);
    }

    [Fact]
    public async Task TupleWithNamesInActionDeclaration()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    System.Action x = (a$$, b: b)
                }
            }
            """);
    }

    [Fact]
    public async Task TupleWithNamesInActionDeclaration2()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    System.Action x = (a: a, b$$)
                }
            }
            """);
    }

    [Fact]
    public async Task TupleWithNamesInVarDeclaration()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    var x = (a: a, b$$)
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
    public async Task BuilderForLinqExpression()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
    public async Task NotInTypeParameter()
    {
        await VerifyNotBuilderAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611477")]
    public async Task ExtensionMethodFaultTolerance()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834609")]
    public async Task LambdaWithAutomaticBraceCompletion()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
    public async Task ThisConstructorInitializer()
    {
        await VerifyBuilderAsync("""
            using System;
            class X 
            { 
                X(Func<X> x) : this($$) { } 
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
    public async Task BaseConstructorInitializer()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/887842")]
    public async Task PreprocessorExpression()
    {
        await VerifyBuilderAsync("""
            class C
            {
            #if $$
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/967254")]
    public async Task ImplicitArrayInitializerAfterNew()
    {
        await VerifyNotBuilderAsync("""
            using System;

            class a
            {
                void goo()
                {
                    int[] a = new $$;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task NamespaceDeclaration_Unqualified()
    {
        await VerifyBuilderAsync(@"namespace $$");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task NamespaceDeclaration_Qualified()
    {
        await VerifyBuilderAsync(@"namespace A.$$");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task FileScopedNamespaceDeclaration_Unqualified()
    {
        await VerifyBuilderAsync(@"namespace $$;");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task FileScopedNamespaceDeclaration_Qualified()
    {
        await VerifyBuilderAsync(@"namespace A.$$;");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task PartialClassName()
    {
        await VerifyBuilderAsync(@"partial class $$");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task PartialStructName()
    {
        await VerifyBuilderAsync(@"partial struct $$");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
    public async Task PartialInterfaceName()
    {
        await VerifyBuilderAsync(@"partial interface $$");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12818")]
    public async Task UnwrapParamsArray()
    {
        await VerifyBuilderAsync("""
            using System;
            class C {
                C(params Action<int>[] a) {
                    new C($$
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72225")]
    public async Task UnwrapParamsCollection()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12818")]
    public async Task DoNotUnwrapRegularArray()
    {
        await VerifyNotBuilderAsync("""
            using System;
            class C {
                C(Action<int>[] a) {
                    new C($$
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47662")]
    public async Task LambdaExpressionInImplicitObjectCreation()
    {
        await VerifyBuilderAsync("""
            using System;
            class C {
                C(Action<int> a) {
                    C c = new($$
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15443")]
    public async Task NotBuilderWhenDelegateInferredRightOfDotInInvocation()
    {
        await VerifyNotBuilderAsync("""
            class C {
            	Action a = Task.$$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15443")]
    public async Task NotBuilderInTypeArgument()
    {
        await VerifyNotBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16176")]
    public async Task NotBuilderForLambdaAfterNew()
    {
        await VerifyNotBuilderAsync("""
            class C {
            	Action a = new $$
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20937")]
    public async Task AsyncLambda()
    {
        await VerifyBuilderAsync("""
            using System;
            using System.Threading.Tasks;
            class Program
            {
                public void B(Func<int, int, Task<int>> f) { }

                void A()
                {
                    B(async($$
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20937")]
    public async Task AsyncLambdaAfterComma()
    {
        await VerifyBuilderAsync("""
            using System;
            using System.Threading.Tasks;
            class Program
            {
                public void B(Func<int, int, Task<int>> f) { }

                void A()
                {
                    B(async(p1, $$
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod1()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod2()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod3()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod4()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod5()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod6()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithExtensionAndInstanceMethod7()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
    public async Task WithNonDelegateExtensionAndInstanceMethod1()
    {
        await VerifyNotBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInDeclarationPattern()
    {
        await VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is int o$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInDeclarationPattern2()
    {
        await VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is System.Collections.Generic.List<int> an$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInRecursivePattern()
    {
        await VerifyBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is { P: 1 } o$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInPropertyPattern()
    {
        await VerifyBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is { P: int o$$ })
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInAndPattern()
    {
        await VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is 1 and int a$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInAndOrPattern()
    {
        await VerifyBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is (int or 1) and int a$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInSwitchStatement()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestInSwitchExpression()
    {
        await VerifyBuilderAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestMissingInNotPattern_Declaration()
    {
        await VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is not int o$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestMissingInNotPattern_Declaration2()
    {
        await VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is not (1 and int o$$))
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestMissingInNotPattern_Recursive()
    {
        await VerifyNotBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is not { P: 1 } o$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestMissingInOrPattern()
    {
        await VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is 1 or int o$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestMissingInAndOrPattern()
    {
        await VerifyNotBuilderAsync("""
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is 1 or int and int o$$)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public async Task TestMissingInRecursiveOrPattern()
    {
        await VerifyNotBuilderAsync("""
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is null or { P: 1 } o$$)
                }
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/46927"), CombinatorialData]
    public async Task FirstArgumentOfInvocation_NoParameter(bool hasTypedChar)
    {
        await VerifyNotBuilderAsync($@"
using System;
interface Foo
{{
    bool Bar() => true;
}}
class P
{{
    void M(Foo f)
    {{
        f.Bar({(hasTypedChar ? "s" : "")}$$
    }}
}}");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/46927"), CombinatorialData]
    public async Task FirstArgumentOfInvocation_PossibleLambdaExpression(bool isLambda, bool hasTypedChar)
    {
        var overload = isLambda
            ? "bool Bar(Func<int, bool> predicate) => true;"
            : "bool Bar(int x) => true;";

        var markup = $@"
using System;
interface Foo
{{
    bool Bar() => true;
    {overload}
}}
class P
{{
    void M(Foo f)
    {{
        f.Bar({(hasTypedChar ? "s" : "")}$$
    }}
}}";
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
    public async Task FirstArgumentOfInvocation_WithOverloadAcceptEmptyArgumentList(string overloadParameterList)
    {
        await VerifyBuilderAsync($@"
using System;
interface Foo
{{
    bool Bar({overloadParameterList}) => true;
    bool Bar(Func<int, bool> predicate) => true;
}}
class P
{{
    void M(Foo f)
    {{
        f.Bar($$)
    }}
}}");
    }

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
