// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    public class SelectionValidatorTests : ExtractMethodBase
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest1()
        {
            var code = "{|b:using System;|}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest2()
        {
            var code = @"{|b:namespace A|}
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest3()
        {
            var code = @"namespace {|b:A|}
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest4()
        {
            var code = @"{|b:class|} A
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest5()
        {
            var code = @"class {|b:A|}
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest6()
        {
            var code = @"class A : {|b:object|}
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest7()
        {
            var code = @"class A : object, {|b:IDisposable|}
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest8()
        {
            var code = @"class A<{|b:T|}>
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest9()
        {
            var code = @"class A<T> where {|b:T|} : class
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest10()
        {
            var code = @"class A<T> where T : {|b:IDisposable|}
{
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest11()
        {
            var code = @"class A
{
    {|b:A|} Method()
    {
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest12()
        {
            var code = @"class A
{
    A Method({|b:A|} a)
    {
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest13()
        {
            var code = @"class A
{
    A Method(A {|b:a|})
    {
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest14()
        {
            var code = @"class A
{
    [{|b:Foo|}]
    A Method(A a)
    {
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest15()
        {
            var code = @"class A
{
    [Foo({|b:A|}=1)]
    A Method(A a)
    {
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest16()
        {
            var code = @"class A
{
    [Foo(A={|b:1|})]
    A Method(A a)
    {
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest17()
        {
            var code = @"class A
{
    const int {|b:i|} = 1;
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest18()
        {
            var code = @"class A
{
    const {|b:int|} i = 1;
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest19()
        {
            var code = @"class A
{
    const int i = {|b:1|};
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest20()
        {
            var code = @"class A
{
    const int i = {|r:{|b:1 + |}2|};
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest21()
        {
            var code = @"class A
{
    const int {|b:i = 1 + |}2;
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest22()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest23()
        {
            var code = @"class A
{
    void Method1()
    {
        {|b:int i = 1;
    }|}
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest24()
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
            await TestSelectionAsync(code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest25()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest26()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest27()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest28()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest29()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest30()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest31()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest32()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest33()
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
            await TestSelectionAsync(code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest34()
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
            await TestSelectionAsync(code);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest35()
        {
            var code = @"class A
{
    void Method1()
    {
        {|b:// test|}
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest36()
        {
            var code = @"class A
{
    IEnumerable<int> Method1()
    {
        {|r:{|b:yield return 1;|}|}
    }
}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest37()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest38()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(540082)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest39()
        {
            var code = @"class A
{
    void Method1()
    {
        {|r:{|b:System|}.Console.WriteLine(1);|}
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540082)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest40()
        {
            var code = @"class A
{
    void Method1()
    {
        {|r:{|b:System.Console|}.WriteLine(1);|}
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540082)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest41()
        {
            var code = @"class A
{
    void Method1()
    {
        {|r:{|b:System.Console.WriteLine|}(1);|}
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540082)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest42()
        {
            var code = @"class A
{
    void Method1()
    {
{|r:        System.{|b:Console|}.WriteLine(1);|}
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540082)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest43()
        {
            var code = @"class A
{
    void Method1()
    {
{|r:        System.{|b:Console.WriteLine|}(1);|}
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540082)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest44()
        {
            var code = @"class A
{
    void Method1()
    {
{|r:        System.Console.{|b:WriteLine|}(1);|}
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(539242)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest45()
        {
            var code = @"class A
{
    void Method1()
    {
        short[,] arr = {|r:new short[,] { {|b:{ 19, 19, 19 }|}, { 19, 19, 19 } }|};
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(539242)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest46()
        {
            var code = @"class A
{
    void Method1()
    {
        short[,] arr = {|r:{ {|b:{ 19, 19, 19 }|}, { 19, 19, 19 } }|};
    }
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540338)]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectionTest47()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectIfWithReturn()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectConstIfWithReturn()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectReturnButNotAllCodePathsContainAReturn()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectIfBranchWhereNotAllPathsReturn()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectCatchFilterClause()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectCatchFilterClause2()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectWithinCatchFilterClause()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectWithinCatchFilterClause2()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectLValueOfPlusEqualsOperator()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectRValueOfPlusEqualsOperator()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectRValueOfPredecrementOperator()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectArrayWithDecrementIndex()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectCastOperator()
        {
            var code = @"class A
{
    int method(string foo)
    {
        String bar = {|b:(String)foo|};
        return bar.Length;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectLHSOfPostIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:i|}++|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectPostIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:i{|b:++|}|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectRHSOfPreIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:++|}i|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectPreIncrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:++|}i|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectPreDecrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:--|}i|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectLHSOfPostDecrementOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        return {|r:{|b:i|}--|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectUnaryPlusOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:+|}i|};
        return j;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectUnaryMinusOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:-|}i|};
        return j;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectLogicalNegationOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:!|}i|};
        return j;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectBitwiseNegationOperator()
        {
            var code = @"class A
{
    int method(int i)
    {
        int j = {|r:{|b:~|}i|};
        return j;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectCastOperator2()
        {
            var code = @"class A
{
    int method(double i)
    {
        int j = {|r:{|b:(int)|}i|};
        return j;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectInvalidSubexpressionToExpand()
        {
            var code = @"class A
{
    public int method(int a, int b, int c)
    {
        return {|r:a + {|b:b + c|}|};
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectValidSubexpressionAndHenceDontExpand()
        {
            var code = @"class A
{
    public int method(int a, int b, int c)
    {
        return {|b:a + b|} + c;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectLHSOfMinusEqualsOperator()
        {
            var code = @"class A
{
    public int method(int a, int b)
    {
        {|r:{|b:a|} -= b;|}
        return a;
    }
}";
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectInnerBlockPartially()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectInnerBlockWithoutBracesPartially()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectBeginningBrace()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectAcrossBlocks1()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectMethodParameters()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectChainedInvocations1()
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
            await TestSelectionAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SelectChainedInvocations2()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(540474)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task GotoStatement()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(540481)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task BugFix6750()
        {
            var code = @"using System;

class Program
{
    int[] array = new int[{|b:1|}];
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(540481)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task BugFix6750_1()
        {
            var code = @"using System;

class Program
{
    int[] array = {|r:new int[{|b:1|}] { 1 }|};
}";
            await TestSelectionAsync(code);
        }

        [WorkItem(542201)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task MalformedCode_NoOuterType()
        {
            var code = @"x(a){
{|b:for ();|}
}
";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [WorkItem(542210)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task NoQueryContinuation()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(540787)]
        [WorkItem(542722)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task DontCrash()
        {
            await IterateAllAsync(TestResource.AllInOneCSharpCode);
        }

        [WorkItem(9931, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task ExtractMethodIdentifierAtEndOfInteractiveBuffer()
        {
            var code = @"using System.Console;
WriteLine();

{|r:{|b:Diagnostic|}|}";
            await TestSelectionAsync(code, expectedFail: true);
        }

        [WorkItem(543020)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task MemberAccessStructAsExpression()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(543140)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TypeOfExpression()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(543186)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task AnonymousTypeMember1()
        {
            var code = @"using System;
class C { void M() { {|r:var x = new { {|b:String|} = true }; |}} }
";
            await TestSelectionAsync(code);
        }

        [WorkItem(543186)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task AnonymousTypeMember2()
        {
            var code = @"using System;
class C { void M() { 
var String = 1;
{|r:var x = new { {|b:String|} };|}
} }
";
            await TestSelectionAsync(code);
        }

        [WorkItem(543186)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task AnonymousTypeMember3()
        {
            var code = @"using System;
class C { void M() { var x = new { String = {|b:true|} }; } }
";
            await TestSelectionAsync(code);
        }

        [WorkItem(543186)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task AnonymousTypeMember4()
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
            await TestSelectionAsync(code);
        }

        [Fact, WorkItem(543984)]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task AddressOfExpr1()
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
            await TestSelectionAsync(code);
        }

        [Fact, WorkItem(543984)]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task AddressOfExpr2()
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
            await TestSelectionAsync(code);
        }

        [Fact, WorkItem(544627)]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task BaseKeyword()
        {
            var code = @"class C
{
    void Foo()
    {
        {|r:{|b:base|}.ToString();|}
    }
}
";
            await TestSelectionAsync(code);
        }

        [WorkItem(545057)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task RefvalueKeyword()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(531286)]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task NoCrashOnThrowWithoutCatchClause()
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
            await TestSelectionAsync(code, expectedFail: true);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SimpleConditionalAccessExpressionSelectFirstExpression()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task SimpleConditionalAccessExpressionSelectSecondExpression()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task NestedConditionalAccessExpressionWithMemberBindingExpression()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task NestedConditionalAccessExpressionWithMemberBindingExpressionSelectSecondExpression()
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
            await TestSelectionAsync(code);
        }

        [WorkItem(751, "https://github.com/dotnet/roslyn/issues/751")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task NestedConditionalAccessExpressionWithInvocationExpression()
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
            await TestSelectionAsync(code);
        }
    }
}
