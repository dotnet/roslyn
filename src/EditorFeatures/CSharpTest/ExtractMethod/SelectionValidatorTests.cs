// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    public class SelectionValidatorTests : ExtractMethodBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest1()
        {
            var code = "{|b:using System;|}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest2()
        {
            var code = @"{|b:namespace A|}
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest3()
        {
            var code = @"namespace {|b:A|}
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest4()
        {
            var code = @"{|b:class|} A
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest5()
        {
            var code = @"class {|b:A|}
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest6()
        {
            var code = @"class A : {|b:object|}
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest7()
        {
            var code = @"class A : object, {|b:IDisposable|}
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest8()
        {
            var code = @"class A<{|b:T|}>
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest9()
        {
            var code = @"class A<T> where {|b:T|} : class
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest10()
        {
            var code = @"class A<T> where T : {|b:IDisposable|}
{
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest11()
        {
            var code = @"class A
{
    {|b:A|} Method()
    {
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest12()
        {
            var code = @"class A
{
    A Method({|b:A|} a)
    {
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest13()
        {
            var code = @"class A
{
    A Method(A {|b:a|})
    {
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest14()
        {
            var code = @"class A
{
    [{|b:Foo|}]
    A Method(A a)
    {
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest15()
        {
            var code = @"class A
{
    [Foo({|b:A|}=1)]
    A Method(A a)
    {
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest16()
        {
            var code = @"class A
{
    [Foo(A={|b:1|})]
    A Method(A a)
    {
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest17()
        {
            var code = @"class A
{
    const int {|b:i|} = 1;
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest18()
        {
            var code = @"class A
{
    const {|b:int|} i = 1;
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest19()
        {
            var code = @"class A
{
    const int i = {|b:1|};
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest20()
        {
            var code = @"class A
{
    const int i = {|r:{|b:1 + |}2|};
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest21()
        {
            var code = @"class A
{
    const int {|b:i = 1 + |}2;
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest22()
        {
            var code = @"class A
{
    void Method1()
    {
        {|b:int i = 1;
    }

    void Method2()
    {
        int b = 2;|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest23()
        {
            var code = @"class A
{
    void Method1()
    {
        {|b:int i = 1;
    }|}
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest24()
        {
            var code = @"class A
{
    void Method1()
    {
#region A
        {|b:int i = 1;|}
#endRegion
    }
}";
            TestSelection(code);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest25()
        {
            var code = @"class A
{
    void Method1()
    {
{|b:#region A
        int i = 1;|}
#endRegion
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest26()
        {
            var code = @"class A
{
    void Method1()
    {
#region A
        {|b:int i = 1;
#endregion|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest27()
        {
            var code = @"class A
{
    void Method1()
    {
#region A
{|b:#endregion
        int i = 1;|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest28()
        {
            var code = @"class A
{
    void Method1()
    {
#if true
        {|b:int i = 1;
#endif|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest29()
        {
            var code = @"class A
{
    void Method1()
    {
{|b:#if true
        int i = 1;|}
#endif
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest30()
        {
            var code = @"class A
{
    void Method1()
    {
#if true
{|b:#endif
        int i = 1;|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest31()
        {
            var code = @"class A
{
    void Method1()
    {
#if false
{|b:#else
        int i = 1;|}
#endif
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest32()
        {
            var code = @"class A
{
    void Method1()
    {
#if false
{|b:#elsif true
        int i = 1;|}
#endif
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest33()
        {
            var code = @"class A
{
    void Method1()
    {
{|b:#if true
        int i = 1;
#endif|}
    }
}";
            TestSelection(code);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest34()
        {
            var code = @"class A
{
    void Method1()
    {
{|b:#region
        int i = 1;
#endregion|}
    }
}";
            TestSelection(code);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest35()
        {
            var code = @"class A
{
    void Method1()
    {
        {|b:// test|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest36()
        {
            var code = @"class A
{
    IEnumerable<int> Method1()
    {
        {|r:{|b:yield return 1;|}|}
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest37()
        {
            var code = @"class A
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
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest38()
        {
            var code = @"class A
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
}";
            TestSelection(code);
        }

        [WorkItem(540082)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest39()
        {
            var code = @"class A
{
    void Method1()
    {
        {|r:{|b:System|}.Console.WriteLine(1);|}
    }
}";
            TestSelection(code);
        }

        [WorkItem(540082)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest40()
        {
            var code = @"class A
{
    void Method1()
    {
        {|r:{|b:System.Console|}.WriteLine(1);|}
    }
}";
            TestSelection(code);
        }

        [WorkItem(540082)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest41()
        {
            var code = @"class A
{
    void Method1()
    {
        {|r:{|b:System.Console.WriteLine|}(1);|}
    }
}";
            TestSelection(code);
        }

        [WorkItem(540082)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest42()
        {
            var code = @"class A
{
    void Method1()
    {
{|r:        System.{|b:Console|}.WriteLine(1);|}
    }
}";
            TestSelection(code);
        }

        [WorkItem(540082)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest43()
        {
            var code = @"class A
{
    void Method1()
    {
{|r:        System.{|b:Console.WriteLine|}(1);|}
    }
}";
            TestSelection(code);
        }

        [WorkItem(540082)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest44()
        {
            var code = @"class A
{
    void Method1()
    {
{|r:        System.Console.{|b:WriteLine|}(1);|}
    }
}";
            TestSelection(code);
        }

        [WorkItem(539242)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest45()
        {
            var code = @"class A
{
    void Method1()
    {
        short[,] arr = {|r:new short[,] { {|b:{ 19, 19, 19 }|}, { 19, 19, 19 } }|};
    }
}";
            TestSelection(code);
        }

        [WorkItem(539242)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest46()
        {
            var code = @"class A
{
    void Method1()
    {
        short[,] arr = {|r:{ {|b:{ 19, 19, 19 }|}, { 19, 19, 19 } }|};
    }
}";
            TestSelection(code);
        }

        [WorkItem(540338)]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectionTest47()
        {
            var code = @"using System;
class C
{
    void M()
    {
        Action<string> d = s => Console.Write(s);
        {|b:d +=|}
    }
}
";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectIfWithReturn()
        {
            var code = @"class A
{
    public void Method1()
    {
        bool b = true;
        {|b:if (b)
            return;|}
        return;
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectConstIfWithReturn()
        {
            var code = @"class A
{
    public void Method1()
    {
        const bool b = true;
        {|b:if (b)
            return;|}
        Console.WriteLine();
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectReturnButNotAllCodePathsContainAReturn()
        {
            var code = @"class A
{
    public void Method1(bool b1, bool b2)
    {
        if (b1)
        {
            {|b:if (b2)
                return;
            Console.WriteLine();|}
        }
        Console.WriteLine();
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectIfBranchWhereNotAllPathsReturn()
        {
            var code = @"class A
{
    int Method8(int i)
    {
        {|b:if (i > 100)
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
        }|}
        return i;
    }
}";
            TestSelection(code, expectedFail: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectCatchFilterClause()
        {
            var code = @"class A
{
    int method()
    {
        try
        {
            Console.Write(5);
        }
        catch (Exception ex) if ({|b:ex.Message == ""foo""|})
        {
            throw;
        }
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectCatchFilterClause2()
        {
            var code = @"class A
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
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectWithinCatchFilterClause()
        {
            var code = @"class A
{
    int method()
    {
        try
        {
            Console.Write(5);
        }
        catch (Exception ex) if ({|b:ex.Message|} == ""foo"")
        {
            throw;
        }
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectWithinCatchFilterClause2()
        {
            var code = @"class A
{
    int method()
    {
        try
        {
            Console.Write(5);
        }
        catch (Exception ex) if (ex.Message == {|b:""foo""|})
        {
            throw;
        }
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectLValueOfPlusEqualsOperator()
        {
            var code = @"class A
{
    int method()
    {
        int i = 0;
        {|r:{|b:i|} += 1;|}
        return i;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectRValueOfPlusEqualsOperator()
        {
            var code = @"class A
{
    int method()
    {
        int i = 0;
        i += {|b:1|};
        return i;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectRValueOfPredecrementOperator()
        {
            var code = @"class A
{
    string method(string s, int i)
    {
        string[] myvar = new string[i];
        myvar[0] = s;
        myvar[{|r:--{|b:i|}|}] = s + i.ToString();
        return myvar[i];
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectArrayWithDecrementIndex()
        {
            var code = @"class A
{
    string method(string s, int i)
    {
        string[] myvar = new string[i];
        myvar[0] = s;
        {|r:{|b:myvar[--i]|} = s + i.ToString();|}
        return myvar[i];
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectCastOperator()
        {
            var code = @"class A
{
    int method(string foo)
    {
        String bar = {|b:(String)foo|};
        return bar.Length;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectLHSOfPostIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:i|}++|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectPostIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:i{|b:++|}|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectRHSOfPreIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:++|}i|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectPreIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:++|}i|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectPreDecrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:--|}i|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectLHSOfPostDecrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:i|}--|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectUnaryPlusOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:+|}i|};
        return j;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectUnaryMinusOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:-|}i|};
        return j;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectLogicalNegationOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:!|}i|};
        return j;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectBitwiseNegationOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:~|}i|};
        return j;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectCastOperator2()
        {
            var code = @"class A
{
    int method(double i)
    {
        int j = {|r:{|b:(int)|}i|};
        return j;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectInvalidSubexpressionToExpand()
        {
            var code = @"class A
{
    public int method(int a, int b, int c)
    {
        return {|r:a + {|b:b + c|}|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectValidSubexpressionAndHenceDontExpand()
        {
            var code = @"class A
{
    public int method(int a, int b, int c)
    {
        return {|b:a + b|} + c;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectLHSOfMinusEqualsOperator()
        {
            var code = @"class A
{
    public int method(int a, int b)
    {
        {|r:{|b:a|} -= b;|}
        return a;
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectInnerBlockPartially()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectInnerBlockWithoutBracesPartially()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectBeginningBrace()
        {
            var code = @"using System;
using System.Collections;

class A
{
    void method()
    {
        if (true) {|r:{|b:{|} }|}
    }
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectAcrossBlocks1()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectMethodParameters()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectChainedInvocations1()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SelectChainedInvocations2()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WorkItem(540474)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void GotoStatement()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WorkItem(540481)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix6750()
        {
            var code = @"using System;

class Program
{
    int[] array = new int[{|b:1|}];
}";
            TestSelection(code);
        }

        [WorkItem(540481)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BugFix6750_1()
        {
            var code = @"using System;

class Program
{
    int[] array = {|r:new int[{|b:1|}] { 1 }|};
}";
            TestSelection(code);
        }

        [WorkItem(542201)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MalformedCode_NoOuterType()
        {
            var code = @"x(a){
{|b:for ();|}
}
";
            TestSelection(code, expectedFail: true);
        }

        [WorkItem(542210)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NoQueryContinuation()
        {
            var code = @"using System.Linq;
 
class P
{
    static void Main()
    {
        var src = new int[] { 4, 5 };
        var q = {|r:from x in src
                select x into y
                {|b:select y|}|};
    }
}";
            TestSelection(code);
        }

        [WorkItem(540787)]
        [WorkItem(542722)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void DontCrash()
        {
            IterateAll(TestResource.AllInOneCSharpCode);
        }

        [WorkItem(9931, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void ExtractMethodIdentifierAtEndOfInteractiveBuffer()
        {
            var code = @"using System.Console;
WriteLine();

{|r:{|b:Diagnostic|}|}";
            TestSelection(code, expectedFail: true);
        }

        [WorkItem(543020)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void MemberAccessStructAsExpression()
        {
            var code = @"struct S
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
                {|r:{|b:s|}.Z = 10f;|}
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
} ";
            TestSelection(code);
        }

        [WorkItem(543140)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void TypeOfExpression()
        {
            var code = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine({|r:typeof({|b:Dictionary<,>|})|}.IsGenericTypeDefinition);
    }
}";
            TestSelection(code);
        }

        [WorkItem(543186)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void AnonymousTypeMember1()
        {
            var code = @"using System;
class C { void M() { {|r:var x = new { {|b:String|} = true }; |}} }
";
            TestSelection(code);
        }

        [WorkItem(543186)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void AnonymousTypeMember2()
        {
            var code = @"using System;
class C { void M() { 
var String = 1;
{|r:var x = new { {|b:String|} };|}
} }
";
            TestSelection(code);
        }

        [WorkItem(543186)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void AnonymousTypeMember3()
        {
            var code = @"using System;
class C { void M() { var x = new { String = {|b:true|} }; } }
";
            TestSelection(code);
        }

        [WorkItem(543186)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void AnonymousTypeMember4()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var contacts = {|r:new[]
        {
            new {
                Name = ""ddd"",
                PhoneNumbers = new[] { ""206"", ""425"" }
            },
            new {
                {|b:Name|} = ""sss"",
                PhoneNumbers = new[] { ""206"" }
            }
        }|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, WorkItem(543984)]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void AddressOfExpr1()
        {
            var code = @"
class C
{
    unsafe void M()
    {
        int i = 5;
        int* j = {|r:&{|b:i|}|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, WorkItem(543984)]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void AddressOfExpr2()
        {
            var code = @"
class C
{
    unsafe void M()
    {
        int i = 5;
        int* j = {|b:&i|};
    }
}";
            TestSelection(code);
        }

        [WpfFact, WorkItem(544627)]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void BaseKeyword()
        {
            var code = @"class C
{
    void Foo()
    {
        {|r:{|b:base|}.ToString();|}
    }
}
";
            TestSelection(code);
        }

        [WorkItem(545057)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void RefvalueKeyword()
        {
            var code = @"using System;
 
class A
{
    static void Foo(__arglist)
    {
        var argIterator = new ArgIterator(__arglist);
        var typedReference = argIterator.GetNextArg();
        Console.WriteLine(__reftype(typedReference));
        Console.WriteLine({|r:__refvalue(typedReference, {|b:Int32|})|});
    }
}";
            TestSelection(code);
        }

        [WorkItem(531286)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NoCrashOnThrowWithoutCatchClause()
        {
            var code = @"public class Test
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
}";
            TestSelection(code, expectedFail: true);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SimpleConditionalAccessExpressionSelectFirstExpression()
        {
            var code = @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var l = {|r:{|b:a|}?.Length |}?? 0;
    }
}
class A
{
    public int Length { get; internal set; }
}";
            TestSelection(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void SimpleConditionalAccessExpressionSelectSecondExpression()
        {
            var code = @"using System;
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
}";
            TestSelection(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NestedConditionalAccessExpressionWithMemberBindingExpression()
        {
            var code = @"using System;
 
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var l = {|r:a?.{|b:Prop|}?.Length |}?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    public int Length { get; set; }
}";
            TestSelection(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NestedConditionalAccessExpressionWithMemberBindingExpressionSelectSecondExpression()
        {
            var code = @"using System;
 
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
}";
            TestSelection(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public void NestedConditionalAccessExpressionWithInvocationExpression()
        {
            var code = @"using System;
 
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var l = {|r:a?.{|b:Method()|}?.Length |}?? 0;
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
}";
            TestSelection(code);
        }
    }
}
