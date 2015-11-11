// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.LambdaSimplifier;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.LambdaSimplifier
{
    public class LambdaSimplifierTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new LambdaSimplifierCodeRefactoringProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixAll1()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); } void Bar(Func<int,string> f); string Quux(int i); }",
@"using System; class C { void Foo() { Bar(Quux); } void Bar(Func<int,string> f); string Quux(int i); }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixCoContravariance1()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); } void Bar(Func<object,string> f); string Quux(object o); }",
@"using System; class C { void Foo() { Bar(Quux); } void Bar(Func<object,string> f); string Quux(object o); }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixCoContravariance2()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); } void Bar(Func<string, object> f); string Quux(object o); }",
@"using System; class C { void Foo() { Bar(Quux); } void Bar(Func<string, object> f); string Quux(object o); }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixCoContravariance3()
        {
            await TestMissingAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); } void Bar(Func<string, string> f); object Quux(object o); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixCoContravariance4()
        {
            await TestMissingAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); } void Bar(Func<object, object> f); string Quux(string o); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixCoContravariance5()
        {
            await TestMissingAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); } void Bar(Func<object, string> f); object Quux(string o); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixAll2()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar((s1, s2) [||]=> Quux(s1, s2)); } void Bar(Func<int,bool,string> f); string Quux(int i, bool b); }",
@"using System; class C { void Foo() { Bar(Quux); } void Bar(Func<int,bool,string> f); string Quux(int i, bool b); }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixAll3()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar((s1, s2) [||]=> { return Quux(s1, s2); }); } void Bar(Func<int,bool,string> f); string Quux(int i, bool b); }",
@"using System; class C { void Foo() { Bar(Quux); } void Bar(Func<int,bool,string> f); string Quux(int i, bool b); }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixAll4()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar((s1, s2) [||]=> { return this.Quux(s1, s2); }); } void Bar(Func<int,bool,string> f); string Quux(int i, bool b); }",
@"using System; class C { void Foo() { Bar(this.Quux); } void Bar(Func<int,bool,string> f); string Quux(int i, bool b); }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestFixOneOrAll()
        {
            await TestAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); Bar(s => Quux(s)); } void Bar(Func<int,string> f); string Quux(int i); }",
@"using System; class C { void Foo() { Bar(Quux); Bar(s => Quux(s)); } void Bar(Func<int,string> f); string Quux(int i); }",
                index: 0);

            await TestAsync(
@"using System; class C { void Foo() { Bar(s [||]=> Quux(s)); Bar(s => Quux(s)); } void Bar(Func<int,string> f); string Quux(int i); }",
@"using System; class C { void Foo() { Bar(Quux); Bar(Quux); } void Bar(Func<int,string> f); string Quux(int i); }",
                index: 1);
        }

        [WorkItem(542562)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestMissingOnAmbiguity1()
        {
            await TestMissingAsync(
@"

using System;
class A
{
    static void Foo<T>(T x) where T : class { }
    static void Bar(Action<int> x) { }
    static void Bar(Action<string> x) { }
    static void Main()
    {
        Bar(x => [||]Foo(x));
    }
}");
        }

        [WorkItem(627092)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestMissingOnLambdaWithDynamic_1()
        {
            await TestMissingAsync(
@"using System;
 
class Program
{
    static void Main()
    {
        C<string>.InvokeFoo();
    }
}

class C<T>
{
    public static void InvokeFoo()
    {
        Action<dynamic, string> foo = (x, y) => [||]C<T>.Foo(x, y); // Simplify lambda expression
        foo(1, "");
    }

    static void Foo(object x, object y)
    {
        Console.WriteLine(""Foo(object x, object y)"");
    }

        static void Foo(object x, T y)
        {
            Console.WriteLine(""Foo(object x, T y)"");
        }
    }");
        }

        [WorkItem(627092)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestMissingOnLambdaWithDynamic_2()
        {
            await TestMissingAsync(
@"using System;
 
class Program
{
    static void Main()
    {
        C<string>.InvokeFoo();
    }
}

class Casd<T>
{
    public static void InvokeFoo()
    {
        Action<dynamic> foo = x => [||]Casd<T>.Foo(x); // Simplify lambda expression
        foo(1, "");
    }

    private static void Foo(dynamic x)
    {
        throw new NotImplementedException();
    }

    static void Foo(object x, object y)
    {
        Console.WriteLine(""Foo(object x, object y)"");
    }

        static void Foo(object x, T y)
        {
            Console.WriteLine(""Foo(object x, T y)"");
        }
    }");
        }

        [WorkItem(544625)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task ParenthesizeIfParseChanges()
        {
            var code = @"
using System;
class C
{
    static void M()
    {
        C x = new C();
        int y = 1;
        Bar(() [||]=> { return Console.ReadLine(); } < x, y > (1 + 2));
    }

    static void Bar(object a, object b) { }
    public static bool operator <(Func<string> y, C x) { return true; }
    public static bool operator >(Func<string> y, C x) { return true; }
}";

            var expected = @"
using System;
class C
{
    static void M()
    {
        C x = new C();
        int y = 1;
        Bar((Console.ReadLine) < x, y > (1 + 2));
    }

    static void Bar(object a, object b) { }
    public static bool operator <(Func<string> y, C x) { return true; }
    public static bool operator >(Func<string> y, C x) { return true; }
}";

            await TestAsync(code, expected, compareTokens: false);
        }

        [WorkItem(545856)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestWarningOnSideEffects()
        {
            await TestAsync(
@"using System ; class C { void Main ( ) { Func < string > a = ( ) [||]=> new C ( ) . ToString ( ) ; } } ",
@"using System ; class C { void Main ( ) { Func < string > a = {|Warning:new C ()|} . ToString ; } } ");
        }

        [WorkItem(545994)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsLambdaSimplifier)]
        public async Task TestNonReturnBlockSyntax()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( ) { Action a = [||]( ) => { Console . WriteLine ( ) ; } ; } } ",
@"using System ; class Program { static void Main ( ) { Action a = Console . WriteLine ; } } ");
        }
    }
}
