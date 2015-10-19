// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InvertIf;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.InvertIf
{
    public class InvertIfTests : AbstractCSharpCodeActionTest
    {
        private void TestFixOne(
            string initial,
            string expected)
        {
            Test(CreateTreeText(initial), CreateTreeText(expected), index: 0);
        }

        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new InvertIfCodeRefactoringProvider();
        }

        private string CreateTreeText(string initial)
        {
            return
@"class A
{
  void Foo()
  {
" + initial + @"
  }
}";
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestIdentifier()
        {
            TestFixOne(
@"[||]if (a) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestNotIdentifier()
        {
            TestFixOne(
@"[||]if (!a) { a(); } else { b(); }",
@"if (a) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestEqualsEquals()
        {
            TestFixOne(
@"[||]if (a == b) { a(); } else { b(); }",
@"if (a != b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestNotEquals()
        {
            TestFixOne(
@"[||]if (a != b) { a(); } else { b(); }",
@"if (a == b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestGreaterThan()
        {
            TestFixOne(
@"[||]if (a > b) { a(); } else { b(); }",
@"if (a <= b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestGreaterThanEquals()
        {
            TestFixOne(
@"[||]if (a >= b) { a(); } else { b(); }",
@"if (a < b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestLessThan()
        {
            TestFixOne(
@"[||]if (a < b) { a(); } else { b(); }",
@"if (a >= b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestLessThanEquals()
        {
            TestFixOne(
@"[||]if (a <= b) { a(); } else { b(); }",
@"if (a > b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestParens()
        {
            TestFixOne(
@"[||]if ((a)) { a(); } else { b(); }",
@"if (!a) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestIs()
        {
            TestFixOne(
@"[||]if (a is Foo) { a(); } else { b(); }",
@"if (!(a is Foo)) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestCall()
        {
            TestFixOne(
@"[||]if (a.Foo()) { a(); } else { b(); }",
@"if (!a.Foo()) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOr()
        {
            TestFixOne(
@"[||]if (a || b) { a(); } else { b(); }",
@"if (!a && !b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOr2()
        {
            TestFixOne(
@"[||]if (!a || !b) { a(); } else { b(); }",
@"if (a && b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestAnd()
        {
            TestFixOne(
@"[||]if (a && b) { a(); } else { b(); }",
@"if (!a || !b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestAnd2()
        {
            TestFixOne(
@"[||]if (!a && !b) { a(); } else { b(); }",
@"if (a || b) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestParenthesizeAndForPrecedence()
        {
            TestFixOne(
@"[||]if (a && b || c) { a(); } else { b(); }",
@"if ((!a || !b) && !c) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestPlus()
        {
            TestFixOne(
@"[||]if (a + b) { a(); } else { b(); }",
@"if (!(a + b)) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestTrue()
        {
            TestFixOne(
@"[||]if (true) { a(); } else { b(); }",
@"if (false) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestFalse()
        {
            TestFixOne(
@"[||]if (false) { a(); } else { b(); }",
@"if (true) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestTrueAndFalse()
        {
            TestFixOne(
@"[||]if (true && false) { a(); } else { b(); }",
@"if (false || true) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestCurlies1()
        {
            TestFixOne(
@"[||]if (a) a(); else b();",
@"if (!a) b(); else a();");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestCurlies2()
        {
            TestFixOne(
@"[||]if (a) { a(); } else b();",
@"if (!a) b(); else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestCurlies3()
        {
            TestFixOne(
@"[||]if (a) a(); else { b(); }",
@"if (!a) { b(); } else a();");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestIfElseIf()
        {
            TestFixOne(
@"[||]if (a) { a(); } else if (b) { b(); }",
@"if (!a) { if (b) { b(); } } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestIfElseIf2()
        {
            TestFixOne(
@"[||]if (a) { a(); } else if (b) { b(); } else { c(); }",
@"if (!a) { if (b) { b(); } else { c(); } } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestNested()
        {
            TestFixOne(
@"[||]if (((a == b) && (c != d)) || ((e < f) && (!g))) { a(); } else { b(); }",
@"if ((a != b || c == d) && (e >= f || g)) { b(); } else { a(); }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestKeepTriviaWithinExpression()
        {
            TestFixOne(
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
@"if (!a &&
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestMissingOnNonEmptySpan()
        {
            TestMissing(
@"class C { void F() { [|if (a) { a(); } else { b(); }|] } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition1()
        {
            TestMissing(
@"
class C 
{
    void F()
    {
#line hidden
        [||]if (a) { a(); } else { b(); }
#line default
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition2()
        {
            TestMissing(
@"
class C 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition3()
        {
            TestMissing(
@"
class C 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition4()
        {
            TestMissing(
@"
class C 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition5()
        {
            TestMissing(
@"
class C 
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition6()
        {
            Test(
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
}", compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestOverlapsHiddenPosition7()
        {
            Test(
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
#line default", compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero()
        {
            TestFixOne(
@"string x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero2()
        {
            TestFixOne(
@"string[] x; [||]if (x.Length > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.Length == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero3()
        {
            TestFixOne(
@"string x; [||]if (x.Length > 0x0) { a(); } else { b(); } } } ",
@"string x; if (x.Length == 0x0) { b(); } else { a(); } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero4()
        {
            TestFixOne(
@"string x; [||]if (0 < x.Length) { a(); } else { b(); } } } ",
@"string x; if (0 == x.Length) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero5()
        {
            TestFixOne(
@"byte x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"byte x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero6()
        {
            TestFixOne(
@"ushort x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ushort x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero7()
        {
            TestFixOne(
@"uint x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"uint x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero8()
        {
            TestFixOne(
@"ulong x = 1; [||]if (0 < x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 == x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero9()
        {
            TestFixOne(
@"ulong x = 1; [||]if (0 == x) { a(); } else { b(); } } } ",
@"ulong x = 1; if (0 < x) { b(); } else { a(); } } } ");
        }

        [WorkItem(545986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero10()
        {
            TestFixOne(
@"ulong x = 1; [||]if (x == 0) { a(); } else { b(); } } } ",
@"ulong x = 1; if (x > 0) { b(); } else { a(); } } } ");
        }

        [WorkItem(530505)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestSimplifyToLengthEqualsZero11()
        {
            TestFixOne(
@"string[] x; [||]if (x.LongLength > 0) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string[] x; if (x.LongLength == 0) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestDoesNotSimplifyToLengthEqualsZero()
        {
            TestFixOne(
@"string x; [||]if (x.Length >= 0) { a(); } else { b(); } } } ",
@"string x; if (x.Length < 0) { b(); } else { a(); } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)]
        public void TestDoesNotSimplifyToLengthEqualsZero2()
        {
            TestFixOne(
@"string x; [||]if (x.Length > 0.0f) { GreaterThanZero(); } else { EqualsZero(); } } } ",
@"string x; if (x.Length <= 0.0f) { EqualsZero(); } else { GreaterThanZero(); } } } ");
        }
    }
}
