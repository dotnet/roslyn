// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.InvertIf
{
    public class InvertIfTests : AbstractCSharpCodeActionTest
    {
        private async Task TestFixOneAsync(
            string initial,
            string expected)
        {
            await TestInRegularAndScriptAsync(CreateTreeText(initial), CreateTreeText(expected));
        }

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new InvertIfCodeRefactoringProvider();

        private string CreateTreeText(string initial)
        {
            return
@"class A
{
    void Goo()
    {
" + initial + @"
    }
}";
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestIdentifier()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else { b(); }",
@"        if (!a) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestNotIdentifier()
        {
            await TestFixOneAsync(
@"[||]if (!a) { a(); } else { b(); }",
@"        if (a) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestEqualsEquals()
        {
            await TestFixOneAsync(
@"[||]if (a == b) { a(); } else { b(); }",
@"        if (a != b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestNotEquals()
        {
            await TestFixOneAsync(
@"[||]if (a != b) { a(); } else { b(); }",
@"        if (a == b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestGreaterThan()
        {
            await TestFixOneAsync(
@"[||]if (a > b) { a(); } else { b(); }",
@"        if (a <= b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestGreaterThanEquals()
        {
            await TestFixOneAsync(
@"[||]if (a >= b) { a(); } else { b(); }",
@"        if (a < b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestLessThan()
        {
            await TestFixOneAsync(
@"[||]if (a < b) { a(); } else { b(); }",
@"        if (a >= b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestLessThanEquals()
        {
            await TestFixOneAsync(
@"[||]if (a <= b) { a(); } else { b(); }",
@"        if (a > b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestParens()
        {
            await TestFixOneAsync(
@"[||]if ((a)) { a(); } else { b(); }",
@"        if (!a) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestIs()
        {
            await TestFixOneAsync(
@"[||]if (a is Goo) { a(); } else { b(); }",
@"        if (!(a is Goo)) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestCall()
        {
            await TestFixOneAsync(
@"[||]if (a.Goo()) { a(); } else { b(); }",
@"        if (!a.Goo()) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOr()
        {
            await TestFixOneAsync(
@"[||]if (a || b) { a(); } else { b(); }",
@"        if (!a && !b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOr2()
        {
            await TestFixOneAsync(
@"[||]if (!a || !b) { a(); } else { b(); }",
@"        if (a && b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestAnd()
        {
            await TestFixOneAsync(
@"[||]if (a && b) { a(); } else { b(); }",
@"        if (!a || !b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestAnd2()
        {
            await TestFixOneAsync(
@"[||]if (!a && !b) { a(); } else { b(); }",
@"        if (a || b) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestParenthesizeAndForPrecedence()
        {
            await TestFixOneAsync(
@"[||]if (a && b || c) { a(); } else { b(); }",
@"        if ((!a || !b) && !c) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestPlus()
        {
            await TestFixOneAsync(
@"[||]if (a + b) { a(); } else { b(); }",
@"        if (!(a + b)) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestTrue()
        {
            await TestFixOneAsync(
@"[||]if (true) { a(); } else { b(); }",
@"        if (false) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestFalse()
        {
            await TestFixOneAsync(
@"[||]if (false) { a(); } else { b(); }",
@"        if (true) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestTrueAndFalse()
        {
            await TestFixOneAsync(
@"[||]if (true && false) { a(); } else { b(); }",
@"        if (false || true) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestCurlies1()
        {
            await TestFixOneAsync(
@"[||]if (a) a(); else b();",
@"        if (!a) b();
        else a();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestCurlies2()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else b();",
@"        if (!a) b();
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestCurlies3()
        {
            await TestFixOneAsync(
@"[||]if (a) a(); else { b(); }",
@"        if (!a) { b(); }
        else a();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestIfElseIf()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else if (b) { b(); }",
@"        if (!a)
        {
            if (b) { b(); }
        }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestIfElseIf2()
        {
            await TestFixOneAsync(
@"[||]if (a) { a(); } else if (b) { b(); } else { c(); }",
@"        if (!a)
        {
            if (b) { b(); } else { c(); }
        }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestNested()
        {
            await TestFixOneAsync(
@"[||]if (((a == b) && (c != d)) || ((e < f) && (!g))) { a(); } else { b(); }",
@"        if ((a != b || c == d) && (e >= f || g)) { b(); }
        else { a(); }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestKeepTriviaWithinExpression()
        {
            await TestFixOneAsync(
@"[||]if (a ||
    b &&
    c < // comment
    d)
{
    a();
}
else
{
    b();
}",
@"        if (!a &&
            (!b ||
            c >= // comment
            d))
        {
            b();
        }
        else
        {
            a();
        }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestMissingOnNonEmptySpan()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [|if (a)
        {
            a();
        }
        else
        {
            b();
        }|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
#line hidden
        [||]if (a)
        {
            a();
        }
        else
        {
            b();
        }
#line default
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
#line hidden
            a();
#line default
        }
        else
        {
            b();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
            a();
        }
        else
        {
#line hidden
            b();
#line default
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
#line hidden
            a();
        }
        else
        {
            b();
#line default
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void F()
    {
        [||]if (a)
        {
            a();
#line hidden
        }
        else
        {
#line default
            b();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition6()
        {
            await TestInRegularAndScriptAsync(
@"
#line hidden
class C 
{
    void F()
    {
#line default
        [||]if (a)
        {
            a();
        }
        else
        { 
            b();
        }
    }
}",

@"
#line hidden
class C 
{
    void F()
    {
#line default
        if (!a)
        {
            b();
        }
        else
        {
            a();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestOverlapsHiddenPosition7()
        {
            await TestInRegularAndScriptAsync(
@"
#line hidden
class C 
{
    void F()
    {
#line default
        [||]if (a)
        {
            a();
        }
        else
        { 
            b();
        }
#line hidden
    }
}
#line default",

@"
#line hidden
class C 
{
    void F()
    {
#line default
        if (!a)
        {
            b();
        }
        else
        {
            a();
        }
#line hidden
    }
}
#line default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); }
    } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero2()
        {
            await TestFixOneAsync(
@"string[] x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); }
    } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero3()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length > 0x0) { a(); } else { b(); } } } ",
@"string x; if (x.Length == 0x0) { b(); } else { a(); }
    } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero4()
        {
            await TestFixOneAsync(
@"string x; [||]if (0 < x.Length) { a(); } else { b(); } } } ",
@"string x; if (0 == x.Length) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero5()
        {
            await TestFixOneAsync(
@"byte x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"byte x = 1; if (0 == x) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero6()
        {
            await TestFixOneAsync(
@"ushort x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ushort x = 1; if (0 == x) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero7()
        {
            await TestFixOneAsync(
@"uint x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"uint x = 1; if (0 == x) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero8()
        {
            await TestFixOneAsync(
@"ulong x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 == x) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero9()
        {
            await TestFixOneAsync(
@"ulong x = 1; [||]if (0 == x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 < x) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(545986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545986")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero10()
        {
            await TestFixOneAsync(
@"ulong x = 1; [||]if (x == 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x > 0) { b(); } else { a(); }
    } } ");
        }

        [WorkItem(530505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530505")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestSimplifyToLengthEqualsZero11()
        {
            await TestFixOneAsync(
@"string[] x; [||]if (x.LongLength > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.LongLength == 0) { EqualsZero(); } else { GreaterThanZero(); }
    } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestDoesNotSimplifyToLengthEqualsZero()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length >= 0) { a(); } else { b(); } } } ",
@"string x; if (x.Length < 0) { b(); } else { a(); }
    } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestDoesNotSimplifyToLengthEqualsZero2()
        {
            await TestFixOneAsync(
@"string x; [||]if (x.Length > 0.0f) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length <= 0.0f) { EqualsZero(); } else { GreaterThanZero(); }
    } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless01()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (!c)
                return;
            f();
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            if (c)
                f();
            else
                return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless02()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (!c)
            {
                continue;
            }

            f();
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (c)
            {
                f();
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless03()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (c)
            {
                f();
            }
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (!c)
            {
                continue;
            }

            f();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless04()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (c)
                break;
            return;
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (!c)
                return;
            break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless05()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (!c)
            {
                return;
            }
            break;
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            [||]if (c)
            {
                break;
            }
            return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless06()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            {
                [||]if (c)
                {
                    f();
                }
            }
        }
    }
}",
@"class C
{
    void M()
    {
        foreach (var item in list)
        {
            {
                [||]if (!c)
                {
                    continue;
                }

                f();
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless07()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [||]if (c)
        {
            f();
        }
    }
}",
@"class C
{
    void M()
    {
        [||]if (!c)
        {
            return;
        }

        f();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless08()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                [||]if (c)
                {
                    f();
                    f();
                }
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                [||]if (!c)
                {
                    break;
                }

                f();
                f();
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless09()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                [||]if (c)
                {
                    if (c)
                    {
                        return 1;
                    }
                }
                return 2;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                [||]if (!c)
                {
                    return 2;
                }

                if (c)
                {
                    return 1;
                }
                return 2;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless10()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                if (c)
                {
                    [||]if (c)
                    {
                        return 1;
                    }
                }
                return 2;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                if (c)
                {
                    [||]if (!c)
                    {
                    }
                    else
                    {
                        return 1;
                    }
                }
                return 2;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless11()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                [||]if (c)
                {
                    f();
                }
                g();
                g();
                break;
        }
    }
}",
@"class C
{
    void M()
    {
        switch (o)
        {
            case 1:
                [||]if (!c)
                {
                }
                else
                {
                    f();
                }
                g();
                g();
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless12()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        while (c)
        {
            if (c)
            {
                [||]if (c)
                {
                    continue;
                }
                if (c())
                    return;
            }
        }
    }
}",
@"class C
{
    void M()
    {
        while (c)
        {
            if (c)
            {
                [||]if (!c)
                {
                    if (c())
                        return;
                }
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public async Task TestElseless13()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        while (c)
        {
            {
                [||]if (c)
                {
                    continue;
                }
                if (c())
                    return;
            }
        }
    }
}",
@"class C
{
    void M()
    {
        while (c)
        {
            {
                [||]if (!c)
                {
                    if (c())
                        return;
                }
            }
        }
    }
}");
        }
    }
}
