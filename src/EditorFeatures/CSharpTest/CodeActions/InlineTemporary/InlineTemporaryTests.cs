// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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

        private async Task TestFixOneAsync(string initial, string expected, bool compareTokens = true)
        {
            await TestAsync(GetTreeText(initial), GetTreeText(expected), index: 0);
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task NotWithNoInitializer1()
        {
            await TestMissingAsync(GetTreeText(@"{ int [||]x; System.Console.WriteLine(x); }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task NotWithNoInitializer2()
        {
            await TestMissingAsync(GetTreeText(@"{ int [||]x = ; System.Console.WriteLine(x); }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task NotOnSecondWithNoInitializer()
        {
            await TestMissingAsync(GetTreeText(@"{ int x = 42, [||]y; System.Console.WriteLine(y); }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task NotOnField()
        {
            await TestMissingAsync(@"class C { int [||]x = 42; void M() { System.Console.WriteLine(x); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task SingleStatement()
        {
            await TestMissingAsync(GetTreeText(@"{ int [||]x = 27; }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task MultipleDeclarators_First()
        {
            await TestMissingAsync(GetTreeText(@"{ int [||]x = 0, y = 1, z = 2; }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task MultipleDeclarators_Second()
        {
            await TestMissingAsync(GetTreeText(@"{ int x = 0, [||]y = 1, z = 2; }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task MultipleDeclarators_Last()
        {
            await TestMissingAsync(GetTreeText(@"{ int x = 0, y = 1, [||]z = 2; }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Escaping1()
        {
            await TestFixOneAsync(@"{ int [||]x = 0; Console.WriteLine(x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Escaping2()
        {
            await TestFixOneAsync(@"{ int [||]@x = 0; Console.WriteLine(x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Escaping3()
        {
            await TestFixOneAsync(@"{ int [||]@x = 0; Console.WriteLine(@x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Escaping4()
        {
            await TestFixOneAsync(@"{ int [||]x = 0; Console.WriteLine(@x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Escaping5()
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

            await TestAsync(code, expected, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Call()
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

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Conversion_NoChange()
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

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Conversion_NoConversion()
        {
            await TestFixOneAsync(@"{ int [||]x = 3; x.ToString(); }",
                       @"{ 3.ToString(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Conversion_DifferentOverload()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Conversion_DifferentMethod()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Conversion_SameMethod()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task NoCastOnVar()
        {
            await TestFixOneAsync(@"{ var [||]x = 0; Console.WriteLine(x); }",
                       @"{ Console.WriteLine(0); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DoubleAssignment()
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

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestAnonymousType1()
        {
            await TestFixOneAsync(@"{ int [||]x = 42; var a = new { x }; }",
                       @"{ var a = new { x = 42 }; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestParenthesizedAtReference_Case3()
        {
            await TestFixOneAsync(@"{ int [||]x = 1 + 1; int y = x * 2; }",
                       @"{ int y = (1 + 1) * 2; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontBreakOverloadResolution_Case5()
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

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontTouchUnrelatedBlocks()
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

            await TestAsync(code, expected, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestLambdaParenthesizeAndCast_Case7()
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

            await TestAsync(code, expected, compareTokens: false);
        }

        [WorkItem(538094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParseAmbiguity1()
        {
            await TestAsync(
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

        [WorkItem(538094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094"), WorkItem(541462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541462")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParseAmbiguity2()
        {
            await TestAsync(
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

        [WorkItem(538094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538094")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParseAmbiguity3()
        {
            await TestAsync(
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

        [WorkItem(544924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544924")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParseAmbiguity4()
        {
            await TestAsync(
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

        [WorkItem(544613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544613")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParseAmbiguity5()
        {
            await TestAsync(
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

        [WorkItem(538131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538131")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestArrayInitializer()
        {
            await TestFixOneAsync(@"{ int[] [||]x = { 3, 4, 5 }; int a = Array.IndexOf(x, 3); }",
                       @"{ int a = Array.IndexOf(new int[] { 3, 4, 5 }, 3);  }");
        }

        [WorkItem(545657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545657")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestArrayInitializer2()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(545657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545657")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestArrayInitializer3()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_RefParameter1()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_RefParameter2()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_AssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_AddAssignExpression1()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_AddAssignExpression2()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_SubtractAssignExpression()
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

            await TestAsync(initial, expected, index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_MultiplyAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_DivideAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_ModuloAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_AndAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_OrAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_ExclusiveOrAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_LeftShiftAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_RightShiftAssignExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_PostIncrementExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_PreIncrementExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_PostDecrementExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_PreDecrementExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_AddressOfExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(545342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545342")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConflict_UsedBeforeDeclaration()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Preprocessor1()
        {
            await TestFixOneAsync(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Preprocessor2()
        {
            await TestFixOneAsync(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Preprocessor3()
        {
            await TestFixOneAsync(@"
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

        [WorkItem(540164, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540164")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TriviaOnArrayInitializer()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ProperlyFormatWhenRemovingDeclarator1()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ProperlyFormatWhenRemovingDeclarator2()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540156")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ProperlyFormatWhenRemovingDeclarator3()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540186, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540186")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ProperlyFormatAnonymousTypeMember()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(6356, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineToAnonymousTypeProperty()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528075")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineIntoDelegateInvocation()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(541341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541341")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineAnonymousMethodIntoNullCoalescingExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(541341, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541341")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineLambdaIntoNullCoalescingExpression()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForBoxingOperation1()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForBoxingOperation2()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForBoxingOperation3()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForBoxingOperation4()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(538079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538079")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForBoxingOperation5()
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

            await TestAsync(initial, expected, index: 0, compareTokens: false);
        }

        [WorkItem(540278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestLeadingTrivia()
        {
            await TestAsync(
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

        [WorkItem(540278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestLeadingAndTrailingTrivia()
        {
            await TestAsync(
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

        [WorkItem(540278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestTrailingTrivia()
        {
            await TestAsync(
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

        [WorkItem(540278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540278")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestPreprocessor()
        {
            await TestAsync(
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

        [WorkItem(540277, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540277")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestFormatting()
        {
            await TestAsync(
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

        [WorkItem(541694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541694")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestSwitchSection()
        {
            await TestAsync(
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

        [WorkItem(542647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542647")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task UnparenthesizeExpressionIfNeeded1()
        {
            await TestAsync(
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

        [WorkItem(545619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545619")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task UnparenthesizeExpressionIfNeeded2()
        {
            await TestAsync(
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

        [WorkItem(542656, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542656")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizeIfNecessary1()
        {
            await TestAsync(
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

        [WorkItem(544626, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544626")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizeIfNecessary2()
        {
            await TestAsync(
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

        [WorkItem(544415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544415")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizeAddressOf1()
        {
            await TestAsync(
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

        [WorkItem(544922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544922")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizeAddressOf2()
        {
            await TestAsync(
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

        [WorkItem(544921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544921")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizePointerIndirection1()
        {
            await TestAsync(
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

        [WorkItem(544614, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544614")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizePointerIndirection2()
        {
            await TestAsync(
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

        [WorkItem(544563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544563")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontInlineStackAlloc()
        {
            await TestMissingAsync(
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

        [WorkItem(543744, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543744")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineTempLambdaExpressionCastingError()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForNull()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastIfNeeded1()
        {
            await TestAsync(
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

        [WorkItem(545161, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545161")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastIfNeeded2()
        {
            await TestAsync(
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

        [WorkItem(544612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544612")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineIntoBracketedList()
        {
            await TestAsync(
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

        [WorkItem(542648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542648")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task ParenthesizeAfterCastIfNeeded()
        {
            await TestAsync(
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

        [WorkItem(544635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544635")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForEnumZeroIfBoxed()
        {
            await TestAsync(
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

        [WorkItem(544636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544636")]
        [WorkItem(554010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForMethodGroupIfNeeded1()
        {
            await TestAsync(
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

        [WorkItem(544978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544978")]
        [WorkItem(554010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554010")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForMethodGroupIfNeeded2()
        {
            await TestAsync(
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

        [WorkItem(545103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545103")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontInsertCastForTypeThatNoLongerBindsToTheSameType()
        {
            await TestAsync(
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

        [WorkItem(545170, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545170")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCorrectCastForDelegateCreationExpression()
        {
            await TestAsync(
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

        [WorkItem(545523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545523")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontInsertCastForObjectCreationIfUnneeded()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontInsertCastInForeachIfUnneeded01()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastInForeachIfNeeded01()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastInForeachIfNeeded02()
        {
            await TestAsync(
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

        [WorkItem(545601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastToKeepGenericMethodInference()
        {
            await TestAsync(
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

        [WorkItem(545601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastForKeepImplicitArrayInference()
        {
            await TestAsync(
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

        [WorkItem(545601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertASingleCastToNotBreakOverloadResolution()
        {
            await TestAsync(
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

        [WorkItem(545601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertASingleCastToNotBreakOverloadResolutionInLambdas()
        {
            await TestAsync(
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

        [WorkItem(545601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545601")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertASingleCastToNotBreakResolutionOfOperatorOverloads()
        {
            await TestAsync(
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

        [WorkItem(545561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545561")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastToNotBreakOverloadResolutionInUncheckedContext()
        {
            await TestAsync(
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

        [WorkItem(545564, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545564")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastToNotBreakOverloadResolutionInUnsafeContext()
        {
            await TestAsync(
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

        [WorkItem(545783, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545783")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InsertCastToNotBreakOverloadResolutionInNestedLambdas()
        {
            await TestAsync(
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

        [WorkItem(546069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546069")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestBrokenVariableDeclarator()
        {
            await TestMissingAsync(
@"class C
{
    static void M()
    {
        int [||]a[10] = { 0, 0 };
        System.Console.WriteLine(a);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestHiddenRegion1()
        {
            await TestMissingAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestHiddenRegion2()
        {
            await TestMissingAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestHiddenRegion3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestHiddenRegion4()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestHiddenRegion5()
        {
            await TestMissingAsync(
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

        [WorkItem(530743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530743")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineFromLabeledStatement()
        {
            await TestAsync(
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

        [WorkItem(529698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529698")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineCompoundAssignmentIntoInitializer()
        {
            await TestAsync(
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

        [WorkItem(609497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Bugfix_609497()
        {
            await TestAsync(
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

        [WorkItem(636319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636319")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Bugfix_636319()
        {
            await TestAsync(
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

        [WorkItem(609492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609492")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Bugfix_609492()
        {
            await TestAsync(
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

        [WorkItem(529950, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529950")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineTempDoesNotInsertUnnecessaryExplicitTypeInLambdaParameter()
        {
            await TestAsync(
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

        [WorkItem(619425, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/619425")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Bugfix_619425_RestrictedSimpleNameExpansion()
        {
            await TestAsync(
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

        [WorkItem(529840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529840")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task Bugfix_529840_DetectSemanticChangesAtInlineSite()
        {
            await TestAsync(
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

        [WorkItem(1091946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091946")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConditionalAccessWithConversion()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestSimpleConditionalAccess()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConditionalAccessWithConditionalExpression()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        [WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")]
        public async Task TestConditionalAccessWithExtensionMethodInvocation()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        [WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")]
        public async Task TestConditionalAccessWithExtensionMethodInvocation_2()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestAliasQualifiedNameIntoInterpolation()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConditionalExpressionIntoInterpolation()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestConditionalExpressionIntoInterpolationWithFormatClause()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task TestInvocationExpressionIntoInterpolation()
        {
            await TestAsync(
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontParenthesizeInterpolatedStringWithNoInterpolation()
        {
            await TestAsync(
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task DontParenthesizeInterpolatedStringWithInterpolation()
        {
            await TestAsync(
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineFormattableStringIntoCallSiteRequiringFormattableString()
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

            await TestAsync(initial, expected, compareTokens: false);
        }

        [WorkItem(4624, "https://github.com/dotnet/roslyn/issues/4624")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)]
        public async Task InlineFormattableStringIntoCallSiteWithFormattableStringOverload()
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
            await TestAsync(initial, expected, compareTokens: false);
        }
    }
}
