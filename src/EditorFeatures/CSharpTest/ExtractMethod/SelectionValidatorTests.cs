// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public sealed class SelectionValidatorTests : ExtractMethodBase
{
    [Fact]
    public Task SelectionTest1()
        => TestSelectionAsync("""
            {|b:using System;|}
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest2()
        => TestSelectionAsync("""
            {|b:namespace A|}
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest3()
        => TestSelectionAsync("""
            namespace {|b:A|}
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest4()
        => TestSelectionAsync("""
            {|b:class|} A
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest5()
        => TestSelectionAsync("""
            class {|b:A|}
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest6()
        => TestSelectionAsync("""
            class A : {|b:object|}
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest7()
        => TestSelectionAsync("""
            class A : object, {|b:IDisposable|}
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest8()
        => TestSelectionAsync("""
            class A<{|b:T|}>
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest9()
        => TestSelectionAsync("""
            class A<T> where {|b:T|} : class
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest10()
        => TestSelectionAsync("""
            class A<T> where T : {|b:IDisposable|}
            {
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest11()
        => TestSelectionAsync("""
            class A
            {
                {|b:A|} Method()
                {
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest12()
        => TestSelectionAsync("""
            class A
            {
                A Method({|b:A|} a)
                {
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest13()
        => TestSelectionAsync("""
            class A
            {
                A Method(A {|b:a|})
                {
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest14()
        => TestSelectionAsync("""
            class A
            {
                [{|b:Goo|}]
                A Method(A a)
                {
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest15()
        => TestSelectionAsync("""
            class A
            {
                [Goo({|b:A|}=1)]
                A Method(A a)
                {
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest16()
        => TestSelectionAsync("""
            class A
            {
                [Goo(A={|b:1|})]
                A Method(A a)
                {
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest17()
        => TestSelectionAsync("""
            class A
            {
                const int {|b:i|} = 1;
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest18()
        => TestSelectionAsync("""
            class A
            {
                const {|b:int|} i = 1;
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest19()
        => TestSelectionAsync("""
            class A
            {
                const int i = {|b:1|};
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest20()
        => TestSelectionAsync("""
            class A
            {
                const int i = {|r:{|b:1 + |}2|};
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest21()
        => TestSelectionAsync("""
            class A
            {
                const int {|b:i = 1 + |}2;
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest22()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|b:int i = 1;
                }

                void Method2()
                {
                    int b = 2;|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest23()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|b:int i = 1;
                }|}
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest24()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #region A
                    {|b:int i = 1;|}
            #endRegion
                }
            }
            """);

    [Fact]
    public Task SelectionTest25()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|b:#region A
                    int i = 1;|}
            #endRegion
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest26()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #region A
                    {|b:int i = 1;
            #endregion|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest27()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #region A
            {|b:#endregion
                    int i = 1;|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest28()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #if true
                    {|b:int i = 1;
            #endif|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest29()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|b:#if true
                    int i = 1;|}
            #endif
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest30()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #if true
            {|b:#endif
                    int i = 1;|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest31()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #if false
            {|b:#else
                    int i = 1;|}
            #endif
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest32()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            #if false
            {|b:#elsif true
                    int i = 1;|}
            #endif
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest33()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|b:#if true
                    int i = 1;
            #endif|}
                }
            }
            """);

    [Fact]
    public Task SelectionTest34()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|b:#region
                    int i = 1;
            #endregion|}
                }
            }
            """);

    [Fact]
    public Task SelectionTest35()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|b:// test|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest36()
        => TestSelectionAsync("""
            class A
            {
                IEnumerable<int> Method1()
                {
                    {|r:{|b:yield return 1;|}|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest37()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    try
                    {
                    }
                    catch
                    {
                        {|b:throw;|}
                    }
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectionTest38()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    try
                    {
                    }
                    catch
                    {
                        {|b:throw new Exception();|}
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public Task SelectionTest39()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|r:{|b:System|}.Console.WriteLine(1);|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public Task SelectionTest40()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|r:{|b:System.Console|}.WriteLine(1);|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public Task SelectionTest41()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|r:{|b:System.Console.WriteLine|}(1);|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public Task SelectionTest42()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|r:        System.{|b:Console|}.WriteLine(1);|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public Task SelectionTest43()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|r:        System.{|b:Console.WriteLine|}(1);|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public Task SelectionTest44()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|r:        System.Console.{|b:WriteLine|}(1);|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539242")]
    public Task SelectionTest45()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    short[,] arr = new short[,] { {|r:{|b:{ 19, 19, 19 }|}|}, { 19, 19, 19 } };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539242")]
    public Task SelectionTest46()
        => TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    short[,] arr = { {|r:{|b:{ 19, 19, 19 }|}|}s, { 19, 19, 19 } };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540338")]
    public Task SelectionTest47()
        => TestSelectionAsync("""
            using System;
            class C
            {
                void M()
                {
                    Action<string> d = s => Console.Write(s);
                    {|b:d +=|}
                }
            }
            """, expectedFail: true);

    [Fact]
    public Task SelectIfWithReturn()
        => TestExtractMethodAsync("""
            class A
            {
                public void Method1()
                {
                    bool b = true;
                    [|if (b)
                        return;|]
                    return;
                }
            }
            """, """
            class A
            {
                public void Method1()
                {
                    bool b = true;
                    bool flowControl = NewMethod(b);
                    if (!flowControl)
                    {
                        return;
                    }
                    return;
                }

                private static bool NewMethod(bool b)
                {
                    if (b)
                        return false;
                    return true;
                }
            }
            """);

    [Fact]
    public Task SelectConstIfWithReturn()
        => TestSelectionAsync("""
            class A
            {
                public void Method1()
                {
                    const bool b = true;
                    {|b:if (b)
                        return;|}
                    Console.WriteLine();
                }
            }
            """);

    [Fact]
    public Task SelectReturnButNotAllCodePathsContainAReturn()
        => TestExtractMethodAsync("""
            class A
            {
                public void Method1(bool b1, bool b2)
                {
                    if (b1)
                    {
                        [|if (b2)
                            return;
                        Console.WriteLine();|]
                    }
                    Console.WriteLine();
                }
            }
            """, """
            class A
            {
                public void Method1(bool b1, bool b2)
                {
                    if (b1)
                    {
                        bool flowControl = NewMethod(b2);
                        if (!flowControl)
                        {
                            return;
                        }
                    }
                    Console.WriteLine();
                }

                private static bool NewMethod(bool b2)
                {
                    if (b2)
                        return false;
                    Console.WriteLine();
                    return true;
                }
            }
            """);

    [Fact]
    public Task SelectIfBranchWhereNotAllPathsReturn()
        => TestExtractMethodAsync("""
            class A
            {
                int Method8(int i)
                {
                    [|if (i > 100)
                    {
                        return i++;
                    }
                    else if (i > 90)
                    {
                        return i--;
                    }
                    else
                    {
                        i++;
                    }|]
                    return i;
                }
            }
            """, """
            class A
            {
                int Method8(int i)
                {
                    (bool flowControl, int value) = NewMethod(ref i);
                    if (!flowControl)
                    {
                        return value;
                    }
                    return i;
                }

                private static (bool flowControl, int value) NewMethod(ref int i)
                {
                    if (i > 100)
                    {
                        return (flowControl: false, value: i++);
                    }
                    else if (i > 90)
                    {
                        return (flowControl: false, value: i--);
                    }
                    else
                    {
                        i++;
                    }

                    return (flowControl: true, value: default);
                }
            }
            """);

    [Fact]
    public Task SelectCatchFilterClause()
        => TestSelectionAsync("""
            class A
            {
                int method()
                {
                    try
                    {
                        Console.Write(5);
                    }
                    catch (Exception ex) if ({|b:ex.Message == "goo"|})
                    {
                        throw;
                    }
                }
            }
            """);

    [Fact]
    public Task SelectCatchFilterClause2()
        => TestSelectionAsync("""
            class A
            {
                int method()
                {
                    int i = 5;
                    try
                    {
                        Console.Write(5);
                    }
                    catch (Exception ex) if ({|b:i == 5|})
                    {
                        Console.Write(5);
                        i = 0;
                    }
                }
            }
            """);

    [Fact]
    public Task SelectWithinCatchFilterClause()
        => TestSelectionAsync("""
            class A
            {
                int method()
                {
                    try
                    {
                        Console.Write(5);
                    }
                    catch (Exception ex) if ({|b:ex.Message|} == "goo")
                    {
                        throw;
                    }
                }
            }
            """);

    [Fact]
    public Task SelectWithinCatchFilterClause2()
        => TestSelectionAsync("""
            class A
            {
                int method()
                {
                    try
                    {
                        Console.Write(5);
                    }
                    catch (Exception ex) if (ex.Message == {|b:"goo"|})
                    {
                        throw;
                    }
                }
            }
            """);

    [Fact]
    public Task SelectLValueOfPlusEqualsOperator()
        => TestSelectionAsync("""
            class A
            {
                int method()
                {
                    int i = 0;
                    {|r:{|b:i|} += 1;|}
                    return i;
                }
            }
            """);

    [Fact]
    public Task SelectRValueOfPlusEqualsOperator()
        => TestSelectionAsync("""
            class A
            {
                int method()
                {
                    int i = 0;
                    i += {|b:1|};
                    return i;
                }
            }
            """);

    [Fact]
    public Task SelectRValueOfPredecrementOperator()
        => TestSelectionAsync("""
            class A
            {
                string method(string s, int i)
                {
                    string[] myvar = new string[i];
                    myvar[0] = s;
                    myvar[{|r:--{|b:i|}|}] = s + i.ToString();
                    return myvar[i];
                }
            }
            """);

    [Fact]
    public Task SelectArrayWithDecrementIndex()
        => TestSelectionAsync("""
            class A
            {
                string method(string s, int i)
                {
                    string[] myvar = new string[i];
                    myvar[0] = s;
                    {|r:{|b:myvar[--i]|} = s + i.ToString();|}
                    return myvar[i];
                }
            }
            """);

    [Fact]
    public Task SelectCastOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(string goo)
                {
                    String bar = {|b:(String)goo|};
                    return bar.Length;
                }
            }
            """);

    [Fact]
    public Task SelectLHSOfPostIncrementOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:i|}++|};
                }
            }
            """);

    [Fact]
    public Task SelectPostIncrementOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:i{|b:++|}|};
                }
            }
            """);

    [Fact]
    public Task SelectRHSOfPreIncrementOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:++|}i|};
                }
            }
            """);

    [Fact]
    public Task SelectPreIncrementOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:++|}i|};
                }
            }
            """);

    [Fact]
    public Task SelectPreDecrementOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:--|}i|};
                }
            }
            """);

    [Fact]
    public Task SelectLHSOfPostDecrementOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:i|}--|};
                }
            }
            """);

    [Fact]
    public Task SelectUnaryPlusOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:+|}i|};
                    return j;
                }
            }
            """);

    [Fact]
    public Task SelectUnaryMinusOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:-|}i|};
                    return j;
                }
            }
            """);

    [Fact]
    public Task SelectLogicalNegationOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:!|}i|};
                    return j;
                }
            }
            """);

    [Fact]
    public Task SelectBitwiseNegationOperator()
        => TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:~|}i|};
                    return j;
                }
            }
            """);

    [Fact]
    public Task SelectCastOperator2()
        => TestSelectionAsync("""
            class A
            {
                int method(double i)
                {
                    int j = {|r:{|b:(int)|}i|};
                    return j;
                }
            }
            """);

    [Fact]
    public Task SelectInvalidSubexpressionToExpand()
        => TestSelectionAsync("""
            class A
            {
                public int method(int a, int b, int c)
                {
                    return {|r:a + {|b:b + c|}|};
                }
            }
            """);

    [Fact]
    public Task SelectValidSubexpressionAndHenceDoNotExpand()
        => TestSelectionAsync("""
            class A
            {
                public int method(int a, int b, int c)
                {
                    return {|b:a + b|} + c;
                }
            }
            """);

    [Fact]
    public Task SelectLHSOfMinusEqualsOperator()
        => TestSelectionAsync("""
            class A
            {
                public int method(int a, int b)
                {
                    {|r:{|b:a|} -= b;|}
                    return a;
                }
            }
            """);

    [Fact]
    public Task SelectInnerBlockPartially()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class A
            {
                void method()
                {
                    ArrayList ar = null;
                    foreach (object var in ar)
                    {
                        {|r:{|b:System.Console.WriteLine();
                        foreach (object var2 in ar)
                        {
                            System.Console.WriteLine();|}
                        }|}
                    }
                }
            }
            """);

    [Fact]
    public Task SelectInnerBlockWithoutBracesPartially()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class A
            {
                void method()
                {
                    while (true)
                    {
                        int i = 0;
            {|r:            if (i == 0)
                            Console.WriteLine(){|b:;
                        Console.WriteLine();|}|}
                    }
                }
            }
            """);

    [Fact]
    public Task SelectBeginningBrace()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class A
            {
                void method()
                {
                    if (true) {|r:{|b:{|} }|}
                }
            }
            """);

    [Fact]
    public Task SelectAcrossBlocks1()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class A
            {
                void method()
                {
                    if (true)
                    {
            {|r:            for (int i = 0; i < 100; i++)
                        {
                            {|b:System.Console.WriteLine();
                        }
                        System.Console.WriteLine();|}|}
                    }
                }
            }
            """);

    [Fact]
    public Task SelectMethodParameters()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class A
            {
                void method()
                {
                    double x1 = 10;
                    double y1 = 20;
                    double z1 = 30;
                    double ret = {|r:sum({|b:ref x1, y1, z1|})|};
                }
                double sum(ref double x, double y, double z)
                {
                    x++;
                    return x + y + z;
                }
            }
            """);

    [Fact]
    public Task SelectChainedInvocations1()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class Test
            {
                class B
                {
                    public int c()
                    {
                        return 100;
                    }
                }
                class A
                {
                    public B b = new B();
                }

                void method()
                {
                    A a = new A();
                    {|b:a.b|}.c();
                }
            }
            """);

    [Fact]
    public Task SelectChainedInvocations2()
        => TestSelectionAsync("""
            using System;
            using System.Collections;

            class Test
            {
                class B
                {
                    public int c()
                    {
                        return 100;
                    }
                }
                class A
                {
                    public B b = new B();
                }

                void method()
                {
                    A a = new A();
            {|r:        a.{|b:b.c()|}|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540474")]
    public Task GotoStatement()
        => TestSelectionAsync("""
            using System;
            using System.Reflection.Emit; 

            class Program
            {
                public delegate R Del<in T, out R>(T arg);
                static void Main(string[] args)
                {
                    Del<ArgumentException, Exception> del = {|r:(arg) =>
                    {
                        goto {|b:Label|};
                    Label:
                        return new ArgumentException();
                    }|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540481")]
    public Task BugFix6750()
        => TestSelectionAsync("""
            using System;

            class Program
            {
                int[] array = new int[{|b:1|}];
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540481")]
    public Task BugFix6750_1()
        => TestSelectionAsync("""
            using System;

            class Program
            {
                int[] array = new int[{|r:{|b:1|}|}] { 1 };
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542201")]
    public Task MalformedCode_NoOuterType()
        => TestSelectionAsync("""
            x(a){
            {|b:for ();|}
            }
            """, expectedFail: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542210")]
    public Task NoQueryContinuation()
        => TestSelectionAsync("""
            using System.Linq;

            class P
            {
                static void Main()
                {
                    var src = new int[] { 4, 5 };
                    var q = {|r:from x in src
                            select x into y
                            {|b:select y|}|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542722")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540787")]
    public async Task DoNotCrash()
        => await IterateAllAsync(TestResource.AllInOneCSharpCode);

    [Fact, WorkItem(9931, "DevDiv_Projects/Roslyn")]
    public Task ExtractMethodIdentifierAtEndOfInteractiveBuffer()
        => TestSelectionAsync("""
            using System.Console;
            WriteLine();

            {|r:{|b:Diagnostic|}|}
            """, expectedFail: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543020")]
    public Task MemberAccessStructAsExpression()
        => TestSelectionAsync("""
            struct S
            {
                public float X;
                public float Y;
                public float Z;

                void M()
                {
                    if (3 < 3.4)
                    {
                        S s;
                        if (s.X < 3)
                        {
                            s = GetS();
                            {|r:{|b:s|}|}.Z = 10f;
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                    }
                }

                private static S GetS()
                {
                    return new S();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543140")]
    public Task TypeOfExpression()
        => TestSelectionAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine({|r:typeof({|b:Dictionary<,>|})|}.IsGenericTypeDefinition);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public Task AnonymousTypeMember1()
        => TestSelectionAsync("""
            using System;
            class C { void M() { {|r:var x = new { {|b:String|} = true };|} } }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public Task AnonymousTypeMember2()
        => TestSelectionAsync("""
            using System;
            class C { void M() { 
            var String = 1;
            var x = new { {|r:{|b:String|}|} };
            } }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public Task AnonymousTypeMember3()
        => TestSelectionAsync("""
            using System;
            class C { void M() { var x = new { String = {|b:true|} }; } }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public Task AnonymousTypeMember4()
        => TestSelectionAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var contacts = {|r:new[]
                    {
                        new {
                            Name = "ddd",
                            PhoneNumbers = new[] { "206", "425" }
                        },
                        new {
                            {|b:Name|} = "sss",
                            PhoneNumbers = new[] { "206" }
                        }
                    }|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543984")]
    public Task AddressOfExpr1()
        => TestSelectionAsync("""
            class C
            {
                unsafe void M()
                {
                    int i = 5;
                    int* j = {|r:&{|b:i|}|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543984")]
    public Task AddressOfExpr2()
        => TestSelectionAsync("""
            class C
            {
                unsafe void M()
                {
                    int i = 5;
                    int* j = {|b:&i|};
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544627")]
    public Task BaseKeyword()
        => TestSelectionAsync("""
            class C
            {
                void Goo()
                {
                    {|r:{|b:base|}.ToString();|}
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545057")]
    public Task RefvalueKeyword()
        => TestSelectionAsync("""
            using System;

            class A
            {
                static void Goo(__arglist)
                {
                    var argIterator = new ArgIterator(__arglist);
                    var typedReference = argIterator.GetNextArg();
                    Console.WriteLine(__reftype(typedReference));
                    Console.WriteLine({|r:__refvalue(typedReference, {|b:Int32|})|});
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531286")]
    public Task NoCrashOnThrowWithoutCatchClause()
        => TestSelectionAsync("""
            public class Test
            {
                delegate int D();
                static void Main()
                {
                    try
                    { }
                    catch
                    { }
                    finally
                    {
                        {|b:((D)delegate { throw; return 0; })();|}
                    }
                    return 1;
                }
            }
            """, expectedFail: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public Task SimpleConditionalAccessExpressionSelectFirstExpression()
        => TestSelectionAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    A a = new A();
                    var l = {|r:{|b:a|}|}?.Length ?? 0;
                }
            }
            class A
            {
                public int Length { get; internal set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public Task SimpleConditionalAccessExpressionSelectSecondExpression()
        => TestSelectionAsync("""
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    A a = new A();
                    var l = {|r:a?.{|b:Length|}|} ?? 0;
                }
            }
            class A
            {
                public int Length { get; internal set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public Task NestedConditionalAccessExpressionWithMemberBindingExpression()
        => TestSelectionAsync("""
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    A a = new A();
                    var l = {|r:a?.{|b:Prop|}?.Length|} ?? 0;
                }
            }
            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int Length { get; set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public Task NestedConditionalAccessExpressionWithMemberBindingExpressionSelectSecondExpression()
        => TestSelectionAsync("""
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    A a = new A();
                    var l = {|r:a?.Prop?.{|b:Length|}|} ?? 0;
                }
            }
            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int Length { get; set; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public Task NestedConditionalAccessExpressionWithInvocationExpression()
        => TestSelectionAsync("""
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    A a = new A();
                    var l = {|r:a?.{|b:Method()|}?.Length|} ?? 0;
                }
            }
            class A
            {
                public B Method()
                {
                    return new B();
                }
            }
            class B
            {
                public int Length { get; set; }
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1228916")]
    public async Task DoNotCrashPastEndOfLine()
    {
        //                    11 1
        //          012345678901 2

        // Markup parsing doesn't produce the right spans here, so supply one ourselves.
        // Can be removed when https://github.com/dotnet/roslyn-sdk/issues/637 is fixed

        // This span covers just the "\n"
        var span = new TextSpan(12, 1);

        await TestSelectionAsync("class C { }\r\n", expectedFail: true, textSpanOverride: span);
    }
}
