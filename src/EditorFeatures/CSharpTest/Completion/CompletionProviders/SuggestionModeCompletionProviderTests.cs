﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.SuggestionMode;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class SuggestionModeCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public SuggestionModeCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new CSharpSuggestionModeCompletionProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterFirstExplicitArgument()
        {
            await VerifyNotBuilderAsync(AddInsideMethod(@"Func<int, int, int> f = (int x, i $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterFirstImplicitArgument()
        {
            await VerifyNotBuilderAsync(AddInsideMethod(@"Func<int, int, int> f = (x, i $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterFirstImplicitArgumentInMethodCall()
        {
            var markup = @"class c
{
    private void bar(Func<int, int, bool> f) { }
    
    private void foo()
    {
        bar((x, i $$
    }
}
";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterFirstExplicitArgumentInMethodCall()
        {
            var markup = @"class c
{
    private void bar(Func<int, int, bool> f) { }
    
    private void foo()
    {
        bar((int x, i $$
    }
}
";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DelegateTypeExpected1()
        {
            var markup = @"using System;

class c
{
    private void bar(Func<int, int, bool> f) { }
    
    private void foo()
    {
        bar($$
    }
}
";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DelegateTypeExpected2()
        {
            await VerifyBuilderAsync(AddUsingDirectives("using System;", AddInsideMethod(@"Func<int, int, int> f = $$")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectInitializerDelegateType()
        {
            var markup = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public Func<int> myfunc { get; set; }
}

class a
{
    void foo()
    {
        var b = new Program() { myfunc = $$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, WorkItem(817145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/817145"), Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitArrayInitializer()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        Func<int, int>[] myfunc = new Func<int, int>[] { $$;
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ImplicitArrayInitializerUnknownType()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        var a = new [] { $$;
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ImplicitArrayInitializerKnownDelegateType()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        var a = new [] { x => 2 * x, $$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TernaryOperatorUnknownType()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        var a = true ? $$
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TernaryOperatorKnownDelegateType1()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        var a = true ? x => x * 2 : $$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TernaryOperatorKnownDelegateType2()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        Func<int, int> a = true ? $$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OverloadTakesADelegate1()
        {
            var markup = @"using System;

class a
{
    void foo(int a) { }
    void foo(Func<int, int> a) { }

    void bar()
    {
        this.foo($$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OverloadTakesDelegate2()
        {
            var markup = @"using System;

class a
{
    void foo(int i, int a) { }
    void foo(int i, Func<int, int> a) { }

    void bar()
    {
        this.foo(1, $$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExplicitCastToDelegate()
        {
            var markup = @"using System;

class a
{

    void bar()
    {
        (Func<int, int>) ($$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(860580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860580")]
        public async Task ReturnStatement()
        {
            var markup = @"using System;

class a
{
    Func<int, int> bar()
    {
        return $$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BuilderInAnonymousType1()
        {
            var markup = @"using System;

class a
{
    int bar()
    {
        var q = new {$$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BuilderInAnonymousType2()
        {
            var markup = @"using System;

class a
{
    int bar()
    {
        var q = new {$$ 1, 2 };
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BuilderInAnonymousType3()
        {
            var markup = @"using System;
class a
{
    int bar()
    {
        var q = new {Name = 1, $$ };
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BuilderInFromClause()
        {
            var markup = @"using System;
using System.Linq;

class a
{
    int bar()
    {
        var q = from $$
    }
}";
            await VerifyBuilderAsync(markup.ToString());
        }

        [WorkItem(823968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/823968")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BuilderInJoinClause()
        {
            var markup = @"using System;
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
}";
            await VerifyBuilderAsync(markup.ToString());
        }

        [WorkItem(544290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544290")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParenthesizedLambdaArgument()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$, e) => { });
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(544379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IncompleteParenthesizedLambdaArgument()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler((a$$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(544379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544379")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IncompleteNestedParenthesizedLambdaArgument()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler(((a$$
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParenthesizedExpressionInVarDeclaration()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = (a$$
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleExpressionInVarDeclaration()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = (a$$, b)
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleExpressionInVarDeclaration2()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = (a, b$$)
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IncompleteLambdaInActionDeclaration()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        System.Action x = (a$$, b)
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleWithNamesInActionDeclaration()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        System.Action x = (a$$, b: b)
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleWithNamesInActionDeclaration2()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        System.Action x = (a: a, b$$)
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleWithNamesInVarDeclaration()
        {
            var markup = @"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = (a: a, b$$)
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [WorkItem(546363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BuilderForLinqExpression()
        {
            var markup = @"using System;
using System.Linq.Expressions;
 
public class Class
{
    public void Foo(Expression<Action<int>> arg)
    {
        Foo($$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(546363, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546363")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotInTypeParameter()
        {
            var markup = @"using System;
using System.Linq.Expressions;
 
public class Class
{
    public void Foo(Expression<Action<int>> arg)
    {
        Enumerable.Empty<$$
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        [WorkItem(611477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611477")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodFaultTolerance()
        {
            var markup = @"using System;
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
";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(834609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834609")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LambdaWithAutomaticBraceCompletion()
        {
            var markup = @"using System;
using System;
 
public class Class
{
    public void Foo()
    {
        EventHandler h = (s$$)
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(858112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThisConstructorInitializer()
        {
            var markup = @"using System;
class X 
{ 
    X(Func<X> x) : this($$) { } 
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(858112, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858112")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BaseConstructorInitializer()
        {
            var markup = @"using System;
class B
{
    public B(Func<B> x) {}
}

class D : B 
{ 
    D() : base($$) { } 
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(887842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/887842")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PreprocessorExpression()
        {
            var markup = @"class C
{
#if $$
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(967254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/967254")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ImplicitArrayInitializerAfterNew()
        {
            var markup = @"using System;

class a
{
    void foo()
    {
        int[] a = new $$;
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceDeclaration_Unqualified()
        {
            var markup = @"namespace $$";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceDeclaration_Qualified()
        {
            var markup = @"namespace A.$$";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialClassName()
        {
            var markup = @"partial class $$";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialStructName()
        {
            var markup = @"partial struct $$";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialInterfaceName()
        {
            var markup = @"partial interface $$";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(12818, "https://github.com/dotnet/roslyn/issues/12818")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UnwrapParamsArray()
        {
            var markup = @"
using System;
class C {
    C(params Action<int>[] a) {
        new C($$
    }
}";
            await VerifyBuilderAsync(markup);
        }

        [WorkItem(12818, "https://github.com/dotnet/roslyn/issues/12818")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotUnwrapRegularArray()
        {
            var markup = @"
using System;
class C {
    C(Action<int>[] a) {
        new C($$
    }
}";
            await VerifyNotBuilderAsync(markup);
        }

        private async Task VerifyNotBuilderAsync(string markup)
        {
            await VerifyWorkerAsync(markup, isBuilder: false);
        }

        private async Task VerifyBuilderAsync(string markup)
        {
            await VerifyWorkerAsync(markup, isBuilder: true);
        }

        private async Task VerifyWorkerAsync(string markup, bool isBuilder)
        {
            string code;
            int position;
            MarkupTestFile.GetPosition(markup, out code, out position);

            using (var workspaceFixture = new CSharpTestWorkspaceFixture())
            {
                var document1 = await workspaceFixture.UpdateDocumentAsync(code, SourceCodeKind.Regular);
                await CheckResultsAsync(document1, position, isBuilder);

                if (await CanUseSpeculativeSemanticModelAsync(document1, position))
                {
                    var document2 = await workspaceFixture.UpdateDocumentAsync(code, SourceCodeKind.Regular, cleanBeforeUpdate: false);
                    await CheckResultsAsync(document2, position, isBuilder);
                }
            }
        }

        private async Task CheckResultsAsync(Document document, int position, bool isBuilder)
        {
            var triggerInfo = CompletionTrigger.CreateInsertionTrigger('a');
            var service = GetCompletionService(document.Project.Solution.Workspace);
            var completionList = await service.GetContextAsync(
                service.ExclusiveProviders?[0], document, position, triggerInfo,
                options: null, cancellationToken: CancellationToken.None);

            if (isBuilder)
            {
                Assert.NotNull(completionList);
                Assert.NotNull(completionList.SuggestionModeItem);
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
