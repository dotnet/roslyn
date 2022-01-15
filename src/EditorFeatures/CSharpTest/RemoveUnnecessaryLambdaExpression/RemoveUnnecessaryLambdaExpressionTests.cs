// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryLambdaExpression
{
    using VerifyCS = CSharpCodeFixVerifier<
       CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer,
       CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider>;

    public class RemoveUnnecessaryLambdaExpressionTests
    {
        private static async Task TestInRegularAndScriptAsync(string testCode, string fixedCode, LanguageVersion version = LanguageVersion.Preview)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = version,
            }.RunAsync();
        }

        private static Task TestMissingInRegularAndScriptAsync(string testCode, LanguageVersion version = LanguageVersion.Preview)
            => TestInRegularAndScriptAsync(testCode, testCode, version);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestMissingInCSharp10()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar(s => Quux(s));
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}", LanguageVersion.CSharp10);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task Test1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|s => |]Quux(s));
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestNotOnStaticLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar(static s => Quux(s));
    }

    void Bar(Func<int, string> f) { }
    static string Quux(int i) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestNotOnConversionToObject()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        object o = (int s) => Quux(s));
    }

    void Bar(Func<int, string> f) { }
    static string Quux(int i) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestWithParenthesizedLambda()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|(int s) => |]Quux(s));
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestWithAnonymousMethod()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|delegate (int s) { return |]Quux(s); });
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<int, string> f) { }
    string Quux(int i) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestWithAnonymousMethodNoParameterList()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|delegate { return |]Quux(); });
    }

    void Bar(Func<string> f) { }
    string Quux() => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<string> f) { }
    string Quux() => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestFixCoContravariance1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|s => |]Quux(s));
    }

    void Bar(Func<object, string> f) { }
    string Quux(object o) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<object, string> f) { }
    string Quux(object o) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestFixCoContravariance2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|s => |]Quux(s));
    }

    void Bar(Func<string, object> f) { }
    string Quux(object o) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<string, object> f) { }
    string Quux(object o) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestFixCoContravariance3()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar(s => Quux({|CS1503:s|}));
    }

    void Bar(Func<string, string> f) { }
    object Quux(object o) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestFixCoContravariance4()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar(s => Quux(s));
    }

    void Bar(Func<object, object> f) { }
    string Quux(string o) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestFixCoContravariance5()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar(s => Quux(s));
    }

    void Bar(Func<object, string> f) { }
    object Quux(string o) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestTwoArgs()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|(s1, s2) => |]Quux(s1, s2));
    }

    void Bar(Func<int, bool, string> f) { }
    string Quux(int i, bool b) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<int, bool, string> f) { }
    string Quux(int i, bool b) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestReturnStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|(s1, s2) => {
            return |]Quux(s1, s2);
        });
    }

    void Bar(Func<int, bool, string> f) { }
    string Quux(int i, bool b) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(Quux);
    }

    void Bar(Func<int, bool, string> f) { }
    string Quux(int i, bool b) => default;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestReturnStatement2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo()
    {
        Bar([|(s1, s2) => {
            return |]this.Quux(s1, s2);
        });
    }

    void Bar(Func<int, bool, string> f) { }
    string Quux(int i, bool b) => default;
}",
@"using System;

class C
{
    void Goo()
    {
        Bar(this.Quux);
    }

    void Bar(Func<int, bool, string> f) { }
    string Quux(int i, bool b) => default;
}");
        }

        [WorkItem(542562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestMissingOnAmbiguity1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class A
{
    static void Goo<T>(T x)
    {
    }

    static void Bar(Action<int> x)
    {
    }

    static void Bar(Action<string> x)
    {
    }

    static void Main()
    {
        {|CS0121:Bar|}(x => Goo(x));
    }
}");
        }

        [WorkItem(542562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542562")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestWithConstraint()
        {
            var code = @"
using System;
class A
{
    static void Goo<T>(T x) where T : class
    {
    }

    static void Bar(Action<int> x)
    {
    }

    static void Bar(Action<string> x)
    {
    }

    static void Main()
    {
        Bar([|x => |]Goo(x));
    }
}";

            var expected = @"
using System;
class A
{
    static void Goo<T>(T x) where T : class
    {
    }

    static void Bar(Action<int> x)
    {
    }

    static void Bar(Action<string> x)
    {
    }

    static void Main()
    {
        Bar(Goo);
    }
}";
            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(627092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627092")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestMissingOnLambdaWithDynamic_1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        C<string>.InvokeGoo();
    }
}

class C<T>
{
    public static void InvokeGoo()
    {
        Action<dynamic, string> goo = (x, y) => [||]C<T>.Goo(x, y); // Simplify lambda expression
        goo(1, """");
    }

    static void Goo(object x, object y)
    {
        Console.WriteLine(""Goo(object x, object y)"");
    }

    static void Goo(object x, T y)
    {
        Console.WriteLine(""Goo(object x, T y)"");
    }
}");
        }

        [WorkItem(627092, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627092")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestMissingOnLambdaWithDynamic_2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        C<string>.InvokeGoo();
    }
}

class Casd<T>
{
    public static void InvokeGoo()
    {
        Action<dynamic> goo = x => [||]Casd<T>.Goo(x); // Simplify lambda expression
        goo(1, """");
    }

    private static void Goo(dynamic x)
    {
        throw new NotImplementedException();
    }

    static void Goo(object x, object y)
    {
        Console.WriteLine(""Goo(object x, object y)"");
    }

    static void Goo(object x, T y)
    {
        Console.WriteLine(""Goo(object x, T y)"");
    }
}");
        }

        [WorkItem(544625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544625")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
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
        Bar([|() => { return |]Console.ReadLine(); } < x, y > (1 + 2));
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

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WorkItem(545856, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545856")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestNotWithSideEffects()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Main()
    {
        Func<string> a = () => new C().ToString();
    }
}");
        }

        [WorkItem(545994, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545994")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryLambdaExpression)]
        public async Task TestExpressionStatement()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        Action a = [|() => {
            |]Console.WriteLine();
        };
    }
}",
@"using System;

class Program
{
    static void Main()
    {
        Action a = Console.WriteLine;
    }
}");
        }
    }
}
