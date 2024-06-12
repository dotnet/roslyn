// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class SuggestionModeCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(CSharpSuggestionModeCompletionProvider);

        [Fact]
        public async Task AfterFirstExplicitArgument()
        {
            // The right-hand-side parses like a possible deconstruction or tuple type
            await VerifyBuilderAsync(AddInsideMethod(@"Func<int, int, int> f = (int x, i $$"));
        }

        [Fact]
        public async Task AfterFirstImplicitArgument()
        {
            // The right-hand-side parses like a possible deconstruction or tuple type
            await VerifyBuilderAsync(AddInsideMethod(@"Func<int, int, int> f = (x, i $$"));
        }

        [Fact]
        public async Task AfterFirstImplicitArgumentInMethodCall()
        {
            var markup = """
                class c
                {
                    private void bar(Func<int, int, bool> f) { }

                    private void goo()
                    {
                        bar((x, i $$
                    }
                }
                """;
            // The right-hand-side parses like a possible deconstruction or tuple type
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task AfterFirstExplicitArgumentInMethodCall()
        {
            var markup = """
                class c
                {
                    private void bar(Func<int, int, bool> f) { }

                    private void goo()
                    {
                        bar((int x, i $$
                    }
                }
                """;
            // Could be a deconstruction expression
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task DelegateTypeExpected1()
        {
            var markup = """
                using System;

                class c
                {
                    private void bar(Func<int, int, bool> f) { }

                    private void goo()
                    {
                        bar($$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task DelegateTypeExpected2()
            => await VerifyBuilderAsync(AddUsingDirectives("using System;", AddInsideMethod(@"Func<int, int, int> f = $$")));

        [Fact]
        public async Task ObjectInitializerDelegateType()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817145")]
        public async Task ExplicitArrayInitializer()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        Func<int, int>[] myfunc = new Func<int, int>[] { $$;
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task ImplicitArrayInitializerUnknownType()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        var a = new [] { $$;
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task ImplicitArrayInitializerKnownDelegateType()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        var a = new [] { x => 2 * x, $$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task TernaryOperatorUnknownType()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        var a = true ? $$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task TernaryOperatorKnownDelegateType1()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        var a = true ? x => x * 2 : $$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task TernaryOperatorKnownDelegateType2()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        Func<int, int> a = true ? $$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task OverloadTakesADelegate1()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task OverloadTakesDelegate2()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task ExplicitCastToDelegate()
        {
            var markup = """
                using System;

                class a
                {

                    void bar()
                    {
                        (Func<int, int>) ($$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860580")]
        public async Task ReturnStatement()
        {
            var markup = """
                using System;

                class a
                {
                    Func<int, int> bar()
                    {
                        return $$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task BuilderInAnonymousType1()
        {
            var markup = """
                using System;

                class a
                {
                    int bar()
                    {
                        var q = new {$$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task BuilderInAnonymousType2()
        {
            var markup = """
                using System;

                class a
                {
                    int bar()
                    {
                        var q = new {$$ 1, 2 };
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task BuilderInAnonymousType3()
        {
            var markup = """
                using System;
                class a
                {
                    int bar()
                    {
                        var q = new {Name = 1, $$ };
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
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
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$, e) => { });
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
        public async Task IncompleteParenthesizedLambdaArgument()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
        public async Task IncompleteNestedParenthesizedLambdaArgument()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.CancelKeyPress += new ConsoleCancelEventHandler(((a$$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task ParenthesizedExpressionInVarDeclaration()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = (a$$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
        public async Task TestInObjectCreation()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Program x = new P$$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
        public async Task TestInArrayCreation()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Program[] x = new $$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24432")]
        public async Task TestInArrayCreation2()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        Program[] x = new Pr$$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task TupleExpressionInVarDeclaration()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = (a$$, b)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task TupleExpressionInVarDeclaration2()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = (a, b$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task IncompleteLambdaInActionDeclaration()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        System.Action x = (a$$, b)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact]
        public async Task TupleWithNamesInActionDeclaration()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        System.Action x = (a$$, b: b)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task TupleWithNamesInActionDeclaration2()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        System.Action x = (a: a, b$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact]
        public async Task TupleWithNamesInVarDeclaration()
        {
            var markup = """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        var x = (a: a, b$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
        public async Task BuilderForLinqExpression()
        {
            var markup = """
                using System;
                using System.Linq.Expressions;

                public class Class
                {
                    public void Goo(Expression<Action<int>> arg)
                    {
                        Goo($$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
        public async Task NotInTypeParameter()
        {
            var markup = """
                using System;
                using System.Linq.Expressions;

                public class Class
                {
                    public void Goo(Expression<Action<int>> arg)
                    {
                        Enumerable.Empty<$$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611477")]
        public async Task ExtensionMethodFaultTolerance()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834609")]
        public async Task LambdaWithAutomaticBraceCompletion()
        {
            var markup = """
                using System;
                using System;

                public class Class
                {
                    public void Goo()
                    {
                        EventHandler h = (s$$)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        public async Task ThisConstructorInitializer()
        {
            var markup = """
                using System;
                class X 
                { 
                    X(Func<X> x) : this($$) { } 
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        public async Task BaseConstructorInitializer()
        {
            var markup = """
                using System;
                class B
                {
                    public B(Func<B> x) {}
                }

                class D : B 
                { 
                    D() : base($$) { } 
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/887842")]
        public async Task PreprocessorExpression()
        {
            var markup = """
                class C
                {
                #if $$
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/967254")]
        public async Task ImplicitArrayInitializerAfterNew()
        {
            var markup = """
                using System;

                class a
                {
                    void goo()
                    {
                        int[] a = new $$;
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceDeclaration_Unqualified()
        {
            var markup = @"namespace $$";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceDeclaration_Qualified()
        {
            var markup = @"namespace A.$$";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task FileScopedNamespaceDeclaration_Unqualified()
        {
            var markup = @"namespace $$;";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task FileScopedNamespaceDeclaration_Qualified()
        {
            var markup = @"namespace A.$$;";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task PartialClassName()
        {
            var markup = @"partial class $$";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task PartialStructName()
        {
            var markup = @"partial struct $$";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task PartialInterfaceName()
        {
            var markup = @"partial interface $$";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12818")]
        public async Task UnwrapParamsArray()
        {
            var markup = """
                using System;
                class C {
                    C(params Action<int>[] a) {
                        new C($$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12818")]
        public async Task DoNotUnwrapRegularArray()
        {
            var markup = """
                using System;
                class C {
                    C(Action<int>[] a) {
                        new C($$
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47662")]
        public async Task LambdaExpressionInImplicitObjectCreation()
        {
            var markup = """
                using System;
                class C {
                    C(Action<int> a) {
                        C c = new($$
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15443")]
        public async Task NotBuilderWhenDelegateInferredRightOfDotInInvocation()
        {
            var markup = """
                class C {
                	Action a = Task.$$
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15443")]
        public async Task NotBuilderInTypeArgument()
        {
            var markup = """
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
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16176")]
        public async Task NotBuilderForLambdaAfterNew()
        {
            var markup = """
                class C {
                	Action a = new $$
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20937")]
        public async Task AsyncLambda()
        {
            var markup = """
                using System;
                using System.Threading.Tasks;
                class Program
                {
                    public void B(Func<int, int, Task<int>> f) { }

                    void A()
                    {
                        B(async($$
                """;

            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20937")]
        public async Task AsyncLambdaAfterComma()
        {
            var markup = """
                using System;
                using System.Threading.Tasks;
                class Program
                {
                    public void B(Func<int, int, Task<int>> f) { }

                    void A()
                    {
                        B(async(p1, $$
                """;

            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod1()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod2()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod3()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod4()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod5()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod6()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithExtensionAndInstanceMethod7()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28586")]
        public async Task WithNonDelegateExtensionAndInstanceMethod1()
        {
            var markup = """
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
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInDeclarationPattern()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is int o$$)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInDeclarationPattern2()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is System.Collections.Generic.List<int> an$$)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInRecursivePattern()
        {
            var markup = """
                class C
                {
                    int P { get; }

                    void M(C test)
                    {
                        if (test is { P: 1 } o$$)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInPropertyPattern()
        {
            var markup = """
                class C
                {
                    int P { get; }

                    void M(C test)
                    {
                        if (test is { P: int o$$ })
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInAndPattern()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is 1 and int a$$)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInAndOrPattern()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is (int or 1) and int a$$)
                    }
                }
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInSwitchStatement()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestInSwitchExpression()
        {
            var markup = """
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
                """;
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestMissingInNotPattern_Declaration()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is not int o$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestMissingInNotPattern_Declaration2()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is not (1 and int o$$))
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestMissingInNotPattern_Recursive()
        {
            var markup = """
                class C
                {
                    int P { get; }

                    void M(C test)
                    {
                        if (test is not { P: 1 } o$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestMissingInOrPattern()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is 1 or int o$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestMissingInAndOrPattern()
        {
            var markup = """
                class C
                {
                    void M()
                    {
                        var e = new object();
                        if (e is 1 or int and int o$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
        public async Task TestMissingInRecursiveOrPattern()
        {
            var markup = """
                class C
                {
                    int P { get; }

                    void M(C test)
                    {
                        if (test is null or { P: 1 } o$$)
                    }
                }
                """;
            await VerifyNotBuilderAsync(markup);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/46927"), CombinatorialData]
        public async Task FirstArgumentOfInvocation_NoParameter(bool hasTypedChar)
        {
            var markup = $@"
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
}}";
            await VerifyNotBuilderAsync(markup);
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
            var markup = $@"
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
}}";
            await VerifyBuilderAsync(markup);
        }

        private async Task VerifyNotBuilderAsync(string markup)
            => await VerifyWorkerAsync(markup, isBuilder: false);

        private async Task VerifyBuilderAsync(string markup)
            => await VerifyWorkerAsync(markup, isBuilder: true);

        private async Task VerifyWorkerAsync(string markup, bool isBuilder)
        {
            MarkupTestFile.GetPosition(markup, out var code, out int position);

            using (var workspaceFixture = new CSharpTestWorkspaceFixture())
            {
                workspaceFixture.GetWorkspace(GetComposition());
                var document1 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular);
                await CheckResultsAsync(document1, position, isBuilder);

                if (await CanUseSpeculativeSemanticModelAsync(document1, position))
                {
                    var document2 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular, cleanBeforeUpdate: false);
                    await CheckResultsAsync(document2, position, isBuilder);
                }
            }
        }

        private async Task CheckResultsAsync(Document document, int position, bool isBuilder)
        {
            var triggerInfos = new List<CompletionTrigger>();
            triggerInfos.Add(CompletionTrigger.CreateInsertionTrigger('a'));
            triggerInfos.Add(CompletionTrigger.Invoke);
            triggerInfos.Add(CompletionTrigger.CreateDeletionTrigger('z'));

            var service = GetCompletionService(document.Project);
            var provider = Assert.Single(service.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet<string>.Empty));

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
}
