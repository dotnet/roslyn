// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveDeclarationNearReference;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.MoveDeclarationNearReference
{
    public class MoveDeclarationNearReferenceTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new MoveDeclarationNearReferenceCodeRefactoringProvider();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMove1()
        {
            Test(
@"class C { void M() { int [||]x; { Console.WriteLine(x); } } }",
@"class C { void M() { { int x; Console.WriteLine(x); } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMove2()
        {
            Test(
@"class C { void M() { int [||]x; Console.WriteLine(); Console.WriteLine(x); } }",
@"class C { void M() { Console.WriteLine(); int x; Console.WriteLine(x); } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMove3()
        {
            Test(
@"class C { void M() { int [||]x; Console.WriteLine(); { Console.WriteLine(x); } { Console.WriteLine(x); } }",
@"class C { void M() { Console.WriteLine(); int x; { Console.WriteLine(x); } { Console.WriteLine(x); } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMove4()
        {
            Test(
@"class C { void M() { int [||]x; Console.WriteLine(); { Console.WriteLine(x); } }",
@"class C { void M() { Console.WriteLine(); { int x; Console.WriteLine(x); } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestAssign1()
        {
            Test(
@"class C { void M() { int [||]x; { x = 5; Console.WriteLine(x); } } }",
@"class C { void M() { { int x = 5; Console.WriteLine(x); } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestAssign2()
        {
            Test(
@"class C { void M() { int [||]x = 0; { x = 5; Console.WriteLine(x); } } }",
@"class C { void M() { { int x = 5; Console.WriteLine(x); } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestAssign3()
        {
            Test(
@"class C { void M() { var [||]x = (short)0; { x = 5; Console.WriteLine(x); } } }",
@"class C { void M() { { var x = (short)0; x = 5; Console.WriteLine(x); } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMissing1()
        {
            TestMissing(
@"class C { void M() { int [||]x; Console.WriteLine(x); } }");
        }

        [WorkItem(538424)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMissingWhenReferencedInDeclaration()
        {
            TestMissing(
@"class Program { static void Main ( ) { object [ ] [||]x = { x = null } ; x . ToString ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMissingWhenInDeclarationGroup()
        {
            TestMissing(
@"class Program { static void Main ( ) { int [||]i = 5; int j = 10; Console.WriteLine(i); } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        [WorkItem(541475)]
        public void Regression8190()
        {
            TestMissing(
@"class Program { void M() { { object x; [|object|] } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestFormatting()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        int [||]i = 5; Console.WriteLine();
        Console.Write(i);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
        int i = 5; Console.Write(i);
    }
}",
index: 0,
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMissingInHiddenBlock1()
        {
            TestMissing(
@"class Program
{
    void Main()
    {
        int [|x|] = 0;
        Foo();
#line hidden
        Bar(x);
    }
#line default
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestMissingInHiddenBlock2()
        {
            TestMissing(
@"class Program
{
    void Main()
    {
        int [|x|] = 0;
        Foo();
#line hidden
        Foo();
#line default
        Bar(x);
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestAvailableInNonHiddenBlock1()
        {
            Test(
@"#line default
class Program
{
    void Main()
    {
        int [||]x = 0;
        Foo();
        Bar(x);
#line hidden
    }
#line default
}",
@"#line default
class Program
{
    void Main()
    {
        Foo();
        int x = 0;
        Bar(x);
#line hidden
    }
#line default
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestAvailableInNonHiddenBlock2()
        {
            Test(
@"class Program
{
    void Main()
    {
        int [||]x = 0;
        Foo();
#line hidden
        Foo();
#line default
        Foo();
        Bar(x);
    }
}",
@"class Program
{
    void Main()
    {
        Foo();
#line hidden
        Foo();
#line default
        Foo();
        int x = 0;
        Bar(x);
    }
}",
compareTokens: false);
        }

        [WorkItem(545435)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestWarnOnChangingScopes1()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var [||]@lock = new object ( ) ; new [ ] { 1 } . AsParallel ( ) . ForAll ( ( i ) => { lock ( @lock ) { } } ) ; } } ",
@"using System . Linq ; class Program { void Main ( ) { new [ ] { 1 } . AsParallel ( ) . ForAll ( ( i ) => { {|Warning:var @lock = new object ( ) ;|} lock ( @lock ) { } } ) ; } } ");
        }

        [WorkItem(545435)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void TestWarnOnChangingScopes2()
        {
            Test(
@"using System ; using System . Linq ; class Program { void Main ( ) { var [||]i = 0 ; foreach ( var v in new [ ] { 1 } ) { Console . Write ( i ) ; i ++ ; } } } ",
@"using System ; using System . Linq ; class Program { void Main ( ) { foreach ( var v in new [ ] { 1 } ) { {|Warning:var i = 0 ;|} Console . Write ( i ) ; i ++ ; } } } ");
        }

        [WorkItem(545840)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void InsertCastIfNecessary1()
        {
            Test(
@"using System;

static class C
{
    static int Outer(Action<int> x, object y) { return 1; }
    static int Outer(Action<string> x, string y) { return 2; }

    static void Inner(int x, int[] y) { }
    unsafe static void Inner(string x, int*[] y) { }

    static void Main()
    {
        var [||]a = Outer(x => Inner(x, null), null);
        unsafe
        {
            Console.WriteLine(a);
        }
    }
}",

@"using System;

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
            var a = Outer(x => Inner(x, null), (object)null);
            Console.WriteLine(a);
        }
    }
}",
compareTokens: false);
        }

        [WorkItem(545835)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void InsertCastIfNecessary2()
        {
            Test(
@"using System;

class X
{
    static int Foo(Func<int?, byte> x, object y) { return 1; }
    static int Foo(Func<X, byte> x, string y) { return 2; }

    const int Value = 1000;
    static void Main()
    {
        var [||]a = Foo(X => (byte)X.Value, null);
        unchecked
        {
            Console.WriteLine(a);
        }
    }
}",

@"using System;

class X
{
    static int Foo(Func<int?, byte> x, object y) { return 1; }
    static int Foo(Func<X, byte> x, string y) { return 2; }

    const int Value = 1000;
    static void Main()
    {
        unchecked
        {
            var a = Foo(X => (byte)X.Value, (object)null);
            Console.WriteLine(a);
        }
    }
}",
compareTokens: false);
        }

        [WorkItem(546267)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
        public void MissingIfNotInDeclarationSpan()
        {
            TestMissing(
@"using System;
using System.Collections.Generic;
using System.Linq;
 
class Program
{
    static void Main(string[] args)
    {
        // Comment [||]about foo!
        // Comment about foo!
        // Comment about foo!
        // Comment about foo!
        // Comment about foo!
        // Comment about foo!
        // Comment about foo!
        int foo;
 
        Console.WriteLine();
        Console.WriteLine(foo);
    }
}");
        }
    }
}
