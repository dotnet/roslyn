// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.InlineTemporary
{
    public class InlineTemporaryTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new InlineTemporaryCodeRefactoringProvider();
        }

        private void TestFixOne(string initial, string expected, bool compareTokens = true)
        {
            Test(GetTreeText(initial), GetTreeText(expected), index: 0);
        }

        private string GetTreeText(string initial)
        {
            return @"class C
{
    void F() " + initial + @"
}";
        }

        private SyntaxNode GetNodeToFix(dynamic initialRoot, int declaratorIndex)
        {
            return initialRoot.Members[0].Members[0].Body.Statements[0].Declaration.Variables[declaratorIndex];
        }

        private SyntaxNode GetFixedNode(dynamic fixedRoot)
        {
            return fixedRoot.Members[0].Members[0].BodyOpt;
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void NotWithNoInitializer1()
        {
            TestMissing(GetTreeText(@"{ int [||]x; System.Console.WriteLine(x); }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void NotWithNoInitializer2()
        {
            TestMissing(GetTreeText(@"{ int [||]x = ; System.Console.WriteLine(x); }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void NotOnSecondWithNoInitializer()
        {
            TestMissing(GetTreeText(@"{ int x = 42, [||]y; System.Console.WriteLine(y); }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void NotOnField()
        {
            TestMissing(@"class C { int [||]x = 42; void M() { System.Console.WriteLine(x); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void SingleStatement()
        {
            TestMissing(GetTreeText(@"{ int [||]x = 27; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void MultipleDeclarators_First()
        {
            TestMissing(GetTreeText(@"{ int [||]x = 0, y = 1, z = 2; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void MultipleDeclarators_Second()
        {
            TestMissing(GetTreeText(@"{ int x = 0, [||]y = 1, z = 2; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void MultipleDeclarators_Last()
        {
            TestMissing(GetTreeText(@"{ int x = 0, y = 1, [||]z = 2; }"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Escaping1()
        {
            TestFixOne(@"{ int [||]x = 0; Console.WriteLine(x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Escaping2()
        {
            TestFixOne(@"{ int [||]@x = 0; Console.WriteLine(x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Escaping3()
        {
            TestFixOne(@"{ int [||]@x = 0; Console.WriteLine(@x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Escaping4()
        {
            TestFixOne(@"{ int [||]x = 0; Console.WriteLine(@x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Escaping5()
        {
            var code = @"
using System.Linq;
class C
{
    static void Main()
    {
        var @where[||] = 0;
        var q = from e in """" let a = new { @where } select a;
    }
}";

            var expected = @"
using System.Linq;
class C
{
    static void Main()
    {
        var q = from e in """" let a = new { @where = 0 } select a;
    }
}";

            Test(code, expected, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Call()
        {
            var code = @"
using System;
class C 
{
    public void M()
    {
        int [||]x = 1 + 1;
        x.ToString();
    }
}";

            var expected = @"
using System;
class C 
{
    public void M()
    {
        (1 + 1).ToString();
    }
}";

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Conversion_NoChange()
        {
            var code = @"
using System;
class C 
{
    public void M()
    {
        double [||]x = 3;
        x.ToString();
    }
}";

            var expected = @"
using System;
class C 
{
    public void M()
    {
        ((double)3).ToString();
    }
}";

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Conversion_NoConversion()
        {
            TestFixOne(@"{ int [||]x = 3; x.ToString(); }",
                       @"{ 3.ToString(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Conversion_DifferentOverload()
        {
            Test(
@"
using System;
class C
{
    void M()
    {
        double [||]x = 3;
        Console.WriteLine(x);
    }
}",

@"
using System;
class C
{
    void M()
    {
        Console.WriteLine((double)3);
    }
}",
index: 0,
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Conversion_DifferentMethod()
        {
            Test(
@"
class Base 
{
    public void M(object o) { }
}
class Derived : Base
{
    public void M(string s) { }
}
class C
{
    void F()
    {
        Base [||]b = new Derived();
        b.M(""hi"");
    }
}
",

@"
class Base 
{
    public void M(object o) { }
}
class Derived : Base
{
    public void M(string s) { }
}
class C
{
    void F()
    {
        ((Base)new Derived()).M(""hi"");
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Conversion_SameMethod()
        {
            Test(
@"
class Base 
{
    public void M(int i) { }
}
class Derived : Base
{
    public void M(string s) { }
}
class C
{
    void F()
    {
        Base [||]b = new Derived();
        b.M(3);
    }
}
",

@"
class Base 
{
    public void M(int i) { }
}
class Derived : Base
{
    public void M(string s) { }
}
class C
{
    void F()
    {
        new Derived().M(3);
    }
}
",
    index: 0,
    compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void NoCastOnVar()
        {
            TestFixOne(@"{ var [||]x = 0; Console.WriteLine(x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DoubleAssignment()
        {
            var code = @"
class C
{
    void M()
    {
        int [||]x = x = 1;
        int y = x;
    }
}";

            var expected = @"
class C
{
    void M()
    {
        int y = 1;
    }
}";

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestAnonymousType1()
        {
            TestFixOne(@"{ int [||]x = 42; var a = new { x }; }",
                       @"{ var a = new { x = 42 }; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestParenthesizedAtReference_Case3()
        {
            TestFixOne(@"{ int [||]x = 1 + 1; int y = x * 2; }",
                       @"{ int y = (1 + 1) * 2; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontBreakOverloadResolution_Case5()
        {
            var code = @"
class C
{
    void Foo(object o) { }
    void Foo(int i) { }

    void M()
    {
        object [||]x = 1 + 1;
        Foo(x);
    }
}";

            var expected = @"
class C
{
    void Foo(object o) { }
    void Foo(int i) { }

    void M()
    {
        Foo((object)(1 + 1));
    }
}";

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontTouchUnrelatedBlocks()
        {
            var code = @"
class C
{
    void M()
    {
        int [||]x = 1;
        { Unrelated(); }
        Foo(x);
    }
}";

            var expected = @"
class C
{
    void M()
    {
        { Unrelated(); }
        Foo(1);
    }
}";

            Test(code, expected, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestLambdaParenthesizeAndCast_Case7()
        {
            var code = @"
class C
{
    void M()
    {
        System.Func<int> [||]x = () => 1;
        int y = x();
    }
}";

            var expected = @"
class C
{
    void M()
    {
        int y = ((System.Func<int>)(() => 1))();
    }
}";

            Test(code, expected, compareTokens: false);
        }

        [WorkItem(538094)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParseAmbiguity1()
        {
            Test(
@"
class C
{
    void F(object a, object b)
    {
        int x = 2;
        bool [||]y = x > (f);
        F(x < x, y);
    }
    int f = 0;
}",
@"
class C
{
    void F(object a, object b)
    {
        int x = 2;
        F(x < x, (x > (f)));
    }
    int f = 0;
}", index: 0, compareTokens: false);
        }

        [WorkItem(538094), WorkItem(541462)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParseAmbiguity2()
        {
            Test(
@"
class C
{
    void F(object a, object b)
    {
        int x = 2;
        object [||]y = x > (f);
        F(x < x, y);
    }
    int f = 0;
}",
@"
class C
{
    void F(object a, object b)
    {
        int x = 2;
        F(x < x, (x > (f)));
    }
    int f = 0;
}", index: 0, compareTokens: false);
        }

        [WorkItem(538094)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParseAmbiguity3()
        {
            Test(
@"
class C
{
    void F(object a, object b)
    {
        int x = 2;
        bool [||]y = x > (int)1;
        F(x < x, y);
    }
    int f = 0;
}",
@"
class C
{
    void F(object a, object b)
    {
        int x = 2;
        F(x < x, (x > (int)1));
    }
    int f = 0;
}",
 index: 0, compareTokens: false);
        }

        [WorkItem(544924)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParseAmbiguity4()
        {
            Test(
@"
class Program
{
    static void Main()
    {
        int x = 2;
        int y[||] = (1+2);
        Bar(x < x, x > y);
    }

    static void Bar(object a, object b)
    {
    }
}",
@"
class Program
{
    static void Main()
    {
        int x = 2;
        Bar(x < x, (x > (1 + 2)));
    }

    static void Bar(object a, object b)
    {
    }
}", index: 0, compareTokens: false);
        }

        [WorkItem(544613)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParseAmbiguity5()
        {
            Test(
@"
class Program
{
    static void Main()
    {
        int x = 2;
        int y[||] = (1 + 2);
        var z = new[] { x < x, x > y };
    }
}",
@"
class Program
{
    static void Main()
    {
        int x = 2;
        var z = new[] { x < x, (x > (1 + 2)) };
    }
}", index: 0, compareTokens: false);
        }

        [WorkItem(538131)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestArrayInitializer()
        {
            TestFixOne(@"{ int[] [||]x = { 3, 4, 5 }; int a = Array.IndexOf(x, 3); }",
                       @"{ int a = Array.IndexOf(new int[] { 3, 4, 5 }, 3);  }");
        }

        [WorkItem(545657)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestArrayInitializer2()
        {
            var initial = @"
class Program
{ 
    static void Main()
    {
        int[] [||]x = { 3, 4, 5 };
        System.Array a = x;
    }
}";

            var expected = @"
class Program
{ 
    static void Main()
    {
        System.Array a = new int[] { 3, 4, 5 };
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(545657)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestArrayInitializer3()
        {
            var initial = @"
class Program
{ 
    static void Main()
    {
        int[] [||]x = {
                          3,
                          4,
                          5
                      };
        System.Array a = x;
    }
}";

            var expected = @"
class Program
{ 
    static void Main()
    {
        System.Array a = new int[] {
            3,
            4,
            5
        };
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_RefParameter1()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]x = 0;
        Foo(ref x);
        Foo(x);
    }

    void Foo(int x)
    {
    }

    void Foo(ref int x)
    {
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int x = 0;
        Foo(ref {|Conflict:x|});
        Foo(0);
    }

    void Foo(int x)
    {
    }

    void Foo(ref int x)
    {
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_RefParameter2()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]x = 0;
        Foo(x, ref x);
    }

    void Foo(int x, ref int y)
    {
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int x = 0;
        Foo(0, ref {|Conflict:x|});
    }

    void Foo(int x, ref int y)
    {
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_AssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i = 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} = 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_AddAssignExpression1()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i += 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} += 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_AddAssignExpression2()
        {
            var initial =
@"using System;
class C
{
    static int x;

    static void M()
    {
        int [||]x = (x = 0) + (x += 1);
        int y = x;
    }
}";

            var expected =
@"using System;
class C
{
    static int x;

    static void M()
    {
        int x = ({|Conflict:x|} = 0) + ({|Conflict:x|} += 1);
        int y = 0 + ({|Conflict:x|} += 1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_SubtractAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i -= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} -= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_MultiplyAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i *= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} *= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_DivideAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i /= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} /= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_ModuloAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i %= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} %= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_AndAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i &= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} &= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_OrAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i |= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} |= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_ExclusiveOrAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i ^= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} ^= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_LeftShiftAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i <<= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} <<= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_RightShiftAssignExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i >>= 2;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|} >>= 2;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_PostIncrementExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i++;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|}++;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_PreIncrementExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        ++i;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        ++{|Conflict:i|};
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_PostDecrementExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        i--;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        {|Conflict:i|}--;
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_PreDecrementExpression()
        {
            var initial =
@"using System;
class Program
{
    void Main()
    {
        int [||]i = 1;
        --i;
        Console.WriteLine(i);
    }
}";

            var expected =
@"using System;
class Program
{
    void Main()
    {
        int i = 1;
        --{|Conflict:i|};
        Console.WriteLine(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_AddressOfExpression()
        {
            var initial = @"
class C
{
    unsafe void M()
    {
        int x = 0;
        var y[||] = &x;
        var z = &y;
    }
}";

            var expected = @"
class C
{
    unsafe void M()
    {
        int x = 0;
        var y = &x;
        var z = &{|Conflict:y|};
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(545342)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConflict_UsedBeforeDeclaration()
        {
            var initial =
@"class Program
{
    static void Main(string[] args)
    {
        var x = y;
        var y[||] = 45;
    }
}";

            var expected =
@"class Program
{
    static void Main(string[] args)
    {
        var x = {|Conflict:y|};
        var y = 45;
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Preprocessor1()
        {
            TestFixOne(@"
{
    int [||]x = 1,
#if true
        y,
#endif
        z;

    int a = x;
}",
                       @"{
        int
#if true
            y,
#endif
            z;

        int a = 1;
    }",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Preprocessor2()
        {
            TestFixOne(@"
{
    int y,
#if true
        [||]x = 1,
#endif
        z;

    int a = x;
}",
                       @"{
        int y,
#if true

#endif
            z;

        int a = 1;
    }",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Preprocessor3()
        {
            TestFixOne(@"
{
    int y,
#if true
        z,
#endif
        [||]x = 1;

    int a = x;
}",
                       @"{
        int y,
#if true
            z
#endif
            ;

        int a = 1;
    }",
compareTokens: false);
        }

        [WorkItem(540164)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TriviaOnArrayInitializer()
        {
            var initial =
@"class C
{
    void M()
    {
        int[] [||]a = /**/{ 1 };
        Foo(a);
    }
}";

            var expected =
@"class C
{
    void M()
    {
        Foo(new int[]/**/{ 1 });
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540156)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ProperlyFormatWhenRemovingDeclarator1()
        {
            var initial =
@"class C
{
    void M()
    {
        int [||]i = 1, j = 2, k = 3;
        System.Console.Write(i);
    }
}";

            var expected =
@"class C
{
    void M()
    {
        int j = 2, k = 3;
        System.Console.Write(1);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540156)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ProperlyFormatWhenRemovingDeclarator2()
        {
            var initial =
@"class C
{
    void M()
    {
        int i = 1, [||]j = 2, k = 3;
        System.Console.Write(j);
    }
}";

            var expected =
@"class C
{
    void M()
    {
        int i = 1, k = 3;
        System.Console.Write(2);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540156)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ProperlyFormatWhenRemovingDeclarator3()
        {
            var initial =
@"class C
{
    void M()
    {
        int i = 1, j = 2, [||]k = 3;
        System.Console.Write(k);
    }
}";

            var expected =
@"class C
{
    void M()
    {
        int i = 1, j = 2;
        System.Console.Write(3);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540186)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ProperlyFormatAnonymousTypeMember()
        {
            var initial =
@"class C
{
    void M()
    {
        var [||]x = 123;
        var y = new { x };
    }
}";

            var expected =
@"class C
{
    void M()
    {
        var y = new { x = 123 };
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(6356, "DevDiv_Projects/Roslyn")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineToAnonymousTypeProperty()
        {
            var initial =
@"class C
{
    void M()
    {
        var [||]x = 123;
        var y = new { x = x };
    }
}";

            var expected =
@"class C
{
    void M()
    {
        var y = new { x = 123 };
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528075)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineIntoDelegateInvocation()
        {
            var initial =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Action<string[]> [||]del = Main;
        del(null);
    }
}";

            var expected =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        ((Action<string[]>)Main)(null);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(541341)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineAnonymousMethodIntoNullCoalescingExpression()
        {
            var initial =
@"using System;
 
class Program
{
    static void Main()
    {
        Action [||]x = delegate { };
        Action y = x ?? null;
    }
}";

            var expected =
@"using System;
 
class Program
{
    static void Main()
    {
        Action y = (Action)delegate { } ?? null;
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(541341)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineLambdaIntoNullCoalescingExpression()
        {
            var initial =
@"using System;
 
class Program
{
    static void Main()
    {
        Action [||]x = () => { };
        Action y = x ?? null;
    }
}";

            var expected =
@"using System;
 
class Program
{
    static void Main()
    {
        Action y = (Action)(() => { }) ?? null;
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForBoxingOperation1()
        {
            var initial =
@"using System;
class A
{
    static void Main()
    {
        long x[||] = 1;
        object z = x;
        Console.WriteLine((long)z);
    }
}";

            var expected =
@"using System;
class A
{
    static void Main()
    {
        object z = (long)1;
        Console.WriteLine((long)z);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForBoxingOperation2()
        {
            var initial =
@"using System;
class A
{
    static void Main()
    {
        int y = 1;
        long x[||] = y;
        object z = x;
        Console.WriteLine((long)z);
    }
}";

            var expected =
@"using System;
class A
{
    static void Main()
    {
        int y = 1;
        object z = (long)y;
        Console.WriteLine((long)z);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForBoxingOperation3()
        {
            var initial =
@"using System;
class A
{
    static void Main()
    {
        byte x[||] = 1;
        object z = x;
        Console.WriteLine((byte)z);
    }
}";

            var expected =
@"using System;
class A
{
    static void Main()
    {
        object z = (byte)1;
        Console.WriteLine((byte)z);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForBoxingOperation4()
        {
            var initial =
@"using System;
class A
{
    static void Main()
    {
        sbyte x[||] = 1;
        object z = x;
        Console.WriteLine((sbyte)z);
    }
}";

            var expected =
@"using System;
class A
{
    static void Main()
    {
        object z = (sbyte)1;
        Console.WriteLine((sbyte)z);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForBoxingOperation5()
        {
            var initial =
@"using System;
class A
{
    static void Main()
    {
        short x[||] = 1;
        object z = x;
        Console.WriteLine((short)z);
    }
}";

            var expected =
@"using System;
class A
{
    static void Main()
    {
        object z = (short)1;
        Console.WriteLine((short)z);
    }
}";

            Test(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540278)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestLeadingTrivia()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        // Leading
        int [||]i = 10;
        //print
        Console.Write(i);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        // Leading
        //print
        Console.Write(10);
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(540278)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestLeadingAndTrailingTrivia()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        // Leading
        int [||]i = 10; // Trailing
        //print
        Console.Write(i);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        // Leading
        // Trailing
        //print
        Console.Write(10);
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(540278)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestTrailingTrivia()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        int [||]i = 10; // Trailing
        //print
        Console.Write(i);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        // Trailing
        //print
        Console.Write(10);
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(540278)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestPreprocessor()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
#if true
        int [||]i = 10; 
        //print
        Console.Write(i);
#endif
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
#if true
        //print
        Console.Write(10);
#endif
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(540277)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestFormatting()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        int [||]i = 5; int j = 110;
        Console.Write(i + j);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        int j = 110;
        Console.Write(5 + j);
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(541694)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestSwitchSection()
        {
            Test(
@"using System;
class C
{
    void M()
    {
        switch (10)
        {
            default:
                int i[||] = 10;
                Console.WriteLine(i);
                break;
        }
    }
}",
@"using System;
class C
{
    void M()
    {
        switch (10)
        {
            default:
                Console.WriteLine(10);
                break;
        }
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(542647)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void UnparenthesizeExpressionIfNeeded1()
        {
            Test(
@"
using System;
class C
{
    static Action X;
    static void M()
    {
        var [||]y = (X);
        y();
    }
}
",

@"
using System;
class C
{
    static Action X;
    static void M()
    {
        X();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545619)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void UnparenthesizeExpressionIfNeeded2()
        {
            Test(
@"
using System;
class Program
{
    static void Main()
    {
        Action x = Console.WriteLine;
        Action y[||] = x;
        y();
    }
}
",

@"
using System;
class Program
{
    static void Main()
    {
        Action x = Console.WriteLine;
        x();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(542656)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizeIfNecessary1()
        {
            Test(
            @"using System;
using System.Collections;
using System.Linq;

class A
{
    static void Main()
    {
        var [||]q = from x in """" select x;
        if (q is IEnumerable)
        {
        }
    }
}",
            @"using System;
using System.Collections;
using System.Linq;

class A
{
    static void Main()
    {
        if ((from x in """" select x) is IEnumerable)
        {
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544626)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizeIfNecessary2()
        {
            Test(
            @"
using System;
class C
{
    static void Main()
    {
        Action<string> f[||] = Foo<string>;
        Action<string> g = null;
        var h = f + g;
    }

    static void Foo<T>(T y) { }
}",
            @"
using System;
class C
{
    static void Main()
    {
        Action<string> g = null;
        var h = Foo + g;
    }

    static void Foo<T>(T y) { }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544415)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizeAddressOf1()
        {
            Test(
            @"
using System;
unsafe class C
{
    static void M()
    {
        int x;
        int* p[||] = &x;
        var i = (Int32)p;
    }
}",
            @"
using System;
unsafe class C
{
    static void M()
    {
        int x;
        var i = (Int32)(&x);
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544922)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizeAddressOf2()
        {
            Test(
            @"
using System;
unsafe class C
{
    static void M()
    {
        int x;
        int* p[||] = &x;
        var i = p->ToString();
    }
}",
            @"
using System;
unsafe class C
{
    static void M()
    {
        int x;
        var i = (&x)->ToString();
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544921)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizePointerIndirection1()
        {
            Test(
            @"
using System;
unsafe class C
{
    static void M()
    {
        int* x = null;
        int p[||] = *x;
        var i = (Int64)p;
    }
}",
            @"
using System;
unsafe class C
{
    static void M()
    {
        int* x = null;
        var i = (Int64)(*x);
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544614)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizePointerIndirection2()
        {
            Test(
            @"
using System;
unsafe class C
{
    static void M()
    {
        int** x = null;
        int* p[||] = *x;
        var i = p[1].ToString();
    }
}",
            @"
using System;
unsafe class C
{
    static void M()
    {
        int** x = null;
        var i = (*x)[1].ToString();
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544563)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontInlineStackAlloc()
        {
            TestMissing(
            @"
using System;
unsafe class C
{
    static void M()
    {
        int* values[||] = stackalloc int[20];
        int* copy = values;
        int* p = &values[1];
        int* q = &values[15];
    }
}");
        }

        [WorkItem(543744)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineTempLambdaExpressionCastingError()
        {
            Test(
            @"using System;
class Program
{
    static void Main(string[] args)
    {
        Func<int?,int?> [||]lam = (int? s) => { return s; };
        Console.WriteLine(lam);
    }
}",

            @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine((Func<int?, int?>)((int? s) => { return s; }));
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForNull()
        {
            Test(
            @"
using System;
class C
{
    void M()
    {
        string [||]x = null;
        Console.WriteLine(x);
    }
}",

            @"
using System;
class C
{
    void M()
    {
        Console.WriteLine((string)null);
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastIfNeeded1()
        {
            Test(
            @"
class C
{
    void M()
    {
        long x[||] = 1;
        System.IComparable<long> y = x;
    }
}",
            @"
class C
{
    void M()
    {
        System.IComparable<long> y = (long)1;
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastIfNeeded2()
        {
            Test(
            @"
using System;
 
class C
{
    static void Main()
    {
        Foo(x => { int [||]y = x[0]; x[1] = y; });
    }
 
    static void Foo(Action<int[]> x) { }
    static void Foo(Action<string[]> x) { }
}",
            @"
using System;
 
class C
{
    static void Main()
    {
        Foo((Action<int[]>)(x => { x[1] = x[0]; }));
    }
 
    static void Foo(Action<int[]> x) { }
    static void Foo(Action<string[]> x) { }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(544612)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineIntoBracketedList()
        {
            Test(
            @"
class C
{
    void M()
    {
        var c = new C();
        int x[||] = 1;
        c[x] = 2;
    }

    int this[object x] { set { } }
}",

            @"
class C
{
    void M()
    {
        var c = new C();
        c[1] = 2;
    }

    int this[object x] { set { } }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(542648)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void ParenthesizeAfterCastIfNeeded()
        {
            Test(
            @"
using System;

enum E { }

class Program
{
    static void Main()
    {
        E x[||] = (global::E) -1;
        object y = x;
    }
}",

            @"
using System;

enum E { }

class Program
{
    static void Main()
    {
        object y = (global::E)-1;
    }
}",
            index: 0,
            parseOptions: null,
            compareTokens: false);
        }

        [WorkItem(544635)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForEnumZeroIfBoxed()
        {
            Test(
            @"
using System;
class Program
{
    static void M()
    {
        DayOfWeek x[||] = 0;
        object y = x;
        Console.WriteLine(y);
    }
}",

            @"
using System;
class Program
{
    static void M()
    {
        object y = (DayOfWeek)0;
        Console.WriteLine(y);
    }
}",
            index: 0,
            parseOptions: null,
            compareTokens: false);
        }

        [WorkItem(544636)]
        [WorkItem(554010)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForMethodGroupIfNeeded1()
        {
            Test(
            @"
using System;
class Program
{
    static void M()
    {
        Action a[||] = Console.WriteLine;
        Action b = a + Console.WriteLine;
    }
}",

            @"
using System;
class Program
{
    static void M()
    {
        Action b = (Action)Console.WriteLine + Console.WriteLine;
    }
}",
            index: 0,
            parseOptions: null,
            compareTokens: false);
        }

        [WorkItem(544978)]
        [WorkItem(554010)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForMethodGroupIfNeeded2()
        {
            Test(
            @"
using System;
class Program
{
    static void Main()
    {
        Action a[||] = Console.WriteLine;
        object b = a;
    }
}",

            @"
using System;
class Program
{
    static void Main()
    {
        object b = (Action)Console.WriteLine;
    }
}",
            index: 0,
            parseOptions: null,
            compareTokens: false);
        }

        [WorkItem(545103)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontInsertCastForTypeThatNoLongerBindsToTheSameType()
        {
            Test(
            @"
class A<T>
{
    static T x;
    class B<U>
    {
        static void Foo()
        {
            var y[||] = x;
            var z = y;
        }
    }
}",

            @"
class A<T>
{
    static T x;
    class B<U>
    {
        static void Foo()
        {
            var z = x;
        }
    }
}",
            index: 0,
            parseOptions: null,
            compareTokens: false);
        }

        [WorkItem(545170)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCorrectCastForDelegateCreationExpression()
        {
            Test(
            @"
using System;
 
class Program
{
    static void Main()
    {
        Predicate<object> x[||] = y => true;
        var z = new Func<string, bool>(x);
    }
}
",

            @"
using System;
 
class Program
{
    static void Main()
    {
        var z = new Func<string, bool>(y => true);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545523)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontInsertCastForObjectCreationIfUnneeded()
        {
            Test(
            @"
using System;
class Program
{
    static void Main()
    {
        Exception e[||] = new ArgumentException();
        Type b = e.GetType();
    }
}
",

            @"
using System;
class Program
{
    static void Main()
    {
        Type b = new ArgumentException().GetType();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontInsertCastInForeachIfUnneeded01()
        {
            Test(
            @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        IEnumerable<char> s[||] = ""abc"";
        foreach (var x in s)
            Console.WriteLine(x);
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
        foreach (var x in ""abc"")
            Console.WriteLine(x);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastInForeachIfNeeded01()
        {
            Test(
            @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        IEnumerable s[||] = ""abc"";
        foreach (object x in s)
            Console.WriteLine(x);
    }
}
",

            @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        foreach (object x in (IEnumerable)""abc"")
            Console.WriteLine(x);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastInForeachIfNeeded02()
        {
            Test(
            @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        IEnumerable s[||] = ""abc"";
        foreach (char x in s)
            Console.WriteLine(x);
    }
}
",

            @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        foreach (char x in (IEnumerable)""abc"")
            Console.WriteLine(x);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastToKeepGenericMethodInference()
        {
            Test(
            @"
using System;
class C
{
    static T Foo<T>(T x, T y) { return default(T); }

    static void M()
    {
        long [||]x = 1;
        IComparable<long> c = Foo(x, x);
    }
}
",

            @"
using System;
class C
{
    static T Foo<T>(T x, T y) { return default(T); }

    static void M()
    {
        IComparable<long> c = Foo<long>(1, 1);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastForKeepImplicitArrayInference()
        {
            Test(
            @"
class C
{
    static void M()
    {
        object x[||] = null;
        var a = new[] { x, x };
        Foo(a);
    }

    static void Foo(object[] o) { }
}
",

            @"
class C
{
    static void M()
    {
        var a = new[] { null, (object)null };
        Foo(a);
    }

    static void Foo(object[] o) { }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertASingleCastToNotBreakOverloadResolution()
        {
            Test(
            @"
class C
{
    static void M()
    {
        long x[||] = 42;
        Foo(x, x);
    }

    static void Foo(int x, int y) { }
    static void Foo(long x, long y) { }
}",

            @"
class C
{
    static void M()
    {
        Foo(42, (long)42);
    }

    static void Foo(int x, int y) { }
    static void Foo(long x, long y) { }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertASingleCastToNotBreakOverloadResolutionInLambdas()
        {
            Test(
            @"
using System;
class C
{
    static void M()
    {
        long x[||] = 42;
        Foo(() => { return x; }, () => { return x; });
    }

    static void Foo(Func<int> x, Func<int> y) { }
    static void Foo(Func<long> x, Func<long> y) { }
}",

            @"
using System;
class C
{
    static void M()
    {
        Foo(() => { return 42; }, (Func<long>)(() => { return 42; }));
    }

    static void Foo(Func<int> x, Func<int> y) { }
    static void Foo(Func<long> x, Func<long> y) { }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545601)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertASingleCastToNotBreakResolutionOfOperatorOverloads()
        {
            Test(
            @"
using System;
class C
{
    private int value;

    void M()
    {
        C x[||] = 42;
        Console.WriteLine(x + x);
    }

    public static int operator +(C x, C y)
    {
        return x.value + y.value;
    }

    public static implicit operator C(int l)
    {
        var c = new C();
        c.value = l;
        return c;
    }

    static void Main()
    {
        new C().M();
    }
}",

            @"
using System;
class C
{
    private int value;

    void M()
    {
        Console.WriteLine(42 + (C)42);
    }

    public static int operator +(C x, C y)
    {
        return x.value + y.value;
    }

    public static implicit operator C(int l)
    {
        var c = new C();
        c.value = l;
        return c;
    }

    static void Main()
    {
        new C().M();
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545561)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastToNotBreakOverloadResolutionInUncheckedContext()
        {
            Test(
            @"
using System;

class X
{
    static int Foo(Func<int?, byte> x, object y) { return 1; }
    static int Foo(Func<X, byte> x, string y) { return 2; }

    const int Value = 1000;
    static void Main()
    {
        var a[||] = Foo(X => (byte)X.Value, null);
        unchecked
        {
            Console.WriteLine(a);
        }
    }
}",

            @"
using System;

class X
{
    static int Foo(Func<int?, byte> x, object y) { return 1; }
    static int Foo(Func<X, byte> x, string y) { return 2; }

    const int Value = 1000;
    static void Main()
    {
        unchecked
        {
            Console.WriteLine(Foo(X => (byte)X.Value, (object)null));
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545564)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastToNotBreakOverloadResolutionInUnsafeContext()
        {
            Test(
            @"
using System;

static class C
{
    static int Outer(Action<int> x, object y) { return 1; }
    static int Outer(Action<string> x, string y) { return 2; }

    static void Inner(int x, int[] y) { }
    unsafe static void Inner(string x, int*[] y) { }

    static void Main()
    {
        var a[||] = Outer(x => Inner(x, null), null);
        unsafe
        {
            Console.WriteLine(a);
        }
    }
}",

            @"
using System;

static class C
{
    static int Outer(Action<int> x, object y) { return 1; }
    static int Outer(Action<string> x, string y) { return 2; }

    static void Inner(int x, int[] y) { }
    unsafe static void Inner(string x, int*[] y) { }

    static void Main()
    {
        unsafe
        {
            Console.WriteLine(Outer(x => Inner(x, null), (object)null));
        }
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(545783)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InsertCastToNotBreakOverloadResolutionInNestedLambdas()
        {
            Test(
            @"
using System;

class C
{
    static void Foo(Action<object> a) { }
    static void Foo(Action<string> a) { }

    static void Main()
    {
        Foo(x =>
        {
            string s[||] = x;
            var y = s;
        });
    }
}",

            @"
using System;

class C
{
    static void Foo(Action<object> a) { }
    static void Foo(Action<string> a) { }

    static void Main()
    {
        Foo((Action<string>)(x =>
        {
            var y = x;
        }));
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(546069)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestBrokenVariableDeclarator()
        {
            TestMissing(
@"class C
{
    static void M()
    {
        int [||]a[10] = { 0, 0 };
        System.Console.WriteLine(a);
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestHiddenRegion1()
        {
            TestMissing(
@"class Program
{
    void Main()
    {
        int [|x|] = 0;

        #line hidden
        Foo(x);
        #line default
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestHiddenRegion2()
        {
            TestMissing(
@"class Program
{
    void Main()
    {
        int [|x|] = 0;

        Foo(x);
        #line hidden
        Foo(x);
        #line default
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestHiddenRegion3()
        {
            Test(
@"#line default
class Program
{
    void Main()
    {
        int [|x|] = 0;

        Foo(x);
        #line hidden
        Foo();
        #line default
    }
}",
@"#line default
class Program
{
    void Main()
    {

        Foo(0);
        #line hidden
        Foo();
        #line default
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestHiddenRegion4()
        {
            Test(
@"#line default
class Program
{
    void Main()
    {
        int [||]x = 0;

        Foo(x);
#line hidden
        Foo();
#line default
        Foo(x);
    }
}",
@"#line default
class Program
{
    void Main()
    {

        Foo(0);
#line hidden
        Foo();
#line default
        Foo(0);
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestHiddenRegion5()
        {
            TestMissing(
@"class Program
{
    void Main()
    {
        int [||]x = 0;

        Foo(x);
#line hidden
        Foo(x);
#line default
        Foo(x);
    }
}");
        }

        [WorkItem(530743)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineFromLabeledStatement()
        {
            Test(
            @"
using System;
 
class Program
{
    static void Main()
    {
    label:
        int [||]x = 1;
        Console.WriteLine();
        int y = x;        
    }
}",

            @"
using System;
 
class Program
{
    static void Main()
    {
    label:
        Console.WriteLine();
        int y = 1;        
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineCompoundAssignmentIntoInitializer()
        {
            Test(
            @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        int x = 0;
        int y[||] = x += 1;
        var z = new List<int> { y };
    }
}",

            @"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        int x = 0;
        var z = new List<int> { (x += 1) };
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(609497)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Bugfix_609497()
        {
            Test(
            @"
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        IList<dynamic> x[||] = new List<object>();
        IList<object> y = x;
    }
}",

            @"
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        IList<object> y = new List<object>();
    }
}",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(636319)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Bugfix_636319()
        {
            Test(
            @"
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        IList<object> x[||] = new List<dynamic>();
        IList<dynamic> y = x;
    }
}
",

            @"
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        IList<dynamic> y = new List<dynamic>();
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(609492)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Bugfix_609492()
        {
            Test(
            @"
using System;
 
class Program
{
    static void Main()
    {
        ValueType x[||] = 1;
        object y = x;
    }
}
",

            @"
using System;
 
class Program
{
    static void Main()
    {
        object y = 1;
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529950)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineTempDoesNotInsertUnnecessaryExplicitTypeInLambdaParameter()
        {
            Test(
            @"
using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => { var z[||] = x; Action a = () => z.GetType(); }, y), null);
    }
}
",

            @"
using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => { Action a = () => x.GetType(); }, y), null);
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(619425)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Bugfix_619425_RestrictedSimpleNameExpansion()
        {
            Test(
            @"
class A<B>
{
    class C : A<C>
    {
        class B : C
        {
            void M()
            {
                var x[||] = new C[0];
                C[] y = x;
            }
        }
    }
}
",

            @"
class A<B>
{
    class C : A<C>
    {
        class B : C
        {
            void M()
            {
                C[] y = new C[0];
            }
        }
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(529840)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void Bugfix_529840_DetectSemanticChangesAtInlineSite()
        {
            Test(
            @"
using System;
 
class A
{
    static void Main()
    {
        var a[||] = new A(); // Inline a
        Foo(a);
    }
 
    static void Foo(long x)
    {
        Console.WriteLine(x);
    }
 
    public static implicit operator int (A x)
    {
        return 1;
    }
 
    public static explicit operator long (A x)
    {
        return 2;
    }
}
",

            @"
using System;
 
class A
{
    static void Main()
    {
        // Inline a
        Foo(new A());
    }
 
    static void Foo(long x)
    {
        Console.WriteLine(x);
    }
 
    public static implicit operator int (A x)
    {
        return 1;
    }
 
    public static explicit operator long (A x)
    {
        return 2;
    }
}
",
            index: 0,
            compareTokens: false);
        }

        [WorkItem(1091946)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConditionalAccessWithConversion()
        {
            Test(
            @"
class A
{
    bool M(string[] args)
    {
        var [|x|] = args[0];
        return x?.Length == 0;
    }
}
", @"
class A
{
    bool M(string[] args)
    {
        return args[0]?.Length == 0;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestSimpleConditionalAccess()
        {
            Test(
            @"
class A
{
    void M(string[] args)
    {
        var [|x|] = args.Length.ToString();
        var y = x?.ToString();
    }
}
", @"
class A
{
    void M(string[] args)
    {
        var y = args.Length.ToString()?.ToString();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConditionalAccessWithConditionalExpression()
        {
            Test(
            @"
class A
{
    void M(string[] args)
    {
        var [|x|] = args[0]?.Length ?? 10;
        var y = x == 10 ? 10 : 4;
    }
}
", @"
class A
{
    void M(string[] args)
    {
        var y = (args[0]?.Length ?? 10) == 10 ? 10 : 4;
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        [WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")]
        public void TestConditionalAccessWithExtensionMethodInvocation()
        {
            Test(
            @"
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var [|assembly|] = t?.Something().First();
            var identity = assembly?.ToArray();
        }
        return null;
    }
}
", @"
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var identity = t?.Something().First()?.ToArray();
        }
        return null;
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        [WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")]
        public void TestConditionalAccessWithExtensionMethodInvocation_2()
        {
            Test(
            @"
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }

    public static Func<C> Something2(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var [|assembly|] = (t?.Something2())()?.Something().First();
            var identity = assembly?.ToArray();
        }
        return null;
    }
}
", @"
using System;
using System.Collections.Generic;
using System.Linq;

static class M
{
    public static IEnumerable<string> Something(this C cust)
    {
        throw new NotImplementedException();
    }

    public static Func<C> Something2(this C cust)
    {
        throw new NotImplementedException();
    }
}

class C
{
    private object GetAssemblyIdentity(IEnumerable<C> types)
    {
        foreach (var t in types)
        {
            var identity = (t?.Something2())()?.Something().First()?.ToArray();
        }
        return null;
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestAliasQualifiedNameIntoInterpolation()
        {
            Test(
            @"
class A
{
    void M()
    {
        var [|g|] = global::System.Guid.Empty;
        var s = $""{g}"";
    }
}
", @"
class A
{
    void M()
    {
        var s = $""{(global::System.Guid.Empty)}"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConditionalExpressionIntoInterpolation()
        {
            Test(
            @"
class A
{
    bool M(bool b)
    {
        var [|x|] = b ? 19 : 23;
        var s = $""{x}"";
    }
}
", @"
class A
{
    bool M(bool b)
    {
        var s = $""{(b ? 19 : 23)}"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestConditionalExpressionIntoInterpolationWithFormatClause()
        {
            Test(
            @"
class A
{
    bool M(bool b)
    {
        var [|x|] = b ? 19 : 23;
        var s = $""{x:x}"";
    }
}
", @"
class A
{
    bool M(bool b)
    {
        var s = $""{(b ? 19 : 23):x}"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void TestInvocationExpressionIntoInterpolation()
        {
            Test(
            @"
class A
{
    public static void M(string s)
    {
        var [|x|] = s.ToUpper();
        var y = $""{x}"";
    }
}
", @"
class A
{
    public static void M(string s)
    {
        var y = $""{s.ToUpper()}"";
    }
}");
        }

        [WorkItem(4583, "https://github.com/dotnet/roslyn/issues/4583")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontParenthesizeInterpolatedStringWithNoInterpolation()
        {
            Test(
            @"
class C
{
    public void M()
    {
        var [|s1|] = $""hello"";
        var s2 = string.Replace(s1, ""world"");
    }
}
", @"
class C
{
    public void M()
    {
        var s2 = string.Replace($""hello"", ""world"");
    }
}");
        }

        [WorkItem(4583, "https://github.com/dotnet/roslyn/issues/4583")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void DontParenthesizeInterpolatedStringWithInterpolation()
        {
            Test(
            @"
class C
{
    public void M(int x)
    {
        var [|s1|] = $""hello {x}"";
        var s2 = string.Replace(s1, ""world"");
    }
}
", @"
class C
{
    public void M(int x)
    {
        var s2 = string.Replace($""hello {x}"", ""world"");
    }
}");
        }

        [WorkItem(4583, "https://github.com/dotnet/roslyn/issues/4583")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineFormattableStringIntoCallSiteRequiringFormattableString()
        {
            const string initial = @"
using System;
" + CodeSnippets.FormattableStringType + @"
class C
{
    static void M(FormattableString s)
    {
    }

    static void N(int x, int y)
    {
        FormattableString [||]s = $""{x}, {y}"";
        M(s);
    }
}";

            const string expected = @"
using System;
" + CodeSnippets.FormattableStringType + @"
class C
{
    static void M(FormattableString s)
    {
    }

    static void N(int x, int y)
    {
        M($""{x}, {y}"");
    }
}";

            Test(initial, expected, compareTokens: false);
        }

        [WorkItem(4624, "https://github.com/dotnet/roslyn/issues/4624")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public void InlineFormattableStringIntoCallSiteWithFormattableStringOverload()
        {
            const string initial = @"
using System;
" + CodeSnippets.FormattableStringType + @"
class C
{
    static void M(string s) { }
    static void M(FormattableString s) { }

    static void N(int x, int y)
    {
        FormattableString [||]s = $""{x}, {y}"";
        M(s);
    }
}";

            const string expected = @"
using System;
" + CodeSnippets.FormattableStringType + @"
class C
{
    static void M(string s) { }
    static void M(FormattableString s) { }

    static void N(int x, int y)
    {
        M((FormattableString)$""{x}, {y}"");
    }
}";
            Test(initial, expected, compareTokens: false);
        }
    }
}
