// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ExtractMethod
{
    public class ExtractMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new ExtractMethodCodeRefactoringProvider();
        }

        [WorkItem(540799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestPartialSelection()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { bool b = true ; System . Console . WriteLine ( [|b != true|] ? b = true : b = false ) ; } } ",
@"class Program { static void Main ( string [ ] args ) { bool b = true ; System . Console . WriteLine ( {|Rename:NewMethod|} ( b ) ? b = true : b = false ) ; } private static bool NewMethod ( bool b ) { return b != true ; } } ",
index: 0);
        }

        [WorkItem(540796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540796")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestReadOfDataThatDoesNotFlowIn()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { int x = 1 ; object y = 0 ; [|int s = true ? fun ( x ) : fun ( y ) ;|] } private static T fun < T > ( T t ) { return t ; } } ",
@"class Program { static void Main ( string [ ] args ) { int x = 1 ; object y = 0 ; {|Rename:NewMethod|} ( x , y ) ; } private static void NewMethod ( int x , object y ) { int s = true ? fun ( x ) : fun ( y ) ; } private static T fun < T > ( T t ) { return t ; } } ",
index: 0);
        }

        [WorkItem(540819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540819")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnGoto()
        {
            await TestMissingAsync(@"delegate int del ( int i ) ; class C { static void Main ( string [ ] args ) { del q = x => { [|goto label2 ; return x * x ;|] } ; label2 : return ; } } ");
        }

        [WorkItem(540819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540819")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestOnStatementAfterUnconditionalGoto()
        {
            await TestAsync(
@"delegate int del ( int i ) ; class C { static void Main ( string [ ] args ) { del q = x => { goto label2 ; [|return x * x ;|] } ; label2 : return ; } } ",
@"delegate int del ( int i ) ; class C { static void Main ( string [ ] args ) { del q = x => { goto label2 ; return {|Rename:NewMethod|} ( x ) ; } ; label2 : return ; } private static int NewMethod ( int x ) { return x * x ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnNamespace()
        {
            await TestAsync(
@"class Program { void Main ( ) { [|System|] . Console . WriteLine ( 4 ) ; } } ",
@"class Program { void Main ( ) { {|Rename:NewMethod|} ( ) ; } private static void NewMethod ( ) { System . Console . WriteLine ( 4 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnType()
        {
            await TestAsync(
@"class Program { void Main ( ) { [|System . Console|] . WriteLine ( 4 ) ; } } ",
@"class Program { void Main ( ) { {|Rename:NewMethod|} ( ) ; } private static void NewMethod ( ) { System . Console . WriteLine ( 4 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnBase()
        {
            await TestAsync(
@"class Program { void Main ( ) { [|base|] . ToString ( ) ; } } ",
@"class Program { void Main ( ) { {|Rename:NewMethod|} ( ) ; } private void NewMethod ( ) { base . ToString ( ) ; } } ");
        }

        [WorkItem(545623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545623")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestOnActionInvocation()
        {
            await TestAsync(
@"using System ; class C { public static Action X { get ; set ; } } class Program { void Main ( ) { [|C . X|] ( ) ; } } ",
@"using System ; class C { public static Action X { get ; set ; } } class Program { void Main ( ) { {|Rename:GetX|} ( ) ( ) ; } private static Action GetX ( ) { return C . X ; } } ");
        }

        [WorkItem(529841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529841"), WorkItem(714632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task DisambiguateCallSiteIfNecessary1()
        {
            await TestAsync(
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

        [WorkItem(529841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529841"), WorkItem(714632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task DisambiguateCallSiteIfNecessary2()
        {
            await TestAsync(
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

        [WorkItem(530709, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530709")]
        [WorkItem(632182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632182")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task DontOverparenthesize()
        {
            await TestAsync(
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

        [WorkItem(632182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632182")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task DontOverparenthesizeGenerics()
        {
            await TestAsync(
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

        [WorkItem(984831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task PreserveCommentsBeforeDeclaration_1()
        {
            await TestAsync(
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

        [WorkItem(984831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task PreserveCommentsBeforeDeclaration_2()
        {
            await TestAsync(
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

        [WorkItem(984831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task PreserveCommentsBeforeDeclaration_3()
        {
            await TestAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTuple()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| (int, int) x = (1, 2); |]  System . Console . WriteLine ( x.Item1 ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int, int) x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.Item1); } private static (int, int) NewMethod() { return (1, 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationWithNames()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| (int a, int b) x = (1, 2); |]  System . Console . WriteLine ( x.a ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int a, int b) x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.a); } private static (int a, int b) NewMethod() { return (1, 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationWithSomeNames()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| (int a, int) x = (1, 2); |]  System . Console . WriteLine ( x.a ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int a, int) x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.a); } private static (int a, int) NewMethod() { return (1, 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleLiteralWithNames()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| (int, int) x = (a: 1, b: 2); |]  System . Console . WriteLine ( x.Item1 ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int, int) x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.Item1); } private static (int, int) NewMethod() { return (a: 1, b: 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationAndLiteralWithNames()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| (int a, int b) x = (c: 1, d: 2); |]  System . Console . WriteLine ( x.a ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int a, int b) x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.a); } private static (int a, int b) NewMethod() { return (c: 1, d: 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleIntoVar()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| var x = (c: 1, d: 2); |]  System . Console . WriteLine ( x.c ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int c, int d) x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.c); } private static (int c, int d) NewMethod() { return (c: 1, d: 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task RefactorWithoutSystemValueTuple()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| var x = (c: 1, d: 2); |]  System . Console . WriteLine ( x.c ); } } ",
@"class Program { static void Main ( string [ ] args ) { object x = {|Rename:NewMethod|}(); System.Console.WriteLine(x.c); } private static object NewMethod() { return (c: 1, d: 2); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleWithNestedNamedTuple()
        {
            // This is not the best refactoring, but this is an edge case
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { [| var x = new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: ""hello"", b: ""world"")); |]  System . Console . WriteLine ( x.c ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { (int, int, int, int, int, int, int, string, string) x = {|Rename:NewMethod|}(); System . Console . WriteLine ( x.c ); } private static (int, int, int, int, int, int, int, string, string) NewMethod() { return new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: ""hello"", b: ""world"")); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TestDeconstruction()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { var (x, y) = [| (1, 2) |];  System . Console . WriteLine ( x ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { var (x, y) = {|Rename:NewMethod|}(); System . Console . WriteLine ( x ); } private static (int, int) NewMethod() { return (1, 2); } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TestDeconstruction2()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { var (x, y) = (1, 2); var z = [| 3; |]  System . Console . WriteLine ( z ); } } " + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program { static void Main ( string [ ] args ) { var (x, y) = (1, 2); int z = {|Rename:NewMethod|}(); System . Console . WriteLine ( z ); } private static int NewMethod() { return 3; } }" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.OutVar)]
        public async Task TestOutVar()
        {
            await TestAsync(
@"class C { static void M(int i) { int r; [|r = M1(out int y, i);|] System.Console.WriteLine(r + y); } } ",
@"class C { static void M(int i) { int r; int y; {|Rename:NewMethod|}(i, out r, out y); System.Console.WriteLine(r + y); } 
private static void NewMethod(int i, out int r, out int y) { r = M1(out y, i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task TestIsPattern()
        {
            await TestAsync(
@"class C { static void M(int i) { int r; [|r = M1(3 is int y, i);|] System.Console.WriteLine(r + y); } } ",
@"class C { static void M(int i) { int r; int y; {|Rename:NewMethod|}(i, out r, out y); System.Console.WriteLine(r + y); }
private static void NewMethod(int i, out int r, out int y) { r = M1(3 is int {|Conflict:y|}, i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task TestOutVarAndIsPattern()
        {
            await TestAsync(
@"class C
{
    static void M()
    {
        int r;
        [|r = M1(out /*out*/ int /*int*/ y /*y*/) + M2(3 is int z);|]
        System.Console.WriteLine(r + y + z);
    }
} ",
@"class C
{
    static void M()
    {
        int r;
        int y, z;
        {|Rename:NewMethod|}(out r, out y, out z);
        System.Console.WriteLine(r + y + z);
    }

    private static void NewMethod(out int r, out int y, out int z)
    {
        r = M1(out /*out*/  /*int*/ y /*y*/) + M2(3 is int {|Conflict:z|});
    }
} ",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task ConflictingOutVarLocals()
        {
            await TestAsync(
@"class C { static void M() { int r; [| r = M1(out int y); { M2(out int y); System.Console.Write(y); }|] System.Console.WriteLine(r + y); } } ",
@"class C { static void M() { int r; int y; {|Rename:NewMethod|}(out r, out y); System.Console.WriteLine(r + y); }
private static void NewMethod(out int r, out int y) { r = M1(out y); { M2(out int y); System.Console.Write(y); } } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task ConflictingPatternLocals()
        {
            await TestAsync(
@"class C { static void M() { int r; [| r = M1(1 is int y); { M2(2 is int y); System.Console.Write(y); }|] System.Console.WriteLine(r + y); } } ",
@"class C { static void M() { int r; int y; {|Rename:NewMethod|}(out r, out y); System.Console.WriteLine(r + y); }
private static void NewMethod(out int r, out int y) { r = M1(1 is int {|Conflict:y|}); { M2(2 is int y); System.Console.Write(y); } } } ");
        }
     }
}