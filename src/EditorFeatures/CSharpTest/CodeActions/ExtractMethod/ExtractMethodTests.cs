// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ExtractMethod;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ExtractMethod
{
    public class ExtractMethodTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new ExtractMethodCodeRefactoringProvider();
        }

        [WorkItem(540799)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestPartialSelection()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { bool b = true ; System . Console . WriteLine ( [|b != true|] ? b = true : b = false ) ; } } ",
@"class Program { static void Main ( string [ ] args ) { bool b = true ; System . Console . WriteLine ( {|Rename:NewMethod|} ( b ) ? b = true : b = false ) ; } private static bool NewMethod ( bool b ) { return b != true ; } } ",
index: 0);
        }

        [WorkItem(540796)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestReadOfDataThatDoesNotFlowIn()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { int x = 1 ; object y = 0 ; [|int s = true ? fun ( x ) : fun ( y ) ;|] } private static T fun < T > ( T t ) { return t ; } } ",
@"class Program { static void Main ( string [ ] args ) { int x = 1 ; object y = 0 ; {|Rename:NewMethod|} ( x , y ) ; } private static void NewMethod ( int x , object y ) { int s = true ? fun ( x ) : fun ( y ) ; } private static T fun < T > ( T t ) { return t ; } } ",
index: 0);
        }

        [WorkItem(540819)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestMissingOnGoto()
        {
            TestMissing(@"delegate int del ( int i ) ; class C { static void Main ( string [ ] args ) { del q = x => { [|goto label2 ; return x * x ;|] } ; label2 : return ; } } ");
        }

        [WorkItem(540819)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestOnStatementAfterUnconditionalGoto()
        {
            Test(
@"delegate int del ( int i ) ; class C { static void Main ( string [ ] args ) { del q = x => { goto label2 ; [|return x * x ;|] } ; label2 : return ; } } ",
@"delegate int del ( int i ) ; class C { static void Main ( string [ ] args ) { del q = x => { goto label2 ; return {|Rename:NewMethod|} ( x ) ; } ; label2 : return ; } private static int NewMethod ( int x ) { return x * x ; } } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestMissingOnNamespace()
        {
            Test(
@"class Program { void Main ( ) { [|System|] . Console . WriteLine ( 4 ) ; } } ",
@"class Program { void Main ( ) { {|Rename:NewMethod|} ( ) ; } private static void NewMethod ( ) { System . Console . WriteLine ( 4 ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestMissingOnType()
        {
            Test(
@"class Program { void Main ( ) { [|System . Console|] . WriteLine ( 4 ) ; } } ",
@"class Program { void Main ( ) { {|Rename:NewMethod|} ( ) ; } private static void NewMethod ( ) { System . Console . WriteLine ( 4 ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestMissingOnBase()
        {
            Test(
@"class Program { void Main ( ) { [|base|] . ToString ( ) ; } } ",
@"class Program { void Main ( ) { {|Rename:NewMethod|} ( ) ; } private void NewMethod ( ) { base . ToString ( ) ; } } ");
        }

        [WorkItem(545623)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void TestOnActionInvocation()
        {
            Test(
@"using System ; class C { public static Action X { get ; set ; } } class Program { void Main ( ) { [|C . X|] ( ) ; } } ",
@"using System ; class C { public static Action X { get ; set ; } } class Program { void Main ( ) { {|Rename:GetX|} ( ) ( ) ; } private static Action GetX ( ) { return C . X ; } } ");
        }

        [WorkItem(529841), WorkItem(714632)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void DisambiguateCallSiteIfNecessary1()
        {
            Test(
@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Foo([|x => 0|], y => 0, z, z);
    }

    static void Foo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Foo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Foo<byte, byte>({|Rename:NewMethod|}(), y => 0, z, z);
    }

    private static Func<byte, byte> NewMethod()
    {
        return x => 0;
    }

    static void Foo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Foo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

compareTokens: false);
        }

        [WorkItem(529841), WorkItem(714632)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void DisambiguateCallSiteIfNecessary2()
        {
            Test(
@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Foo([|x => 0|], y => { return 0; }, z, z);
    }

    static void Foo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Foo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Foo<byte, byte>({|Rename:NewMethod|}(), y => { return 0; }, z, z);
    }

    private static Func<byte, byte> NewMethod()
    {
        return x => 0;
    }

    static void Foo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Foo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

compareTokens: false);
        }

        [WorkItem(530709)]
        [WorkItem(632182)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void DontOverparenthesize()
        {
            Test(
@"using System;

static class C
{
    static void Ex(this string x) { }

    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, int y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => [|x|].Ex(), y), - -1);
    }
}

static class E
{
    public static void Ex(this int x) { }
}",

@"using System;

static class C
{
    static void Ex(this string x) { }

    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, int y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => {|Rename:GetX|}(x).Ex(), y), (object)- -1);
    }

    private static string GetX(string x)
    {
        return x;
    }
}

static class E
{
    public static void Ex(this int x) { }
}",

parseOptions: Options.Regular);
        }

        [WorkItem(632182)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void DontOverparenthesizeGenerics()
        {
            Test(
@"using System;

static class C
{
    static void Ex<T>(this string x) { }

    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, int y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => [|x|].Ex<int>(), y), - -1);
    }
}

static class E
{
    public static void Ex<T>(this int x) { }
}",

@"using System;

static class C
{
    static void Ex<T>(this string x) { }

    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, int y) { Console.WriteLine(2); }

    static void Main()
    {
        Outer(y => Inner(x => {|Rename:GetX|}(x).Ex<int>(), y), (object)- -1);
    }

    private static string GetX(string x)
    {
        return x;
    }
}

static class E
{
    public static void Ex<T>(this int x) { }
}",

parseOptions: Options.Regular);
        }

        [WorkItem(984831)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void PreserveCommentsBeforeDeclaration_1()
        {
            Test(
@"class Construct
{
    public void Do() { }
    static void Main(string[] args)
    {
        [|Construct obj1 = new Construct();
        obj1.Do();
        /* Interesting comment. */
        Construct obj2 = new Construct();
        obj2.Do();|]
        obj1.Do();
        obj2.Do();
    }
}",

@"class Construct
{
    public void Do() { }
    static void Main(string[] args)
    {
        Construct obj1, obj2;
        {|Rename:NewMethod|}(out obj1, out obj2);
        obj1.Do();
        obj2.Do();
    }

    private static void NewMethod(out Construct obj1, out Construct obj2)
    {
        obj1 = new Construct();
        obj1.Do();
        /* Interesting comment. */
        obj2 = new Construct();
        obj2.Do();
    }
}",

compareTokens: false);
        }

        [WorkItem(984831)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void PreserveCommentsBeforeDeclaration_2()
        {
            Test(
@"class Construct
{
    public void Do() { }
    static void Main(string[] args)
    {
        [|Construct obj1 = new Construct();
        obj1.Do();
        /* Interesting comment. */
        Construct obj2 = new Construct();
        obj2.Do();
        /* Second Interesting comment. */
        Construct obj3 = new Construct();
        obj3.Do();|]
        obj1.Do();
        obj2.Do();
        obj3.Do();
    }
}",

@"class Construct
{
    public void Do() { }
    static void Main(string[] args)
    {
        Construct obj1, obj2, obj3;
        {|Rename:NewMethod|}(out obj1, out obj2, out obj3);
        obj1.Do();
        obj2.Do();
        obj3.Do();
    }

    private static void NewMethod(out Construct obj1, out Construct obj2, out Construct obj3)
    {
        obj1 = new Construct();
        obj1.Do();
        /* Interesting comment. */
        obj2 = new Construct();
        obj2.Do();
        /* Second Interesting comment. */
        obj3 = new Construct();
        obj3.Do();
    }
}",

compareTokens: false);
        }

        [WorkItem(984831)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public void PreserveCommentsBeforeDeclaration_3()
        {
            Test(
@"class Construct
{
    public void Do() { }
    static void Main(string[] args)
    {
        [|Construct obj1 = new Construct();
        obj1.Do();
        /* Interesting comment. */
        Construct obj2 = new Construct(), obj3 = new Construct();
        obj2.Do();
        obj3.Do();|]
        obj1.Do();
        obj2.Do();
        obj3.Do();
    }
}",

@"class Construct
{
    public void Do() { }
    static void Main(string[] args)
    {
        Construct obj1, obj2, obj3;
        {|Rename:NewMethod|}(out obj1, out obj2, out obj3);
        obj1.Do();
        obj2.Do();
        obj3.Do();
    }

    private static void NewMethod(out Construct obj1, out Construct obj2, out Construct obj3)
    {
        obj1 = new Construct();
        obj1.Do();
        /* Interesting comment. */
        obj2 = new Construct();
        obj3 = new Construct();
        obj2.Do();
        obj3.Do();
    }
}",

compareTokens: false);
        }
    }
}
