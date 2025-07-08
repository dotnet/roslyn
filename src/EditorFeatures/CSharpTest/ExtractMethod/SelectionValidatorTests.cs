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
    public async Task SelectionTest1()
    {
        await TestSelectionAsync("""
            {|b:using System;|}
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest2()
    {
        await TestSelectionAsync("""
            {|b:namespace A|}
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest3()
    {
        await TestSelectionAsync("""
            namespace {|b:A|}
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest4()
    {
        await TestSelectionAsync("""
            {|b:class|} A
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest5()
    {
        await TestSelectionAsync("""
            class {|b:A|}
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest6()
    {
        await TestSelectionAsync("""
            class A : {|b:object|}
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest7()
    {
        await TestSelectionAsync("""
            class A : object, {|b:IDisposable|}
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest8()
    {
        await TestSelectionAsync("""
            class A<{|b:T|}>
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest9()
    {
        await TestSelectionAsync("""
            class A<T> where {|b:T|} : class
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest10()
    {
        await TestSelectionAsync("""
            class A<T> where T : {|b:IDisposable|}
            {
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest11()
    {
        await TestSelectionAsync("""
            class A
            {
                {|b:A|} Method()
                {
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest12()
    {
        await TestSelectionAsync("""
            class A
            {
                A Method({|b:A|} a)
                {
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest13()
    {
        await TestSelectionAsync("""
            class A
            {
                A Method(A {|b:a|})
                {
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest14()
    {
        await TestSelectionAsync("""
            class A
            {
                [{|b:Goo|}]
                A Method(A a)
                {
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest15()
    {
        await TestSelectionAsync("""
            class A
            {
                [Goo({|b:A|}=1)]
                A Method(A a)
                {
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest16()
    {
        await TestSelectionAsync("""
            class A
            {
                [Goo(A={|b:1|})]
                A Method(A a)
                {
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest17()
    {
        await TestSelectionAsync("""
            class A
            {
                const int {|b:i|} = 1;
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest18()
    {
        await TestSelectionAsync("""
            class A
            {
                const {|b:int|} i = 1;
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest19()
    {
        await TestSelectionAsync("""
            class A
            {
                const int i = {|b:1|};
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest20()
    {
        await TestSelectionAsync("""
            class A
            {
                const int i = {|r:{|b:1 + |}2|};
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest21()
    {
        await TestSelectionAsync("""
            class A
            {
                const int {|b:i = 1 + |}2;
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest22()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest23()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|b:int i = 1;
                }|}
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest24()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest25()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest26()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest27()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest28()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest29()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest30()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest31()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest32()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest33()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest34()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest35()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|b:// test|}
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest36()
    {
        await TestSelectionAsync("""
            class A
            {
                IEnumerable<int> Method1()
                {
                    {|r:{|b:yield return 1;|}|}
                }
            }
            """, expectedFail: true);
    }

    [Fact]
    public async Task SelectionTest37()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectionTest38()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public async Task SelectionTest39()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|r:{|b:System|}.Console.WriteLine(1);|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public async Task SelectionTest40()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|r:{|b:System.Console|}.WriteLine(1);|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public async Task SelectionTest41()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    {|r:{|b:System.Console.WriteLine|}(1);|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public async Task SelectionTest42()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|r:        System.{|b:Console|}.WriteLine(1);|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public async Task SelectionTest43()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|r:        System.{|b:Console.WriteLine|}(1);|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540082")]
    public async Task SelectionTest44()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
            {|r:        System.Console.{|b:WriteLine|}(1);|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539242")]
    public async Task SelectionTest45()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    short[,] arr = new short[,] { {|r:{|b:{ 19, 19, 19 }|}|}, { 19, 19, 19 } };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539242")]
    public async Task SelectionTest46()
    {
        await TestSelectionAsync("""
            class A
            {
                void Method1()
                {
                    short[,] arr = { {|r:{|b:{ 19, 19, 19 }|}|}s, { 19, 19, 19 } };
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540338")]
    public async Task SelectionTest47()
    {
        await TestSelectionAsync("""
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
    }

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
    public async Task SelectConstIfWithReturn()
    {
        await TestSelectionAsync("""
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
    }

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
    public async Task SelectCatchFilterClause()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectCatchFilterClause2()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectWithinCatchFilterClause()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectWithinCatchFilterClause2()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectLValueOfPlusEqualsOperator()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectRValueOfPlusEqualsOperator()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectRValueOfPredecrementOperator()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectArrayWithDecrementIndex()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectCastOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(string goo)
                {
                    String bar = {|b:(String)goo|};
                    return bar.Length;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectLHSOfPostIncrementOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:i|}++|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectPostIncrementOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:i{|b:++|}|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectRHSOfPreIncrementOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:++|}i|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectPreIncrementOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:++|}i|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectPreDecrementOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:--|}i|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectLHSOfPostDecrementOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    return {|r:{|b:i|}--|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectUnaryPlusOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:+|}i|};
                    return j;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectUnaryMinusOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:-|}i|};
                    return j;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectLogicalNegationOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:!|}i|};
                    return j;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectBitwiseNegationOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(int i)
                {
                    int j = {|r:{|b:~|}i|};
                    return j;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectCastOperator2()
    {
        await TestSelectionAsync("""
            class A
            {
                int method(double i)
                {
                    int j = {|r:{|b:(int)|}i|};
                    return j;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectInvalidSubexpressionToExpand()
    {
        await TestSelectionAsync("""
            class A
            {
                public int method(int a, int b, int c)
                {
                    return {|r:a + {|b:b + c|}|};
                }
            }
            """);
    }

    [Fact]
    public async Task SelectValidSubexpressionAndHenceDoNotExpand()
    {
        await TestSelectionAsync("""
            class A
            {
                public int method(int a, int b, int c)
                {
                    return {|b:a + b|} + c;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectLHSOfMinusEqualsOperator()
    {
        await TestSelectionAsync("""
            class A
            {
                public int method(int a, int b)
                {
                    {|r:{|b:a|} -= b;|}
                    return a;
                }
            }
            """);
    }

    [Fact]
    public async Task SelectInnerBlockPartially()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectInnerBlockWithoutBracesPartially()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectBeginningBrace()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectAcrossBlocks1()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectMethodParameters()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectChainedInvocations1()
    {
        await TestSelectionAsync("""
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
    }

    [Fact]
    public async Task SelectChainedInvocations2()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540474")]
    public async Task GotoStatement()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540481")]
    public async Task BugFix6750()
    {
        await TestSelectionAsync("""
            using System;

            class Program
            {
                int[] array = new int[{|b:1|}];
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540481")]
    public async Task BugFix6750_1()
    {
        await TestSelectionAsync("""
            using System;

            class Program
            {
                int[] array = new int[{|r:{|b:1|}|}] { 1 };
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542201")]
    public async Task MalformedCode_NoOuterType()
    {
        await TestSelectionAsync("""
            x(a){
            {|b:for ();|}
            }
            """, expectedFail: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542210")]
    public async Task NoQueryContinuation()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542722")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540787")]
    public async Task DoNotCrash()
        => await IterateAllAsync(TestResource.AllInOneCSharpCode);

    [Fact, WorkItem(9931, "DevDiv_Projects/Roslyn")]
    public async Task ExtractMethodIdentifierAtEndOfInteractiveBuffer()
    {
        await TestSelectionAsync("""
            using System.Console;
            WriteLine();

            {|r:{|b:Diagnostic|}|}
            """, expectedFail: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543020")]
    public async Task MemberAccessStructAsExpression()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543140")]
    public async Task TypeOfExpression()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public async Task AnonymousTypeMember1()
    {
        await TestSelectionAsync("""
            using System;
            class C { void M() { {|r:var x = new { {|b:String|} = true };|} } }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public async Task AnonymousTypeMember2()
    {
        await TestSelectionAsync("""
            using System;
            class C { void M() { 
            var String = 1;
            var x = new { {|r:{|b:String|}|} };
            } }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public async Task AnonymousTypeMember3()
    {
        await TestSelectionAsync("""
            using System;
            class C { void M() { var x = new { String = {|b:true|} }; } }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543186")]
    public async Task AnonymousTypeMember4()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543984")]
    public async Task AddressOfExpr1()
    {
        await TestSelectionAsync("""
            class C
            {
                unsafe void M()
                {
                    int i = 5;
                    int* j = {|r:&{|b:i|}|};
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543984")]
    public async Task AddressOfExpr2()
    {
        await TestSelectionAsync("""
            class C
            {
                unsafe void M()
                {
                    int i = 5;
                    int* j = {|b:&i|};
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544627")]
    public async Task BaseKeyword()
    {
        await TestSelectionAsync("""
            class C
            {
                void Goo()
                {
                    {|r:{|b:base|}.ToString();|}
                }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545057")]
    public async Task RefvalueKeyword()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531286")]
    public async Task NoCrashOnThrowWithoutCatchClause()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public async Task SimpleConditionalAccessExpressionSelectFirstExpression()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public async Task SimpleConditionalAccessExpressionSelectSecondExpression()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public async Task NestedConditionalAccessExpressionWithMemberBindingExpression()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public async Task NestedConditionalAccessExpressionWithMemberBindingExpressionSelectSecondExpression()
    {
        await TestSelectionAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/751")]
    public async Task NestedConditionalAccessExpressionWithInvocationExpression()
    {
        await TestSelectionAsync("""
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
    }

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
