// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryCast
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpRemoveUnnecessaryCastDiagnosticAnalyzer,
        CSharpRemoveUnnecessaryCastCodeFixProvider>;

    public class RemoveUnnecessaryCastTests
    {
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public void TestStandardProperty(AnalyzerProperty property)
            => VerifyCS.VerifyStandardProperty(property);

        [WorkItem(545979, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545979")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToErrorType()
        {
            var source =
@"class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in ({|CS0246:ErrorType|})s)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(5,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(5, 20, 5, 20),
                    // /0/Test0.cs(5,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(5, 22, 5, 22),
                },
                source);
        }

        [WorkItem(545137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545137"), WorkItem(870550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/870550")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame1()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    static void Main()
    {
        int x = 2;
        int i = 1;
        Goo(x < [|(int)|]i, x > (2 + 3));
    }
 
    static void Goo(bool a, bool b) { }
}",

            @"
class Program
{
    static void Main()
    {
        int x = 2;
        int i = 1;
        Goo(x < (i), x > (2 + 3));
    }
 
    static void Goo(bool a, bool b) { }
}");
        }

        [WorkItem(545146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545146")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame2()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
 
class C
{
    static void Main()
    {
        Action a = Console.WriteLine;
        ([|(Action)|]a)();
    }
}",

            @"
using System;
 
class C
{
    static void Main()
    {
        Action a = Console.WriteLine;
        a();
    }
}");
        }

        [WorkItem(545160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545160")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ParenthesizeToKeepParseTheSame3()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
 
class Program
{
    static void Main()
    {
        var x = (Decimal)[|(int)|]-1;
    }
}",

            @"
using System;
 
class Program
{
    static void Main()
    {
        var x = (Decimal)(-1);
    }
}");
        }

        [WorkItem(545138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545138")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveTypeParameterCastToObject()
        {
            var source =
@"class D
{
    void Goo<T>(T obj)
    {
        int x = (int)(object)obj;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545139, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545139")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastInIsTest()
        {
            var source =
@"using System;

class D
{
    static void Main()
    {
        DayOfWeek[] a = { };
        Console.WriteLine((object)a is int[]);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545142, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545142")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastNeedForUserDefinedOperator()
        {
            var source =
@"class A
{
    public static implicit operator A(string x)
    {
        return new A();
    }
}

class Program
{
    static void Main()
    {
        A x = (string)null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545142, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545142")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastNotNeededForUserDefinedOperator()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"class A
{
    public static implicit operator A(string x)
    {
        return new A();
    }
}

class Program
{
    static void Main()
    {
        A x = [|(string)|]"""";
    }
}", @"class A
{
    public static implicit operator A(string x)
    {
        return new A();
    }
}

class Program
{
    static void Main()
    {
        A x = """";
    }
}");
        }

        [WorkItem(545143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545143")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemovePointerCast1()
        {
            var source =
@"unsafe class C
{
    static unsafe void Main()
    {
        var x = (int)(int*)null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545144, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545144")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToObjectFromDelegateComparison()
        {
            // The cast below can't be removed because it would result in the Delegate
            // op_Equality operator overload being used over reference equality.

            var source =
@"using System;

class Program
{
    static void Main()
    {
        Action a = Console.WriteLine;
        Action b = Console.WriteLine;
        Console.WriteLine(a == (object)b);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545145")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToAnonymousMethodWhenOnLeftOfAsCast()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

class C
{
    static void Main()
    {
        var x = (Action)delegate {
        }

        [|as Action|];
    }
}",
@"using System;

class C
{
    static void Main()
    {
        var x = (Action)delegate {
        };
    }
}");
        }

        [WorkItem(545147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastInFloatingPointOperation()
        {
            var source =
@"class C
{
    static void Main()
    {
        int x = 1;
        double y = (double)x / 2;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545157")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution1()
        {
            var source =
@"using System;

class Program
{
    static void Main()
    {
        Goo(x => (int)x);
    }

    static void Goo(Func<int, object> x)
    {
    }

    static void Goo(Func<string, object> x)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution2()
        {
            var source =
@"using System;

class Program
{
    static void Main()
    {
        var x = (IComparable<int>)1;
        Goo(x);
    }

    static void Goo(IComparable<int> x)
    {
    }

    static void Goo(int x)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution3()
        {
            var source =
@"using System;

class Program
{
    static void Main()
    {
        var x = (IComparable<int>)1;
        var y = x;
        Goo(y);
    }

    static void Goo(IComparable<int> x)
    {
    }

    static void Goo(int x)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545747")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastWhichChangesTypeOfInferredLocal()
        {
            var source =
@"class C
{
    static void Main()
    {
        var x = (long)1;
        x = long.MaxValue;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545159, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545159")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNeededCastToIListOfObject()
        {
            var source =
@"using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        Action<object>[] x = {
        };
        Goo(x);
    }

    static void Goo<T>(Action<T>[] x)
    {
        var y = (IList<Action<object>>)(IList<object>)x;
        Console.WriteLine(y.Count);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545287"), WorkItem(880752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInParameterDefaultValue()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    static void M1(int? i1 = [|(int?)|]null)
    {
    }
}",

@"
class Program
{
    static void M1(int? i1 = null)
    {
    }
}");
        }

        [WorkItem(545289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545289")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInReturnStatement()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    static long M2()
    {
        return [|(long)|]5;
    }
}",

@"
class Program
{
    static long M2()
    {
        return 5;
    }
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda1()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => [|(long)|]5;
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => 5;
    }
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda2()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => { return [|(long)|]5; };
    }
}",

@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => { return 5; };
    }
}");
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda3()
        {
            var source =
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => { return [|(long)|]5; };
    }
}";
            var fixedSource =
@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => { return 5; };
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [WorkItem(545288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInLambda4()
        {
            var source =
            @"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => [|(long)|]5;
    }
}";
            var fixedSource =
@"
using System;
class Program
{
    static void M1()
    {
        Func<long> f1 = () => 5;
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression1()
        {
            var source =
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? [|(long)|]4 : [|(long)|]5;
    }
}";
            var fixedSource =
@"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? 4 : 5;
    }
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveUnneededCastInConditionalExpression3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? 4 : [|(long)|]5;
    }
}",
                @"
class Test
{
    public static void Main()
    {
        int b = 5;

        long f1 = (b == 5) ? 4 : 5;
    }
}");
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNeededCastInConditionalExpression()
        {
            var source =
@"class Test
{
    public static void Main()
    {
        int b = 5;
        var f1 = (b == 5) ? 4 : (long)5;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInConditionalExpression4()
        {
            var source =
            @"
class Test
{
    public static void Main()
    {
        int b = 5;

        var f1 = (b == 5) ? [|(long)|]4 : [|(long)|]5;
    }
}";
            var fixedSource =
@"
class Test
{
    public static void Main()
    {
        int b = 5;

        var f1 = (b == 5) ? (long)4 : 5;
    }
}";
            var batchFixedSource =
@"
class Test
{
    public static void Main()
    {
        int b = 5;

        var f1 = (b == 5) ? 4 : (long)5;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                BatchFixedCode = batchFixedSource,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                DiagnosticSelector = diagnostics => diagnostics[1],
            }.RunAsync();
        }

        [WorkItem(545459, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545459")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideADelegateConstructor()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
class Test
{
    delegate void D(int x);

    static void Main(string[] args)
    {
        var cd1 = new D([|(Action<int>)|]M1);
    }

    public static void M1(int i) { }
}",

@"
using System;
class Test
{
    delegate void D(int x);

    static void Main(string[] args)
    {
        var cd1 = new D(M1);
    }

    public static void M1(int i) { }
}");
        }

        [WorkItem(545419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545419")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveTriviaWhenRemovingCast()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
class Test
{
    public static void Main()
    {
        Func<Func<int>> f2 = () =>
        {
            return [|(Func<int>)|](/*Lambda returning int const*/() => 5 /*Const returned is 5*/);
        };
    }
}",

@"
using System;
class Test
{
    public static void Main()
    {
        Func<Func<int>> f2 = () =>
        {
            return /*Lambda returning int const*/() => 5 /*Const returned is 5*/;
        };
    }
}");
        }

        [WorkItem(545422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545422")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideCaseLabel()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case [|(long)|]5:
                break;
        }
    }
}",

@"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case 5:
                break;
        }
    }
}");
        }

        [WorkItem(545578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545578")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInsideGotoCaseStatement()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case 5:
                goto case [|(long)|]5;
                break;
        }
    }
}",

@"
class Test
{
    static void Main()
    {
        switch (5L)
        {
            case 5:
                goto case 5;
                break;
        }
    }
}");
        }

        [WorkItem(545595, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545595")]
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInCollectionInitializer()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var z = new List<long> { [|(long)0|] };
    }
}",

@"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var z = new List<long> { 0 };
    }
}");
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWhichInCollectionInitializer1()
        {
            var source =
@"using System;
using System.Collections.Generic;

class X : List<int>
{
    void Add(object x)
    {
        Console.WriteLine(1);
    }

    void Add(string x)
    {
        Console.WriteLine(2);
    }

    static void Main()
    {
        var z = new X { [|(object)""""|] };
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
        [WpfFact(Skip = "529787"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWhichInCollectionInitializer2()
        {
            var source =
@"using System;
using System.Collections.Generic;

class X : List<int>
{
    void Add(object x)
    {
        Console.WriteLine(1);
    }

    void Add(string x)
    {
        Console.WriteLine(2);
    }

    static void Main()
    {
        X z = new X { [|(object)""""|] };
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545607")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastInArrayInitializer()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class X
{
    static void Goo()
    {
        string x = "";
        var s = new object[] { [|(object)|]x };
    }
}",
                new[]
                {
                    // /0/Test0.cs(6,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(6, 20, 6, 20),
                    // /0/Test0.cs(6,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(6, 22, 6, 22),
                },
@"
class X
{
    static void Goo()
    {
        string x = "";
        var s = new object[] { x };
    }
}");
        }

        [WorkItem(545616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededCastWithOverloadedBinaryOperator()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
class MyAction
{
    static void Goo()
    {
        MyAction x = null;
        var y = x + [|(Action)|]delegate { };
    }
 
    public static MyAction operator +(MyAction x, Action y)
    {
        throw new NotImplementedException();
    }
}",

@"
using System;
class MyAction
{
    static void Goo()
    {
        MyAction x = null;
        var y = x + delegate { };
    }
 
    public static MyAction operator +(MyAction x, Action y)
    {
        throw new NotImplementedException();
    }
}");
        }

        [WorkItem(545616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromLambdaToDelegateWithVar1()
        {
            var source = @"
using System;
class MyAction
{
    static void Goo()
    {
        var y = (Action)(() => {});
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545616, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastFromLambdaToDelegateWithTypedVariable()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
class MyAction
{
    static void Goo()
    {
        Action y = [|(Action)|](() => { });
    }
}",
                @"
using System;
class MyAction
{
    static void Goo()
    {
        Action y = () => { };
    }
}");
        }

        [WorkItem(545822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545822")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastShouldInsertWhitespaceWhereNeededToKeepCorrectParsing()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
 
class Program
{
    static void Goo<T>()
    {
        Action a = null;
        var x = [|(Action)|](Goo<Guid>)==a;
    }
}",

@"
using System;
 
class Program
{
    static void Goo<T>()
    {
        Action a = null;
        var x = (Goo<Guid>) == a;
    }
}");
        }

        [WorkItem(545560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545560")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWithExplicitUserDefinedConversion()
        {
            var source =
@"using System;

class A
{
    public static explicit operator long(A x)
    {
        return 1;
    }

    public static implicit operator int(A x)
    {
        return 2;
    }

    static void Main()
    {
        var a = new A();
        long x = (long)a;
        long y = a;
        Console.WriteLine(x); // 1
        Console.WriteLine(y); // 2
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545608, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545608")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWithImplicitUserDefinedConversion()
        {
            var source =
@"class X
{
    static void Goo()
    {
        X x = null;
        object y = (string)x;
    }

    public static implicit operator string(X x)
    {
        return "";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(11,16): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(11, 16, 11, 16),
                    // /0/Test0.cs(11,18): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(11, 18, 11, 18),
                },
                source);
        }

        [WorkItem(545941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWithImplicitConversionInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            var source =
@"using System;

class E
{
    public static implicit operator Exception(E e)
    {
        return new Exception();
    }

    static void Main()
    {
        throw (Exception)new E();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545981")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInThrow()
        {
            // The cast below can't be removed because the throw statement expects
            // an expression of type Exception -- not an expression convertible to
            // Exception.

            var source =
@"using System;

class C
{
    static void Main()
    {
        object ex = new Exception();
        throw (Exception)ex;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInThrow()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;

class E
{
    static void Main()
    {
        throw [|(Exception)|]new Exception();
    }
}
",

@"
using System;

class E
{
    static void Main()
    {
        throw new Exception();
    }
}
");
        }

        [WorkItem(545945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545945")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryDowncast()
        {
            var source =
@"class C
{
    void Goo(object y)
    {
        int x = (int)y;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545591")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWithinLambda()
        {
            var source =
@"using System;

class Program
{
    static void Main()
    {
        Boo(x => Goo(x, y => (int)x), null);
    }

    static void Boo(Action<int> x, object y)
    {
        Console.WriteLine(1);
    }

    static void Boo(Action<string> x, string y)
    {
        Console.WriteLine(2);
    }

    static void Goo(int x, Func<int, int> y)
    {
    }

    static void Goo(string x, Func<string, string> y)
    {
    }

    static void Goo(string x, Func<int, int> y)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545606, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545606")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastFromNullToTypeParameter()
        {
            var source =
@"class X
{
    static void Goo<T, S>() where T : class, S
    {
        S y = (T)null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545744, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInImplicitlyTypedArray()
        {
            var source =
@"class X
{
    static void Goo()
    {
        string x = "";
        var s = new[] { (object)x };
        s[0] = 1;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(5,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(5, 20, 5, 20),
                    // /0/Test0.cs(5,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(5, 22, 5, 22),
                },
                source);
        }

        [WorkItem(545750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545750")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToBaseType()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class X
{
    static void Main()
    {
        var s = ([|(object)|]new X()).ToString();
    }

    public override string ToString()
    {
        return "";
    }
}",
                new[]
                {
                    // /0/Test0.cs(11,16): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(11, 16, 11, 16),
                    // /0/Test0.cs(11,18): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(11, 18, 11, 18),
                },
@"
class X
{
    static void Main()
    {
        var s = new X().ToString();
    }

    public override string ToString()
    {
        return "";
    }
}");
        }

        [WorkItem(545855, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545855")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryLambdaToDelegateCast()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
using System.Collections.Generic;
using System.Reflection;

static class Program
{
    static void Main()
    {
        FieldInfo[] fields = typeof(Exception).GetFields();
        Console.WriteLine(fields.Any([|(Func<FieldInfo, bool>)|](field => field.IsStatic)));
    }

    static bool Any<T>(this IEnumerable<T> s, Func<T, bool> predicate)
    {
        return false;
    }

    static bool Any<T>(this ICollection<T> s, Func<T, bool> predicate)
    {
        return true;
    }
}
",

@"
using System;
using System.Collections.Generic;
using System.Reflection;

static class Program
{
    static void Main()
    {
        FieldInfo[] fields = typeof(Exception).GetFields();
        Console.WriteLine(fields.Any(field => field.IsStatic));
    }

    static bool Any<T>(this IEnumerable<T> s, Func<T, bool> predicate)
    {
        return false;
    }

    static bool Any<T>(this ICollection<T> s, Func<T, bool> predicate)
    {
        return true;
    }
}
");
        }

        [WorkItem(529816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInQueryExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select [|(long)|]0);
    }
}",

@"
using System;

class A
{
    int Select(Func<int, long> x) { return 1; }

    static void Main()
    {
        Console.WriteLine(from y in new A() select 0);
    }
}");
        }

        [WorkItem(529816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInQueryExpression()
        {
            var source =
@"using System;

class A
{
    int Select(Func<int, long> x)
    {
        return 1;
    }

    int Select(Func<int, int> x)
    {
        return 2;
    }

    static void Main()
    {
        Console.WriteLine(from y in new A()
                          select (long)0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545848, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545848")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConstructorInitializer()
        {
            var source =
@"using System;

class C
{
    static void Goo(int x, Func<int, int> y)
    {
    }

    static void Goo(string x, Func<string, string> y)
    {
    }

    C(Action<int> x, object y)
    {
        Console.WriteLine(1);
    }

    C(Action<string> x, string y)
    {
        Console.WriteLine(2);
    }

    C() : this(x => Goo(x, y => (int)x), null)
    {
    }

    static void Main()
    {
        new C();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastFromTypeParameterToInterface()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

interface IIncrementable
{
    int Value { get; }

    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }
}

class C : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }
}

static class Program
{
    static void Main()
    {
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y)
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        ([|(IIncrementable)|]y).Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}",
@"using System;

interface IIncrementable
{
    int Value { get; }

    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }
}

class C : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }
}

static class Program
{
    static void Main()
    {
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y)
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        y.Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}");
        }

        [WorkItem(529831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastFromTypeParameterToInterface()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;

interface IIncrementable
{
    int Value { get; }
    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

class C: IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

static class Program
{
    static void Main()
    {
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y) 
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        ([|(IIncrementable)|]y).Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}
",
 @"
using System;

interface IIncrementable
{
    int Value { get; }
    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

class C: IIncrementable
{
    public int Value { get; private set; }
    public void Increment() { Value++; }
}

static class Program
{
    static void Main()
    {
        Goo(new S(), new C());
    }

    static void Goo<TAny, TClass>(TAny x, TClass y) 
        where TAny : IIncrementable
        where TClass : class, IIncrementable
    {
        ((IIncrementable)x).Increment(); // False Unnecessary Cast
        y.Increment(); // Unnecessary Cast - OK

        Console.WriteLine(x.Value);
        Console.WriteLine(y.Value);
    }
}
");
        }

        [WorkItem(545877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545877")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotCrashOnIncompleteMethodDeclaration()
        {
            // This test has intentional syntax errors
            var source =
@"using System;

class A
{
    static void Main()
    {
        byte{|CS1001:|}{|CS1002:|}
        {|CS0411:Goo|}(x => 1, (byte)1);
    }

    static void Goo<T, S>(T x, {|CS1001:{|CS1031:)|}|}
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(46423, "https://github.com/dotnet/roslyn/issues/46423")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnneededTargetTypedCast()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
class Other
{
    public short GetScopeIdForTelemetry(FixAllScope scope)
        => [|(short)|](scope switch
        {
            FixAllScope.Document => 1,
            FixAllScope.Project => 2,
            FixAllScope.Solution => 3,
            _ => 4,
        });

    public enum FixAllScope
    {
        Document,
        Project,
        Solution,
        Other
    }
}",
@"
class Other
{
    public short GetScopeIdForTelemetry(FixAllScope scope)
        => scope switch
        {
            FixAllScope.Document => 1,
            FixAllScope.Project => 2,
            FixAllScope.Solution => 3,
            _ => 4,
        };

    public enum FixAllScope
    {
        Document,
        Project,
        Solution,
        Other
    }
}"
);
        }

        [WorkItem(545777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545777")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveImportantTrailingTrivia()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    static void Main()
    {
        long x =
#if true
            [|(long)|] // Remove Unnecessary Cast
#endif
            1;
    }
}
",

            @"
class Program
{
    static void Main()
    {
        long x =
#if true
            // Remove Unnecessary Cast
#endif
            1;
    }
}
");
        }

        [WorkItem(529791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529791")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToNullable1()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class X
{
    static void Goo()
    {
        object x = [|(string)|]null;
        object y = [|(int?)|]null;
    }
}
",

            @"
class X
{
    static void Goo()
    {
        object x = null;
        object y = null;
    }
}
");
        }

        [WorkItem(545842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastToNullable2()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + [|(long?)|] y;
    }
}
",

            @"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + y;
    }
}
");
        }

        [WorkItem(545850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545850")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveSurroundingParentheses()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    static void Main()
    {
        int x = 1;
        ([|(int)|]x).ToString();
    }
}
",

            @"
class Program
{
    static void Main()
    {
        int x = 1;
        x.ToString();
    }
}
");
        }

        [WorkItem(529846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastFromTypeParameterToObject()
        {
            var source =
@"class C
{
    static void Goo<T>(T x, object y)
    {
        if ((object)x == y)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545858, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545858")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastFromDelegateTypeToMulticastDelegate()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        Action x = Console.WriteLine;
        Action y = Console.WriteLine;
        Console.WriteLine((MulticastDelegate)x == y);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545857, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545857")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInSizeOfArrayCreationExpression1()
        {
            // The cast below can't be removed because it would result in the implicit
            // conversion to int being called instead.

            var source =
@"using System;

class C
{
    static void Main()
    {
        Console.WriteLine(new int[(long)default(C)].Length);
    }

    public static implicit operator long(C x)
    {
        return 1;
    }

    public static implicit operator int(C x)
    {
        return 2;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInSizeOfArrayCreationExpression2()
        {
            // Array bounds must be an int, so the cast below can't be removed.

            var source =
@"class C
{
    static void Main()
    {
        var a = new int[(int)decimal.Zero];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529842")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInTernaryExpression()
        {
            var source =
@"using System;

class X
{
    public static implicit operator string(X x)
    {
        return x.ToString();
    }

    static void Main()
    {
        bool b = true;
        X x = new X();
        Console.WriteLine(b ? (string)null : x);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882"), WorkItem(880752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInConstructorInitializer1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    C(int x) { }
    C() : this([|(int)|]1) { }
}
",

@"
class C
{
    C(int x) { }
    C() : this(1) { }
}
");
        }

        [WorkItem(545958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545958"), WorkItem(880752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInConstructorInitializer2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System.Collections;

class C
{
    C(int x) { }
    C(object x) { }
    C() : this([|(IEnumerable)|]"""") { }
}
",

@"
using System.Collections;

class C
{
    C(int x) { }
    C(object x) { }
    C() : this("""") { }
}
");
        }

        [WorkItem(545957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545957")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastInConstructorInitializer3()
        {
            var source =
@"class C
{
    C(int x)
    {
    }

    C() : this((long)1)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(7,16): error CS1503: Argument 1: cannot convert from 'long' to 'int'
                DiagnosticResult.CompilerError("CS1503").WithSpan(7, 16, 7, 23).WithArguments("1", "long", "int"),
                source);
        }

        [WorkItem(545842, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToNullableInArithmeticExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + [|(long?)|]y;
    }
}
",

@"
static class C
{
    static void Main()
    {
        int? x = 1;
        long y = 2;
        long? z = x + y;
    }
}
");
        }

        [WorkItem(545942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545942")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromValueTypeToObjectInReferenceEquality()
        {
            // Note: The cast below can't be removed because it would result in an
            // illegal reference equality test between object and a value type.

            var source =
@"using System;

class Program
{
    static void Main()
    {
        object x = 1;
        Console.WriteLine(x == (object)1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545962, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545962")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastWhenExpressionDoesntBind()
        {
            // Note: The cast below can't be removed because its expression doesn't bind.

            var source =
@"using System;

class Program
{
    static void Main()
    {
        ((IDisposable){|CS0103:x|}).Dispose();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545944")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastBeforePointerDereference1()
        {
            // Note: The cast below can't be removed because it would result in *null,
            // which is illegal.

            var source =
@"unsafe class C
{
    int x = *(int*)null;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545978")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastBeforePointerDereference2()
        {
            // Note: The cast below can't be removed because it would result in dereferencing
            // void*, which is illegal.

            var source =
@"unsafe class C
{
    static void Main()
    {
        void* p = null;
        int x = *(int*)p;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastBeforePointerDereference3()
        {
            // Conservatively disable cast simplifications for casts involving pointer conversions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            var source =
@"class C
{
    public unsafe float ReadSingle(byte* ptr)
    {
        return *(float*)ptr;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNumericCastInUncheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            var source =
@"class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        if (unchecked((uint)byteCount) > (_endPointer - _currentPointer))
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNumericCastInUncheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            var source =
@"class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        unchecked
        {
            if (((uint)byteCount) > (_endPointer - _currentPointer))
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNumericCastInCheckedExpression()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            var source =
@"class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        if (checked((uint)byteCount) > (_endPointer - _currentPointer))
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(2691, "https://github.com/dotnet/roslyn/issues/2691")]
        [WorkItem(2987, "https://github.com/dotnet/roslyn/issues/2987")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNumericCastInCheckedStatement()
        {
            // Conservatively disable cast simplifications within explicit checked/unchecked statements.
            // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

            var source =
@"class C
{
    private unsafe readonly byte* _endPointer;
    private unsafe byte* _currentPointer;

    private unsafe void CheckBounds(int byteCount)
    {
        checked
        {
            if (((uint)byteCount) > (_endPointer - _currentPointer))
            {
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(26640, "https://github.com/dotnet/roslyn/issues/26640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToByteFromIntInConditionalExpression()
        {
            var source = @"class C
{
    object M1(bool b)
    {
        return b ? (byte)1 : (byte)0;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(26640, "https://github.com/dotnet/roslyn/issues/26640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToDoubleFromIntWithTwoInConditionalExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    object M1(bool b)
    {
        return b ? [|(double)|]1 : [|(double)|]0;
    }
}",
@"class C
{
    object M1(bool b)
    {
        return b ? 1 : (double)0;
    }
}");
        }

        [WorkItem(26640, "https://github.com/dotnet/roslyn/issues/26640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToDoubleFromIntInConditionalExpression()
        {
            var source =
@"class C
{
    object M1(bool b)
    {
        return b ? 1 : (double)0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(26640, "https://github.com/dotnet/roslyn/issues/26640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToUIntFromCharInConditionalExpression()
        {
            var source =
@"class C
{
    object M1(bool b)
    {
        return b ? '1' : (uint)'0';
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(26640, "https://github.com/dotnet/roslyn/issues/26640")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryNumericCastToSameTypeInConditionalExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    object M1(bool b)
    {
        return b ? [|(int)|]1 : 0;
    }
}",
@"class C
{
    object M1(bool b)
    {
        return b ? 1 : 0;
    }
}");
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInAttribute()
        {
            var source =
@"using System;

[A((byte)0)]
class A : Attribute
{
    public A(object x)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInAttribute()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"using System;

[A([|(int)|]0)]
class A : Attribute
{
    public A(object x)
    {
    }
}",
                @"using System;

[A(0)]
class A : Attribute
{
    public A(object x)
    {
    }
}");
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveImplicitConstantConversionToDifferentType()
        {
            var source =
@"using System;

class A : Attribute
{
    public A()
    {
        object x = (byte)0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveImplicitConstantConversionToSameType()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"using System;

class A : Attribute
{
    public A()
    {
        object x = [|(int)|]0;
    }
}",
                @"using System;

class A : Attribute
{
    public A()
    {
        object x = 0;
    }
}");
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNumericConversionBoxed()
        {
            var source =
@"using System;

class A : Attribute
{
    public A(int i)
    {
        object x = (long)i;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNumericConversionNotBoxed()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"using System;

class A : Attribute
{
    public A(int i)
    {
        long x = [|(long)|]i;
    }
}",
                @"using System;

class A : Attribute
{
    public A(int i)
    {
        long x = i;
    }
}");
        }

        [WorkItem(545894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNonConstantCastInAttribute()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"using System;

[A({|CS0182:[|(IComparable)|]0|})]
class A : Attribute
{
    public A(object x)
    {
    }
}",
                @"using System;

[A(0)]
class A : Attribute
{
    public A(object x)
    {
    }
}");
        }

        [WorkItem(39042, "https://github.com/dotnet/roslyn/issues/39042")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastForImplicitNumericCastsThatLoseInformation()
        {
            var source =
@"using System;

class A
{
    public A(long x)
    {
        long y = (long)(double)x;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        #region Interface Casts

        [WorkItem(545889, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545889")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForUnsealedType()
        {
            // Note: The cast below can't be removed because X is not sealed.

            var source =
@"using System;

class X : IDisposable
{
    static void Main()
    {
        X x = new Y();
        ((IDisposable)x).Dispose();
    }

    public void Dispose()
    {
        Console.WriteLine(""X.Dispose"");
    }
}

class Y : X, IDisposable
{
    void IDisposable.Dispose()
    {
        Console.WriteLine(""Y.Dispose"");
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceForSealedType1()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)|]new C()).Goo();
    }
}
",
@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo();
    }
}
");
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceForSealedType2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(([|(I)|]new C()).Goo);
    }
}
",
                @"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    static void Main()
    {
        Console.WriteLine(new C().Goo);
    }
}
");
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceForSealedType3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public C Instance { get { return new C(); } }

    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    void Main()
    {
        Console.WriteLine(([|(I)|]Instance).Goo);
    }
}
",
                @"
using System;

interface I
{
    string Goo { get; }
}

sealed class C : I
{
    public C Instance { get { return new C(); } }

    public string Goo
    {
        get
        {
            return ""Nikov Rules"";
        }
    }

    void Main()
    {
        Console.WriteLine(Instance.Goo);
    }
}
");
        }

        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType4()
        {
            var source =
@"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ((I)new C()).Goo();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [WorkItem(545890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceForSealedTypeWhenParameterValuesDifferButExplicitValueIsProvided()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)|]new C()).Goo(2);
    }
}
",
@"
using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo(2);
    }
}
");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType6()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            var source =
@"using System;

interface I
{
    void Goo(int x = 0, int y = 0);
}

sealed class C : I
{
    public void Goo(int y = 0, int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ((I)new C()).Goo(x: 1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceWhenNoDefaultArgsPassedAndValuesAreTheSame()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)|]new C()).Goo();
    }
}",
@"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 0)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo();
    }
}");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceWhenDefaultArgPassedAndValuesAreDifferent()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)|]new C()).Goo(2);
    }
}",
@"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo(2);
    }
}");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceWhenNoDefaultArgsPassedAndValuesAreDifferent()
        {
            var source = @"using System;

interface I
{
    void Goo(int x = 0);
}

sealed class C : I
{
    public void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ((I)new C()).Goo();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceWhenNamesAreTheSameAndNoNameProvided()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

interface I
{
    void Goo(int x);
}

sealed class C : I
{
    public void Goo(int x)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        ([|(I)|]new C()).Goo(0);
    }
}",
@"using System;

interface I
{
    void Goo(int x);
}

sealed class C : I
{
    public void Goo(int x)
    {
        Console.WriteLine(x);
    }

    static void Main()
    {
        new C().Goo(0);
    }
}");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastToInterfaceWhenNamesAreDifferentAndNoNameProvided()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"using System;

interface I
{
    void Goo(int x);
}

sealed class C : I
{
    public void Goo(int y)
    {
        Console.WriteLine(y);
    }

    static void Main()
    {
        ([|(I)|]new C()).Goo(0);
    }
}",
@"using System;

interface I
{
    void Goo(int x);
}

sealed class C : I
{
    public void Goo(int y)
    {
        Console.WriteLine(y);
    }

    static void Main()
    {
        new C().Goo(0);
    }
}");
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceWhenNamesAreDifferentAndNameProvided()
        {
            var source = @"using System;

interface I
{
    void Goo(int x);
}

sealed class C : I
{
    public void Goo(int y)
    {
        Console.WriteLine(y);
    }

    static void Main()
    {
        ((I)new C()).Goo(x: 0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType7()
        {
            var source =
@"
using System;

interface I
{
    int this[int x = 0, int y = 0] { get; }
}

sealed class C : I
{
    public int this[int x = 0, int y = 0]
    {
        get
        {
            return x * 2;
        }
    }

    static void Main()
    {
        Console.WriteLine(((I)new C())[x: 1]);
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType8()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because the specified named arguments refer to parameters that
            // appear at different positions in the member signatures.

            var source =
@"using System;

interface I
{
    int this[int x = 0, int y = 0] { get; }
}

sealed class C : I
{
    public int this(int y = 0, int x = 0)
    {
        get
        {
            return x * 2;
        }
    }

    static void Main()
    {
        Console.WriteLine(((I)new C())[x: 1]);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(8,18): error CS0535: 'C' does not implement interface member 'I.this[int, int]'
                    DiagnosticResult.CompilerError("CS0535").WithSpan(8, 18, 8, 19).WithArguments("C", "I.this[int, int]"),
                    // /0/Test0.cs(10,16): error CS0548: 'C.this[(int y, ?), int]': property or indexer must have at least one accessor
                    DiagnosticResult.CompilerError("CS0548").WithSpan(10, 16, 10, 20).WithArguments("C.this[(int y, ?), int]"),
                    // /0/Test0.cs(10,20): error CS1003: Syntax error, '[' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 20, 10, 21).WithArguments("[", "("),
                    // /0/Test0.cs(10,27): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type '(int y, ?)'
                    DiagnosticResult.CompilerError("CS1750").WithSpan(10, 27, 10, 27).WithArguments("int", "(int y, ?)"),
                    // /0/Test0.cs(10,27): error CS1001: Identifier expected
                    DiagnosticResult.CompilerError("CS1001").WithSpan(10, 27, 10, 28),
                    // /0/Test0.cs(10,27): error CS1026: ) expected
                    DiagnosticResult.CompilerError("CS1026").WithSpan(10, 27, 10, 28),
                    // /0/Test0.cs(10,27): error CS8124: Tuple must contain at least two elements.
                    DiagnosticResult.CompilerError("CS8124").WithSpan(10, 27, 10, 28),
                    // /0/Test0.cs(10,41): error CS1003: Syntax error, ']' expected
                    DiagnosticResult.CompilerError("CS1003").WithSpan(10, 41, 10, 42).WithArguments("]", ")"),
                    // /0/Test0.cs(10,41): error CS1014: A get or set accessor expected
                    DiagnosticResult.CompilerError("CS1014").WithSpan(10, 41, 10, 42),
                    // /0/Test0.cs(10,41): error CS1514: { expected
                    DiagnosticResult.CompilerError("CS1514").WithSpan(10, 41, 10, 42),
                    // /0/Test0.cs(10,42): error CS1014: A get or set accessor expected
                    DiagnosticResult.CompilerError("CS1014").WithSpan(10, 42, 10, 42),
                    // /0/Test0.cs(12,12): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(12, 12, 12, 12),
                    // /0/Test0.cs(16,6): error CS1513: } expected
                    DiagnosticResult.CompilerError("CS1513").WithSpan(16, 6, 16, 6),
                },
                source);
        }

        [WorkItem(545883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545883")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForSealedType9()
        {
            // Note: The cast below can't be removed (even though C is sealed)
            // because it would result in binding to a Dispose method that doesn't
            // implement IDisposable.Dispose().

            var source =
@"using System;
using System.IO;

sealed class C : MemoryStream
{
    static void Main()
    {
        C s = new C();
        ((IDisposable)s).Dispose();
    }

    new public void Dispose()
    {
        Console.WriteLine(""new Dispose()"");
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545887")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToInterfaceForStruct1()
        {
            // Note: The cast below can't be removed because the cast boxes 's' and
            // unboxing would change program behavior.

            var source =
@"using System;

interface IIncrementable
{
    int Value { get; }

    void Increment();
}

struct S : IIncrementable
{
    public int Value { get; private set; }

    public void Increment()
    {
        Value++;
    }

    static void Main()
    {
        var s = new S();
        ((IIncrementable)s).Increment();
        Console.WriteLine(s.Value);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [WorkItem(545834, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545834")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForStruct2()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        ([|(IDisposable)|]GetEnumerator()).Dispose();
    }

    static List<int>.Enumerator GetEnumerator()
    {
        var x = new List<int> { 1, 2, 3 };
        return x.GetEnumerator();
    }
}
",
@"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        GetEnumerator().Dispose();
    }

    static List<int>.Enumerator GetEnumerator()
    {
        var x = new List<int> { 1, 2, 3 };
        return x.GetEnumerator();
    }
}
");
        }

        [WorkItem(544655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544655")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToICloneableForDelegate()
        {
            // Note: The cast below can be removed because delegates are implicitly
            // sealed.

            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;

class C
{
    static void Main()
    {
        Action a = () => { };
        var c = ([|(ICloneable)|]a).Clone();
    }
}
",

@"
using System;

class C
{
    static void Main()
    {
        Action a = () => { };
        var c = a.Clone();
    }
}
");
        }

        [WorkItem(545926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545926")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToICloneableForArray()
        {
            // Note: The cast below can be removed because arrays are implicitly
            // sealed.

            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;

class C
{
    static void Main()
    {
        var a = new[] { 1, 2, 3 };
        var c = ([|(ICloneable)|]a).Clone(); 
    }
}
",

@"
using System;

class C
{
    static void Main()
    {
        var a = new[] { 1, 2, 3 };
        var c = a.Clone(); 
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToInterfaceForString()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;
using System.Collections.Generic;

class C
{
    static void Main(string s)
    {
        IEnumerable<char> i = [|(IEnumerable<char>)|]s;
    }
}
",

@"
using System;
using System.Collections.Generic;

class C
{
    static void Main(string s)
    {
        IEnumerable<char> i = s;
    }
}
");
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleForEnum()
        {
            // Note: The cast below can be removed because enums are implicitly
            // sealed.

            await VerifyCS.VerifyCodeFixAsync(
            @"
using System;

class Program
{
    static void Main()
    {
        Enum e = DayOfWeek.Monday;
        var y = ([|(IConvertible)|]e).GetTypeCode();
    }
}
",

@"
using System;

class Program
{
    static void Main()
    {
        Enum e = DayOfWeek.Monday;
        var y = e.GetTypeCode();
    }
}
");
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepCastToIConvertibleOnNonCopiedStruct()
        {
            var source = @"
using System;

class Program
{
    static void Main(DateTime dt)
    {
        var y = ((IConvertible)dt).GetTypeCode();
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleOnCopiedStruct1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class Program
{
    static void Main()
    {
        var y = ([|(IConvertible)|](DateTime.Now + TimeSpan.Zero)).GetTypeCode();
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        var y = (DateTime.Now + TimeSpan.Zero).GetTypeCode();
    }
}
");
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepCastToIConvertibleOnByRefIndexer()
        {
            var source = @"
using System;

class Program
{
    ref DateTime this[int i] => ref this[i];

    void Main()
    {
        var y = ((IConvertible)this[0]).GetTypeCode();
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleOnIndexer()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class Program
{
    DateTime this[int i] => default;

    void Main()
    {
        var y = ([|(IConvertible)|]this[0]).GetTypeCode();
    }
}
",
                @"
using System;

class Program
{
    DateTime this[int i] => default;

    void Main()
    {
        var y = this[0].GetTypeCode();
    }
}
");
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepCastToIConvertibleOnByRefProperty()
        {
            var source = @"
using System;

class Program
{
    ref DateTime X => ref X;

    void Main()
    {
        var y = ((IConvertible)X).GetTypeCode();
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleOnProperty()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class Program
{
    DateTime X => default;

    void Main()
    {
        var y = ([|(IConvertible)|]X).GetTypeCode();
    }
}
",
                @"
using System;

class Program
{
    DateTime X => default;

    void Main()
    {
        var y = X.GetTypeCode();
    }
}
");
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepCastToIConvertibleOnByRefMethod()
        {
            var source = @"
using System;

class Program
{
    ref DateTime X() => ref X();

    void Main()
    {
        var y = ((IConvertible)X()).GetTypeCode();
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529897, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIConvertibleOnMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class Program
{
    DateTime X() => default;

    void Main()
    {
        var y = ([|(IConvertible)|]X()).GetTypeCode();
    }
}
",
                @"
using System;

class Program
{
    DateTime X() => default;

    void Main()
    {
        var y = X().GetTypeCode();
    }
}
");
        }

        #endregion

        #region ParamArray Parameter Casts

        [WorkItem(545141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545141")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToObjectInParamArrayArg1()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        Goo((object)null);
    }

    static void Goo(params object[] x)
    {
        Console.WriteLine(x.Length);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToIntArrayInParamArrayArg2()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        Goo((int[])null);
    }

    static void Goo(params object[] x)
    {
        Console.WriteLine(x.Length);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToObjectArrayInParamArrayArg3()
        {
            var source =
@"using System;

class C
{
    static void Main()
    {
        Goo((object[])null);
    }

    static void Goo(params object[][] x)
    {
        Console.WriteLine(x.Length);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayArg1()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(object[])|]null);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToStringArrayInParamArrayArg2()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(string[])|]null);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToIntArrayInParamArrayArg3()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class C
{
    static void Goo(params int[] x) { }

    static void Main()
    {
        Goo([|(int[])|]null);
    }
}
",

@"
class C
{
    static void Goo(params int[] x) { }

    static void Main()
    {
        Goo(null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayArg4()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(object[])|]null, null);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null, null);
    }
}
");
        }

        [WorkItem(529911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectInParamArrayArg5()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo([|(object)|]null, null);
    }
}
",

@"
class C
{
    static void Goo(params object[] x) { }

    static void Main()
    {
        Goo(null, null);
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastToObjectArrayInParamArrayWithNamedArgument()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
class C
{
    static void Main()
    {
        Goo(x: [|(object[])|]null);
    }

    static void Goo(params object[] x) { }
}
",
                @"
class C
{
    static void Main()
    {
        Goo(x: null);
    }

    static void Goo(params object[] x) { }
}
");
        }

        #endregion

        #region ForEach Statements

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInForEach1()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            var source =
@"using System.Collections;

class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in (IEnumerable)s)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(7,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(7, 20, 7, 20),
                    // /0/Test0.cs(7,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(7, 22, 7, 22),
                },
                source);
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInForEach2()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement.

            var source =
@"using System.Collections.Generic;

class Program
{
    static void Main()
    {
        object s = "";
        foreach (object x in (IEnumerable<char>)s)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(7,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(7, 20, 7, 20),
                    // /0/Test0.cs(7,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(7, 22, 7, 22),
                },
                source);
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInForEach3()
        {
            // The cast below can't be removed because it would result an error
            // in the foreach statement since C doesn't contain a GetEnumerator()
            // method.

            var source =
@"using System.Collections;

class D
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}

class C
{
    public static implicit operator D(C c)
    {
        return new D();
    }

    static void Main()
    {
        object s = "";
        foreach (object x in (D)new C())
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(20,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(20, 20, 20, 20),
                    // /0/Test0.cs(20,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(20, 22, 20, 22),
                },
                source);
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInForEach4()
        {
            // The cast below can't be removed because it would result in
            // C.GetEnumerator() being called rather than D.GetEnumerator().

            var source =
@"using System;
using System.Collections;

class D
{
    public IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}

class C
{
    public IEnumerator GetEnumerator()
    {
        yield return 2;
    }

    public static implicit operator D(C c)
    {
        return new D();
    }

    static void Main()
    {
        object s = "";
        foreach (object x in (D)new C())
        {
            Console.WriteLine(x);
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(26,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(26, 20, 26, 20),
                    // /0/Test0.cs(26,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(26, 22, 26, 22),
                },
                source);
        }

        [WorkItem(545961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInForEach5()
        {
            // The cast below can't be removed because it would change the
            // type of 'x'.

            var source =
@"using System;

class Program
{
    static void Main()
    {
        string[] s = {
            ""A""
        };
        foreach (var x in (Array)s)
        {
            var y = x;
            y = 1;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        #endregion

        [WorkItem(545925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastIfOverriddenMethodHasIncompatibleParameterList()
        {
            // Note: The cast below can't be removed because the parameter list
            // of Goo and its override have different default values.

            var source =
@"using System;

abstract class Y
{
    public abstract void Goo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        ((Y)new X()).Goo();
    }

    public override void Goo(int x = 2)
    {
        Console.WriteLine(x);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastIfOverriddenMethodHaveCompatibleParameterList()
        {
            // Note: The cast below can be removed because the parameter list
            // of Goo and its override have the same default values.

            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

abstract class Y
{
    public abstract void Goo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        ([|(Y)|]new X()).Goo();
    }

    public override void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }
}
",

@"
using System;

abstract class Y
{
    public abstract void Goo(int x = 1);
}

class X : Y
{
    static void Main()
    {
        new X().Goo();
    }

    public override void Goo(int x = 1)
    {
        Console.WriteLine(x);
    }
}
");
        }

        [WorkItem(529916, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529916")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInReceiverForMethodGroup()
        {
            // Note: The cast below can be removed because the it results in
            // the same method group.

            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

static class Program
{
    static void Main()
    {
        Action a = ([|(string)|]"""").Goo;
    }

    static void Goo(this string x) { }
}
",

@"
using System;

static class Program
{
    static void Main()
    {
        Action a = """".Goo;
    }

    static void Goo(this string x) { }
}
");
        }

        [WorkItem(609497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task Bugfix_609497()
        {
            var source =
@"using System;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        Goo().Wait();
    }

    static async Task Goo()
    {
        Task task = Task.FromResult(0);
        Console.WriteLine(await (dynamic)task);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545995")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToDifferentTypeWithSameName()
        {
            // Note: The cast below cannot be removed because the it results in
            // a different overload being picked.

            var source =
@"using System;
using MyInt = System.Int32;

namespace System
{
    public struct Int32
    {
        public static implicit operator Int32(int x)
        {
            return default(Int32);
        }
    }
}

class A
{
    static void Goo(int x)
    {
        Console.WriteLine(""int"");
    }

    static void Goo(MyInt x)
    {
        Console.WriteLine(""MyInt"");
    }

    static void Main()
    {
        Goo((MyInt)0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545921")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastWhichWouldChangeAttributeOverloadResolution1()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            var source =
@"using System;

[Flags]
enum EEEnum
{
    Flag1 = 0x2,
    Flag2 = 0x1,
}

class MyAttributeAttribute : Attribute
{
    public MyAttributeAttribute(EEEnum e)
    {
    }

    public MyAttributeAttribute(short e)
    {
    }

    public void Goo(EEEnum e)
    {
    }

    public void Goo(short e)
    {
    }

    [MyAttribute((EEEnum)0x0)]
    public void Bar()
    {
        Goo((EEEnum)0x0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(608180, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608180")]
        [WorkItem(624252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624252")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastIfArgumentIsRestricted_TypedReference()
        {
            var source =
@"using System;

class Program
{
    static void Main(string[] args)
    {
    }

    static void v(dynamic x)
    {
        var y = default(TypedReference);
        dd((object)x, y);
    }

    static void dd(object obj, TypedReference d)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments()
        {
            var source =
@"using System;

class Program
{
    static void Main()
    {
        C<string>.InvokeGoo(0);
    }
}

class C<T>
{
    public static void InvokeGoo(dynamic x)
    {
        Console.WriteLine(Goo(x, (object)"""", """"));
    }

    static void Goo(int x, string y, T z)
    {
    }

    static bool Goo(int x, object y, object z)
    {
        return true;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments_Bracketed()
        {
            var source =
@"class C<T>
{
    int this[int x, T s, string d = ""abc""]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }

    int this[int x, object s, object d]
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }

    void Goo(dynamic xx)
    {
        var y = this[x: xx, s: """", d: (object)""""];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt()
        {
            var source =
@"class C
{
    static bool Goo(dynamic d)
    {
        d((object)"""");
        return true;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_1()
        {
            var source =
@"class C
{
    static bool Goo(dynamic d)
    {
        d.goo((object)"""");
        return true;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_2()
        {
            var source =
@"class C
{
    static bool Goo(dynamic d)
    {
        d.goo.bar.goo((object)"""");
        return true;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_3()
        {
            var source =
@"class C
{
    static bool Goo(dynamic d)
    {
        d.goo().bar().goo((object)"""");
        return true;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(627107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments_1()
        {
            var source =
@"using System;

class Program
{
    static void Main()
    {
        C<string>.InvokeGoo(0);
    }
}

class C<T>
{
    public static void InvokeGoo(dynamic x)
    {
        Console.WriteLine(Goo((object)"""", x, """"));
    }

    static void Goo(string y, int x, T z)
    {
    }

    static bool Goo(object y, int x, object z)
    {
        return true;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(545998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545998")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastWhichWouldChangeAttributeOverloadResolution2()
        {
            // Note: The cast below cannot be removed because it would result in
            // a different attribute constructor being picked

            var source =
@"using System;

[A(new[] { (long)0 })]
class A : Attribute
{
    public A(long[] x)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529894")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotUnnecessaryCastFromEnumToUint()
        {
            var source =
@"using System;

enum E
{
    X = -1
}

class C
{
    static void Main()
    {
        E x = E.X;
        Console.WriteLine((uint)x > 0);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(529846, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotUnnecessaryCastFromTypeParameterToObject()
        {
            var source =
@"class C
{
    static void Goo<T>(T x, object y)
    {
        if ((object)x == y)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(640136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640136")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastAndParseCorrect()
        {
            var source =
@"
using System;
using System.Threading.Tasks;
 
class C
{
    void Goo(Task<Action> x)
    {
        (([|(Task<Action>)|]x).Result)();
    }
}
";
            var fixedSource =
@"
using System;
using System.Threading.Tasks;
 
class C
{
    void Goo(Task<Action> x)
    {
        (x.Result)();
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { fixedSource },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(9,10): error CS0118: 'x' is a variable but is used like a type
                        DiagnosticResult.CompilerError("CS0118").WithSpan(9, 10, 9, 11),
                        // /0/Test0.cs(9,20): error CS1525: Invalid expression term ')'
                        DiagnosticResult.CompilerError("CS1525").WithSpan(9, 20, 9, 21).WithArguments(")"),
                    },
                },
                // The code fix in this case does not produce valid code or a valid syntax tree
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [WorkItem(626026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/626026")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastIfUserDefinedExplicitCast()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        B bar = new B();
        A a = (A)bar;
    }
}

public struct A
{
    public static explicit operator A(B b)
    {
        return new A();
    }
}

public struct B
{
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(768895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768895")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInTernary()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        object x = null;
        int y = (bool)x ? 1 : 0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(770187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770187")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInSwitchExpression()
        {
            var source =
@"namespace ConsoleApplication23
{
    class Program
    {
        static void Main(string[] args)
        {
            int goo = 0;
            switch ((E)goo)
            {
                case E.A:
                case E.B:
                    return;
            }
        }
    }

    enum E
    {
        A,
        B,
        C
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(844482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844482")]
        [WorkItem(2761, "https://github.com/dotnet/roslyn/issues/2761")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromBaseToDerivedWithExplicitReference()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        C x = null;
        C y = null;
        y = (D)x;
    }
}

class C
{
}

class D : C
{
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(3254, "https://github.com/dotnet/roslyn/issues/3254")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToTypeParameterWithExceptionConstraint()
        {
            var source =
@"using System;

class Program
{
    private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : Exception
    {
        if (!condition)
        {
            throw (TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition);
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(3254, "https://github.com/dotnet/roslyn/issues/3254")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
        {
            var source =
@"using System;

class Program
{
    private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : ArgumentException
    {
        if (!condition)
        {
            throw (TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition);
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(8111, "https://github.com/dotnet/roslyn/issues/8111")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastThatChangesShapeOfAnonymousTypeObject()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = (int)Directions.South };
    }

    public enum Directions
    {
        North,
        East,
        South,
        West
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(8111, "https://github.com/dotnet/roslyn/issues/8111")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastThatDoesntChangeShapeOfAnonymousTypeObject()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = [|(Directions)|]Directions.South };
    }

    public enum Directions
    {
        North,
        East,
        South,
        West
    }
}",

@"class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = Directions.South };
    }

    public enum Directions
    {
        North,
        East,
        South,
        West
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task Tuple()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    void Main()
    {
        (int, string) tuple = [|((int, string))|](1, ""hello"");
    }
}",
@"class C
{
    void Main()
    {
        (int, string) tuple = (1, ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TupleWithDifferentNames()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    void Main()
    {
        (int a, string) tuple = [|((int, string d))|](1, f: ""hello"");
    }
}",
@"class C
{
    void Main()
    {
        (int a, string) tuple = (1, f: ""hello"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [WorkItem(24791, "https://github.com/dotnet/roslyn/issues/24791")]
        public async Task SimpleBoolCast()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    bool M()
    {
        if (![|(bool)|]M()) throw null;
        throw null;
    }
}",
@"class C
{
    bool M()
    {
        if (!M()) throw null;
        throw null;
    }
}");
        }

        [WorkItem(12572, "https://github.com/dotnet/roslyn/issues/12572")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastThatUnboxes()
        {
            // The cast below can't be removed because it could throw a null ref exception.
            var source =
@"using System;

class Program
{
    static void Main()
    {
        object i = null;
        switch ((int)i)
        {
            case 0:
                Console.WriteLine(0);
                break;
            case 1:
                Console.WriteLine(1);
                break;
            case 2:
                Console.WriteLine(2);
                break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(17029, "https://github.com/dotnet/roslyn/issues/17029")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnEnumComparison1()
        {
            var source =
@"
enum TransferTypeKey
{
    Transfer,
    TransferToBeneficiary
}

class Program
{
    static void Main(dynamic p)
    {
        if (p.TYP != (int)TransferTypeKey.TransferToBeneficiary)
          throw new InvalidOperationException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(13,21): error CS0246: The type or namespace name 'InvalidOperationException' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithSpan(13, 21, 13, 46).WithArguments("InvalidOperationException"),
                source);
        }

        [WorkItem(17029, "https://github.com/dotnet/roslyn/issues/17029")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnEnumComparison2()
        {
            var source =
@"
enum TransferTypeKey
{
    Transfer,
    TransferToBeneficiary
}

class Program
{
    static void Main(dynamic p)
    {
        if ((int)TransferTypeKey.TransferToBeneficiary != p.TYP)
          throw new InvalidOperationException();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(13,21): error CS0246: The type or namespace name 'InvalidOperationException' could not be found (are you missing a using directive or an assembly reference?)
                DiagnosticResult.CompilerError("CS0246").WithSpan(13, 21, 13, 46).WithArguments("InvalidOperationException"),
                source);
        }

        [WorkItem(18978, "https://github.com/dotnet/roslyn/issues/18978")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnCallToMethodWithParamsArgs()
        {
            var source =
@"
class Program
{
    public static void Main(string[] args)
    {
        var takesArgs = new[] { ""Hello"", ""World"" };
        TakesParams((object)takesArgs);
    }

    private static void TakesParams(params object[] goo)
    {
        Console.WriteLine(goo.Length);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(12,9): error CS0103: The name 'Console' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(12, 9, 12, 16).WithArguments("Console"),
                source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastOnCallToMethodWithInvalidParamsArgs()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
class Program
{
    public static void Main(string[] args)
    {
        TakesParams([|(string)|]null);
    }

    private static void TakesParams({|CS0225:params|} string wrongDefined)
    {
    }
}",
                @"
class Program
{
    public static void Main(string[] args)
    {
        TakesParams(null);
    }

    private static void TakesParams({|CS0225:params|} string wrongDefined)
    {
    }
}");
        }

        [WorkItem(18978, "https://github.com/dotnet/roslyn/issues/18978")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnCallToMethodWithParamsArgsIfImplicitConversionExists()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class Program
{
    public static void Main(string[] args)
    {
        var takesArgs = new[] { ""Hello"", ""World"" };
        TakesParams([|(System.IComparable[])|]takesArgs);
    }

    private static void TakesParams(params object[] goo)
    {
        System.Console.WriteLine(goo.Length);
    }
}",
@"
class Program
{
    public static void Main(string[] args)
    {
        var takesArgs = new[] { ""Hello"", ""World"" };
        TakesParams(takesArgs);
    }

    private static void TakesParams(params object[] goo)
    {
        System.Console.WriteLine(goo.Length);
    }
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgs()
        {
            var source =
@"
using System;
using System.Reflection;

sealed class MarkAttribute : Attribute
{
  public readonly string[] Arr;

  public MarkAttribute(params string[] arr)
  {
    Arr = arr;
  }
}
[Mark((string)null)]   // wrong instance of: IDE0004 Cast is redundant.
static class Program
{
  static void Main()
  {
  }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(29264, "https://github.com/dotnet/roslyn/issues/29264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnDictionaryIndexer()
        {
            var source =
@"
using System;
using System.Reflection;
using System.Collections.Generic;

static class Program
{
    enum TestEnum
    {
        Test,
    }

    static void Main()
    {
        Dictionary<int, string> Icons = new Dictionary<int, string>
        {
            [(int) TestEnum.Test] = null,
        };
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(29264, "https://github.com/dotnet/roslyn/issues/29264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnDictionaryIndexer()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
using System.Reflection;
using System.Collections.Generic;

static class Program
{
    enum TestEnum
    {
        Test,
    }

    static void Main()
    {
        Dictionary<int, string> Icons = new Dictionary<int, string>
        {
            [[|(int)|] 0] = null,
        };
    }
}",
                @"
using System;
using System.Reflection;
using System.Collections.Generic;

static class Program
{
    enum TestEnum
    {
        Test,
    }

    static void Main()
    {
        Dictionary<int, string> Icons = new Dictionary<int, string>
        {
            [0] = null,
        };
    }
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgsAndProperty()
        {
            var source =
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(params string[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark((string)null, Prop = 1)] 
static class Program
{
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgsPropertyAndOtherArg()
        {
            var source =
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params string[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(true, (string)null, Prop = 1)] 
static class Program
{
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgsNamedArgsAndProperty()
        {
            var source =
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params string[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(arr: (string)null, otherArg: true, Prop = 1)]
static class Program
{
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveCastOnCallToAttributeWithInvalidParamsArgs()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, {|CS0225:params|} string wrongDefined)
    {
    }
    public int Prop { get; set; }
}

[Mark(true, [|(string)|]null, Prop = 1)]
static class Program
{
}",
                @"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, {|CS0225:params|} string wrongDefined)
    {
    }
    public int Prop { get; set; }
}

[Mark(true, null, Prop = 1)]
static class Program
{
}");
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnCallToAttributeWithParamsArgsWithImplicitCast()
        {
            var source =
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params object[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(arr: [|(object[])|]new[] { ""Hello"", ""World"" }, otherArg: true, Prop = 1)]
static class Program
{
}";
            var fixedSource =
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute(bool otherArg, params object[] arr)
    {
    }
    public int Prop { get; set; }
}

[Mark(arr: (new[] { ""Hello"", ""World"" }), otherArg: true, Prop = 1)]
static class Program
{
}";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(11,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                        DiagnosticResult.CompilerError("CS0182").WithSpan(11, 2, 11, 75),
                    },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(11,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                        DiagnosticResult.CompilerError("CS0182").WithSpan(11, 2, 11, 67),
                    },
                },
            }.RunAsync();
        }

        [WorkItem(20630, "https://github.com/dotnet/roslyn/issues/20630")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnCallToAttributeWithCastInPropertySetter()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute()
    {
    }
    public int Prop { get; set; }
}

[Mark(Prop = [|(int)|]1)]
static class Program
{
}",
@"
using System;
sealed class MarkAttribute : Attribute
{
    public MarkAttribute()
    {
    }
    public int Prop { get; set; }
}

[Mark(Prop = 1)]
static class Program
{
}");
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [InlineData("-")]
        [InlineData("+")]
        public async Task DoNotRemoveCastOnInvalidUnaryOperatorEnumValue1(string op)
        {
            var source =
$@"
enum Sign
    {{
        Positive = 1,
        Negative = -1
    }}

    class T
    {{
        void Goo()
        {{
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( {op}((int) mySign) );
        }}
    }}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [InlineData("-")]
        [InlineData("+")]
        public async Task DoNotRemoveCastOnInvalidUnaryOperatorEnumValue2(string op)
        {
            var source =
$@"
enum Sign
    {{
        Positive = 1,
        Negative = -1
    }}

    class T
    {{
        void Goo()
        {{
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( {op}(int) mySign );
        }}
    }}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnValidUnaryOperatorEnumValue()
        {
            var source =
@"
enum Sign
    {
        Positive = 1,
        Negative = -1
    }

    class T
    {
        void Goo()
        {
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( ~[|(int)|] mySign );
        }
    }";
            var fixedSource =
@"
enum Sign
    {
        Positive = 1,
        Negative = -1
    }

    class T
    {
        void Goo()
        {
            Sign mySign = Sign.Positive;
            Sign invertedSign = [|(Sign)|] ( ~mySign);
        }
    }";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedState =
                {
                    Sources = { fixedSource },
                    MarkupHandling = MarkupMode.Allow,
                },
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            }.RunAsync();
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastOnValidUnaryOperatorEnumValue_Nullable()
        {
            var source =
@"
enum Sign
{
    Positive = 1,
    Negative = -1
}

class T
{
    void Goo()
    {
        Sign mySign = Sign.Positive;
        Sign? invertedSign = (Sign?)(~[|(int)|] mySign);
    }
}";
            var fixedSource =
@"
enum Sign
{
    Positive = 1,
    Negative = -1
}

class T
{
    void Goo()
    {
        Sign mySign = Sign.Positive;
        Sign? invertedSign = (Sign?)(~mySign);
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            }.RunAsync();
        }

        [WorkItem(18510, "https://github.com/dotnet/roslyn/issues/18510")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEnumCastToDifferentRepresentation()
        {
            var source =
@"
enum Sign
    {
        Positive = 1,
        Negative = -1
    }

    class T
    {
        void Goo()
        {
            Sign mySign = Sign.Positive;
            Sign invertedSign = (Sign) ( ~(long) mySign );
        }
    }";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [WorkItem(25456, "https://github.com/dotnet/roslyn/issues/25456#issuecomment-373549735")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInSwitchCase()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInSwitchCase_CastInsideParentheses()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case ((bool)default):
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInSwitchCase_DefaultInsideParentheses()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)(default):
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInSwitchCase_RemoveDoubleCast()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case [|(bool)|][|(bool)|]default:
                break;
        }
    }
}";
            var fixedSource =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [WorkItem(12631, "https://github.com/dotnet/roslyn/issues/12631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveRedundantBoolCast()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class C
{
    void M()
    {
        var a = true;
        var b = ![|(bool)|]a;
    }
}",
@"
class C
{
    void M()
    {
        var a = true;
        var b = !a;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when true:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_CastInsideParentheses()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case ((bool)default) when true:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_DefaultInsideParentheses()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)(default) when true:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_RemoveDoubleCast()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case [|(bool)|][|(bool)|]default when true:
                break;
        }
    }
}";
            var fixedSource =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when true:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_RemoveInsideWhenClause()
        {
            var source =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when [|(bool)|]default:
                break;
        }
    }
}";
            var fixedSource =
@"
class C
{
    void M()
    {
        switch (true)
        {
            case (bool)default when default:
                break;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternIs()
        {
            var source =
@"
class C
{
    void M()
    {
        if (true is (bool)default);
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternIs_CastInsideParentheses()
        {
            var source =
@"
class C
{
    void M()
    {
        if (true is ((bool)default));
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternIs_DefaultInsideParentheses()
        {
            var source =
@"
class C
{
    void M()
    {
        if (true is (bool)(default));
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPatternIs_RemoveDoubleCast()
        {
            var source =
@"
class C
{
    void M()
    {
        if (true is [|(bool)|][|(bool)|]default);
    }
}";
            var fixedSource =
@"
class C
{
    void M()
    {
        if (true is (bool)default) ;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                LanguageVersion = LanguageVersion.CSharp7_1,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPropertyPattern1()
        {
            var source =
@"
class C
{
    void M(string s)
    {
        if (s is { Length: (int)default })
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotIntroduceDefaultLiteralInPropertyPattern2()
        {
            var source =
@"
class C
{
    void M(string s)
    {
        if (s is { Length: ((int)default) })
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(27239, "https://github.com/dotnet/roslyn/issues/27239")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotOfferToRemoveCastWhereNoConversionExists()
        {
            var source =
                @"
using System;

class C
{
    void M()
    {
        object o = null;
        TypedReference r2 = {|CS0030:(TypedReference)o|};
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotOfferToRemoveCastWhenAccessingHiddenProperty()
        {
            var source = @"
using System.Collections.Generic;
class Fruit
{
    public IDictionary<string, object> Properties { get; set; }
}
class Apple : Fruit
{
    public new IDictionary<string, object> Properties { get; }
}
class Tester
{
    public void Test()
    {
        var a = new Apple();
        ((Fruit)a).Properties[""Color""] = ""Red"";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(31963, "https://github.com/dotnet/roslyn/issues/31963")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotOfferToRemoveCastInConstructorWhenItNeeded()
        {
            var source = @"
class IntegerWrapper
{
    public IntegerWrapper(int value)
    {
    }
}
enum Goo
{
    First,
    Second
}
class Tester
{
    public void Test()
    {
        var a = new IntegerWrapper((int)Goo.First);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(31963, "https://github.com/dotnet/roslyn/issues/31963")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotOfferToRemoveCastInBaseConstructorInitializerWhenItNeeded()
        {
            var source =
@"
class B
{
    B(int a)
    {
    }
}
class C : B
{
    C(double a) : base((int)a)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(10,19): error CS0122: 'B.B(int)' is inaccessible due to its protection level
                DiagnosticResult.CompilerError("CS0122").WithSpan(10, 19, 10, 23).WithArguments("B.B(int)"),
                source);
        }

        [WorkItem(31963, "https://github.com/dotnet/roslyn/issues/31963")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotOfferToRemoveCastInConstructorInitializerWhenItNeeded()
        {
            var source =
@"
class B
{
    B(int a)
    {
    }

    B(double a) : this((int)a)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(10220, "https://github.com/dotnet/roslyn/issues/10220")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveObjectCastInParamsCall()
        {
            var source =
@"
using System;
using System.Diagnostics;

class Program
{
    static void Main(string[] args)
    {
        object[] arr = { 1, 2, 3 };
        testParams((object)arr);
    }

    static void testParams(params object[] ps)
    {
        Console.WriteLine(ps.Length);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(22804, "https://github.com/dotnet/roslyn/issues/22804")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromNullableToUnderlyingType()
        {
            var source =
            @"
using System.Text;

class C
{
    private void M()
    {
        StringBuilder numbers = new StringBuilder();
        int?[] position = new int?[2];
        numbers[(int)position[1]] = 'x';
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(41433, "https://github.com/dotnet/roslyn/issues/41433")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromIntPtrToPointer()
        {
            var source =
            @"
using System;

class C
{
    unsafe int Test(IntPtr safePointer)
    {
        return ((int*)safePointer)[0];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(38599, "https://github.com/dotnet/roslyn/issues/38599")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromIntPtrToPointerInReturn()
        {
            var source =
            @"
using System;

class Program
{
    public static unsafe int Read(IntPtr pointer, int offset)
    {
        return ((int*)pointer)[offset];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(32491, "https://github.com/dotnet/roslyn/issues/32491")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromIntPtrToPointerWithTypeParameter()
        {
            var source =
            @"
using System;

struct Block<T>
    where T : unmanaged
{
    IntPtr m_ptr;
    unsafe ref T GetRef( int index )
    {
        return ref ((T*)m_ptr)[index];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(25021, "https://github.com/dotnet/roslyn/issues/25021")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromIntPtrToPointerWithAddressAndCast()
        {
            var source =
            @"
using System;

class C
{
    private unsafe void goo()
    {
        var address = IntPtr.Zero;
        var bar = (int*)&((long*)address)[10];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(38347, "https://github.com/dotnet/roslyn/issues/38347")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestArgToLocalFunction1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class Program
{
    public static void M()
    {
        for (int i = 0; i < 1; i++)
        {
            long a = 0, b = 0;

            SameScope([|(decimal)|]a + [|(decimal)|]b);

            static void SameScope(decimal sum) { }
        }
    }
}",
@"
class Program
{
    public static void M()
    {
        for (int i = 0; i < 1; i++)
        {
            long a = 0, b = 0;

            SameScope(a + (decimal)b);

            static void SameScope(decimal sum) { }
        }
    }
}");
        }

        [WorkItem(38347, "https://github.com/dotnet/roslyn/issues/38347")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestArgToLocalFunction2()
        {
            var source =
@"
class Program
{
    public static void M()
    {
        for (int i = 0; i < 1; i++)
        {
            long a = 0, b = 0;

            SameScope([|(decimal)|]a + [|(decimal)|]b);

            static void SameScope(decimal sum) { }
        }
    }
}";
            var fixedSource =
@"
class Program
{
    public static void M()
    {
        for (int i = 0; i < 1; i++)
        {
            long a = 0, b = 0;

            SameScope((decimal)a + b);

            static void SameScope(decimal sum) { }
        }
    }
}";
            var batchFixedSource =
@"
class Program
{
    public static void M()
    {
        for (int i = 0; i < 1; i++)
        {
            long a = 0, b = 0;

            SameScope(a + (decimal)b);

            static void SameScope(decimal sum) { }
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedSource,
                BatchFixedCode = batchFixedSource,
                CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                DiagnosticSelector = diagnostics => diagnostics[1],
            }.RunAsync();
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString1()
        {
            var source =
            @"
using System;

class C
{
    private void goo()
    {
        object x = (IFormattable)$"""";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString1_1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class C
{
    private void goo()
    {
        IFormattable x = [|(IFormattable)|]$"""";
    }
}",
                @"
using System;

class C
{
    private void goo()
    {
        IFormattable x = $"""";
    }
}");
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString2()
        {
            var source =
            @"
using System;

class C
{
    private void goo()
    {
        object x = (FormattableString)$"""";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString2_2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class C
{
    private void goo()
    {
        FormattableString x = [|(FormattableString)|]$"""";
    }
}",
                @"
using System;

class C
{
    private void goo()
    {
        FormattableString x = $"""";
    }
}");
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString3()
        {
            var source =
            @"
using System;

class C
{
    private void goo()
    {
        bar((FormattableString)$"""");
    }

    private void bar(string s) { }
    private void bar(FormattableString s) { }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString4()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

class C
{
    private void goo()
    {
        bar([|(FormattableString)|]$"""");
    }

    private void bar(FormattableString s) { }
}",
@"
using System;

class C
{
    private void goo()
    {
        bar($"""");
    }

    private void bar(FormattableString s) { }
}");
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString5()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

class C
{
    private void goo()
    {
        object o = [|(string)|]$"""";
    }
}",
@"
using System;

class C
{
    private void goo()
    {
        object o = $"""";
    }
}");
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString6()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

class C
{
    private void goo()
    {
        bar([|(IFormattable)|]$"""");
    }

    private void bar(IFormattable s) { }
}",
@"
using System;

class C
{
    private void goo()
    {
        bar($"""");
    }

    private void bar(IFormattable s) { }
}");
        }

        [WorkItem(36631, "https://github.com/dotnet/roslyn/issues/36631")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFormattableString7()
        {
            var source =
            @"
using System;

class C
{
    private void goo()
    {
        object x = (IFormattable)$@"""";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestMissingOnInterfaceCallOnNonSealedClass()
        {
            var source =
@"
using System;

public class DbContext : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine(""Base called"");
    }
}

public class MyContext : DbContext, IDisposable
{
    void IDisposable.Dispose()
    {
        Console.WriteLine(""Derived called"");
    }
}

class C
{
    private readonly DbContext _dbContext = new MyContext();

    static void Main()
    {
        ((IDisposable)_dbContext).Dispose();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                // /0/Test0.cs(26,23): error CS0120: An object reference is required for the non-static field, method, or property 'C._dbContext'
                DiagnosticResult.CompilerError("CS0120").WithSpan(26, 23, 26, 33).WithArguments("C._dbContext"),
                source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestMissingOnInterfaceCallOnNonReadOnlyStruct()
        {
            var source =
@"
using System;

public struct DbContext : IDisposable
{
    public int DisposeCount;
    public void Dispose()
    {
        DisposeCount++'
    }
}

class C
{
    private DbContext _dbContext = new MyContext();

    static void Main()
    {
        ((IDisposable)_dbContext).Dispose();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(9,23): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(9, 23, 9, 23),
                    // /0/Test0.cs(9,23): error CS1011: Empty character literal
                    DiagnosticResult.CompilerError("CS1011").WithSpan(9, 23, 9, 23),
                    // /0/Test0.cs(9,23): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 23, 9, 24),
                    // /0/Test0.cs(9,24): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 24, 9, 24),
                    // /0/Test0.cs(15,40): error CS0246: The type or namespace name 'MyContext' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(15, 40, 15, 49).WithArguments("MyContext"),
                    // /0/Test0.cs(19,23): error CS0120: An object reference is required for the non-static field, method, or property 'C._dbContext'
                    DiagnosticResult.CompilerError("CS0120").WithSpan(19, 23, 19, 33).WithArguments("C._dbContext"),
                },
                source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestMissingOnInterfaceCallOnReadOnlyStruct()
        {
            // We technically could support this.  But we choose not to for simplicity. While semantics could be
            // preserved, the semantics around interfaces are subtle and we don't want to make a change that might
            // negatively impact the user if they make other code changes.
            var source =
@"
using System;

public struct DbContext : IDisposable
{
    public int DisposeCount;
    public void Dispose()
    {
        DisposeCount++'
    }
}

class C
{
    private readonly DbContext _dbContext = new MyContext();

    static void Main()
    {
        ((IDisposable)_dbContext).Dispose();
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source,
                new[]
                {
                    // /0/Test0.cs(9,23): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(9, 23, 9, 23),
                    // /0/Test0.cs(9,23): error CS1011: Empty character literal
                    DiagnosticResult.CompilerError("CS1011").WithSpan(9, 23, 9, 23),
                    // /0/Test0.cs(9,23): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 23, 9, 24),
                    // /0/Test0.cs(9,24): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(9, 24, 9, 24),
                    // /0/Test0.cs(15,49): error CS0246: The type or namespace name 'MyContext' could not be found (are you missing a using directive or an assembly reference?)
                    DiagnosticResult.CompilerError("CS0246").WithSpan(15, 49, 15, 58).WithArguments("MyContext"),
                    // /0/Test0.cs(19,23): error CS0120: An object reference is required for the non-static field, method, or property 'C._dbContext'
                    DiagnosticResult.CompilerError("CS0120").WithSpan(19, 23, 19, 33).WithArguments("C._dbContext"),
                },
                source);
        }

        [WorkItem(34326, "https://github.com/dotnet/roslyn/issues/34326")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestOnInterfaceCallOnSealedClass()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

public sealed class DbContext : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine(""Base called"");
    }
}

class C
{
    private readonly DbContext _dbContext = null;

    void Main()
    {
        ([|(IDisposable)|]_dbContext).Dispose();
    }
}",
                @"
using System;

public sealed class DbContext : IDisposable
{
    public void Dispose()
    {
        Console.WriteLine(""Base called"");
    }
}

class C
{
    private readonly DbContext _dbContext = null;

    void Main()
    {
        _dbContext.Dispose();
    }
}");
        }

        [WorkItem(29726, "https://github.com/dotnet/roslyn/issues/29726")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestDefaultLiteralWithNullableCastInCoalesce()
        {
            var source =
@"
using System;

public class C
{
    public void Goo()
    {
        int x = (int?)(int)default ?? 42;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(6309, "https://github.com/dotnet/roslyn/issues/6309")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFPIdentityThatMustRemain1()
        {
            var source =
@"
using System;

public class C
{
    float X() => 2 / (float)X();
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFPIdentityThatMustRemain2()
        {
            var source =
@"
using System;

public class C
{
    void M()
    {
        float f1 = 0.00000000002f;
        float f2 = 1 / f1;
        double d = (float)f2;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestFPIdentityThatMustRemain3()
        {
            var source =
@"
using System;

public class C
{
    void M()
    {
        float f1 = 0.00000000002f;
        float f2 = 1 / f1;
        float f3 = (float)f2;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnFieldRead()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    float f;

    void M()
    {
        var v = [|(float)|]f;
    }
}",
@"
using System;

public class C
{
    float f;

    void M()
    {
        var v = f;
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnFieldWrite()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    float f;

    void M(float f1)
    {
        f = [|(float)|]f1;
    }
}",
@"
using System;

public class C
{
    float f;

    void M(float f1)
    {
        f = f1;
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityInFieldInitializer()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    static float f1;
    static float f2 = [|(float)|]f1;
}",
@"
using System;

public class C
{
    static float f1;
    static float f2 = f1;
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnArrayRead()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    float[] f;

    void M()
    {
        var v = [|(float)|]f[0];
    }
}",
@"
using System;

public class C
{
    float[] f;

    void M()
    {
        var v = f[0];
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnArrayWrite()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    float[] f;

    void M(float f2)
    {
        f[0] = [|(float)|]f2;
    }
}",
@"
using System;

public class C
{
    float[] f;

    void M(float f2)
    {
        f[0] = f2;
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnArrayInitializer1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    void M(float f2)
    {
        float[] f = { [|(float)|]f2 };
    }
}",
@"
using System;

public class C
{
    void M(float f2)
    {
        float[] f = { f2 };
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnArrayInitializer2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    void M(float f2)
    {
        float[] f = new float[] { [|(float)|]f2 };
    }
}",
@"
using System;

public class C
{
    void M(float f2)
    {
        float[] f = new float[] { f2 };
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPIdentityOnImplicitArrayInitializer()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    void M(float f2)
    {
        float[] f = new[] { [|(float)|]f2 };
    }
}",
@"
using System;

public class C
{
    void M(float f2)
    {
        float[] f = new[] { f2 };
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPWithBoxing1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    void M()
    {
        float value = 0.0f;
        object boxed = [|(float)|]value;
    }
}",
@"
using System;

public class C
{
    void M()
    {
        float value = 0.0f;
        object boxed = value;
    }
}");
        }

        [WorkItem(34873, "https://github.com/dotnet/roslyn/issues/34873")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFPWithBoxing2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

public class C
{
    void M()
    {
        double value = 0.0;
        object boxed = [|(double)|]value;
    }
}",
@"
using System;

public class C
{
    void M()
    {
        double value = 0.0;
        object boxed = value;
    }
}");
        }

        [WorkItem(37953, "https://github.com/dotnet/roslyn/issues/37953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestCanRemoveFromUnnecessarySwitchExpressionCast1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
using System;

class Program
{
    public static void Main() { }

    public static string GetValue(DayOfWeek value)
        => [|(DayOfWeek)|]value switch
        {
            DayOfWeek.Monday => ""Monday"",
            _ => ""Other"",
        };
}",
@"
using System;

class Program
{
    public static void Main() { }

    public static string GetValue(DayOfWeek value)
        => value switch
        {
            DayOfWeek.Monday => ""Monday"",
            _ => ""Other"",
        };
}");
        }

        [WorkItem(37953, "https://github.com/dotnet/roslyn/issues/37953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestLeaveNecessarySwitchExpressionCast1()
        {
            var source =
@"
using System;

class Program
{
    public static void Main() { }

    public static string GetValue(int value)
        => (DayOfWeek)value switch
        {
            DayOfWeek.Monday => ""Monday"",
            _ => ""Other"",
        };
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrAssignment1()
        {
            var source =
@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        result |= (long)random.Next();
        return result;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrBinary1()
        {
            var source =
@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        var v = result | (long)random.Next();
        return result;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrBinary2()
        {
            var source =
@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        var v = (long)random.Next() | result;
        return result;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithAndAssignment1()
        {

            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        result &= [|(long)|]random.Next();
        return result;
    }
}",
@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        result &= random.Next();
        return result;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithAndBinary1()
        {

            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        var x = result & [|(long)|]random.Next();
        return result;
    }
}",
@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        var x = result & random.Next();
        return result;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithAndBinary2()
        {

            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        var x = [|(long)|]random.Next() & result;
        return result;
    }
}",
@"
using System;

class C
{
    private long Repro()
    {
        var random = new Random();
        long result = random.Next();
        result <<= 32;
        var x = random.Next() & result;
        return result;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase1()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v1 = (((long)i32_hi) << 32) | (long)i32_lo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase2()
        {
            // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
            await VerifyCS.VerifyCodeFixAsync(
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v2 = (ulong)i32_hi | [|(ulong)|]u64;
    }
}",
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v2 = (ulong)i32_hi | u64;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase3()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v3 = (ulong)i32_hi | (ulong)i32_lo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase4()
        {
            // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
            await VerifyCS.VerifyCodeFixAsync(
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v4 = [|(ulong)|](uint)(ushort)i08 | (ulong)i32_lo;
    }
}",
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v4 = (ushort)i08 | (ulong)i32_lo;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase5()
        {

            await VerifyCS.VerifyCodeFixAsync(@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v5 = (int)i08 | [|(int)|]i32_lo;
    }
}",
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v5 = (int)i08 | i32_lo;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase6()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v6 = (((ulong)i32_hi) << 32) | (uint) i32_lo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase7()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v7 = 0x0000BEEFU | (uint)i16;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase8()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v8 = 0xFFFFBEEFU | (uint)i16;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCase9()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int i32_hi = 1;
        int i32_lo = 1;
        ulong u64 = 1;
        sbyte i08 = 1;
        short i16 = -1;

        object v9 = 0xDEADBEEFU | (uint)i16;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable1()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v1 = (((long?)i32_hi) << 32) | (long?)i32_lo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable2()
        {
            // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
            await VerifyCS.VerifyCodeFixAsync(@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v2 = (ulong?)i32_hi | [|(ulong?)|]u64;
    }
}", @"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v2 = (ulong?)i32_hi | u64;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable3()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v3 = (ulong?)i32_hi | (ulong?)i32_lo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable4()
        {
            // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
            await VerifyCS.VerifyCodeFixAsync(@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v4 = [|(ulong?)|][|(uint?)|](ushort?)i08 | (ulong?)i32_lo;
    }
}", @"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v4 = (ushort?)i08 | (ulong?)i32_lo;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable5()
        {

            await VerifyCS.VerifyCodeFixAsync(@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v5 = (int?)i08 | [|(int?)|]i32_lo;
    }
}",
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v5 = (int?)i08 | i32_lo;
    }
}");
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable6()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v6 = (((ulong?)i32_hi) << 32) | (uint?)i32_lo;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable7()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v7 = 0x0000BEEFU | (uint?)i16;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable8()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v8 = 0xFFFFBEEFU | (uint?)i16;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(40414, "https://github.com/dotnet/roslyn/issues/40414")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestSignExtensionWithOrCompilerCaseNullable9()
        {
            var source =
@"
public class sign
{
    public static void Main()
    {
        int? i32_hi = 1;
        int? i32_lo = 1;
        ulong? u64 = 1;
        sbyte? i08 = 1;
        short? i16 = -1;

        object v9 = 0xDEADBEEFU | (uint?)i16;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullCastInSwitch1()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((object)null)
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullCastInSwitch2()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((object)(null))
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullCastInSwitch3()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((bool?)null)
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullCastInSwitch4()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((bool?)(null))
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullCastInSwitch5()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (((object)null))
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveDefaultCastInSwitch1()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((object)default)
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveDefaultCastInSwitch2()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((object)(default))
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveDefaultCastInSwitch3()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((bool?)default)
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveDefaultCastInSwitch4()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch ((bool?)(default))
        {
          case bool _:
            break;
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/20211")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveDoubleNullCastInSwitch1()
        {
            // Removing the 'object' cast would make `case object:` unreachable.
            var source =
@"class Program
{
    static int Main()
    {
        switch ((object)(string)null)
        {
            case null:
                return 0;
            case string:
                return 1;
            case object:
                return 2;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional1()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? (int?)1 : default;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional2()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? ((int?)1) : default;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional3()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? (int?)1 : (default);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional4_CSharp8()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? (int?)1 : null;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInConditional4()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? [|(int?)|]1 : null;
    }
}",
                FixedCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? 1 : null;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional5_CSharp8()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? ((int?)1) : null;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInConditional5()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? ([|(int?)|]1) : null;
    }
}",
                FixedCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? 1 : null;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional6_CSharp8()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? (int?)1 : (null);
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp8,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInConditional6_CSharp8()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? [|(int?)|]1 : (null);
    }
}",
                FixedCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? 1 : (null);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional7()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? default : (int?)1;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional8()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? default : ((int?)1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional9()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? (default) : (int?)1;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional10_CSharp8()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? null : (int?)1;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInConditional10()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? null : [|(int?)|]1;
    }
}",
                FixedCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? null : 1;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional11_CSharp()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? null : ((int?)1);
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInConditional11()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? null : ([|(int?)|]1);
    }
}",
                FixedCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? null : 1;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional12_CSharp8()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? (null) : (int?)1;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp8
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryCastInConditional12()
        {
            await new VerifyCS.Test
            {
                TestCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? (null) : [|(int?)|]1;
    }
}",
                FixedCode = @"class C
{
    void M(bool x)
    {
        int? y = x ? (null) : 1;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional13()
        {
            var source =
@"class C
{
    void M(bool x, int? z)
    {
        var y = x ? (long?)z : null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInConditional14()
        {
            var source =
@"class C
{
    void M(bool x, int? z)
    {
        var y = x ? (long?)z : default;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnecessaryCastInConditional1()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? [|(int)|]1 : default;
    }
}";

            var fixedCode =
@"class C
{
    void M(bool x)
    {
        int? y = x ? 1 : default;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnecessaryCastInConditional2()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? [|(int)|]1 : 0;
    }
}";

            var fixedCode =
@"class C
{
    void M(bool x)
    {
        int? y = x ? 1 : 0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnecessaryCastInConditional3()
        {
            var source =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? [|(int)|]1 : z;
    }
}";

            var fixedCode =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? 1 : z;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnecessaryCastInConditional4()
        {
            var source =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? [|(int?)|]1 : z;
    }
}";

            var fixedCode =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? 1 : z;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnecessaryCastInConditional5()
        {
            var source =
@"class C
{
    void M(bool x)
    {
        int? y = x ? [|(int?)|]1 : 0;
    }
}";
            var fixedCode =
@"class C
{
    void M(bool x)
    {
        int? y = x ? 1 : 0;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInConditional6()
        {
            var source =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? [|(int?)|]z : null;
    }
}";
            var fixedCode =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? z : null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInConditional7()
        {
            var source =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? [|(int?)|]z : default;
    }
}";
            var fixedCode =
@"class C
{
    void M(bool x, int? z)
    {
        int? y = x ? z : default;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20742, "https://github.com/dotnet/roslyn/issues/20742")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNamedArgToParamsParameter1()
        {
            var source =
@"class Program
{
    public void M()
    {
        object[] takesArgs = null;
        TakesParams(bar: (object)takesArgs, goo: true);
    }

    private void TakesParams(bool goo, params object[] bar)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(20742, "https://github.com/dotnet/roslyn/issues/20742")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNamedArgToParamsParameter1()
        {
            var source =
@"class Program
{
    public void M()
    {
        object[] takesArgs = null;
        TakesParams(bar: [|(object[])|]takesArgs, goo: true);
    }

    private void TakesParams(bool goo, params object[] bar)
    {
    }
}";
            var fixedCode =
@"class Program
{
    public void M()
    {
        object[] takesArgs = null;
        TakesParams(bar: takesArgs, goo: true);
    }

    private void TakesParams(bool goo, params object[] bar)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(20742, "https://github.com/dotnet/roslyn/issues/20742")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNamedArgToParamsParameter2()
        {
            var source =
@"class Program
{
    public void M()
    {
        string[] takesArgs = null;
        TakesParams(bar: [|(object[])|]takesArgs, goo: true);
    }

    private void TakesParams(bool goo, params object[] bar)
    {
    }
}";
            var fixedCode =
@"class Program
{
    public void M()
    {
        string[] takesArgs = null;
        TakesParams(bar: takesArgs, goo: true);
    }

    private void TakesParams(bool goo, params object[] bar)
    {
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ObjectCastInInterpolation1()
        {
            var source =
@"class Program
{
    public void M(int x, int z)
    {
        var v = $""x {[|(object)|]1} z"";
    }
}";
            var fixedCode =
@"class Program
{
    public void M(int x, int z)
    {
        var v = $""x {1} z"";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ObjectCastInInterpolation2()
        {
            var source =
@"class Program
{
    public void M(int x, int z)
    {
        var v = $""x {([|(object)|]1)} z"";
    }
}";
            var fixedCode =
@"class Program
{
    public void M(int x, int z)
    {
        var v = $""x {1} z"";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIdentityDoubleCast()
        {
            var source =
@"class Program
{
    public void M(object x)
    {
        var v = [|(int)|](int)x;
    }
}";
            var fixedCode =
@"class Program
{
    public void M(object x)
    {
        var v = (int)x;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison1()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a1 == (object)a2);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison2()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a1 == a2);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison3()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a1 != (object)a2);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison4()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a1 != a2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison5()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a2 == a1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison6()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a2 == a1);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison7()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a2 != (object)a1);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestUnintendedReferenceComparison8()
        {
            var source = @"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        Object a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a2 != a1);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison1()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a1 == (object)a2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison2()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a1 == a2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison3()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a1 != (object)a2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison4()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a1 != a2);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison5()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a2 == (object)a1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison6()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a2 == a1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison7()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine(a2 != (object)a1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task TestIntendedReferenceComparison8()
        {
            var source =
@"using System;

public class Symbol
{
    public static bool operator ==(Symbol a, Symbol b) => false;
    public static bool operator !=(Symbol a, Symbol b) => false;
}

public class MethodSymbol : Symbol
{
}

class Program
{
    void Main()
    {
        MethodSymbol a1 = null;
        MethodSymbol a2 = new MethodSymbol();

        Console.WriteLine((object)a2 != a1);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(44382, "https://github.com/dotnet/roslyn/issues/44382")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnParameterInitializer1()
        {
            var source =
@"enum E : byte { }
class C { void F() { void f(E e = (E)byte.MaxValue) { } } }";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(44382, "https://github.com/dotnet/roslyn/issues/44382")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastOnParameterInitializer2()
        {
            var source =
@"enum E : byte { }
class C { void f(E e = (E)byte.MaxValue) { } }";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(45695, "https://github.com/dotnet/roslyn/issues/45695")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNonObjectCastInsideInterpolation()
        {
            var source =
@"
class Other
{
    void Goo()
    {  
        char c = '4';
        string s = $""{(int)c:X4}"";
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(45695, "https://github.com/dotnet/roslyn/issues/45695")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastInsideInterpolation()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class Other
{
    void Goo()
    {  
        char c = '4';
        string s = $""{[|(object)|]c:X4}"";
    }
}",
@"
class Other
{
    void Goo()
    {  
        char c = '4';
        string s = $""{c:X4}"";
    }
}");
        }

        [WorkItem(47800, "https://github.com/dotnet/roslyn/issues/47800")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveNativeIntCastsAsIdentity()
        {
            var source =
@"using System;

public class C {
    public nint N(IntPtr x) => [|(nint)|]x;
}";
            var fixedCode =
@"using System;

public class C {
    public nint N(IntPtr x) => x;
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(47800, "https://github.com/dotnet/roslyn/issues/47800")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNativeIntCasts()
        {
            var source =
@"using System;

public class C {
    public nuint N(IntPtr x) => (nuint)(nint)x;
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(47800, "https://github.com/dotnet/roslyn/issues/47800")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveNativeUIntCastsAsIdentity()
        {
            var source =
@"using System;

public class C {
    public nuint N(UIntPtr x) => [|(nuint)|]x;
}";
            var fixedCode =
@"using System;

public class C {
    public nuint N(UIntPtr x) => x;
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(51123, "https://github.com/dotnet/roslyn/issues/51123")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNativeIntCastsToInt()
        {
            var source =
@"using System;

public class C {
    public int N(IntPtr x) => (int)(nint)x;
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(47800, "https://github.com/dotnet/roslyn/issues/47800")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNativeUIntCasts()
        {
            var source =
@"using System;

public class C {
    public nint N(UIntPtr x) => (nint)(nuint)x;
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(47800, "https://github.com/dotnet/roslyn/issues/47800")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveIntPtrCastsAsIdentity()
        {
            var source =
@"
using System;

class C
{
    public void M(IntPtr x)
    {
        var v = [|(IntPtr)|]x;
    }
}";
            var fixedCode =
@"
using System;

class C
{
    public void M(IntPtr x)
    {
        var v = x;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(47800, "https://github.com/dotnet/roslyn/issues/47800")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUIntPtrCastsAsIdentity()
        {
            var source =
@"
using System;

class C
{
    public void M(UIntPtr x)
    {
        var v = [|(UIntPtr)|]x;
    }
}";
            var fixedCode =
@"
using System;

class C
{
    public void M(UIntPtr x)
    {
        var v = x;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
        }

        [WorkItem(49140, "https://github.com/dotnet/roslyn/issues/49140")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveBitwiseNotOfUnsignedExtendedValue1()
        {
            var source =
@"
class C
{
    public static ulong P(ulong a, uint b)
    {
        return a & ~(ulong)b;
    }
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(49140, "https://github.com/dotnet/roslyn/issues/49140")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveBitwiseNotOfUnsignedExtendedValue2()
        {
            var source =
@"
class C
{
    public static nuint N(nuint a, uint b)
    {
        return a & ~(nuint)b;
    }
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(49140, "https://github.com/dotnet/roslyn/issues/49140")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveBitwiseNotOfUnsignedExtendedValue3()
        {
            var source =
@"
class C
{
    public static ulong N()
    {
        return ~(ulong)uint.MaxValue;
    }
}";

            var test = new VerifyCS.Test()
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(49140, "https://github.com/dotnet/roslyn/issues/49140")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveBitwiseNotOfSignExtendedValue1()
        {
            var test = new VerifyCS.Test()
            {
                TestCode = @"
class C
{
    public static long P(long a, int b)
    {
        return a & ~[|(long)|]b;
    }
}",
                FixedCode = @"
class C
{
    public static long P(long a, int b)
    {
        return a & ~b;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(49140, "https://github.com/dotnet/roslyn/issues/49140")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveBitwiseNotOfSignExtendedValue2()
        {

            var test = new VerifyCS.Test()
            {
                TestCode = @"
class C
{
    public static nint N(nint a, int b)
    {
        return a & ~[|(nint)|]b;
    }
}",
                FixedCode = @"
class C
{
    public static nint N(nint a, int b)
    {
        return a & ~b;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9
            };

            await test.RunAsync();
        }

        [WorkItem(50000, "https://github.com/dotnet/roslyn/issues/50000")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepNecessaryCastIfRemovalWouldCreateIllegalConditionalExpression()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    ushort Goo(string s)
        => s is null ? (ushort)1234 : ushort.Parse(s);
}
",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(50000, "https://github.com/dotnet/roslyn/issues/50000")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastWhenConditionalExpressionIsLegal()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
class C
{
    uint Goo(string s)
        => s is null ? [|(uint)|]1234 : uint.Parse(s);
}",
                FixedCode = @"
class C
{
    uint Goo(string s)
        => s is null ? 1234 : uint.Parse(s);
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(52524, "https://github.com/dotnet/roslyn/issues/52524")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveForValueTaskConstrutor()
        {
            var source =
@"
#nullable enable

using System.Threading.Tasks;

struct ValueTask<TResult>
{
    public ValueTask(TResult result)
    {
    }

    public ValueTask(Task<TResult> task)
    {
    }
}

class A
{
    static void Main()
    {
        ValueTask<object?> v = new((object?)null);
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(53698, "https://github.com/dotnet/roslyn/issues/53698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveForConditional()
        {
            var source =
@"
using System.Collections.Generic;

class E
{
    private object _o;

    public E First
    {
        get
        {
            return _o is List<E> es ? es[0] : (E)_o;
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(55621, "https://github.com/dotnet/roslyn/issues/55621")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveForNullWithMultipleMatchingParameterTypes()
        {
            var source =
@"
#nullable enable
using System;
public class TestClass
{
	public TestClass(object? value) { }
	public TestClass(Func<object?> value) { }

    public TestClass Create1() => new ((object?)null);
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(56207, "https://github.com/dotnet/roslyn/issues/56207")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveForNintPointerToVoidPointer()
        {
            var source =
@"
using System;
public class TestClass
{
	unsafe void M(nint** ptr)
    {
        nint value = (nint)(void*)*ptr;
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryCastInPattern1()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    void Main(int? n)
    {
        var v = n is [|(int)|]0;
    }
}",

            @"
class Program
{
    void Main(int? n)
    {
        var v = n is 0;
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveUnnecessaryNullableCastInPattern1()
        {
            await VerifyCS.VerifyCodeFixAsync(
            @"
class Program
{
    void Main(int? n)
    {
        var v = n is [|(int?)|]0;
    }
}",

            @"
class Program
{
    void Main(int? n)
    {
        var v = n is 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveBoxingEnumCast()
        {
            var source = @"
using System;
class Program
{
    static void M()
    {
        object y = (DayOfWeek)0;
        Console.WriteLine(y);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveFPCastFromNonFPTypeToWidenedType1()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        double y = x;
        double z = [|(float)|]x;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
        Console.WriteLine(y == z);
    }
}", @"
using System;

class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        double y = x;
        double z = x;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
        Console.WriteLine(y == z);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveFPCastToWidenedType2()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        float x = 0;
        double y = x;
        double z = (float)x;
        Console.WriteLine(x);
        Console.WriteLine(y);
        Console.WriteLine(z);
        Console.WriteLine(y == z);
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastIfOverriddenMethodHasDifferentReturnType()
        {
            var source =
@"using System;

abstract class Y
{
    public abstract object Goo();
}

class X : Y
{
    static void Main()
    {
        var v = ((Y)new X()).Goo();
    }

    public override string {|CS8830:Goo|}()
    {
        return null;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepCastToObjectToPreserveDynamicOverload()
        {
            var source =
@"using System;
 
class C
{
    static void Bar(int x, Action y) { }
    static void Bar(dynamic x, Action y) { }
 
    static void Main()
    {
        Bar((object)1, Console.WriteLine);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepIntToLongCastWithIComparable()
        {
            var source =
@"using System;
 
class C
{
    static void Main()
    {
        IComparable<long> result = (long)0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepCastNeededToPickCorrectOverload()
        {
            // removing the 'byte' cast will switch the overload called.
            var source =
@"
using System;
     
class Program
{
    static void Main()
    {
        byte z = 0;
        Func<byte, byte> p = x => 0;
        Goo(p, y => (byte)0, z, z);
    }
     
    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveExplicitCastToReferenceTypeWhenPassedToDynamic1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
 
class C
{
    static void Bar(dynamic x, Action y) { }
 
    static void Main()
    {
        Bar([|(object)|]1, Console.WriteLine);
    }
}",
                @"
using System;
 
class C
{
    static void Bar(dynamic x, Action y) { }
 
    static void Main()
    {
        Bar(1, Console.WriteLine);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveExplicitCastToReferenceTypeWhenPassedToDynamic2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
 
class C
{
    static void Bar(dynamic x, Action y) { }
 
    static void Main()
    {
        Bar([|(IComparable)|]1, Console.WriteLine);
    }
}",
                @"
using System;
 
class C
{
    static void Bar(dynamic x, Action y) { }
 
    static void Main()
    {
        Bar(1, Console.WriteLine);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task NotOnWidenedNumericStoredInObject1()
        {
            var source =
@"
using System;
     
class Program
{
    static void Main()
    {
        int expr = 0;
        object o1 = (long)expr;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task NotOnWidenedNumericConstantStoredInObject2()
        {
            var source =
@"
using System;
     
class Program
{
    static void Main()
    {
        object o1 = (long)0;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task OnNonWidenedNumericStoredInObject1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
     
class Program
{
    static void Main()
    {
        long lng = 0;
        object o1 = [|(long)|]lng;
    }
}",
                @"
using System;
     
class Program
{
    static void Main()
    {
        long lng = 0;
        object o1 = lng;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task OnNonWidenedNumericConstantStoredInObject2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
     
class Program
{
    static void Main()
    {
        object o1 = [|(long)|]0L;
    }
}",
                @"
using System;
     
class Program
{
    static void Main()
    {
        object o1 = 0L;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DisallowNarrowingNullableNumericAsCast()
        {
            var source =
@"
using System;
     
class Program
{
    static void Main()
    {
        int? data = 1;
        var x = data as byte?;
        Console.WriteLine(x);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task AllowNonNarrowingNullableNumericAsCast()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
     
class Program
{
    static void Main()
    {
        byte? data = 1;
        var x = data [|as byte?|];
        Console.WriteLine(x);
    }
}",
                @"
using System;
     
class Program
{
    static void Main()
    {
        byte? data = 1;
        var x = data;
        Console.WriteLine(x);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanNotRemoveStackallocToVarOutsideOfUnsafeRegion()
        {
            var source = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    static void Main()
    {
        var x = (Span<int>)stackalloc int[8];
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveStackallocToSpanOutsideOfUnsafeRegion()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    static void Main()
    {
        Span<int> x = [|(Span<int>)|]stackalloc int[8]; // cast can be removed
    }
}",
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    static void Main()
    {
        Span<int> x = stackalloc int[8]; // cast can be removed
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanNotRemoveStackallocToVarInsideOfUnsafeRegion1()
        {
            var source = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    static void Main()
    {
        unsafe
        {
            var x = (Span<int>)stackalloc int[8]; // cast can not be removed
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanNotRemoveStackallocToVarInsideOfUnsafeRegion2()
        {
            var source = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        var x = (Span<int>)stackalloc int[8]; // cast can not be removed
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveStackallocToSpanInsideOfUnsafeRegion1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    static void Main()
    {
        unsafe
        {
            Span<int> x = [|(Span<int>)|]stackalloc int[8]; // cast can be removed
        }
    }
}",
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    static void Main()
    {
        unsafe
        {
            Span<int> x = stackalloc int[8]; // cast can be removed
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveStackallocToSpanInsideOfUnsafeRegion2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        Span<int> x = [|(Span<int>)|]stackalloc int[8]; // cast can be removed
    }
}",
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        Span<int> x = stackalloc int[8]; // cast can be removed
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveStackallocToVarInsideOfUnsafeRegion1()
        {
            // possibly incorrect error: https://github.com/dotnet/roslyn/issues/57040
            var source = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        var x = {|CS8346:(int*)stackalloc int[8]|}; // cast can be removed
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveParenthesizedStackallocToVarInsideOfUnsafeRegion1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        var x = [|(Span<int>)|](stackalloc int[8]); // cast can be removed
    }
}",
                @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        var x = (stackalloc int[8]); // cast can be removed
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveStackallocToPointerInsideOfUnsafeRegion1()
        {
            // Possibly a compiler bug.  Unclear why conversion to `(int*)` not allowed.
            // https://github.com/dotnet/roslyn/issues/57040
            var source = @"
using System;
namespace System
{
    public readonly ref struct Span<T> 
    {
        unsafe public Span(void* pointer, int length) { }
    }
}
class Program
{
    unsafe static void Main()
    {
        int* x = {|CS8346:(int*)stackalloc int[8]|}; // cast can be removed
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57062, "https://github.com/dotnet/roslyn/issues/57062")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveIdentityCastInConstantPattern1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class C
{
    void M(object o)
    {
        if (o is [|(int)|]0)
        {
        }
    }
}",
                @"
using System;

class C
{
    void M(object o)
    {
        if (o is 0)
        {
        }
    }
}");
        }

        [WorkItem(57062, "https://github.com/dotnet/roslyn/issues/57062")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveIdentityCastInConstantPattern2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class C
{
    void M(object o)
    {
        if (o is [|(long)|]0L)
        {
        }
    }
}",
                @"
using System;

class C
{
    void M(object o)
    {
        if (o is 0L)
        {
        }
    }
}");
        }

        [WorkItem(57062, "https://github.com/dotnet/roslyn/issues/57062")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNonIdentityCastInConstantPattern1()
        {
            var source =
            @"
using System;

class C
{
    void M(object o)
    {
        if (o is (sbyte)0)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57062, "https://github.com/dotnet/roslyn/issues/57062")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNonIdentityCastInConstantPattern2()
        {
            var source =
            @"
using System;

class C
{
    void M(object o)
    {
        if (o is (sbyte)0 or (short)0)
        {
        }
    }
}";

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(57062, "https://github.com/dotnet/roslyn/issues/57062")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNonIdentityCastInConstantPattern3()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M(object o)
    {
        if (o is (sbyte)1 or (short)1 or [|(int)|]1 or (long)1 or
                (byte)1 or (ushort)1 or (uint)1 or (ulong)1 or
                1.0 or 1.0f or 1.0m)
        {
        }
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(object o)
    {
        if (o is (sbyte)1 or (short)1 or 1 or (long)1 or
                (byte)1 or (ushort)1 or (uint)1 or (ulong)1 or
                1.0 or 1.0f or 1.0m)
        {
        }
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
            }.RunAsync();
        }

        [WorkItem(57062, "https://github.com/dotnet/roslyn/issues/57062")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNonIdentityCastInConstantPattern4()
        {
            var source =
            @"
using System;

class C
{
    void M(object o)
    {
        if (o is (long)0)
        {
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast1()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a1, IAssembly a2)
        => (object)a1 == a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast2()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a1, IAssembly a2)
        => a1 == (object)a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast3()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => (object)a1 == a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast4()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => a1 == (object)a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast5()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => [|(object)|]a1 == [|(object)|]a2;
}", @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => a1 == [|(object)|]a2;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast6()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a1, IAssembly a2)
        => a1 as object == a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast7()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a1, IAssembly a2)
        => a1 == a2 as object;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast8()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => a1 as object == a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast9()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => a1 == a2 as object;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveEqualityWarningSilencingCast10()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => a1 [|as object|] == a2 [|as object|];
}", @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(IAssembly a2, Assembly a1)
        => a1 == a2 as object;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists1()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a2, Assembly a1)
        => (object)a1 == a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists2()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a2, Assembly a1)
        => a1 == (object)a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists3()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a2, Assembly a1)
        => a1 as object == a2;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists4()
        {
            var source =
            @"
interface IAssembly
{
}

class Assembly : IAssembly
{
    public static bool operator ==(Assembly a1, Assembly a2) => false;
    public static bool operator !=(Assembly a1, Assembly a2) => false;

    public override bool Equals(object obj) => false;
    public override int GetHashCode() => 0;
}

class C
{
    bool M(Assembly a2, Assembly a1)
        => a1 == a2 as object;
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => [|(object)|]a1 == a2;
}",
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == [|(object)|]a2;
}",
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => [|(object)|]a1 == [|(object)|]a2;
}",
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface4()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 [|as object|] == a2;
}",
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface5()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2 [|as object|];
}",
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2;
}");
        }

        [WorkItem(57065, "https://github.com/dotnet/roslyn/issues/57065")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface6()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 [|as object|] == a2 [|as object|];
}",
                @"
interface IAssembly
{
}

class C
{
    bool M(IAssembly a2, IAssembly a1)
        => a1 == a2;
}");
        }

        [WorkItem(57063, "https://github.com/dotnet/roslyn/issues/57063")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullableIntToNullableEnumCast()
        {
            var source =
            @"
enum E { }

class Program
{
    static E? Main(object o)
    {
        return (E?)(int?)o;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNRTCast1()
        {
            var source =
            @"
#nullable enable

using System.Collections.Generic;
using System.Linq;

class Node
{
    public readonly string Name = """";
}

class C
{
    void M(IList<Node> nodes)
    {
        Dictionary<string, Node?> map = nodes.ToDictionary(t => t.Name, t => (Node?)t);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938"), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNRTCastInConditional1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
#nullable enable

using System.Collections.Generic;
using System.Linq;

class C
{
    string? M(bool b, string s)
    {
        return b ? [|(string?)|]s : null;
    }
}",
                FixedCode =
@"
#nullable enable

using System.Collections.Generic;
using System.Linq;

class C
{
    string? M(bool b, string s)
    {
        return b ? s : null;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveNRTCastInConditional2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                            @"
#nullable enable

using System.Collections.Generic;
using System.Linq;

class C
{
    string? M(bool b, string s)
    {
        return b ? s : [|(string?)|]null;
    }
}",
                                        @"
#nullable enable

using System.Collections.Generic;
using System.Linq;

class C
{
    string? M(bool b, string s)
    {
        return b ? s : null;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryWideningConstantCastInConditional1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                            @"
class C
{
    void M(int a)
    {
        long f1 = (a == 5) ? 4 : [|(long)|]5;
    }
}",
                                        @"
class C
{
    void M(int a)
    {
        long f1 = (a == 5) ? 4 : 5;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNnecessaryWideningConstantCastInConditional1()
        {
            var source = @"
class C
{
    void M(int a)
    {
        var f1 = (a == 5) ? 4 : (long)5;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryWideningCastInConditional2()
        {
            await VerifyCS.VerifyCodeFixAsync(
                            @"
class C
{
    void M(int a, int b, int c)
    {
        long f1 = (a == 5) ? b : [|(long)|]c;
    }
}",
                                        @"
class C
{
    void M(int a, int b, int c)
    {
        long f1 = (a == 5) ? b : c;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryWideningCastInConditional2()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        var f1 = (a == 5) ? b : (long)c;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryWideningNullableConstantCastInConditional3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                            @"
class C
{
    void M(int a, int b, int c)
    {
        int? f1 = (a == 5) ? [|(int?)|]0 : 1;
    }
}",
                                        @"
class C
{
    void M(int a, int b, int c)
    {
        int? f1 = (a == 5) ? 0 : 1;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryWideningNullableConstantCastInConditional3()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        var f1 = (a == 5) ? (int?)0 : 1;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryWideningNullableCastInConditional3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                            @"
class C
{
    void M(int a, int b, int c)
    {
        int? f1 = (a == 5) ? [|(int?)|]b : c;
    }
}",
                                        @"
class C
{
    void M(int a, int b, int c)
    {
        int? f1 = (a == 5) ? b : c;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryWideningNullableCastInConditional3()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        var f1 = (a == 5) ? (int?)b : c;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryWideningConstantCastInConditionalWithDefault3()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
class C
{
    void M(int a, int b, int c)
    {
        long f1 = (a == 5) ? [|(long)|]0 : default;
    }
}",
                @"
class C
{
    void M(int a, int b, int c)
    {
        long f1 = (a == 5) ? 0 : default;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNnecessaryWideningConstantCastInConditionalWithDefault3()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        var f1 = (a == 5) ? (long)0 : default;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoRemoveUnnecessaryWideningConstantCastInConditionalWithDefault4()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
class C
{
    void M(int a, int b, int c)
    {
        long f1 = (a == 5) ? [|(long)|]b : default;
    }
}",
                @"
class C
{
    void M(int a, int b, int c)
    {
        long f1 = (a == 5) ? b : default;
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryWideningConstantCastInConditionalWithDefault4()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        var f1 = (a == 5) ? (long)b : default;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryWideningNullableConstantCastInConditionalWithDefault3()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        int? f1 = (a == 5) ? (int?)0 : default;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryWideningNullableCastInConditionalWithDefault3()
        {
            var source = @"
class C
{
    void M(int a, int b, int c)
    {
        int? f1 = (a == 5) ? (int?)b : default;
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(56938, "https://github.com/dotnet/roslyn/issues/56938")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveDoubleNullableNumericCast1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(float?)[|(int?)|]2147483647); // Prints -2147483648
    }
}",
                @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(float?)2147483647); // Prints -2147483648
    }
}");
        }

        [WorkItem(56938, "https://github.com/dotnet/roslyn/issues/56938")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task CanRemoveTripleNullableNumericCast1()
        {
            await VerifyCS.VerifyCodeFixAsync(
                @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(double?)[|(long?)|][|(int?)|]1);
    }
}",
                @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine((int)(double?)1);
    }
}");
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveMultipleNullableCastsThroughUserDefinedConversions1()
        {
            var source = @"
struct A
{
}

struct B
{
    public static implicit operator A(B b) => default;
    public static implicit operator B(int a) => default;
}

class P
{
    void M()
    {
        var v = (A?)(B?)0;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveMultipleNullableCastsThroughUserDefinedConversions2()
        {
            var source = @"
struct A
{
}

struct B
{
    public static implicit operator A(B b) => default;
    public static implicit operator B(int a) => default;
}

class P
{
    void M()
    {
        var v = (A?)(B)0;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(57064, "https://github.com/dotnet/roslyn/issues/57064")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveMultipleCastsThroughUserDefinedConversions2()
        {
            var source = @"
struct A
{
}

struct B
{
    public static implicit operator A(B b) => default;
    public static implicit operator B(int a) => default;
}

class P
{
    void M()
    {
        var v = (A)(B)0;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [WorkItem(58171, "https://github.com/dotnet/roslyn/issues/58171")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [CombinatorialData]
        public async Task DoNotRemoveMethodGroupToSpecificDelegateType(LanguageVersion version)
        {
            var source = @"
using System;

class KeyEventArgs : EventArgs
{
}

delegate void KeyEventHandler(object sender, KeyEventArgs e);

class C
{

    void M()
    {
        AddHandler((KeyEventHandler)HandleSymbolKindsPreviewKeyDown);
    }

    void HandleSymbolKindsPreviewKeyDown(object sender, KeyEventArgs e) { }
    void AddHandler(Delegate handler) { }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = version,
            }.RunAsync();
        }

        [WorkItem(58095, "https://github.com/dotnet/roslyn/issues/58095")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullableCastsInTuples()
        {
            var source = @"
using System.Diagnostics;

class C
{
    void M()
    {
        var (isEdge, moreOnes) = (true, false);

        char? expected_a = isEdge ? null : moreOnes ? '1' : '0';
        char? expected_b = isEdge ? null : moreOnes ? '0' : '1';

        (char? expected_a_01, char? expected_b_01) = isEdge ? default : moreOnes ? ((char?)'1', (char?)'0') : ('0', '1');
        (char? expected_a_02, char? expected_b_02) = isEdge ? default : moreOnes ? ('1', '0') : ('0', '1');

        Debug.Assert(expected_a == expected_a_01 && expected_a == expected_a_02);
        Debug.Assert(expected_b == expected_b_01 && expected_b == expected_b_02);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(49954, "https://github.com/dotnet/roslyn/issues/49954")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullableDefaultCast1()
        {
            var source = @"
using System;

class C
{
    protected bool? IsNewResource() =>
        Boolean.TryParse("""", out var b) ? b : (bool?)default;
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
            }.RunAsync();
        }

        [WorkItem(34509, "https://github.com/dotnet/roslyn/issues/34509")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullableDefaultCast2()
        {
            var source = @"
using System;

class C
{
    static long? TestParse(string val) => long.TryParse(val, out var parseResult) ? (long?)parseResult : default;
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(49690, "https://github.com/dotnet/roslyn/issues/49690")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullableGenericCast()
        {
            var source = @"
#nullable enable

using System.Collections.Generic;
using System.Linq;

class C
{
    static IEnumerable<string> DoThis(IEnumerable<string?> notreallynull)
    {
        return notreallynull.Where(s => s is not null) as IEnumerable<string>;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(45925, "https://github.com/dotnet/roslyn/issues/45925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecesssaryPatternCasts1()
        {
            var source = @"
using System;

class C
{
    bool M(object obj)
    {
        return obj is 0 or (uint)0 or (long)0 or (ulong)0 or (short)0 or (ushort)0 or (byte)0 or (sbyte)0 or (float)0
             or (double)0 or (decimal)0 or (AttributeTargets)0;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(37473, "https://github.com/dotnet/roslyn/issues/37473")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecesssaryCastInTupleWrappedInObject1()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    public IEnumerable<object> Bar()
    {
        yield return (""test"", (decimal?)1.23);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(33143, "https://github.com/dotnet/roslyn/issues/33143")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecesssaryCastInTupleWrappedInObject2()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    void M()
    {
        object x = (true, (IEnumerable<int>)new int[0]);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(33043, "https://github.com/dotnet/roslyn/issues/33043")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecesssaryCastInIsNullCheck1()
        {
            var source = @"
using System.Collections.Generic;

public class C
{
    void M()
    {
        if ((int?)1 is null)
        {
        }
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(20617, "https://github.com/dotnet/roslyn/issues/20617")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecesssaryBitwiseNotOnUnsignedValue1()
        {
            var source = @"
using System;

public class C
{
    public static void MethodName()
    {
        const long x = ~(long)~1U;
        Console.WriteLine(x);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(11008, "https://github.com/dotnet/roslyn/issues/11008")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastThatPreventsOverflowInChecked1()
        {
            var source = @"
static class Program
{
    static readonly long x = -(long)int.MinValue;
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(34553, "https://github.com/dotnet/roslyn/issues/34553")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastThatPreventsOverflowInChecked2()
        {
            var source = @"
using System;
class Program
{
    void M()
    {
        Int32 input32 = Int32.MinValue;
        Int64 output64_a = checked(-input32);
        Int64 output64_b = checked(-(Int64)input32);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(11008, "https://github.com/dotnet/roslyn/issues/11008")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromIntToNullableEnum1()
        {
            var source = @"
enum E
{
}

class Program
{
    void M()
    {
        int? num = 1;
        string s = ((E?)num)?.ToString().Replace('a', 'b');
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(11008, "https://github.com/dotnet/roslyn/issues/11008")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveWideningCastInBitwiseOr1()
        {
            var source = @"
class C
{
    public uint fn1(sbyte a, sbyte b)
    {
        return (uint)((a << 8) | (int)b);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(32773, "https://github.com/dotnet/roslyn/issues/32773")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveWideningCastInBitwiseOr2()
        {
            var source = @"
class C
{
    public void fn1(int start, int end)
    {
        var bounds = (((long)end) << 32) | ((long)start);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(25165, "https://github.com/dotnet/roslyn/issues/25165")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastInIllegalDelegateCast()
        {
            var source = @"
using System;
public delegate void DoSomething();

public class Code
{
    private Action _f;
    public Code(DoSomething f)
    {
        Action doNothing = (() => {});
        _f = f ?? {|CS0030:(DoSomething)doNothing|};  
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(31303, "https://github.com/dotnet/roslyn/issues/31303")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveUnsignedCastInBitwiseNot1()
        {
            var source = @"
using System;

public class Code
{
    static void CheckRedundantCast()
    {
        ulong number1 = 0xFFFFFFFFFFFFFFFFL;
        uint number2 = 0xFF;
        ulong myResult = number1 & ~(ulong)number2;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(36755, "https://github.com/dotnet/roslyn/issues/36755")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastInSwitchExpressionArm1()
        {
            var source = @"
class Program
{
    void M()
    {
        string numberString = ""One"";
        Numbers? number = numberString switch
        {
            ""One"" => (Numbers?)Numbers.One,
            ""Two"" => Numbers.Two,
            _ => null,
        };
    }
}

enum Numbers
{
    One,
    Two
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(36782, "https://github.com/dotnet/roslyn/issues/36782")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryCastWithOverloadedNegationAndImplicitConversion1()
        {
            var source = @"
using System;

namespace WrongRedundantCastWarning
{
	struct Flag
	{
		public Flag(int value) => this.Value = value;

		public int Value { get; }

		// This cast is wrongly reported as redundant
		public static FlagSet operator ~(Flag flag) => ~(FlagSet)flag;
	}

	struct FlagSet
	{
		public FlagSet(int value) => this.Value = value;

		public int Value { get; }

		public static implicit operator FlagSet(Flag flag) => new FlagSet(flag.Value);

		public static FlagSet operator ~(FlagSet flagSet) => new FlagSet(~flagSet.Value);
	}

	class Program
	{
		static readonly Flag One = new Flag(1);
		static readonly Flag Two = new Flag(2);

		static void Main(string[] args)
		{
			var flipped = ~Two;

			Console.WriteLine(flipped.Value);
		}
	}
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(37041, "https://github.com/dotnet/roslyn/issues/37041")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNecessaryMethodGroupToDelegateCast1()
        {
            var source = @"
using System;
using System.Collections.Generic;

namespace RedundantCast
{
    class Program
    {
        class A { }
        class B : A { }

        B Goo() { return null; }

        static void Main()
        {
            (new Program()). Run();
        }

        void Run()
        {
            var list = new List<Func<A>>();
            list. Add((Func<B>) Goo);
            switch (list[0])
            {
                case Func<B> value: Console.WriteLine(""B""); break;
                case Func<A> value: Console.WriteLine(""A""); break;
            }
        }
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(54388, "https://github.com/dotnet/roslyn/issues/54388")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveCastFromIntToDecimal()
        {
            var source = @"
using System;

class Program
{
    void M()
    {
        X v = new((decimal)-1);
    }
}

class X
{
    public X(decimal d) { }
    public X(double d) { }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(33285, "https://github.com/dotnet/roslyn/issues/33285")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task DoNotRemoveNullableToStructCast1()
        {
            var source = @"
using System;

namespace System
{
    public readonly struct ReadOnlyMemory<T> : IEquatable<ReadOnlyMemory<T>>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static ReadOnlyMemory<T> Empty => throw new NotImplementedException();
        public bool IsEmpty => throw new NotImplementedException();
        public int Length => throw new NotImplementedException();
        public ReadOnlyMemory(T[] array) => throw new NotImplementedException();
        public ReadOnlyMemory(T[] array, int start, int length) => throw new NotImplementedException();
        public bool Equals(ReadOnlyMemory<T> other) => throw new NotImplementedException();
    }

    public class Lazy<T>
    {
        public bool IsValueCreated => throw new NotImplementedException();
        public T Value => throw new NotImplementedException();
        public Lazy() => throw new NotImplementedException();
        public Lazy(bool isThreadSafe) => throw new NotImplementedException();
        public Lazy(Func<T> valueFactory) => throw new NotImplementedException();
        public Lazy(Func<T> valueFactory, bool isThreadSafe) => throw new NotImplementedException();
        public Lazy(T value) => throw new NotImplementedException();
    }
}

class C
{
    private C(ReadOnlyMemory<byte>? buffer = null)
    {
        var v = new Lazy<ReadOnlyMemory<byte>>((ReadOnlyMemory<byte>)buffer);
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58718, "https://github.com/dotnet/roslyn/issues/58718")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task FunctionPointerWithImplicitOperator()
        {
            var source = @"
unsafe
{
    PointerDelegate<int, int> dp = (PointerDelegate<int, int>)(&Mtd);
}

static int Mtd(int arg) => arg;

public readonly struct PointerDelegate<T, TResult>
{
    private unsafe readonly delegate*<T,TResult> _pointer;

    public unsafe PointerDelegate(delegate*<T, TResult> pointer)
    {
        this._pointer = pointer;
    }

    public TResult Invoke(T param)
    {
        unsafe
        {
            return this._pointer(param);
        }
    }

    public unsafe static implicit operator PointerDelegate<T, TResult>(delegate*<T, TResult> pointer)
    {
        return new(pointer);
    }
}
";
            await new VerifyCS.Test
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58709, "https://github.com/dotnet/roslyn/issues/58709")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task NotOnNarrowingIntCastInTernary()
        {
            var source = @"
class C
{
    protected sbyte ExtractInt8(object data)
    {
	    return (data is sbyte value) ? value : (sbyte)0;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58898, "https://github.com/dotnet/roslyn/issues/58898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task SameNullableTypeOnBothSidesOfConditional1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M()
    {
        var id = true ? [|(Guid?)|]Guid.NewGuid() : [|(Guid?)|]Guid.Empty;
    }
}",
                FixedCode = @"
using System;

class C
{
    void M()
    {
        var id = true ? Guid.NewGuid() : (Guid?)Guid.Empty;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58898, "https://github.com/dotnet/roslyn/issues/58898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task SameNullableTypeOnBothSidesOfConditional2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M(Guid g1, Guid? g2)
    {
        var id = true ? [|(Guid?)|]g1 : g2;
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(Guid g1, Guid? g2)
    {
        var id = true ? g1 : g2;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58898, "https://github.com/dotnet/roslyn/issues/58898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task SameNullableTypeOnBothSidesOfConditional3()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M(Guid? g1, Guid g2)
    {
        var id = true ? g1 : [|(Guid?)|]g2;
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(Guid? g1, Guid g2)
    {
        var id = true ? g1 : g2;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58898, "https://github.com/dotnet/roslyn/issues/58898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task SameNullableTypeOnBothSidesOfConditional4()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M(Guid g1, Guid g2)
    {
        Guid? id = true ? [|(Guid?)|]g1 : g2;
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(Guid g1, Guid g2)
    {
        Guid? id = true ? g1 : g2;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58898, "https://github.com/dotnet/roslyn/issues/58898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task SameNullableTypeOnBothSidesOfConditional5()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M(Guid g1, Guid g2)
    {
        Guid? id = true ? g1 : [|(Guid?)|]g2;
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(Guid g1, Guid g2)
    {
        Guid? id = true ? g1 : g2;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58898, "https://github.com/dotnet/roslyn/issues/58898")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task SameNullableTypeOnBothSidesOfConditional6()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

class C
{
    void M(Guid g1, Guid g2)
    {
        Guid? id = true ? [|(Guid?)|]g1 : [|(Guid?)|]g2;
    }
}",
                FixedCode = @"
using System;

class C
{
    void M(Guid g1, Guid g2)
    {
        Guid? id = true ? g1 : g2;
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58804, "https://github.com/dotnet/roslyn/issues/58804")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ConvertingMethodGroupToObject_CastIsNecessary()
        {
            var code = @"
class C
{
    static object M(object o)
    {
        return (object)o.ToString;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58804, "https://github.com/dotnet/roslyn/issues/58804")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ConvertingMethodGroupToObject_CastIsNecessary2()
        {
            var code = @"
using System;

class C
{
    static T M<T>(object o)
    {
        return (T)(object)o.ToString;
    }

    static T M2<T>(object o) where T : Delegate
    {
        return (T)(object)o.ToString;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }

        [WorkItem(58804, "https://github.com/dotnet/roslyn/issues/58804")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        [InlineData("Delegate")]
        [InlineData("MulticastDelegate")]
        [InlineData("Func<string>")]
        public async Task ConvertingMethodGroupToObject_CastIsUnnecessary(string type)
        {
            var code = $@"
using System;

class C
{{
    static {type} M(object o)
    {{
        return ({type})[|(object)|]o.ToString;
    }}
}}
";
            var fixedCode = $@"
using System;

class C
{{
    static {type} M(object o)
    {{
        return o.ToString;
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp10,
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }

        [WorkItem(58804, "https://github.com/dotnet/roslyn/issues/58804")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task ConvertingMethodGroupToObject_CastIsUnnecessary2()
        {
            var code = @"
using System;

class C
{
    static Delegate M(object o)
    {
        return (Delegate)[|(object)|]o.ToString;
    }
}
";
            var fixedCode = @"
using System;

class C
{
    static Delegate M(object o)
    {
        return o.ToString;
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersion.CSharp10,
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }

        [WorkItem(60248, "https://github.com/dotnet/roslyn/issues/60248")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task RemoveCastInTopLevelPrograms()
        {
            var test = new VerifyCS.Test()
            {
                TestCode = @"
int x = 1;
int y = [|(int)|]x;
",
                FixedCode = @"
int x = 1;
int y = x;
",
                LanguageVersion = LanguageVersion.CSharp10,
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                },
            };

            await test.RunAsync();
        }

        [WorkItem(60292, "https://github.com/dotnet/roslyn/issues/60292")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
        public async Task KeepNecessaryExplicitNullableCast()
        {
            var code = @"
using System;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            for (var i = 0; i < 100; i++)
            {
                bool should = Should();
                Test? test = ShouldTest();
                int? testId = should ? (int?)test : null; // incorrect IDE0004  
            }
        }

        private static bool Should()
        {
            return new Random().Next() % 2 == 0;
        }

        private static Test? ShouldTest()
        {
            var value = new Random().Next(3);
            if (Enum.IsDefined(typeof(Test), value))
                return (Test)value;

            return null;
        }
    }

    public enum Test
    {
        Foo = 1,
        Bar = 2,
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
            }.RunAsync();
        }
    }
}
