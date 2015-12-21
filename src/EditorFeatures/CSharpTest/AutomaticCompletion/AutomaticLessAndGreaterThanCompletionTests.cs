// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition.Hosting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticLessAndGreaterThanCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Creation()
        {
            using (var session = await CreateSessionAsync("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_TopLevel()
        {
            using (var session = await CreateSessionAsync("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_TopLevel2()
        {
            using (var session = await CreateSessionAsync("using System;$$"))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_TypeParameter()
        {
            var code = @"class C$$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_TypeParameter()
        {
            var code = @"class C
{
    void Method$$
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_TypeParameter_Delete()
        {
            var code = @"class C$$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_TypeParameter_Tab()
        {
            var code = @"class C$$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_TypeParameter_GreaterThan()
        {
            var code = @"class C$$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Multiple_Invalid()
        {
            var code = @"class C<$$>";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Multiple_Nested()
        {
            var code = @"class C<T>
{
    C<C$$>
}";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgument_Invalid()
        {
            var code = @"class C
{
    void Method()
    {
        var i = 1;
        var b = i $$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgument1()
        {
            var code = @"class C
{
    void Method()
    {
        var a = new List$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgument2()
        {
            var code = @"class C
{
    void Method()
    {
        var a = typeof(List$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WorkItem(531637)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeParameterReturnType()
        {
            var code = @"class C
{
    List$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(531637)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeParameterInDecl()
        {
            var code = @"class C
{
    void List$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "T");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(531637)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeParameterInDeclWithAsync()
        {
            var code = @"class C
{
    async Task$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(530864)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgumentWithUsing()
        {
            var code = @"using System.Collections.Generic;

class C
{
    void Test()
    {
        List$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(530864)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgumentNoUsing()
        {
            var code = @"class C
{
    void Test()
    {
        List$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WorkItem(1628, "https://github.com/dotnet/roslyn/issues/1628")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotInLessThanComparisonOperation()
        {
            var code = @"using System.Linq;
class C
{
    void Test(int[] args)
    {
        var a = args[0]$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WorkItem(1628, "https://github.com/dotnet/roslyn/issues/1628")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NotInLessThanComparisonOperationAfterConditionalAccessExpression()
        {
            var code = @"using System.Linq;
class C
{
    void Test(object[] args, object[] other)
    {
        var a = args?.First()$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session, expectValidSession: false);
            }
        }

        [WorkItem(1628, "https://github.com/dotnet/roslyn/issues/1628")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgumentInConditionalAccessExpressionSimple()
        {
            var code = @"using System.Linq;
class C
{
    void Test(object[] args)
    {
        args?.OfType$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WorkItem(1628, "https://github.com/dotnet/roslyn/issues/1628")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgumentInConditionalAccessExpressionNested()
        {
            var code = @"class C
{
    void Test()
    {
        Outer<int> t = new Outer<int>();
        t?.GetInner<int>()?.Method$$
    }
}
class Outer<T>
{
    public Inner<U> GetInner<U>()
    {
        return new Inner<U>();
    }
}
class Inner<V>
{
    public void Method<X>() { }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
                Type(session.Session, "int");
                CheckOverType(session.Session);
            }
        }

        [WorkItem(1628, "https://github.com/dotnet/roslyn/issues/1628")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgumentInConditionalAccessExpressionDeeplyNested()
        {
            var code = @"class C
{
    void Test()
    {
        new Outer1<int>()?.GetInner<int>()?.GetInner().DoSomething$$
    }
}
internal class Outer1<T>
{
    public Outer2<U> GetInner<U>()
    {
        return new Outer2<U>();
    }
}
internal class Outer2<U>
{
    public Outer2() { }
    public Inner GetInner()
    {
        return new Inner();
    }
}
internal class Inner
{
    public Inner() { }
    public void DoSomething<V>() { }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WorkItem(1628, "https://github.com/dotnet/roslyn/issues/1628")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task TypeArgumentInConditionalAccessExpressionWithLambdas()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    void Foo(object[] args)
    {
        var a = new Outer();
        a?.M(x => x?.ToString())?.Method$$
    }
}

public class Outer
{
    internal Inner M(Func<object, object> p)
    {
        throw new NotImplementedException();
    }
}

public class Inner
{
    public void Method<U>() { }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        internal async Task<Holder> CreateSessionAsync(string code)
        {
            return CreateSession(
                await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(code),
                BraceCompletionSessionProvider.LessAndGreaterThan.OpenCharacter, BraceCompletionSessionProvider.LessAndGreaterThan.CloseCharacter);
        }
    }
}
