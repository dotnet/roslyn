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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryCast;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryCastDiagnosticAnalyzer,
    CSharpRemoveUnnecessaryCastCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
public class RemoveUnnecessaryCastTests
{
    [Theory, CombinatorialData]
    public void TestStandardProperty(AnalyzerProperty property)
        => VerifyCS.VerifyStandardProperty(property);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545979")]
    public async Task DoNotRemoveCastToErrorType()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    object s = ";
                    foreach (object x in ({|CS0246:ErrorType|})s)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(5,20): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(5, 20, 5, 20),
                // /0/Test0.cs(5,22): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(5, 22, 5, 22),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545137"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/870550")]
    public async Task ParenthesizeToKeepParseTheSame1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                static void Main()
                {
                    int x = 2;
                    int i = 1;
                    Goo(x < [|(int)|]i, x > (2 + 3));
                }

                static void Goo(bool a, bool b) { }
            }
            """,
            """
            class Program
            {
                static void Main()
                {
                    int x = 2;
                    int i = 1;
                    Goo(x < (i), x > (2 + 3));
                }

                static void Goo(bool a, bool b) { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545146")]
    public async Task ParenthesizeToKeepParseTheSame2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    Action a = Console.WriteLine;
                    ([|(Action)|]a)();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main()
                {
                    Action a = Console.WriteLine;
                    a();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545160")]
    public async Task ParenthesizeToKeepParseTheSame3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var x = (Decimal)[|(int)|]-1;
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var x = (Decimal)(-1);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545138")]
    public async Task DoNotRemoveTypeParameterCastToObject()
    {
        var source =
            """
            class D
            {
                void Goo<T>(T obj)
                {
                    int x = (int)(object)obj;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545139")]
    public async Task DoNotRemoveCastInIsTest()
    {
        var source =
            """
            using System;

            class D
            {
                static void Main()
                {
                    DayOfWeek[] a = { };
                    Console.WriteLine((object)a is int[]);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545142")]
    public async Task DoNotRemoveCastNeedForUserDefinedOperator()
    {
        var source =
            """
            class A
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545142")]
    public async Task DoRemoveCastNotNeededForUserDefinedOperator()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class A
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
                    A x = [|(string)|]"";
                }
            }
            """, """
            class A
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
                    A x = "";
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545143")]
    public async Task DoNotRemovePointerCast1()
    {
        var source =
            """
            unsafe class C
            {
                static unsafe void Main()
                {
                    var x = (int)(int*)null;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545144")]
    public async Task DoNotRemoveCastToObjectFromDelegateComparison()
    {
        // The cast below can't be removed because it would result in the Delegate
        // op_Equality operator overload being used over reference equality.

        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = Console.WriteLine;
                    Action b = Console.WriteLine;
                    Console.WriteLine(a == (object)b);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545145")]
    public async Task DoNotRemoveCastToAnonymousMethodWhenOnLeftOfAsCast()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    var x = (Action)delegate {
                    }

                    [|as Action|];
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main()
                {
                    var x = (Action)delegate {
                    };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545147")]
    public async Task DoNotRemoveCastInFloatingPointOperation()
    {
        var source =
            """
            class C
            {
                static void Main()
                {
                    int x = 1;
                    double y = (double)x / 2;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545157")]
    public async Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution1()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
    public async Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution2()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
    public async Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution3()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545747")]
    public async Task DoNotRemoveCastWhichChangesTypeOfInferredLocal()
    {
        var source =
            """
            class C
            {
                static void Main()
                {
                    var x = (long)1;
                    x = long.MaxValue;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545159")]
    public async Task DoNotRemoveNeededCastToIListOfObject()
    {
        var source =
            """
            using System;
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545287"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
    public async Task RemoveUnneededCastInParameterDefaultValue()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                static void M1(int? i1 = [|(int?)|]null)
                {
                }
            }
            """,
            """
            class Program
            {
                static void M1(int? i1 = null)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545289")]
    public async Task RemoveUnneededCastInReturnStatement()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                static long M2()
                {
                    return [|(long)|]5;
                }
            }
            """,
            """
            class Program
            {
                static long M2()
                {
                    return 5;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public async Task RemoveUnneededCastInLambda1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => [|(long)|]5;
                }
            }
            """,
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => 5;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public async Task RemoveUnneededCastInLambda2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => { return [|(long)|]5; };
                }
            }
            """,
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => { return 5; };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public async Task RemoveUnneededCastInLambda3()
    {
        var source =
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => { return [|(long)|]5; };
                }
            }
            """;
        var fixedSource =
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => { return 5; };
                }
            }
            """;

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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public async Task RemoveUnneededCastInLambda4()
    {
        var source =
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => [|(long)|]5;
                }
            }
            """;
        var fixedSource =
            """
            using System;
            class Program
            {
                static void M1()
                {
                    Func<long> f1 = () => 5;
                }
            }
            """;

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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public async Task RemoveUnneededCastInConditionalExpression1()
    {
        var source =
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    long f1 = (b == 5) ? [|(long)|]4 : [|(long)|]5;
                }
            }
            """;
        var fixedSource =
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    long f1 = (b == 5) ? 4 : 5;
                }
            }
            """;

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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public async Task DoNotRemoveUnneededCastInConditionalExpression3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    long f1 = (b == 5) ? 4 : [|(long)|]5;
                }
            }
            """,
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    long f1 = (b == 5) ? 4 : 5;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public async Task DoNotRemoveNeededCastInConditionalExpression()
    {
        var source =
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;
                    var f1 = (b == 5) ? 4 : (long)5;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public async Task RemoveUnneededCastInConditionalExpression4()
    {
        var source =
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    var f1 = (b == 5) ? [|(long)|]4 : [|(long)|]5;
                }
            }
            """;
        var fixedSource =
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    var f1 = (b == 5) ? (long)4 : 5;
                }
            }
            """;
        var batchFixedSource =
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;

                    var f1 = (b == 5) ? 4 : (long)5;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            BatchFixedCode = batchFixedSource,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            DiagnosticSelector = diagnostics => diagnostics[1],
        }.RunAsync();
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545459")]
    public async Task RemoveUnneededCastInsideADelegateConstructor()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class Test
            {
                delegate void D(int x);

                static void Main(string[] args)
                {
                    var cd1 = new D([|(Action<int>)|]M1);
                }

                public static void M1(int i) { }
            }
            """,
            """
            using System;
            class Test
            {
                delegate void D(int x);

                static void Main(string[] args)
                {
                    var cd1 = new D(M1);
                }

                public static void M1(int i) { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545419")]
    public async Task DoNotRemoveTriviaWhenRemovingCast()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545422")]
    public async Task RemoveUnneededCastInsideCaseLabel()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545578")]
    public async Task RemoveUnneededCastInsideGotoCaseStatement()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545595")]
    [Fact(Skip = "529787")]
    public async Task RemoveUnneededCastInCollectionInitializer()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    var z = new List<long> { [|(long)0|] };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    var z = new List<long> { 0 };
                }
            }
            """);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
    [Fact(Skip = "529787")]
    public async Task DoNotRemoveNecessaryCastWhichInCollectionInitializer1()
    {
        var source =
            """
            using System;
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
                    var z = new X { [|(object)""|] };
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
    [Fact(Skip = "529787")]
    public async Task DoNotRemoveNecessaryCastWhichInCollectionInitializer2()
    {
        var source =
            """
            using System;
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
                    X z = new X { [|(object)""|] };
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545607")]
    public async Task RemoveUnneededCastInArrayInitializer()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class X
            {
                static void Goo()
                {
                    string x = "";
                    var s = new object[] { [|(object)|]x };
                }
            }
            """,
            """
            class X
            {
                static void Goo()
                {
                    string x = "";
                    var s = new object[] { x };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
    public async Task RemoveUnneededCastWithOverloadedBinaryOperator()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
    public async Task DoNotRemoveCastFromLambdaToDelegateWithVar1()
    {
        var source = """
            using System;
            class MyAction
            {
                static void Goo()
                {
                    var y = (Action)(() => {});
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545616")]
    public async Task DoRemoveCastFromLambdaToDelegateWithTypedVariable()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            class MyAction
            {
                static void Goo()
                {
                    Action y = [|(Action)|](() => { });
                }
            }
            """,
            """
            using System;
            class MyAction
            {
                static void Goo()
                {
                    Action y = () => { };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545822")]
    public async Task RemoveUnnecessaryCastShouldInsertWhitespaceWhereNeededToKeepCorrectParsing()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Goo<T>()
                {
                    Action a = null;
                    var x = [|(Action)|](Goo<Guid>)==a;
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Goo<T>()
                {
                    Action a = null;
                    var x = (Goo<Guid>) == a;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545560")]
    public async Task DoNotRemoveNecessaryCastWithExplicitUserDefinedConversion()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545608")]
    public async Task DoNotRemoveNecessaryCastWithImplicitUserDefinedConversion()
    {
        var source =
            """
            class X
            {
                static void Goo()
                {
                    X x = null;
                    object y = (string)x;
                }

                public static implicit operator string(X x)
                {
                    return ";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(11,16): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(11, 16, 11, 16),
                // /0/Test0.cs(11,18): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(11, 18, 11, 18),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
    public async Task DoNotRemoveNecessaryCastWithImplicitConversionInThrow()
    {
        // The cast below can't be removed because the throw statement expects
        // an expression of type Exception -- not an expression convertible to
        // Exception.

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545981")]
    public async Task DoNotRemoveNecessaryCastInThrow()
    {
        // The cast below can't be removed because the throw statement expects
        // an expression of type Exception -- not an expression convertible to
        // Exception.

        var source =
            """
            using System;

            class C
            {
                static void Main()
                {
                    object ex = new Exception();
                    throw (Exception)ex;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
    public async Task RemoveUnnecessaryCastInThrow()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class E
            {
                static void Main()
                {
                    throw [|(Exception)|]new Exception();
                }
            }
            """,
            """
            using System;

            class E
            {
                static void Main()
                {
                    throw new Exception();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545945")]
    public async Task DoNotRemoveNecessaryDowncast()
    {
        var source =
            """
            class C
            {
                void Goo(object y)
                {
                    int x = (int)y;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545591")]
    public async Task DoNotRemoveNecessaryCastWithinLambda()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545606")]
    public async Task DoNotRemoveNecessaryCastFromNullToTypeParameter()
    {
        var source =
            """
            class X
            {
                static void Goo<T, S>() where T : class, S
                {
                    S y = (T)null;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545744")]
    public async Task DoNotRemoveNecessaryCastInImplicitlyTypedArray()
    {
        var source =
            """
            class X
            {
                static void Goo()
                {
                    string x = ";
                    var s = new[] { (object)x };
                    s[0] = 1;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(5,20): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(5, 20, 5, 20),
                // /0/Test0.cs(5,22): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(5, 22, 5, 22),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545750")]
    public async Task RemoveUnnecessaryCastToBaseType()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545855")]
    public async Task RemoveUnnecessaryLambdaToDelegateCast()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
    public async Task RemoveUnnecessaryCastInQueryExpression()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class A
            {
                int Select(Func<int, long> x) { return 1; }

                static void Main()
                {
                    Console.WriteLine(from y in new A() select [|(long)|]0);
                }
            }
            """,
            """
            using System;

            class A
            {
                int Select(Func<int, long> x) { return 1; }

                static void Main()
                {
                    Console.WriteLine(from y in new A() select 0);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
    public async Task DoNotRemoveNecessaryCastInQueryExpression()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545848")]
    public async Task DoNotRemoveNecessaryCastInConstructorInitializer()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
    public async Task DoNotRemoveNecessaryCastFromTypeParameterToInterface()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
    public async Task RemoveUnnecessaryCastFromTypeParameterToInterface()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545877")]
    public async Task DoNotCrashOnIncompleteMethodDeclaration()
    {
        // This test has intentional syntax errors
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46423")]
    public async Task RemoveUnneededTargetTypedCast()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """,
            """
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
            }
            """
);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545777")]
    public async Task DoNotRemoveImportantTrailingTrivia()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529791")]
    public async Task RemoveUnnecessaryCastToNullable1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class X
            {
                static void Goo()
                {
                    object x = [|(string)|]null;
                    object y = [|(int?)|]null;
                }
            }
            """,
            """
            class X
            {
                static void Goo()
                {
                    object x = null;
                    object y = null;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
    public async Task RemoveUnnecessaryCastToNullable2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            static class C
            {
                static void Main()
                {
                    int? x = 1;
                    long y = 2;
                    long? z = x + [|(long?)|] y;
                }
            }
            """,
            """
            static class C
            {
                static void Main()
                {
                    int? x = 1;
                    long y = 2;
                    long? z = x + y;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545850")]
    public async Task RemoveSurroundingParentheses()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                static void Main()
                {
                    int x = 1;
                    ([|(int)|]x).ToString();
                }
            }
            """,
            """
            class Program
            {
                static void Main()
                {
                    int x = 1;
                    x.ToString();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
    public async Task DoNotRemoveNecessaryCastFromTypeParameterToObject()
    {
        var source =
            """
            class C
            {
                static void Goo<T>(T x, object y)
                {
                    if ((object)x == y)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545858")]
    public async Task DoNotRemoveNecessaryCastFromDelegateTypeToMulticastDelegate()
    {
        var source =
            """
            using System;

            class C
            {
                static void Main()
                {
                    Action x = Console.WriteLine;
                    Action y = Console.WriteLine;
                    Console.WriteLine((MulticastDelegate)x == y);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545857")]
    public async Task DoNotRemoveNecessaryCastInSizeOfArrayCreationExpression1()
    {
        // The cast below can't be removed because it would result in the implicit
        // conversion to int being called instead.

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545980")]
    public async Task DoNotRemoveNecessaryCastInSizeOfArrayCreationExpression2()
    {
        // Array bounds must be an int, so the cast below can't be removed.

        var source =
            """
            class C
            {
                static void Main()
                {
                    var a = new int[(int)decimal.Zero];
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529842")]
    public async Task DoNotRemoveNecessaryCastInTernaryExpression()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
    public async Task RemoveCastInConstructorInitializer1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                C(int x) { }
                C() : this([|(int)|]1) { }
            }
            """,
            """
            class C
            {
                C(int x) { }
                C() : this(1) { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545958"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
    public async Task RemoveCastInConstructorInitializer2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System.Collections;

            class C
            {
                C(int x) { }
                C(object x) { }
                C() : this([|(IEnumerable)|]"") { }
            }
            """,
            """
            using System.Collections;

            class C
            {
                C(int x) { }
                C(object x) { }
                C() : this("") { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545957")]
    public async Task DoNotRemoveCastInConstructorInitializer3()
    {
        var source =
            """
            class C
            {
                C(int x)
                {
                }

                C() : this((long)1)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(7,16): error CS1503: Argument 1: cannot convert from 'long' to 'int'
            DiagnosticResult.CompilerError("CS1503").WithSpan(7, 16, 7, 23).WithArguments("1", "long", "int"),
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
    public async Task RemoveCastToNullableInArithmeticExpression()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            static class C
            {
                static void Main()
                {
                    int? x = 1;
                    long y = 2;
                    long? z = x + [|(long?)|]y;
                }
            }
            """,
            """
            static class C
            {
                static void Main()
                {
                    int? x = 1;
                    long y = 2;
                    long? z = x + y;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545942")]
    public async Task DoNotRemoveCastFromValueTypeToObjectInReferenceEquality()
    {
        // Note: The cast below can't be removed because it would result in an
        // illegal reference equality test between object and a value type.

        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    object x = 1;
                    Console.WriteLine(x == (object)1);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545962")]
    public async Task DoNotRemoveCastWhenExpressionDoesntBind()
    {
        // Note: The cast below can't be removed because its expression doesn't bind.

        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    ((IDisposable){|CS0103:x|}).Dispose();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545944")]
    public async Task DoNotRemoveNecessaryCastBeforePointerDereference1()
    {
        // Note: The cast below can't be removed because it would result in *null,
        // which is illegal.

        var source =
            """
            unsafe class C
            {
                int x = *(int*)null;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545978")]
    public async Task DoNotRemoveNecessaryCastBeforePointerDereference2()
    {
        // Note: The cast below can't be removed because it would result in dereferencing
        // void*, which is illegal.

        var source =
            """
            unsafe class C
            {
                static void Main()
                {
                    void* p = null;
                    int x = *(int*)p;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2987")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/2691")]
    public async Task DoNotRemoveNecessaryCastBeforePointerDereference3()
    {
        // Conservatively disable cast simplifications for casts involving pointer conversions.
        // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

        var source =
            """
            class C
            {
                public unsafe float ReadSingle(byte* ptr)
                {
                    return *(float*)ptr;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2987")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/2691")]
    public async Task DoNotRemoveNumericCastInUncheckedExpression()
    {
        // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
        // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

        var source =
            """
            class C
            {
                private unsafe readonly byte* _endPointer;
                private unsafe byte* _currentPointer;

                private unsafe void CheckBounds(int byteCount)
                {
                    if (unchecked((uint)byteCount) > (_endPointer - _currentPointer))
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2987")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/2691")]
    public async Task DoNotRemoveNumericCastInUncheckedStatement()
    {
        // Conservatively disable cast simplifications within explicit checked/unchecked statements.
        // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

        var source =
            """
            class C
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2987")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/2691")]
    public async Task DoNotRemoveNumericCastInCheckedExpression()
    {
        // Conservatively disable cast simplifications within explicit checked/unchecked expressions.
        // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

        var source =
            """
            class C
            {
                private unsafe readonly byte* _endPointer;
                private unsafe byte* _currentPointer;

                private unsafe void CheckBounds(int byteCount)
                {
                    if (checked((uint)byteCount) > (_endPointer - _currentPointer))
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2987")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/2691")]
    public async Task DoNotRemoveNumericCastInCheckedStatement()
    {
        // Conservatively disable cast simplifications within explicit checked/unchecked statements.
        // https://github.com/dotnet/roslyn/issues/2987 tracks improving cast simplification for this scenario.

        var source =
            """
            class C
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public async Task DoNotRemoveCastToByteFromIntInConditionalExpression()
    {
        var source = """
            class C
            {
                object M1(bool b)
                {
                    return b ? (byte)1 : (byte)0;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public async Task RemoveCastToDoubleFromIntWithTwoInConditionalExpression()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                object M1(bool b)
                {
                    return b ? [|(double)|]1 : [|(double)|]0;
                }
            }
            """,
            """
            class C
            {
                object M1(bool b)
                {
                    return b ? 1 : (double)0;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public async Task DoNotRemoveCastToDoubleFromIntInConditionalExpression()
    {
        var source =
            """
            class C
            {
                object M1(bool b)
                {
                    return b ? 1 : (double)0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public async Task DoNotRemoveCastToUIntFromCharInConditionalExpression()
    {
        var source =
            """
            class C
            {
                object M1(bool b)
                {
                    return b ? '1' : (uint)'0';
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public async Task RemoveUnnecessaryNumericCastToSameTypeInConditionalExpression()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                object M1(bool b)
                {
                    return b ? [|(int)|]1 : 0;
                }
            }
            """,
            """
            class C
            {
                object M1(bool b)
                {
                    return b ? 1 : 0;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoNotRemoveNecessaryCastInAttribute()
    {
        var source =
            """
            using System;

            [A((byte)0)]
            class A : Attribute
            {
                public A(object x)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoRemoveUnnecessaryCastInAttribute()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            [A([|(int)|]0)]
            class A : Attribute
            {
                public A(object x)
                {
                }
            }
            """,
            """
            using System;

            [A(0)]
            class A : Attribute
            {
                public A(object x)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoNotRemoveImplicitConstantConversionToDifferentType()
    {
        var source =
            """
            using System;

            class A : Attribute
            {
                public A()
                {
                    object x = (byte)0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoRemoveImplicitConstantConversionToSameType()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class A : Attribute
            {
                public A()
                {
                    object x = [|(int)|]0;
                }
            }
            """,
            """
            using System;

            class A : Attribute
            {
                public A()
                {
                    object x = 0;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoNotRemoveNumericConversionBoxed()
    {
        var source =
            """
            using System;

            class A : Attribute
            {
                public A(int i)
                {
                    object x = (long)i;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoRemoveNumericConversionNotBoxed()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class A : Attribute
            {
                public A(int i)
                {
                    long x = [|(long)|]i;
                }
            }
            """,
            """
            using System;

            class A : Attribute
            {
                public A(int i)
                {
                    long x = i;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545894")]
    public async Task DoRemoveNonConstantCastInAttribute()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            [A({|CS0182:[|(IComparable)|]0|})]
            class A : Attribute
            {
                public A(object x)
                {
                }
            }
            """,
            """
            using System;

            [A(0)]
            class A : Attribute
            {
                public A(object x)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39042")]
    public async Task DoNotRemoveNecessaryCastForImplicitNumericCastsThatLoseInformation()
    {
        var source =
            """
            using System;

            class A
            {
                public A(long x)
                {
                    long y = (long)(double)x;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    #region Interface Casts

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545889")]
    public async Task DoNotRemoveCastToInterfaceForUnsealedType()
    {
        // Note: The cast below can't be removed because X is not sealed.

        var source =
            """
            using System;

            class X : IDisposable
            {
                static void Main()
                {
                    X x = new Y();
                    ((IDisposable)x).Dispose();
                }

                public void Dispose()
                {
                    Console.WriteLine("X.Dispose");
                }
            }

            class Y : X, IDisposable
            {
                void IDisposable.Dispose()
                {
                    Console.WriteLine("Y.Dispose");
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task DoRemoveCastToInterfaceForSealedType1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task DoRemoveCastToInterfaceForSealedType2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
                        return "Nikov Rules";
                    }
                }

                static void Main()
                {
                    Console.WriteLine(([|(I)|]new C()).Goo);
                }
            }
            """,
            """
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
                        return "Nikov Rules";
                    }
                }

                static void Main()
                {
                    Console.WriteLine(new C().Goo);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task DoRemoveCastToInterfaceForSealedType3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
                        return "Nikov Rules";
                    }
                }

                void Main()
                {
                    Console.WriteLine(([|(I)|]Instance).Goo);
                }
            }
            """,
            """
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
                        return "Nikov Rules";
                    }
                }

                void Main()
                {
                    Console.WriteLine(Instance.Goo);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    public async Task DoNotRemoveCastToInterfaceForSealedType4()
    {
        var source =
            """
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
                    ((I)new C()).Goo();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task DoRemoveCastToInterfaceForSealedTypeWhenParameterValuesDifferButExplicitValueIsProvided()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoNotRemoveCastToInterfaceForSealedType6()
    {
        // Note: The cast below can't be removed (even though C is sealed)
        // because the specified named arguments refer to parameters that
        // appear at different positions in the member signatures.

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoRemoveCastToInterfaceWhenNoDefaultArgsPassedAndValuesAreTheSame()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoRemoveCastToInterfaceWhenDefaultArgPassedAndValuesAreDifferent()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoNotRemoveCastToInterfaceWhenNoDefaultArgsPassedAndValuesAreDifferent()
    {
        var source = """
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
                    ((I)new C()).Goo();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoRemoveCastToInterfaceWhenNamesAreTheSameAndNoNameProvided()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoRemoveCastToInterfaceWhenNamesAreDifferentAndNoNameProvided()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoNotRemoveCastToInterfaceWhenNamesAreDifferentAndNameProvided()
    {
        var source = """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task DoNotRemoveCastToInterfaceForSealedType7()
    {
        var source =
            """
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
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public async Task DoNotRemoveCastToInterfaceForSealedType8()
    {
        // Note: The cast below can't be removed (even though C is sealed)
        // because the specified named arguments refer to parameters that
        // appear at different positions in the member signatures.

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(8,18): error CS0535: 'C' does not implement interface member 'I.this[int, int]'
                DiagnosticResult.CompilerError("CS0535").WithSpan(8, 18, 8, 19).WithArguments("C", "I.this[int, int]"),
                // /0/Test0.cs(10,16): error CS0548: 'C.this[(int y, ?), int]': property or indexer must have at least one accessor
                DiagnosticResult.CompilerError("CS0548").WithSpan(10, 16, 10, 20).WithArguments("C.this[(int y, ?), int]"),
                // /0/Test0.cs(10,20): error CS1003: Syntax error, '[' expected
                DiagnosticResult.CompilerError("CS1003").WithSpan(10, 20, 10, 21).WithArguments("["),
                // /0/Test0.cs(10,27): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type '(int y, ?)'
                DiagnosticResult.CompilerError("CS1750").WithSpan(10, 27, 10, 27).WithArguments("int", "(int y, ?)"),
                // /0/Test0.cs(10,27): error CS1001: Identifier expected
                DiagnosticResult.CompilerError("CS1001").WithSpan(10, 27, 10, 28),
                // /0/Test0.cs(10,27): error CS1026: ) expected
                DiagnosticResult.CompilerError("CS1026").WithSpan(10, 27, 10, 28),
                // /0/Test0.cs(10,27): error CS8124: Tuple must contain at least two elements.
                DiagnosticResult.CompilerError("CS8124").WithSpan(10, 27, 10, 28),
                // /0/Test0.cs(10,41): error CS1003: Syntax error, ']' expected
                DiagnosticResult.CompilerError("CS1003").WithSpan(10, 41, 10, 42).WithArguments("]"),
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
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545883")]
    public async Task DoNotRemoveCastToInterfaceForSealedType9()
    {
        // Note: The cast below can't be removed (even though C is sealed)
        // because it would result in binding to a Dispose method that doesn't
        // implement IDisposable.Dispose().

        var source =
            """
            using System;
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
                    Console.WriteLine("new Dispose()");
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545887")]
    public async Task DoNotRemoveCastToInterfaceForStruct1()
    {
        // Note: The cast below can't be removed because the cast boxes 's' and
        // unboxing would change program behavior.

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545834")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task RemoveCastToInterfaceForStruct2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544655")]
    public async Task RemoveCastToICloneableForDelegate()
    {
        // Note: The cast below can be removed because delegates are implicitly
        // sealed.

        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    Action a = () => { };
                    var c = ([|(ICloneable)|]a).Clone();
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main()
                {
                    Action a = () => { };
                    var c = a.Clone();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545926")]
    public async Task RemoveCastToICloneableForArray()
    {
        // Note: The cast below can be removed because arrays are implicitly
        // sealed.

        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    var a = new[] { 1, 2, 3 };
                    var c = ([|(ICloneable)|]a).Clone(); 
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Main()
                {
                    var a = new[] { 1, 2, 3 };
                    var c = a.Clone(); 
                }
            }
            """);
    }

    [Fact]
    public async Task RemoveCastToInterfaceForString()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string s)
                {
                    IEnumerable<char> i = [|(IEnumerable<char>)|]s;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main(string s)
                {
                    IEnumerable<char> i = s;
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task RemoveCastToIConvertibleForEnum()
    {
        // Note: The cast below can be removed because enums are implicitly
        // sealed.

        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Enum e = DayOfWeek.Monday;
                    var y = ([|(IConvertible)|]e).GetTypeCode();
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Enum e = DayOfWeek.Monday;
                    var y = e.GetTypeCode();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task KeepCastToIConvertibleOnNonCopiedStruct()
    {
        var source = """
            using System;

            class Program
            {
                static void Main(DateTime dt)
                {
                    var y = ((IConvertible)dt).GetTypeCode();
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task RemoveCastToIConvertibleOnCopiedStruct1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var y = ([|(IConvertible)|](DateTime.Now + TimeSpan.Zero)).GetTypeCode();
                }
            }
            """, """
            using System;

            class Program
            {
                static void Main()
                {
                    var y = (DateTime.Now + TimeSpan.Zero).GetTypeCode();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task KeepCastToIConvertibleOnByRefIndexer()
    {
        var source = """
            using System;

            class Program
            {
                ref DateTime this[int i] => ref this[i];

                void Main()
                {
                    var y = ((IConvertible)this[0]).GetTypeCode();
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task RemoveCastToIConvertibleOnIndexer()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                DateTime this[int i] => default;

                void Main()
                {
                    var y = ([|(IConvertible)|]this[0]).GetTypeCode();
                }
            }
            """,
            """
            using System;

            class Program
            {
                DateTime this[int i] => default;

                void Main()
                {
                    var y = this[0].GetTypeCode();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task KeepCastToIConvertibleOnByRefProperty()
    {
        var source = """
            using System;

            class Program
            {
                ref DateTime X => ref X;

                void Main()
                {
                    var y = ((IConvertible)X).GetTypeCode();
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task RemoveCastToIConvertibleOnProperty()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                DateTime X => default;

                void Main()
                {
                    var y = ([|(IConvertible)|]X).GetTypeCode();
                }
            }
            """,
            """
            using System;

            class Program
            {
                DateTime X => default;

                void Main()
                {
                    var y = X.GetTypeCode();
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task KeepCastToIConvertibleOnByRefMethod()
    {
        var source = """
            using System;

            class Program
            {
                ref DateTime X() => ref X();

                void Main()
                {
                    var y = ((IConvertible)X()).GetTypeCode();
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public async Task RemoveCastToIConvertibleOnMethod()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                DateTime X() => default;

                void Main()
                {
                    var y = ([|(IConvertible)|]X()).GetTypeCode();
                }
            }
            """,
            """
            using System;

            class Program
            {
                DateTime X() => default;

                void Main()
                {
                    var y = X().GetTypeCode();
                }
            }
            """);
    }

    #endregion

    #region ParamArray Parameter Casts

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545141")]
    public async Task DoNotRemoveCastToObjectInParamArrayArg1()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task DoNotRemoveCastToIntArrayInParamArrayArg2()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task DoNotRemoveCastToObjectArrayInParamArrayArg3()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task RemoveCastToObjectArrayInParamArrayArg1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo([|(object[])|]null);
                }
            }
            """,
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo(null);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task RemoveCastToStringArrayInParamArrayArg2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo([|(string[])|]null);
                }
            }
            """,
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo(null);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task RemoveCastToIntArrayInParamArrayArg3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                static void Goo(params int[] x) { }

                static void Main()
                {
                    Goo([|(int[])|]null);
                }
            }
            """,
            """
            class C
            {
                static void Goo(params int[] x) { }

                static void Main()
                {
                    Goo(null);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task RemoveCastToObjectArrayInParamArrayArg4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo([|(object[])|]null, null);
                }
            }
            """,
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo(null, null);
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public async Task RemoveCastToObjectInParamArrayArg5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo([|(object)|]null, null);
                }
            }
            """,
            """
            class C
            {
                static void Goo(params object[] x) { }

                static void Main()
                {
                    Goo(null, null);
                }
            }
            """);
    }

    [Fact]
    public async Task RemoveCastToObjectArrayInParamArrayWithNamedArgument()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                static void Main()
                {
                    Goo(x: [|(object[])|]null);
                }

                static void Goo(params object[] x) { }
            }
            """,
            """
            class C
            {
                static void Main()
                {
                    Goo(x: null);
                }

                static void Goo(params object[] x) { }
            }
            """);
    }

    #endregion

    #region ForEach Statements

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public async Task DoNotRemoveNecessaryCastInForEach1()
    {
        // The cast below can't be removed because it would result an error
        // in the foreach statement.

        var source =
            """
            using System.Collections;

            class Program
            {
                static void Main()
                {
                    object s = ";
                    foreach (object x in (IEnumerable)s)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(7,20): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(7, 20, 7, 20),
                // /0/Test0.cs(7,22): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(7, 22, 7, 22),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public async Task DoNotRemoveNecessaryCastInForEach2()
    {
        // The cast below can't be removed because it would result an error
        // in the foreach statement.

        var source =
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    object s = ";
                    foreach (object x in (IEnumerable<char>)s)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(7,20): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(7, 20, 7, 20),
                // /0/Test0.cs(7,22): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(7, 22, 7, 22),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public async Task DoNotRemoveNecessaryCastInForEach3()
    {
        // The cast below can't be removed because it would result an error
        // in the foreach statement since C doesn't contain a GetEnumerator()
        // method.

        var source =
            """
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
                public static implicit operator D(C c)
                {
                    return new D();
                }

                static void Main()
                {
                    object s = ";
                    foreach (object x in (D)new C())
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(20,20): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(20, 20, 20, 20),
                // /0/Test0.cs(20,22): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(20, 22, 20, 22),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public async Task DoNotRemoveNecessaryCastInForEach4()
    {
        // The cast below can't be removed because it would result in
        // C.GetEnumerator() being called rather than D.GetEnumerator().

        var source =
            """
            using System;
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
                    object s = ";
                    foreach (object x in (D)new C())
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            [
                // /0/Test0.cs(26,20): error CS1010: Newline in constant
                DiagnosticResult.CompilerError("CS1010").WithSpan(26, 20, 26, 20),
                // /0/Test0.cs(26,22): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(26, 22, 26, 22),
            ],
            source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public async Task DoNotRemoveNecessaryCastInForEach5()
    {
        // The cast below can't be removed because it would change the
        // type of 'x'.

        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    string[] s = {
                        "A"
                    };
                    foreach (var x in (Array)s)
                    {
                        var y = x;
                        y = 1;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    #endregion

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
    public async Task DoNotRemoveCastIfOverriddenMethodHasIncompatibleParameterList()
    {
        // Note: The cast below can't be removed because the parameter list
        // of Goo and its override have different default values.

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
    public async Task RemoveCastIfOverriddenMethodHaveCompatibleParameterList()
    {
        // Note: The cast below can be removed because the parameter list
        // of Goo and its override have the same default values.

        await VerifyCS.VerifyCodeFixAsync(
            """
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
            """,
            """
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
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529916")]
    public async Task RemoveCastInReceiverForMethodGroup()
    {
        // Note: The cast below can be removed because the it results in
        // the same method group.

        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            static class Program
            {
                static void Main()
                {
                    Action a = ([|(string)|]"").Goo;
                }

                static void Goo(this string x) { }
            }
            """,
            """
            using System;

            static class Program
            {
                static void Main()
                {
                    Action a = "".Goo;
                }

                static void Goo(this string x) { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
    public async Task Bugfix_609497()
    {
        var source =
            """
            using System;
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545995")]
    public async Task DoNotRemoveCastToDifferentTypeWithSameName()
    {
        // Note: The cast below cannot be removed because the it results in
        // a different overload being picked.

        var source =
            """
            using System;
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
                    Console.WriteLine("int");
                }

                static void Goo(MyInt x)
                {
                    Console.WriteLine("MyInt");
                }

                static void Main()
                {
                    Goo((MyInt)0);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545921")]
    public async Task DoNotRemoveCastWhichWouldChangeAttributeOverloadResolution1()
    {
        // Note: The cast below cannot be removed because it would result in
        // a different attribute constructor being picked

        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624252")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608180")]
    public async Task DoNotRemoveCastIfArgumentIsRestricted_TypedReference()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments()
    {
        var source =
            """
            using System;

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
                    Console.WriteLine(Goo(x, (object)"", ""));
                }

                static void Goo(int x, string y, T z)
                {
                }

                static bool Goo(int x, object y, object z)
                {
                    return true;
                }
                
                static bool Goo(long x, object y, object z)
                {
                    return true;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments_Bracketed()
    {
        var source =
            """
            class C<T>
            {
                int this[int x, T s, string d = "abc"]
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
                
                int this[long x, object s, object d]
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
                    var y = this[x: xx, s: "", d: (object)""];
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt()
    {
        var source =
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d((object)"");
                    return true;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_1()
    {
        var source =
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d.goo((object)"");
                    return true;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_2()
    {
        var source =
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d.goo.bar.goo((object)"");
                    return true;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_3()
    {
        var source =
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d.goo().bar().goo((object)"");
                    return true;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public async Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments_1()
    {
        var source =
            """
            using System;

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
                    Console.WriteLine(Goo((object)"", x, ""));
                }

                static void Goo(string y, int x, T z)
                {
                }

                static bool Goo(object y, int x, object z)
                {
                    return true;
                }
                
                static bool Goo(object y, long x, object z)
                {
                    return true;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545998")]
    public async Task DoNotRemoveCastWhichWouldChangeAttributeOverloadResolution2()
    {
        // Note: The cast below cannot be removed because it would result in
        // a different attribute constructor being picked

        var source =
            """
            using System;

            [A(new[] { (long)0 })]
            class A : Attribute
            {
                public A(long[] x)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529894")]
    public async Task DoNotUnnecessaryCastFromEnumToUint()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
    public async Task DoNotUnnecessaryCastFromTypeParameterToObject()
    {
        var source =
            """
            class C
            {
                static void Goo<T>(T x, object y)
                {
                    if ((object)x == y)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640136")]
    public async Task RemoveUnnecessaryCastAndParseCorrect()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo(Task<Action> x)
                {
                    (([|(Task<Action>)|]x).Result)();
                }
            }
            """;
        var fixedSource =
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo(Task<Action> x)
                {
                    (x.Result)();
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(9,10): error CS0118: 'x' is a variable but is used like a type
                    DiagnosticResult.CompilerError("CS0118").WithSpan(8, 10, 8, 11).WithArguments("x", "variable", "type"),
                    // /0/Test0.cs(9,20): error CS1525: Invalid expression term ')'
                    DiagnosticResult.CompilerError("CS1525").WithSpan(8, 20, 8, 21).WithArguments(")"),
                },
            },
            // The code fix in this case does not produce valid code or a valid syntax tree
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/626026")]
    public async Task DoNotRemoveCastIfUserDefinedExplicitCast()
    {
        var source =
            """
            class Program
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768895")]
    public async Task DoNotRemoveNecessaryCastInTernary()
    {
        var source =
            """
            class Program
            {
                static void Main(string[] args)
                {
                    object x = null;
                    int y = (bool)x ? 1 : 0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770187")]
    public async Task DoNotRemoveNecessaryCastInSwitchExpression()
    {
        var source =
            """
            namespace ConsoleApplication23
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2761")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844482")]
    public async Task DoNotRemoveCastFromBaseToDerivedWithExplicitReference()
    {
        var source =
            """
            class Program
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3254")]
    public async Task DoNotRemoveCastToTypeParameterWithExceptionConstraint()
    {
        var source =
            """
            using System;

            class Program
            {
                private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : Exception
                {
                    if (!condition)
                    {
                        throw (TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition);
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3254")]
    public async Task DoNotRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
    {
        var source =
            """
            using System;

            class Program
            {
                private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : ArgumentException
                {
                    if (!condition)
                    {
                        throw (TException)Activator.CreateInstance(typeof(TException), messageOnFalseCondition);
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8111")]
    public async Task DoNotRemoveCastThatChangesShapeOfAnonymousTypeObject()
    {
        var source =
            """
            class Program
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8111")]
    public async Task RemoveCastThatDoesntChangeShapeOfAnonymousTypeObject()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
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
            }
            """,
            """
            class Program
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
            }
            """);
    }

    [Fact]
    public async Task Tuple()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void Main()
                {
                    (int, string) tuple = [|((int, string))|](1, "hello");
                }
            }
            """,
            """
            class C
            {
                void Main()
                {
                    (int, string) tuple = (1, "hello");
                }
            }
            """);
    }

    [Fact]
    public async Task TupleWithDifferentNames()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void Main()
                {
                    (int a, string) tuple = [|((int, string d))|](1, f: "hello");
                }
            }
            """,
            """
            class C
            {
                void Main()
                {
                    (int a, string) tuple = (1, f: "hello");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24791")]
    public async Task SimpleBoolCast()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                bool M()
                {
                    if (![|(bool)|]M()) throw null;
                    throw null;
                }
            }
            """,
            """
            class C
            {
                bool M()
                {
                    if (!M()) throw null;
                    throw null;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12572")]
    public async Task DoNotRemoveCastThatUnboxes()
    {
        // The cast below can't be removed because it could throw a null ref exception.
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17029")]
    public async Task DoNotRemoveCastOnEnumComparison1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(13,21): error CS0246: The type or namespace name 'InvalidOperationException' could not be found (are you missing a using directive or an assembly reference?)
            DiagnosticResult.CompilerError("CS0246").WithSpan(12, 21, 12, 46).WithArguments("InvalidOperationException"),
            source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17029")]
    public async Task DoNotRemoveCastOnEnumComparison2()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(13,21): error CS0246: The type or namespace name 'InvalidOperationException' could not be found (are you missing a using directive or an assembly reference?)
            DiagnosticResult.CompilerError("CS0246").WithSpan(12, 21, 12, 46).WithArguments("InvalidOperationException"),
            source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18978")]
    public async Task DoNotRemoveCastOnCallToMethodWithParamsArgs()
    {
        var source =
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    var takesArgs = new[] { "Hello", "World" };
                    TakesParams((object)takesArgs);
                }

                private static void TakesParams(params object[] goo)
                {
                    Console.WriteLine(goo.Length);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(12,9): error CS0103: The name 'Console' does not exist in the current context
            DiagnosticResult.CompilerError("CS0103").WithSpan(11, 9, 11, 16).WithArguments("Console"),
            source);
    }

    [Fact]
    public async Task DoRemoveCastOnCallToMethodWithInvalidParamsArgs()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    TakesParams([|(Program)|]null);
                }

                private static void TakesParams({|CS0225:params|} Program wrongDefined)
                {
                }
            }
            """,
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    TakesParams(null);
                }

                private static void TakesParams({|CS0225:params|} Program wrongDefined)
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18978")]
    public async Task RemoveCastOnCallToMethodWithParamsArgsIfImplicitConversionExists()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    var takesArgs = new[] { "Hello", "World" };
                    TakesParams([|(System.IComparable[])|]takesArgs);
                }

                private static void TakesParams(params object[] goo)
                {
                    System.Console.WriteLine(goo.Length);
                }
            }
            """,
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    var takesArgs = new[] { "Hello", "World" };
                    TakesParams(takesArgs);
                }

                private static void TakesParams(params object[] goo)
                {
                    System.Console.WriteLine(goo.Length);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgs()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29264")]
    public async Task DoNotRemoveCastOnDictionaryIndexer()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29264")]
    public async Task RemoveCastOnDictionaryIndexer()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgsAndProperty()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgsPropertyAndOtherArg()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task DoNotRemoveCastOnCallToAttributeWithParamsArgsNamedArgsAndProperty()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task DoRemoveCastOnCallToAttributeWithInvalidParamsArgs()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, {|CS0225:params|} object wrongDefined)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(true, [|(object)|]null, Prop = 1)]
            static class Program
            {
            }
            """,
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, {|CS0225:params|} object wrongDefined)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(true, null, Prop = 1)]
            static class Program
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task RemoveCastOnCallToAttributeWithParamsArgsWithImplicitCast()
    {
        var source =
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params object[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(arr: [|(object[])|]new[] { "Hello", "World" }, otherArg: true, Prop = 1)]
            static class Program
            {
            }
            """;
        var fixedSource =
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params object[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(arr: (new[] { "Hello", "World" }), otherArg: true, Prop = 1)]
            static class Program
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(11,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                    DiagnosticResult.CompilerError("CS0182").WithSpan(10, 2, 10, 75),
                },
            },
            FixedState =
            {
                Sources = { fixedSource },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(11,2): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                    DiagnosticResult.CompilerError("CS0182").WithSpan(10, 2, 10, 67),
                },
            },
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public async Task RemoveCastOnCallToAttributeWithCastInPropertySetter()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/18510")]
    [InlineData("-")]
    [InlineData("+")]
    public async Task DoNotRemoveCastOnInvalidUnaryOperatorEnumValue1(string op)
    {
        var source =
            $$"""
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
                        Sign invertedSign = (Sign) ( {{op}}((int) mySign) );
                    }
                }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/18510")]
    [InlineData("-")]
    [InlineData("+")]
    public async Task DoNotRemoveCastOnInvalidUnaryOperatorEnumValue2(string op)
    {
        var source =
            $$"""
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
                        Sign invertedSign = (Sign) ( {{op}}(int) mySign );
                    }
                }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18510")]
    public async Task RemoveCastOnValidUnaryOperatorEnumValue()
    {
        var source =
            """
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
                }
            """;
        var fixedSource =
            """
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
                }
            """;

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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18510")]
    public async Task RemoveCastOnValidUnaryOperatorEnumValue_Nullable()
    {
        var source =
            """
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
            }
            """;
        var fixedSource =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18510")]
    public async Task DoNotRemoveEnumCastToDifferentRepresentation()
    {
        var source =
            """
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
                }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25456#issuecomment-373549735")]
    public async Task DoNotIntroduceDefaultLiteralInSwitchCase()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInSwitchCase_CastInsideParentheses()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInSwitchCase_DefaultInsideParentheses()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInSwitchCase_RemoveDoubleCast()
    {
        var source =
            """
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
            }
            """;
        var fixedSource =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12631")]
    public async Task RemoveRedundantBoolCast()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M()
                {
                    var a = true;
                    var b = ![|(bool)|]a;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var a = true;
                    var b = !a;
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_CastInsideParentheses()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_DefaultInsideParentheses()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_RemoveDoubleCast()
    {
        var source =
            """
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
            }
            """;
        var fixedSource =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternSwitchCase_RemoveInsideWhenClause()
    {
        var source =
            """
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
            }
            """;
        var fixedSource =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternIs()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    if (true is (bool)default);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternIs_CastInsideParentheses()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    if (true is ((bool)default));
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternIs_DefaultInsideParentheses()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    if (true is (bool)(default));
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPatternIs_RemoveDoubleCast()
    {
        var source =
            """
            class C
            {
                void M()
                {
                    if (true is [|(bool)|][|(bool)|]default);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void M()
                {
                    if (true is (bool)default) ;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            LanguageVersion = LanguageVersion.CSharp7_1,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPropertyPattern1()
    {
        var source =
            """
            class C
            {
                void M(string s)
                {
                    if (s is { Length: (int)default })
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact]
    public async Task DoNotIntroduceDefaultLiteralInPropertyPattern2()
    {
        var source =
            """
            class C
            {
                void M(string s)
                {
                    if (s is { Length: ((int)default) })
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27239")]
    public async Task DoNotOfferToRemoveCastWhereNoConversionExists()
    {
        var source =
            """
            using System;

            class C
            {
                void M()
                {
                    object o = null;
                    TypedReference r2 = {|CS0030:(TypedReference)o|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28412")]
    public async Task DoNotOfferToRemoveCastWhenAccessingHiddenProperty()
    {
        var source = """
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
                    ((Fruit)a).Properties["Color"] = "Red";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31963")]
    public async Task DoNotOfferToRemoveCastInConstructorWhenItNeeded()
    {
        var source = """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31963")]
    public async Task DoNotOfferToRemoveCastInBaseConstructorInitializerWhenItNeeded()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            // /0/Test0.cs(10,19): error CS0122: 'B.B(int)' is inaccessible due to its protection level
            DiagnosticResult.CompilerError("CS0122").WithSpan(9, 19, 9, 23).WithArguments("B.B(int)"),
            source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31963")]
    public async Task DoNotOfferToRemoveCastInConstructorInitializerWhenItNeeded()
    {
        var source =
            """
            class B
            {
                B(int a)
                {
                }

                B(double a) : this((int)a)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10220")]
    public async Task DoNotRemoveObjectCastInParamsCall()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22804")]
    public async Task DoNotRemoveCastFromNullableToUnderlyingType()
    {
        var source =
            """
            using System.Text;

            class C
            {
                private void M()
                {
                    StringBuilder numbers = new StringBuilder();
                    int?[] position = new int?[2];
                    numbers[(int)position[1]] = 'x';
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41433")]
    public async Task DoNotRemoveCastFromIntPtrToPointer()
    {
        var source =
            """
            using System;

            class C
            {
                unsafe int Test(IntPtr safePointer)
                {
                    return ((int*)safePointer)[0];
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38599")]
    public async Task DoNotRemoveCastFromIntPtrToPointerInReturn()
    {
        var source =
            """
            using System;

            class Program
            {
                public static unsafe int Read(IntPtr pointer, int offset)
                {
                    return ((int*)pointer)[offset];
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32491")]
    public async Task DoNotRemoveCastFromIntPtrToPointerWithTypeParameter()
    {
        var source =
            """
            using System;

            struct Block<T>
                where T : unmanaged
            {
                IntPtr m_ptr;
                unsafe ref T GetRef( int index )
                {
                    return ref ((T*)m_ptr)[index];
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25021")]
    public async Task DoNotRemoveCastFromIntPtrToPointerWithAddressAndCast()
    {
        var source =
            """
            using System;

            class C
            {
                private unsafe void goo()
                {
                    var address = IntPtr.Zero;
                    var bar = (int*)&((long*)address)[10];
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38347")]
    public async Task TestArgToLocalFunction1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38347")]
    public async Task TestArgToLocalFunction2()
    {
        var source =
            """
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
            }
            """;
        var fixedSource =
            """
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
            }
            """;
        var batchFixedSource =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            BatchFixedCode = batchFixedSource,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            DiagnosticSelector = diagnostics => diagnostics[1],
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString1()
    {
        var source =
            """
            using System;

            class C
            {
                private void goo()
                {
                    object x = (IFormattable)$"";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString1_1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                private void goo()
                {
                    IFormattable x = [|(IFormattable)|]$"";
                }
            }
            """,
            """
            using System;

            class C
            {
                private void goo()
                {
                    IFormattable x = $"";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString2()
    {
        var source =
            """
            using System;

            class C
            {
                private void goo()
                {
                    object x = (FormattableString)$"";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString2_2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                private void goo()
                {
                    FormattableString x = [|(FormattableString)|]$"";
                }
            }
            """,
            """
            using System;

            class C
            {
                private void goo()
                {
                    FormattableString x = $"";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString3()
    {
        var source =
            """
            using System;

            class C
            {
                private void goo()
                {
                    bar((FormattableString)$"");
                }

                private void bar(string s) { }
                private void bar(FormattableString s) { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                private void goo()
                {
                    bar([|(FormattableString)|]$"");
                }

                private void bar(FormattableString s) { }
            }
            """,
            """
            using System;

            class C
            {
                private void goo()
                {
                    bar($"");
                }

                private void bar(FormattableString s) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                private void goo()
                {
                    object o = [|(string)|]$"";
                }
            }
            """,
            """
            using System;

            class C
            {
                private void goo()
                {
                    object o = $"";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString6()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                private void goo()
                {
                    bar([|(IFormattable)|]$"");
                }

                private void bar(IFormattable s) { }
            }
            """,
            """
            using System;

            class C
            {
                private void goo()
                {
                    bar($"");
                }

                private void bar(IFormattable s) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36631")]
    public async Task TestFormattableString7()
    {
        var source =
            """
            using System;

            class C
            {
                private void goo()
                {
                    object x = (IFormattable)$@"";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task TestMissingOnInterfaceCallOnNonSealedClass()
    {
        var source =
            """
            using System;

            public class DbContext : IDisposable
            {
                public void Dispose()
                {
                    Console.WriteLine("Base called");
                }
            }

            public class MyContext : DbContext, IDisposable
            {
                void IDisposable.Dispose()
                {
                    Console.WriteLine("Derived called");
                }
            }

            class C
            {
                private static readonly DbContext _dbContext = new MyContext();

                static void Main()
                {
                    ((IDisposable)_dbContext).Dispose();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task TestMissingOnInterfaceCallOnNonReadOnlyStruct()
    {
        var source =
            """
            using System;

            public struct DbContext : IDisposable
            {
                public int DisposeCount;
                public void Dispose()
                {
                    DisposeCount++;
                }
            }

            class C
            {
                private static DbContext _dbContext = default;

                static void Main()
                {
                    ((IDisposable)_dbContext).Dispose();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task TestMissingOnInterfaceCallOnReadOnlyStruct()
    {
        // We technically could support this.  But we choose not to for simplicity. While semantics could be
        // preserved, the semantics around interfaces are subtle and we don't want to make a change that might
        // negatively impact the user if they make other code changes.
        var source =
            """
            using System;

            public struct DbContext : IDisposable
            {
                public int DisposeCount;
                public void Dispose()
                {
                    DisposeCount++;
                }
            }

            class C
            {
                private static readonly DbContext _dbContext = default;

                static void Main()
                {
                    ((IDisposable)_dbContext).Dispose();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(
            source,
            source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34326")]
    public async Task TestOnInterfaceCallOnSealedClass()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public sealed class DbContext : IDisposable
            {
                public void Dispose()
                {
                    Console.WriteLine("Base called");
                }
            }

            class C
            {
                private readonly DbContext _dbContext = null;

                void Main()
                {
                    ([|(IDisposable)|]_dbContext).Dispose();
                }
            }
            """,
            """
            using System;

            public sealed class DbContext : IDisposable
            {
                public void Dispose()
                {
                    Console.WriteLine("Base called");
                }
            }

            class C
            {
                private readonly DbContext _dbContext = null;

                void Main()
                {
                    _dbContext.Dispose();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29726")]
    public async Task TestDefaultLiteralWithNullableCastInCoalesce()
    {
        var source =
            """
            using System;

            public class C
            {
                public void Goo()
                {
                    int x = (int?)(int)default ?? 42;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6309")]
    public async Task TestFPIdentityThatMustRemain1()
    {
        var source =
            """
            using System;

            public class C
            {
                float X() => 2 / (float)X();
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestFPIdentityThatMustRemain2()
    {
        var source =
            """
            using System;

            public class C
            {
                void M()
                {
                    float f1 = 0.00000000002f;
                    float f2 = 1 / f1;
                    double d = (float)f2;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestFPIdentityThatMustRemain3()
    {
        var source =
            """
            using System;

            public class C
            {
                void M()
                {
                    float f1 = 0.00000000002f;
                    float f2 = 1 / f1;
                    float f3 = (float)f2;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnFieldRead()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                float f;

                void M()
                {
                    var v = [|(float)|]f;
                }
            }
            """,
            """
            using System;

            public class C
            {
                float f;

                void M()
                {
                    var v = f;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnFieldWrite()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                float f;

                void M(float f1)
                {
                    f = [|(float)|]f1;
                }
            }
            """,
            """
            using System;

            public class C
            {
                float f;

                void M(float f1)
                {
                    f = f1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityInFieldInitializer()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                static float f1;
                static float f2 = [|(float)|]f1;
            }
            """,
            """
            using System;

            public class C
            {
                static float f1;
                static float f2 = f1;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnArrayRead()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                float[] f;

                void M()
                {
                    var v = [|(float)|]f[0];
                }
            }
            """,
            """
            using System;

            public class C
            {
                float[] f;

                void M()
                {
                    var v = f[0];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnArrayWrite()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                float[] f;

                void M(float f2)
                {
                    f[0] = [|(float)|]f2;
                }
            }
            """,
            """
            using System;

            public class C
            {
                float[] f;

                void M(float f2)
                {
                    f[0] = f2;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnArrayInitializer1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                void M(float f2)
                {
                    float[] f = { [|(float)|]f2 };
                }
            }
            """,
            """
            using System;

            public class C
            {
                void M(float f2)
                {
                    float[] f = { f2 };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnArrayInitializer2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                void M(float f2)
                {
                    float[] f = new float[] { [|(float)|]f2 };
                }
            }
            """,
            """
            using System;

            public class C
            {
                void M(float f2)
                {
                    float[] f = new float[] { f2 };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPIdentityOnImplicitArrayInitializer()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                void M(float f2)
                {
                    float[] f = new[] { [|(float)|]f2 };
                }
            }
            """,
            """
            using System;

            public class C
            {
                void M(float f2)
                {
                    float[] f = new[] { f2 };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPWithBoxing1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                void M()
                {
                    float value = 0.0f;
                    object boxed = [|(float)|]value;
                }
            }
            """,
            """
            using System;

            public class C
            {
                void M()
                {
                    float value = 0.0f;
                    object boxed = value;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34873")]
    public async Task TestCanRemoveFPWithBoxing2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C
            {
                void M()
                {
                    double value = 0.0;
                    object boxed = [|(double)|]value;
                }
            }
            """,
            """
            using System;

            public class C
            {
                void M()
                {
                    double value = 0.0;
                    object boxed = value;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37953")]
    public async Task TestCanRemoveFromUnnecessarySwitchExpressionCast1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                public static void Main() { }

                public static string GetValue(DayOfWeek value)
                    => [|(DayOfWeek)|]value switch
                    {
                        DayOfWeek.Monday => "Monday",
                        _ => "Other",
                    };
            }
            """,
            """
            using System;

            class Program
            {
                public static void Main() { }

                public static string GetValue(DayOfWeek value)
                    => value switch
                    {
                        DayOfWeek.Monday => "Monday",
                        _ => "Other",
                    };
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37953")]
    public async Task TestLeaveNecessarySwitchExpressionCast1()
    {
        var source =
            """
            using System;

            class Program
            {
                public static void Main() { }

                public static string GetValue(int value)
                    => (DayOfWeek)value switch
                    {
                        DayOfWeek.Monday => "Monday",
                        _ => "Other",
                    };
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrAssignment1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrBinary1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrBinary2()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithAndAssignment1()
    {

        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithAndBinary1()
    {

        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithAndBinary2()
    {

        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase2()
    {
        // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase3()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase4()
    {
        // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase5()
    {

        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase6()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase7()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase8()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCase9()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable2()
    {
        // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """, """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable3()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable4()
    {
        // there is a sign extension warning both before and after.  so this is not worse to remove the cast.
        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """, """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable5()
    {

        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable6()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable7()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable8()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40414")]
    public async Task TestSignExtensionWithOrCompilerCaseNullable9()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveNullCastInSwitch1()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((object)null)
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveNullCastInSwitch2()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((object)(null))
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveNullCastInSwitch3()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((bool?)null)
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveNullCastInSwitch4()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((bool?)(null))
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveNullCastInSwitch5()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch (((object)null))
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveDefaultCastInSwitch1()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((object)default)
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveDefaultCastInSwitch2()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((object)(default))
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveDefaultCastInSwitch3()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((bool?)default)
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveDefaultCastInSwitch4()
    {
        var source =
            """
            class Program
            {
                static void Main()
                {
                    switch ((bool?)(default))
                    {
                      case bool _:
                        break;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20211")]
    public async Task DoNotRemoveDoubleNullCastInSwitch1()
    {
        // Removing the 'object' cast would make `case object:` unreachable.
        var source =
            """
            class Program
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional1()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (int?)1 : default;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional2()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? ((int?)1) : default;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional3()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (int?)1 : (default);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional4_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (int?)1 : null;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoRemoveUnnecessaryCastInConditional4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? [|(int?)|]1 : null;
                }
            }
            """,
            FixedCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? 1 : null;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional5_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? ((int?)1) : null;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoRemoveUnnecessaryCastInConditional5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? ([|(int?)|]1) : null;
                }
            }
            """,
            FixedCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? 1 : null;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional6_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (int?)1 : (null);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoRemoveUnnecessaryCastInConditional6_CSharp8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? [|(int?)|]1 : (null);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? 1 : (null);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional7()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? default : (int?)1;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional8()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? default : ((int?)1);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional9()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (default) : (int?)1;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional10_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? null : (int?)1;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoRemoveUnnecessaryCastInConditional10()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? null : [|(int?)|]1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? null : 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional11_CSharp()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? null : ((int?)1);
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoRemoveUnnecessaryCastInConditional11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? null : ([|(int?)|]1);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? null : 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional12_CSharp8()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (null) : (int?)1;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoRemoveUnnecessaryCastInConditional12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (null) : [|(int?)|]1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? (null) : 1;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional13()
    {
        var source =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    var y = x ? (long?)z : null;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task DoNotRemoveNecessaryCastInConditional14()
    {
        var source =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    var y = x ? (long?)z : default;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnecessaryCastInConditional1()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? [|(int)|]1 : default;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? 1 : default;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnecessaryCastInConditional2()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? [|(int)|]1 : 0;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? 1 : 0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnecessaryCastInConditional3()
    {
        var source =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? [|(int)|]1 : z;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? 1 : z;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnecessaryCastInConditional4()
    {
        var source =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? [|(int?)|]1 : z;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? 1 : z;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnecessaryCastInConditional5()
    {
        var source =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? [|(int?)|]1 : 0;
                }
            }
            """;
        var fixedCode =
            """
            class C
            {
                void M(bool x)
                {
                    int? y = x ? 1 : 0;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnnecessaryCastInConditional6()
    {
        var source =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? [|(int?)|]z : null;
                }
            }
            """;
        var fixedCode =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? z : null;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem(20211, "https://github.com/dotnet/roslyn/issues/21613")]
    public async Task RemoveUnnecessaryCastInConditional7()
    {
        var source =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? [|(int?)|]z : default;
                }
            }
            """;
        var fixedCode =
            """
            class C
            {
                void M(bool x, int? z)
                {
                    int? y = x ? z : default;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20742")]
    public async Task DoNotRemoveNamedArgToParamsParameter1()
    {
        var source =
            """
            class Program
            {
                public void M()
                {
                    object[] takesArgs = null;
                    TakesParams(bar: (object)takesArgs, goo: true);
                }

                private void TakesParams(bool goo, params object[] bar)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20742")]
    public async Task DoRemoveNamedArgToParamsParameter1()
    {
        var source =
            """
            class Program
            {
                public void M()
                {
                    object[] takesArgs = null;
                    TakesParams(bar: [|(object[])|]takesArgs, goo: true);
                }

                private void TakesParams(bool goo, params object[] bar)
                {
                }
            }
            """;
        var fixedCode =
            """
            class Program
            {
                public void M()
                {
                    object[] takesArgs = null;
                    TakesParams(bar: takesArgs, goo: true);
                }

                private void TakesParams(bool goo, params object[] bar)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20742")]
    public async Task DoRemoveNamedArgToParamsParameter2()
    {
        var source =
            """
            class Program
            {
                public void M()
                {
                    string[] takesArgs = null;
                    TakesParams(bar: [|(object[])|]takesArgs, goo: true);
                }

                private void TakesParams(bool goo, params object[] bar)
                {
                }
            }
            """;
        var fixedCode =
            """
            class Program
            {
                public void M()
                {
                    string[] takesArgs = null;
                    TakesParams(bar: takesArgs, goo: true);
                }

                private void TakesParams(bool goo, params object[] bar)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact]
    public async Task ObjectCastInInterpolation1()
    {
        var source =
            """
            class Program
            {
                public void M(int x, int z)
                {
                    var v = $"x {[|(object)|]1} z";
                }
            }
            """;
        var fixedCode =
            """
            class Program
            {
                public void M(int x, int z)
                {
                    var v = $"x {1} z";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact]
    public async Task ObjectCastInInterpolation2()
    {
        var source =
            """
            class Program
            {
                public void M(int x, int z)
                {
                    var v = $"x {([|(object)|]1)} z";
                }
            }
            """;
        var fixedCode =
            """
            class Program
            {
                public void M(int x, int z)
                {
                    var v = $"x {1} z";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact]
    public async Task TestIdentityDoubleCast()
    {
        var source =
            """
            class Program
            {
                public void M(object x)
                {
                    var v = [|(int)|](int)x;
                }
            }
            """;
        var fixedCode =
            """
            class Program
            {
                public void M(object x)
                {
                    var v = (int)x;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison1()
    {
        var source = """
            using System;

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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison2()
    {
        var source = """
            using System;

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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison3()
    {
        var source = """
            using System;

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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison4()
    {
        var source = """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison5()
    {
        var source = """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison6()
    {
        var source = """
            using System;

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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison7()
    {
        var source = """
            using System;

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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestUnintendedReferenceComparison8()
    {
        var source = """
            using System;

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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison1()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison2()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison3()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison4()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison5()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison6()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison7()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task TestIntendedReferenceComparison8()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44382")]
    public async Task DoNotRemoveCastOnParameterInitializer1()
    {
        var source =
            """
            enum E : byte { }
            class C { void F() { void f(E e = (E)byte.MaxValue) { } } }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44382")]
    public async Task DoNotRemoveCastOnParameterInitializer2()
    {
        var source =
            """
            enum E : byte { }
            class C { void f(E e = (E)byte.MaxValue) { } }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45695")]
    public async Task DoNotRemoveNonObjectCastInsideInterpolation()
    {
        var source =
            """
            class Other
            {
                void Goo()
                {  
                    char c = '4';
                    string s = $"{(int)c:X4}";
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45695")]
    public async Task DoRemoveObjectCastInsideInterpolation()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Other
            {
                void Goo()
                {  
                    char c = '4';
                    string s = $"{[|(object)|]c:X4}";
                }
            }
            """,
            """
            class Other
            {
                void Goo()
                {  
                    char c = '4';
                    string s = $"{c:X4}";
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47800")]
    public async Task RemoveNativeIntCastsAsIdentity()
    {
        var source =
            """
            using System;

            public class C {
                public nint N(IntPtr x) => [|(nint)|]x;
            }
            """;
        var fixedCode =
            """
            using System;

            public class C {
                public nint N(IntPtr x) => x;
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47800")]
    public async Task DoRemoveNativeIntCasts()
    {
        var source =
            """
            using System;

            public class C {
                public nuint N(IntPtr x) => (nuint)(nint)x;
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47800")]
    public async Task RemoveNativeUIntCastsAsIdentity()
    {
        var source =
            """
            using System;

            public class C {
                public nuint N(UIntPtr x) => [|(nuint)|]x;
            }
            """;
        var fixedCode =
            """
            using System;

            public class C {
                public nuint N(UIntPtr x) => x;
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51123")]
    public async Task DoRemoveNativeIntCastsToInt()
    {
        var source =
            """
            using System;

            public class C {
                public int N(IntPtr x) => (int)(nint)x;
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47800")]
    public async Task DoRemoveNativeUIntCasts()
    {
        var source =
            """
            using System;

            public class C {
                public nint N(UIntPtr x) => (nint)(nuint)x;
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47800")]
    public async Task RemoveIntPtrCastsAsIdentity()
    {
        var source =
            """
            using System;

            class C
            {
                public void M(IntPtr x)
                {
                    var v = [|(IntPtr)|]x;
                }
            }
            """;
        var fixedCode =
            """
            using System;

            class C
            {
                public void M(IntPtr x)
                {
                    var v = x;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47800")]
    public async Task RemoveUIntPtrCastsAsIdentity()
    {
        var source =
            """
            using System;

            class C
            {
                public void M(UIntPtr x)
                {
                    var v = [|(UIntPtr)|]x;
                }
            }
            """;
        var fixedCode =
            """
            using System;

            class C
            {
                public void M(UIntPtr x)
                {
                    var v = x;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49140")]
    public async Task DoNotRemoveBitwiseNotOfUnsignedExtendedValue1()
    {
        var source =
            """
            class C
            {
                public static ulong P(ulong a, uint b)
                {
                    return a & ~(ulong)b;
                }
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49140")]
    public async Task DoNotRemoveBitwiseNotOfUnsignedExtendedValue2()
    {
        var source =
            """
            class C
            {
                public static nuint N(nuint a, uint b)
                {
                    return a & ~(nuint)b;
                }
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49140")]
    public async Task DoNotRemoveBitwiseNotOfUnsignedExtendedValue3()
    {
        var source =
            """
            class C
            {
                public static ulong N()
                {
                    return ~(ulong)uint.MaxValue;
                }
            }
            """;

        var test = new VerifyCS.Test()
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49140")]
    public async Task DoRemoveBitwiseNotOfSignExtendedValue1()
    {
        var test = new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                public static long P(long a, int b)
                {
                    return a & ~[|(long)|]b;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public static long P(long a, int b)
                {
                    return a & ~b;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49140")]
    public async Task DoRemoveBitwiseNotOfSignExtendedValue2()
    {

        var test = new VerifyCS.Test()
        {
            TestCode = """
            class C
            {
                public static nint N(nint a, int b)
                {
                    return a & ~[|(nint)|]b;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public static nint N(nint a, int b)
                {
                    return a & ~b;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50000")]
    public async Task KeepNecessaryCastIfRemovalWouldCreateIllegalConditionalExpression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                ushort Goo(string s)
                    => s is null ? (ushort)1234 : ushort.Parse(s);
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50000")]
    public async Task RemoveUnnecessaryCastWhenConditionalExpressionIsLegal()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                uint Goo(string s)
                    => s is null ? [|(uint)|]1234 : uint.Parse(s);
            }
            """,
            FixedCode = """
            class C
            {
                uint Goo(string s)
                    => s is null ? 1234 : uint.Parse(s);
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52524")]
    public async Task DoNotRemoveForValueTaskConstrutor()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53698")]
    public async Task DoNotRemoveForConditional()
    {
        var source =
            """
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
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55621")]
    public async Task DoNotRemoveForNullWithMultipleMatchingParameterTypes()
    {
        var source =
            """
            #nullable enable
            using System;
            public class TestClass
            {
            	public TestClass(object? value) { }
            	public TestClass(Func<object?> value) { }

                public TestClass Create1() => new ((object?)null);
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56207")]
    public async Task DoNotRemoveForNintPointerToVoidPointer()
    {
        var source =
            """
            using System;
            public class TestClass
            {
            	unsafe void M(nint** ptr)
                {
                    nint value = (nint)(void*)*ptr;
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact]
    public async Task RemoveUnnecessaryCastInPattern1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                void Main(int? n)
                {
                    var v = n is [|(int)|]0;
                }
            }
            """,
            """
            class Program
            {
                void Main(int? n)
                {
                    var v = n is 0;
                }
            }
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    public async Task RemoveUnnecessaryNullableCastInPattern1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class Program
            {
                void Main(int? n)
                {
                    var v = n is [|(int?)|]0;
                }
            }
            """,
            """
            class Program
            {
                void Main(int? n)
                {
                    var v = n is 0;
                }
            }
            """);
    }

    [Fact]
    public async Task DoNotRemoveBoxingEnumCast()
    {
        var source = """
            using System;
            class Program
            {
                static void M()
                {
                    object y = (DayOfWeek)0;
                    Console.WriteLine(y);
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoRemoveFPCastFromNonFPTypeToWidenedType1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
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
            }
            """, """
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
            }
            """);
    }

    [Fact]
    public async Task DoNotRemoveFPCastToWidenedType2()
    {
        var source = """
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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoNotRemoveCastIfOverriddenMethodHasDifferentReturnType()
    {
        var source =
            """
            using System;

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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task KeepCastToObjectToPreserveDynamicOverload()
    {
        var source =
            """
            using System;

            class C
            {
                static void Bar(int x, Action y) { }
                static void Bar(dynamic x, Action y) { }

                static void Main()
                {
                    Bar((object)1, Console.WriteLine);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task KeepIntToLongCastWithIComparable()
    {
        var source =
            """
            using System;

            class C
            {
                static void Main()
                {
                    IComparable<long> result = (long)0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task KeepCastNeededToPickCorrectOverload()
    {
        // removing the 'byte' cast will switch the overload called.
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CanRemoveExplicitCastToReferenceTypeWhenPassedToDynamic1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                static void Bar(dynamic x, Action y) { }

                static void Main()
                {
                    Bar([|(object)|]1, Console.WriteLine);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Bar(dynamic x, Action y) { }

                static void Main()
                {
                    Bar(1, Console.WriteLine);
                }
            }
            """);
    }

    [Fact]
    public async Task CanRemoveExplicitCastToReferenceTypeWhenPassedToDynamic2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                static void Bar(dynamic x, Action y) { }

                static void Main()
                {
                    Bar([|(IComparable)|]1, Console.WriteLine);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void Bar(dynamic x, Action y) { }

                static void Main()
                {
                    Bar(1, Console.WriteLine);
                }
            }
            """);
    }

    [Fact]
    public async Task NotOnWidenedNumericStoredInObject1()
    {
        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    int expr = 0;
                    object o1 = (long)expr;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task NotOnWidenedNumericConstantStoredInObject2()
    {
        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    object o1 = (long)0;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task OnNonWidenedNumericStoredInObject1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    long lng = 0;
                    object o1 = [|(long)|]lng;
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    long lng = 0;
                    object o1 = lng;
                }
            }
            """);
    }

    [Fact]
    public async Task OnNonWidenedNumericConstantStoredInObject2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    object o1 = [|(long)|]0L;
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    object o1 = 0L;
                }
            }
            """);
    }

    [Fact]
    public async Task DisallowNarrowingNullableNumericAsCast()
    {
        var source =
            """
            using System;

            class Program
            {
                static void Main()
                {
                    int? data = 1;
                    var x = data as byte?;
                    Console.WriteLine(x);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task AllowNonNarrowingNullableNumericAsCast()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    byte? data = 1;
                    var x = data [|as byte?|];
                    Console.WriteLine(x);
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    byte? data = 1;
                    var x = data;
                    Console.WriteLine(x);
                }
            }
            """);
    }

    [Fact]
    public async Task CanNotRemoveStackallocToVarOutsideOfUnsafeRegion()
    {
        var source = """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CanRemoveStackallocToSpanOutsideOfUnsafeRegion()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact]
    public async Task CanNotRemoveStackallocToVarInsideOfUnsafeRegion1()
    {
        var source = """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CanNotRemoveStackallocToVarInsideOfUnsafeRegion2()
    {
        var source = """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CanRemoveStackallocToSpanInsideOfUnsafeRegion1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact]
    public async Task CanRemoveStackallocToSpanInsideOfUnsafeRegion2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact]
    public async Task CanRemoveStackallocToVarInsideOfUnsafeRegion1()
    {
        // possibly incorrect error: https://github.com/dotnet/roslyn/issues/57040
        var source = """
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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CanRemoveParenthesizedStackallocToVarInsideOfUnsafeRegion1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """,
            """
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
            }
            """);
    }

    [Fact]
    public async Task CanRemoveStackallocToPointerInsideOfUnsafeRegion1()
    {
        // Possibly a compiler bug.  Unclear why conversion to `(int*)` not allowed.
        // https://github.com/dotnet/roslyn/issues/57040
        var source = """
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
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57062")]
    public async Task DoRemoveIdentityCastInConstantPattern1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is [|(int)|]0)
                    {
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is 0)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57062")]
    public async Task DoRemoveIdentityCastInConstantPattern2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is [|(long)|]0L)
                    {
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is 0L)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57062")]
    public async Task DoNotRemoveNonIdentityCastInConstantPattern1()
    {
        var source =
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is (sbyte)0)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57062")]
    public async Task DoNotRemoveNonIdentityCastInConstantPattern2()
    {
        var source =
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is (sbyte)0 or (short)0)
                    {
                    }
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57062")]
    public async Task DoNotRemoveNonIdentityCastInConstantPattern3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
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
            }
            """,
            FixedCode = """
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
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57062")]
    public async Task DoNotRemoveNonIdentityCastInConstantPattern4()
    {
        var source =
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o is (long)0)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast2()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast3()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast4()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """, """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast6()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast7()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast8()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast9()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveEqualityWarningSilencingCast10()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
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
            }
            """, """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists1()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists2()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists3()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoNotRemoveObjectCastToCauseReferenceEqualityWhenUserDefinedComparisonExists4()
    {
        var source =
            """
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
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => [|(object)|]a1 == a2;
            }
            """,
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == [|(object)|]a2;
            }
            """,
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => [|(object)|]a1 == [|(object)|]a2;
            }
            """,
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 [|as object|] == a2;
            }
            """,
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface5()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2 [|as object|];
            }
            """,
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57065")]
    public async Task DoRemoveObjectCastToCauseReferenceEqualityOnInterface6()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 [|as object|] == a2 [|as object|];
            }
            """,
            """
            interface IAssembly
            {
            }

            class C
            {
                bool M(IAssembly a2, IAssembly a1)
                    => a1 == a2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57063")]
    public async Task DoNotRemoveNullableIntToNullableEnumCast()
    {
        var source =
            """
            enum E { }

            class Program
            {
                static E? Main(object o)
                {
                    return (E?)(int?)o;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNRTCast1()
    {
        var source =
            """
            #nullable enable

            using System.Collections.Generic;
            using System.Linq;

            class Node
            {
                public readonly string Name = "";
            }

            class C
            {
                void M(IList<Node> nodes)
                {
                    Dictionary<string, Node?> map = nodes.ToDictionary(t => t.Name, t => (Node?)t);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/56938")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveNRTCastInConditional1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable

            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                string? M(bool b, string s)
                {
                    return b ? [|(string?)|]s : null;
                }
            }
            """,
            FixedCode =
            """
            #nullable enable

            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                string? M(bool b, string s)
                {
                    return b ? s : null;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveNRTCastInConditional2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            #nullable enable

            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                string? M(bool b, string s)
                {
                    return b ? s : [|(string?)|]null;
                }
            }
            """,
            """
            #nullable enable

            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                string? M(bool b, string s)
                {
                    return b ? s : null;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveUnnecessaryWideningConstantCastInConditional1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(int a)
                {
                    long f1 = (a == 5) ? 4 : [|(long)|]5;
                }
            }
            """,
            """
            class C
            {
                void M(int a)
                {
                    long f1 = (a == 5) ? 4 : 5;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNnecessaryWideningConstantCastInConditional1()
    {
        var source = """
            class C
            {
                void M(int a)
                {
                    var f1 = (a == 5) ? 4 : (long)5;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveUnnecessaryWideningCastInConditional2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    long f1 = (a == 5) ? b : [|(long)|]c;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    long f1 = (a == 5) ? b : c;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNecessaryWideningCastInConditional2()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    var f1 = (a == 5) ? b : (long)c;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveUnnecessaryWideningNullableConstantCastInConditional3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    int? f1 = (a == 5) ? [|(int?)|]0 : 1;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    int? f1 = (a == 5) ? 0 : 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNecessaryWideningNullableConstantCastInConditional3()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    var f1 = (a == 5) ? (int?)0 : 1;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveUnnecessaryWideningNullableCastInConditional3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    int? f1 = (a == 5) ? [|(int?)|]b : c;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    int? f1 = (a == 5) ? b : c;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNecessaryWideningNullableCastInConditional3()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    var f1 = (a == 5) ? (int?)b : c;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveUnnecessaryWideningConstantCastInConditionalWithDefault3()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    long f1 = (a == 5) ? [|(long)|]0 : default;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    long f1 = (a == 5) ? 0 : default;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNnecessaryWideningConstantCastInConditionalWithDefault3()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    var f1 = (a == 5) ? (long)0 : default;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoRemoveUnnecessaryWideningConstantCastInConditionalWithDefault4()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    long f1 = (a == 5) ? [|(long)|]b : default;
                }
            }
            """,
            """
            class C
            {
                void M(int a, int b, int c)
                {
                    long f1 = (a == 5) ? b : default;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNecessaryWideningConstantCastInConditionalWithDefault4()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    var f1 = (a == 5) ? (long)b : default;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNecessaryWideningNullableConstantCastInConditionalWithDefault3()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    int? f1 = (a == 5) ? (int?)0 : default;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveNecessaryWideningNullableCastInConditionalWithDefault3()
    {
        var source = """
            class C
            {
                void M(int a, int b, int c)
                {
                    int? f1 = (a == 5) ? (int?)b : default;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56938")]
    public async Task CanRemoveDoubleNullableNumericCast1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Console.WriteLine((int)(float?)[|(int?)|]2147483647); // Prints -2147483648
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Console.WriteLine((int)(float?)2147483647); // Prints -2147483648
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56938")]
    public async Task CanRemoveTripleNullableNumericCast1()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Console.WriteLine((int)(double?)[|(long?)|][|(int?)|]1);
                }
            }
            """,
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Console.WriteLine((int)(double?)1);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveMultipleNullableCastsThroughUserDefinedConversions1()
    {
        var source = """
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
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveMultipleNullableCastsThroughUserDefinedConversions2()
    {
        var source = """
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
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57064")]
    public async Task DoNotRemoveMultipleCastsThroughUserDefinedConversions2()
    {
        var source = """
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
            """;
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58171")]
    [CombinatorialData]
    public async Task DoNotRemoveMethodGroupToSpecificDelegateType(LanguageVersion version)
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = version,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58095")]
    public async Task DoNotRemoveNullableCastsInTuples()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49954")]
    public async Task DoNotRemoveNullableDefaultCast1()
    {
        var source = """
            using System;

            class C
            {
                protected bool? IsNewResource() =>
                    Boolean.TryParse("", out var b) ? b : (bool?)default;
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34509")]
    public async Task DoNotRemoveNullableDefaultCast2()
    {
        var source = """
            using System;

            class C
            {
                static long? TestParse(string val) => long.TryParse(val, out var parseResult) ? (long?)parseResult : default;
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49690")]
    public async Task DoNotRemoveNullableGenericCast()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45925")]
    public async Task DoNotRemoveNecesssaryPatternCasts1()
    {
        var source = """
            using System;

            class C
            {
                bool M(object obj)
                {
                    return obj is 0 or (uint)0 or (long)0 or (ulong)0 or (short)0 or (ushort)0 or (byte)0 or (sbyte)0 or (float)0
                         or (double)0 or (decimal)0 or (AttributeTargets)0;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37473")]
    public async Task DoNotRemoveNecesssaryCastInTupleWrappedInObject1()
    {
        var source = """
            using System.Collections.Generic;

            public class C
            {
                public IEnumerable<object> Bar()
                {
                    yield return ("test", (decimal?)1.23);
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33143")]
    public async Task DoNotRemoveNecesssaryCastInTupleWrappedInObject2()
    {
        var source = """
            using System.Collections.Generic;

            public class C
            {
                void M()
                {
                    object x = (true, (IEnumerable<int>)new int[0]);
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33043")]
    public async Task DoNotRemoveNecesssaryCastInIsNullCheck1()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20617")]
    public async Task DoNotRemoveNecesssaryBitwiseNotOnUnsignedValue1()
    {
        var source = """
            using System;

            public class C
            {
                public static void MethodName()
                {
                    const long x = ~(long)~1U;
                    Console.WriteLine(x);
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11008")]
    public async Task DoNotRemoveCastThatPreventsOverflowInChecked1()
    {
        var source = """
            static class Program
            {
                static readonly long x = -(long)int.MinValue;
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34553")]
    public async Task DoNotRemoveCastThatPreventsOverflowInChecked2()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11008")]
    public async Task DoNotRemoveCastFromIntToNullableEnum1()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11008")]
    public async Task DoNotRemoveWideningCastInBitwiseOr1()
    {
        var source = """
            class C
            {
                public uint fn1(sbyte a, sbyte b)
                {
                    return (uint)((a << 8) | (int)b);
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32773")]
    public async Task DoNotRemoveWideningCastInBitwiseOr2()
    {
        var source = """
            class C
            {
                public void fn1(int start, int end)
                {
                    var bounds = (((long)end) << 32) | ((long)start);
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25165")]
    public async Task DoNotRemoveCastInIllegalDelegateCast()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31303")]
    public async Task DoNotRemoveUnsignedCastInBitwiseNot1()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36755")]
    public async Task DoNotRemoveNecessaryCastInSwitchExpressionArm1()
    {
        var source = """
            class Program
            {
                void M()
                {
                    string numberString = "One";
                    Numbers? number = numberString switch
                    {
                        "One" => (Numbers?)Numbers.One,
                        "Two" => Numbers.Two,
                        _ => null,
                    };
                }
            }

            enum Numbers
            {
                One,
                Two
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36782")]
    public async Task DoNotRemoveNecessaryCastWithOverloadedNegationAndImplicitConversion1()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37041")]
    public async Task DoNotRemoveNecessaryMethodGroupToDelegateCast1()
    {
        var source = """
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
                            case Func<B> value: Console.WriteLine("B"); break;
                            case Func<A> value: Console.WriteLine("A"); break;
                        }
                    }
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54388")]
    public async Task DoNotRemoveCastFromIntToDecimal()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33285")]
    public async Task DoNotRemoveNullableToStructCast1()
    {
        var source = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58718")]
    public async Task FunctionPointerWithImplicitOperator()
    {
        var source = """
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
            """;
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58709")]
    public async Task NotOnNarrowingIntCastInTernary()
    {
        var source = """
            class C
            {
                protected sbyte ExtractInt8(object data)
                {
            	    return (data is sbyte value) ? value : (sbyte)0;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public async Task SameNullableTypeOnBothSidesOfConditional1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    var id = true ? [|(Guid?)|]Guid.NewGuid() : [|(Guid?)|]Guid.Empty;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    var id = true ? Guid.NewGuid() : (Guid?)Guid.Empty;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public async Task SameNullableTypeOnBothSidesOfConditional2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid? g2)
                {
                    var id = true ? [|(Guid?)|]g1 : g2;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid? g2)
                {
                    var id = true ? g1 : g2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public async Task SameNullableTypeOnBothSidesOfConditional3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M(Guid? g1, Guid g2)
                {
                    var id = true ? g1 : [|(Guid?)|]g2;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M(Guid? g1, Guid g2)
                {
                    var id = true ? g1 : g2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public async Task SameNullableTypeOnBothSidesOfConditional4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid g2)
                {
                    Guid? id = true ? [|(Guid?)|]g1 : g2;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid g2)
                {
                    Guid? id = true ? g1 : g2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public async Task SameNullableTypeOnBothSidesOfConditional5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid g2)
                {
                    Guid? id = true ? g1 : [|(Guid?)|]g2;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid g2)
                {
                    Guid? id = true ? g1 : g2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public async Task SameNullableTypeOnBothSidesOfConditional6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid g2)
                {
                    Guid? id = true ? [|(Guid?)|]g1 : [|(Guid?)|]g2;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M(Guid g1, Guid g2)
                {
                    Guid? id = true ? g1 : g2;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58804")]
    public async Task ConvertingMethodGroupToObject_CastIsNecessary()
    {
        var code = """
            class C
            {
                static object M(object o)
                {
                    return (object)o.ToString;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58804")]
    public async Task ConvertingMethodGroupToObject_CastIsNecessary2()
    {
        var code = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58804")]
    [InlineData("Delegate")]
    [InlineData("MulticastDelegate")]
    [InlineData("Func<string>")]
    public async Task ConvertingMethodGroupToObject_CastIsUnnecessary(string type)
    {
        var code = $$"""
            using System;

            class C
            {
                static {{type}} M(object o)
                {
                    return ({{type}})[|(object)|]o.ToString;
                }
            }
            """;
        var fixedCode = $$"""
            using System;

            class C
            {
                static {{type}} M(object o)
                {
                    return o.ToString;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp10,
            NumberOfIncrementalIterations = 2,
            NumberOfFixAllIterations = 2,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58804")]
    public async Task ConvertingMethodGroupToObject_CastIsUnnecessary2()
    {
        var code = """
            using System;

            class C
            {
                static Delegate M(object o)
                {
                    return (Delegate)[|(object)|]o.ToString;
                }
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                static Delegate M(object o)
                {
                    return o.ToString;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp10,
            NumberOfIncrementalIterations = 2,
            NumberOfFixAllIterations = 2,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60248")]
    public async Task RemoveCastInTopLevelPrograms()
    {
        var test = new VerifyCS.Test()
        {
            TestCode = """
            int x = 1;
            int y = [|(int)|]x;
            """,
            FixedCode = """
            int x = 1;
            int y = x;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        };

        await test.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60292")]
    public async Task KeepNecessaryExplicitNullableCast()
    {
        var code = """
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
            """;
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
    }

    [Fact, WorkItem(64346, "https://github.com/dotnet/roslyn/issues/61346")]
    public async Task CanRemoveCastToObjectInStringInterpolation_NullableDisable()
    {
        var code = """
            #nullable disable

            class C
            {
                void M()
                {
                    var v = $"{[|(object)|]0}";
                }
            }
            """;
        var fixedCode = """
            #nullable disable

            class C
            {
                void M()
                {
                    var v = $"{0}";
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact, WorkItem(64346, "https://github.com/dotnet/roslyn/issues/61346")]
    public async Task CanRemoveCastToObjectInStringInterpolation_NullableEnable()
    {
        var code = """
            #nullable enable

            class C
            {
                void M()
                {
                    var v = $"{[|(object)|]0}";
                }
            }
            """;
        var fixedCode = """
            #nullable enable

            class C
            {
                void M()
                {
                    var v = $"{0}";
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact, WorkItem(64346, "https://github.com/dotnet/roslyn/issues/61346")]
    public async Task CanRemoveCastToNullableObjectInStringInterpolation()
    {
        var code = """
            #nullable enable

            class C
            {
                void M()
                {
                    var v = $"{[|(object?)|]0}";
                }
            }
            """;
        var fixedCode = """
            #nullable enable

            class C
            {
                void M()
                {
                    var v = $"{0}";
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28867")]
    public async Task DoNotRemoveNullableCastInConditional()
    {
        var code = """
            class C
            {
                void M()
                {
                    int? a = false ? (int?)1 : default;
                    System.Console.WriteLine(a.HasValue);
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28867")]
    public async Task DoNotRemoveNullableRefCastToVar()
    {
        var code = """
            #nullable enable

            class Bar
            {
            }

            class C
            {
                void Goo(Bar bar)
                {
                    var nullableBar = (Bar?)bar;
                    nullableBar = null;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65005")]
    public async Task DoNotRemoveImplicitNullableBoxingCast1()
    {
        var code = """
            #nullable enable

            using System;

            class C
            {
                void Goo()
                {
                    byte? temp = 10;
                    object box = (int?)temp;

                    Console.WriteLine((int?)box);
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65005")]
    public async Task DoNotRemoveImplicitNullableBoxingCast2()
    {
        var code = """
            #nullable enable

            using System;

            class C
            {
                void Goo()
                {
                    byte? temp = 10;
                    object? box = (int?)temp;

                    Console.WriteLine((int?)box);
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast1()
    {
        var code = """
            using System;
            public struct S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    ((S)s).Increment(); // cast causes local copy of s
                    s.Print();
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast2()
    {
        await VerifyCS.VerifyCodeFixAsync(
            """
            using System;

            public class C {
                double s;

                public void M() {
                    ([|(double)|]s).CompareTo(0); // double is built in.  Methods will not mutate it.
                    Console.WriteLine(s);
                }
            }
            """,
            """
            using System;

            public class C {
                double s;

                public void M() {
                    s.CompareTo(0); // double is built in.  Methods will not mutate it.
                    Console.WriteLine(s);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast3()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public class S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    ([|(S)|]s).Increment(); // safe to remove since this is not a struct
                    s.Print();
                }
            }
            """, """
            using System;
            public class S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    s.Increment(); // safe to remove since this is not a struct
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast4()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public readonly struct S
            {
                public readonly int Field;
                public void Increment() { }
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    ([|(S)|]s).Increment(); // safe to remove since struct is readonly
                    s.Print();
                }
            }
            """, """
            using System;
            public readonly struct S
            {
                public readonly int Field;
                public void Increment() { }
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    s.Increment(); // safe to remove since struct is readonly
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast5()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public struct S
            {
                public readonly int Field;
                public void Increment() { }
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                S GetS() => s;

                public void M() {
                    ([|(S)|]GetS()).Increment(); // safe to remove since not an lvalue.
                    s.Print();
                }
            }
            """, """
            using System;
            public struct S
            {
                public readonly int Field;
                public void Increment() { }
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                S GetS() => s;

                public void M() {
                    GetS().Increment(); // safe to remove since not an lvalue.
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast6()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public struct S
            {
                public readonly int Field;
                public readonly void Increment() { }
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    ([|(S)|]s).Increment(); // safe to remove since method is readonly
                    s.Print();
                }
            }
            """, """
            using System;
            public struct S
            {
                public readonly int Field;
                public readonly void Increment() { }
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    s.Increment(); // safe to remove since method is readonly
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast7()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public struct S
            {
                public readonly int Field;
                public override string ToString() => "";
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    ([|(S)|]s).ToString(); // safe to remove since override of object method
                    s.Print();
                }
            }
            """, """
            using System;
            public struct S
            {
                public readonly int Field;
                public override string ToString() => "";
                public void Print() => Console.WriteLine(Field);
            }
            public class C {
                S s;

                public void M() {
                    s.ToString(); // safe to remove since override of object method
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast8()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public struct S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
                public int Prop => 0;
            }
            public class C {
                S s;

                public void M() {
                    var v = ([|(S)|]s).Prop; // Safe because we assume non-methods don't mutate
                    s.Print();
                }
            }
            """, """
            using System;
            public struct S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
                public int Prop => 0;
            }
            public class C {
                S s;

                public void M() {
                    var v = s.Prop; // Safe because we assume non-methods don't mutate
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61922")]
    public async Task IdentityStructCast9()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System;
            public struct S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
                public int Prop => 0;
            }
            public class C {
                S s;

                public void M() {
                    var v = [|(S)|]s; // Safe because we're not accessing a member
                    s.Print();
                }
            }
            """, """
            using System;
            public struct S {
                public int Field;
                public void Increment() => Field++;
                public void Print() => Console.WriteLine(Field);
                public int Prop => 0;
            }
            public class C {
                S s;

                public void M() {
                    var v = s; // Safe because we're not accessing a member
                    s.Print();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71511")]
    public async Task KeepRequiredCastOnCollectionExpression1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                public class C
                {
                    public IEnumerable<int> M2() => (int[])[1, 2, 3, 4];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71511")]
    public async Task KeepRequiredCastOnCollectionExpression2(
        [CombinatorialValues("IEnumerable<int>", "IReadOnlyCollection<int>", "IReadOnlyList<int>")] string type)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Generic;

                public class C
                {
                    public {{type}} M2() => (List<int>)[1, 2, 3, 4];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71511")]
    public async Task RemoveUnnecessaryCastOnCollectionExpression1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                public class C
                {
                    public int[] M2() => [|(int[])|][1, 2, 3, 4];
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                public class C
                {
                    public int[] M2() => [1, 2, 3, 4];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71511")]
    public async Task RemoveUnnecessaryCastOnCollectionExpression2(
        [CombinatorialValues("ICollection<int>", "IList<int>")] string type,
        [CombinatorialValues("List<int>", "IList<int>")] string castType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Generic;

                public class C
                {
                    public {{type}} M2() => [|({{castType}})|][1, 2, 3, 4];
                }
                """,
            FixedCode = $$"""
                using System.Collections.Generic;

                public class C
                {
                    public {{type}} M2() => [1, 2, 3, 4];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71511")]
    public async Task RemoveUnnecessaryCastOnCollectionExpression3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                public class C
                {
                    void M2()
                    {
                        ReadOnlySpan<int> r = [|(Span<int>)|][1, 2, 3, 4];
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;

                public class C
                {
                    void M2()
                    {
                        ReadOnlySpan<int> r = [1, 2, 3, 4];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71926")]
    public async Task NecessaryDelegateCast1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Runtime.CompilerServices;

                class C
                {
                    static void Main(string[] args)
                    {
                        var main = (Delegate)Main; // IDE0004: Cast is redundant.
                        var x = Unsafe.As<Delegate, object>(ref main);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72134")]
    public async Task NecessaryDelegateCast2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                public class MyClass
                {
                    static void Main()
                    {
                        Goo f = (Action)(() => { }); // IDE0004: Cast is redundant.

                    }
                }

                public class Goo
                {
                    public static implicit operator Goo(Action value)
                    {
                        return default!;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72933")]
    public async Task RemoveCollectionExpressionCastToArray()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Goo(char[] input)
                    {
                    }

                    void Goo(string input)
                    {
                    }

                    void X()
                    {
                        Goo([|(char[])|]['a']);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(char[] input)
                    {
                    }

                    void Goo(string input)
                    {
                    }

                    void X()
                    {
                        Goo(['a']);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }
}
