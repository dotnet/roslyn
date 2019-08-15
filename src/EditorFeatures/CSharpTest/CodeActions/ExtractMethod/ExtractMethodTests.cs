// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ExtractMethod
{
    public class ExtractMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ExtractMethodCodeRefactoringProvider();

        [WorkItem(540799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestPartialSelection()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|b != true|] ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
    }

    private static bool NewMethod(bool b)
    {
        return b != true;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionBodyWhenPossible()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|b != true|] ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
    }

    private static bool NewMethod(bool b) => b != true;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|b != true|] ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
    }

    private static bool NewMethod(bool b) => b != true;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine(

            [|b != true|]
                ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine(

            {|Rename:NewMethod|}(b)
                ? b = true : b = false);
    }

    private static bool NewMethod(bool b) => b != true;
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|b != 
            true|] ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
    }

    private static bool NewMethod(bool b)
    {
        return b !=
                    true;
    }
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|b !=/*
*/true|] ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
    }

    private static bool NewMethod(bool b)
    {
        return b !=/*
*/true;
    }
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine3()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|"""" != @""
""|] ? b = true : b = false);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine({|Rename:NewMethod|}() ? b = true : b = false);
    }

    private static bool NewMethod()
    {
        return """" != @""
"";
    }
}",
options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));
        }

        [WorkItem(540796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540796")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestReadOfDataThatDoesNotFlowIn()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        object y = 0;
        [|int s = true ? fun(x) : fun(y);|]
    }

    private static T fun<T>(T t)
    {
        return t;
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        object y = 0;
        {|Rename:NewMethod|}(x, y);
    }

    private static void NewMethod(int x, object y)
    {
        int s = true ? fun(x) : fun(y);
    }

    private static T fun<T>(T t)
    {
        return t;
    }
}");
        }

        [WorkItem(540819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540819")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnGoto()
        {
            await TestMissingInRegularAndScriptAsync(
@"delegate int del(int i);

class C
{
    static void Main(string[] args)
    {
        del q = x => {
            [|goto label2;
            return x * x;|]
        };
    label2:
        return;
    }
}");
        }

        [WorkItem(540819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540819")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestOnStatementAfterUnconditionalGoto()
        {
            await TestInRegularAndScriptAsync(
@"delegate int del(int i);

class C
{
    static void Main(string[] args)
    {
        del q = x => {
            goto label2;
            [|return x * x;|]
        };
    label2:
        return;
    }
}",
@"delegate int del(int i);

class C
{
    static void Main(string[] args)
    {
        del q = x =>
        {
            goto label2;
            return {|Rename:NewMethod|}(x);
        };
    label2:
        return;
    }

    private static int NewMethod(int x)
    {
        return x * x;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnNamespace()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System|].Console.WriteLine(4);
    }
}",
@"class Program
{
    void Main()
    {
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        System.Console.WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnType()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|System.Console|].WriteLine(4);
    }
}",
@"class Program
{
    void Main()
    {
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        System.Console.WriteLine(4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnBase()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        [|base|].ToString();
    }
}",
@"class Program
{
    void Main()
    {
        {|Rename:NewMethod|}();
    }

    private void NewMethod()
    {
        base.ToString();
    }
}");
        }

        [WorkItem(545623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545623")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestOnActionInvocation()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    public static Action X { get; set; }
}

class Program
{
    void Main()
    {
        [|C.X|]();
    }
}",
@"using System;

class C
{
    public static Action X { get; set; }
}

class Program
{
    void Main()
    {
        {|Rename:GetX|}()();
    }

    private static Action GetX()
    {
        return C.X;
    }
}");
        }

        [WorkItem(529841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529841"), WorkItem(714632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task DisambiguateCallSiteIfNecessary1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Goo([|x => 0|], y => 0, z, z);
    }

    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Goo({|Rename:NewMethod|}(), y => (byte)0, z, z);
    }

    private static Func<byte, byte> NewMethod()
    {
        return x => 0;
    }

    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}");
        }

        [WorkItem(529841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529841"), WorkItem(714632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task DisambiguateCallSiteIfNecessary2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Goo([|x => 0|], y => { return 0; }, z, z);
    }

    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Goo({|Rename:NewMethod|}(), y => { return (byte)0; }, z, z);
    }

    private static Func<byte, byte> NewMethod()
    {
        return x => 0;
    }

    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}");
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
    static void Ex(this string x)
    {
    }

    static void Inner(Action<string> x, string y)
    {
    }

    static void Inner(Action<string> x, int y)
    {
    }

    static void Inner(Action<int> x, int y)
    {
    }

    static void Outer(Action<string> x, object y)
    {
        Console.WriteLine(1);
    }

    static void Outer(Action<int> x, int y)
    {
        Console.WriteLine(2);
    }

    static void Main()
    {
        Outer(y => Inner(x => [|x|].Ex(), y), - -1);
    }
}

static class E
{
    public static void Ex(this int x)
    {
    }
}",

@"using System;

static class C
{
    static void Ex(this string x)
    {
    }

    static void Inner(Action<string> x, string y)
    {
    }

    static void Inner(Action<string> x, int y)
    {
    }

    static void Inner(Action<int> x, int y)
    {
    }

    static void Outer(Action<string> x, object y)
    {
        Console.WriteLine(1);
    }

    static void Outer(Action<int> x, int y)
    {
        Console.WriteLine(2);
    }

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
    public static void Ex(this int x)
    {
    }
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
    static void Ex<T>(this string x)
    {
    }

    static void Inner(Action<string> x, string y)
    {
    }

    static void Inner(Action<string> x, int y)
    {
    }

    static void Inner(Action<int> x, int y)
    {
    }

    static void Outer(Action<string> x, object y)
    {
        Console.WriteLine(1);
    }

    static void Outer(Action<int> x, int y)
    {
        Console.WriteLine(2);
    }

    static void Main()
    {
        Outer(y => Inner(x => [|x|].Ex<int>(), y), - -1);
    }
}

static class E
{
    public static void Ex<T>(this int x)
    {
    }
}",

@"using System;

static class C
{
    static void Ex<T>(this string x)
    {
    }

    static void Inner(Action<string> x, string y)
    {
    }

    static void Inner(Action<string> x, int y)
    {
    }

    static void Inner(Action<int> x, int y)
    {
    }

    static void Outer(Action<string> x, object y)
    {
        Console.WriteLine(1);
    }

    static void Outer(Action<int> x, int y)
    {
        Console.WriteLine(2);
    }

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
    public static void Ex<T>(this int x)
    {
    }
}",

parseOptions: Options.Regular);
        }

        [WorkItem(984831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task PreserveCommentsBeforeDeclaration_1()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(984831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task PreserveCommentsBeforeDeclaration_2()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [WorkItem(984831, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task PreserveCommentsBeforeDeclaration_3()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTuple()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|(int, int) x = (1, 2);|]
        System.Console.WriteLine(x.Item1);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int, int) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.Item1);
    }

    private static (int, int) NewMethod()
    {
        return (1, 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|(int a, int b) x = (1, 2);|]
        System.Console.WriteLine(x.a);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int a, int b) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.a);
    }

    private static (int a, int b) NewMethod()
    {
        return (1, 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationWithSomeNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|(int a, int) x = (1, 2);|]
        System.Console.WriteLine(x.a);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int a, int) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.a);
    }

    private static (int a, int) NewMethod()
    {
        return (1, 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(18311, "https://github.com/dotnet/roslyn/issues/18311")]
        public async Task TestTupleWith1Arity()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program
{
    static void Main(string[] args)
    {
        ValueTuple<int> y = ValueTuple.Create(1);
        [|y.Item1.ToString();|]
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"using System;
class Program
{
    static void Main(string[] args)
    {
        ValueTuple<int> y = ValueTuple.Create(1);
        {|Rename:NewMethod|}(y);
    }

    private static void NewMethod(ValueTuple<int> y)
    {
        y.Item1.ToString();
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleLiteralWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|(int, int) x = (a: 1, b: 2);|]
        System.Console.WriteLine(x.Item1);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int, int) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.Item1);
    }

    private static (int, int) NewMethod()
    {
        return (a: 1, b: 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationAndLiteralWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|(int a, int b) x = (c: 1, d: 2);|]
        System.Console.WriteLine(x.a);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int a, int b) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.a);
    }

    private static (int a, int b) NewMethod()
    {
        return (c: 1, d: 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleIntoVar()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|var x = (c: 1, d: 2);|]
        System.Console.WriteLine(x.c);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int c, int d) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.c);
    }

    private static (int c, int d) NewMethod()
    {
        return (c: 1, d: 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task RefactorWithoutSystemValueTuple()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|var x = (c: 1, d: 2);|]
        System.Console.WriteLine(x.c);
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        (int c, int d) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.c);
    }

    private static (int c, int d) NewMethod()
    {
        return (c: 1, d: 2);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleWithNestedNamedTuple()
        {
            // This is not the best refactoring, but this is an edge case
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        [|var x = new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: ""hello"", b: ""world""));|]
        System.Console.WriteLine(x.c);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        (int, int, int, int, int, int, int, string, string) x = {|Rename:NewMethod|}();
        System.Console.WriteLine(x.c);
    }

    private static (int, int, int, int, int, int, int, string, string) NewMethod()
    {
        return new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: ""hello"", b: ""world""));
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TestDeconstruction()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        var (x, y) = [|(1, 2)|];
        System.Console.WriteLine(x);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        var (x, y) = {|Rename:NewMethod|}();
        System.Console.WriteLine(x);
    }

    private static (int, int) NewMethod()
    {
        return (1, 2);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TestDeconstruction2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        var (x, y) = (1, 2);
        var z = [|3;|]
        System.Console.WriteLine(z);
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs,
@"class Program
{
    static void Main(string[] args)
    {
        var (x, y) = (1, 2);
        int z = {|Rename:NewMethod|}();
        System.Console.WriteLine(z);
    }

    private static int NewMethod()
    {
        return 3;
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.OutVar)]
        public async Task TestOutVar()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M(int i)
    {
        int r;
        [|r = M1(out int y, i);|]
        System.Console.WriteLine(r + y);
    }
}",
@"class C
{
    static void M(int i)
    {
        int r;
        int y;
        {|Rename:NewMethod|}(i, out r, out y);
        System.Console.WriteLine(r + y);
    }

    private static void NewMethod(int i, out int r, out int y)
    {
        r = M1(out y, i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task TestIsPattern()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M(int i)
    {
        int r;
        [|r = M1(3 is int y, i);|]
        System.Console.WriteLine(r + y);
    }
}",
@"class C
{
    static void M(int i)
    {
        int r;
        int y;
        {|Rename:NewMethod|}(i, out r, out y);
        System.Console.WriteLine(r + y);
    }

    private static void NewMethod(int i, out int r, out int y)
    {
        r = M1(3 is int {|Conflict:y|}, i);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task TestOutVarAndIsPattern()
        {
            await TestInRegularAndScriptAsync(
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
} ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task ConflictingOutVarLocals()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        int r;
        [|r = M1(out int y);
        {
            M2(out int y);
            System.Console.Write(y);
        }|]

        System.Console.WriteLine(r + y);
    }
}",
@"class C
{
    static void M()
    {
        int r;
        int y;
        {|Rename:NewMethod|}(out r, out y);

        System.Console.WriteLine(r + y);
    }

    private static void NewMethod(out int r, out int y)
    {
        r = M1(out y);
        {
            M2(out int y);
            System.Console.Write(y);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Patterns)]
        public async Task ConflictingPatternLocals()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void M()
    {
        int r;
        [|r = M1(1 is int y);
        {
            M2(2 is int y);
            System.Console.Write(y);
        }|]

        System.Console.WriteLine(r + y);
    }
}",
@"class C
{
    static void M()
    {
        int r;
        int y;
        {|Rename:NewMethod|}(out r, out y);

        System.Console.WriteLine(r + y);
    }

    private static void NewMethod(out int r, out int y)
    {
        r = M1(1 is int {|Conflict:y|});
        {
            M2(2 is int y);
            System.Console.Write(y);
        }
    }
}");
        }

        [WorkItem(15218, "https://github.com/dotnet/roslyn/issues/15218")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestCancellationTokenGoesLast()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Threading;

class C
{
    void M(CancellationToken ct)
    {
        var v = 0;

        [|if (true)
        {
            ct.ThrowIfCancellationRequested();
            Console.WriteLine(v);
        }|]
    }
}",
@"using System;
using System.Threading;

class C
{
    void M(CancellationToken ct)
    {
        var v = 0;
        {|Rename:NewMethod|}(v, ct);
    }

    private static void NewMethod(int v, CancellationToken ct)
    {
        if (true)
        {
            ct.ThrowIfCancellationRequested();
            Console.WriteLine(v);
        }
    }
}");
        }

        [WorkItem(15219, "https://github.com/dotnet/roslyn/issues/15219")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseVar1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo(int i)
    {
        [|var v = (string)null;

        switch (i)
        {
            case 0: v = ""0""; break;
            case 1: v = ""1""; break;
        }|]

        Console.WriteLine(v);
    }
}",
@"using System;

class C
{
    void Goo(int i)
    {
        var v = {|Rename:NewMethod|}(i);

        Console.WriteLine(v);
    }

    private static string NewMethod(int i)
    {
        var v = (string)null;

        switch (i)
        {
            case 0: v = ""0""; break;
            case 1: v = ""1""; break;
        }

        return v;
    }
}", options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions.TrueWithSuggestionEnforcement));
        }

        [WorkItem(15219, "https://github.com/dotnet/roslyn/issues/15219")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseVar2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void Goo(int i)
    {
        [|var v = (string)null;

        switch (i)
        {
            case 0: v = ""0""; break;
            case 1: v = ""1""; break;
        }|]

        Console.WriteLine(v);
    }
}",
@"using System;

class C
{
    void Goo(int i)
    {
        string v = {|Rename:NewMethod|}(i);

        Console.WriteLine(v);
    }

    private static string NewMethod(int i)
    {
        var v = (string)null;

        switch (i)
        {
            case 0: v = ""0""; break;
            case 1: v = ""1""; break;
        }

        return v;
    }
}", options: Option(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOptions.TrueWithSuggestionEnforcement));
        }

        [Fact]
        [WorkItem(15532, "https://github.com/dotnet/roslyn/issues/15532")]
        public async Task ExtractLocalFunctionCall()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public static void Main()
    {
        void Local() { }
        [|Local();|]
    }
}", @"
class C
{
    public static void Main()
    {
        void Local() { }
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        {|Warning:Local();|}
    }
}");
        }

        [Fact]
        [WorkItem(15532, "https://github.com/dotnet/roslyn/issues/15532")]
        public async Task ExtractLocalFunctionCallWithCapture()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public static void Main(string[] args)
    {
        bool Local() => args == null;
        [|Local();|]
    }
}", @"
class C
{
    public static void Main(string[] args)
    {
        bool Local() => args == null;
        {|Rename:NewMethod|}(args);
    }

    private static void NewMethod(string[] args)
    {
        {|Warning:Local();|}
    }
}");
        }

        [Fact]
        [WorkItem(15532, "https://github.com/dotnet/roslyn/issues/15532")]
        public async Task ExtractLocalFunctionDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(@"
class C
{
    public static void Main()
    {
        [|bool Local() => args == null;|]
        Local();
    }
}");
        }

        [Fact]
        [WorkItem(15532, "https://github.com/dotnet/roslyn/issues/15532")]
        public async Task ExtractLocalFunctionInterior()
        {
            await TestInRegularAndScriptAsync(@"
class C
{
    public static void Main()
    {
        void Local()
        {
            [|int x = 0;
            x++;|]
        }
        Local();
    }
}", @"
class C
{
    public static void Main()
    {
        void Local()
        {|Warning:{
            {|Rename:NewMethod|}();
        }|}
        Local();
    }

    private static void NewMethod()
    {
        int x = 0;
        x++;
    }
}");
        }

        [WorkItem(538229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug3790()
        {
            await TestInRegularAndScriptAsync(@"
class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                [|v = v + i;|]
            }
        }
    }
}", @"
class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                v = {|Rename:NewMethod|}(v, i);
            }
        }
    }

    private static int NewMethod(int v, int i)
    {
        {|Warning:v = v + i;|}
        return v;
    }
}");
        }

        [WorkItem(538229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug3790_1()
        {
            await TestInRegularAndScriptAsync(@"
class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                [|v = v + i|];
            }
        }
    }
}", @"
class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                {|Warning:v = {|Rename:NewMethod|}(v, i)|};
            }
        }
    }

    private static int NewMethod(int v, int i)
    {
        return v + i;
    }
}");
        }

        [WorkItem(538229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task Bug3790_2()
        {
            await TestInRegularAndScriptAsync(@"
class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                [|i = v = v + i|];
            }
        }
    }
}", @"
class Test
{
    void method()
    {
        static void Main(string[] args)
        {
            int v = 0;
            for(int i=0 ; i<5; i++)
            {
                {|Warning:i = {|Rename:NewMethod|}(ref v, i)|};
            }
        }
    }

    private static int NewMethod(ref int v, int i)
    {
        return v = v + i;
    }
}");
        }

        [WorkItem(392560, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestExpressionBodyProperty()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    int field;

    public int Blah => [|this.field|];
}",
@"
class Program
{
    int field;

    public int Blah => {|Rename:GetField|}();

    private int GetField()
    {
        return this.field;
    }
}");
        }

        [WorkItem(392560, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestExpressionBodyIndexer()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    int field;

    public int this[int i] => [|this.field|];
}",
@"
class Program
{
    int field;

    public int this[int i] => {|Rename:GetField|}();

    private int GetField()
    {
        return this.field;
    }
}");
        }

        [WorkItem(392560, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestExpressionBodyPropertyGetAccessor()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    int field;

    public int Blah
    {
        get => [|this.field|];
        set => field = value;
    }
}",
@"
class Program
{
    int field;

    public int Blah
    {
        get => {|Rename:GetField|}();
        set => field = value;
    }

    private int GetField()
    {
        return this.field;
    }
}");
        }

        [WorkItem(392560, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestExpressionBodyPropertySetAccessor()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    int field;

    public int Blah
    {
        get => this.field;
        set => field = [|value|];
    }
}",
@"
class Program
{
    int field;

    public int Blah
    {
        get => this.field;
        set => field = {|Rename:GetValue|}(value);
    }

    private static int GetValue(int value)
    {
        return value;
    }
}");
        }

        [WorkItem(392560, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestExpressionBodyIndexerGetAccessor()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    int field;

    public int this[int i]
    {
        get => [|this.field|];
        set => field = value;
    }
}",
@"
class Program
{
    int field;

    public int this[int i]
    {
        get => {|Rename:GetField|}();
        set => field = value;
    }

    private int GetField()
    {
        return this.field;
    }
}");
        }

        [WorkItem(392560, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestExpressionBodyIndexerSetAccessor()
        {
            await TestInRegularAndScriptAsync(@"
class Program
{
    int field;

    public int this[int i]
    {
        get => this.field;
        set => field = [|value|];
    }
}",
@"
class Program
{
    int field;

    public int this[int i]
    {
        get => this.field;
        set => field = {|Rename:GetValue|}(value);
    }

    private static int GetValue(int value)
    {
        return value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestTupleWithInferredNames()
        {
            await TestAsync(@"
class Program
{
    void M()
    {
        int a = 1;
        var t = [|(a, b: 2)|];
        System.Console.Write(t.a);
    }
}",
@"
class Program
{
    void M()
    {
        int a = 1;
        var t = {|Rename:GetT|}(a);
        System.Console.Write(t.a);
    }

    private static (int a, int b) GetT(int a)
    {
        return (a, b: 2);
    }
}", TestOptions.Regular7_1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestDeconstruction4()
        {
            await TestAsync(@"
class Program
{
    void M()
    {
        [|var (x, y) = (1, 2);|]
        System.Console.Write(x + y);
    }
}",
@"
class Program
{
    void M()
    {
        int x, y;
        {|Rename:NewMethod|}(out x, out y);
        System.Console.Write(x + y);
    }

    private static void NewMethod(out int x, out int y)
    {
        var (x, y) = (1, 2);
    }
}", TestOptions.Regular7_1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public async Task TestDeconstruction5()
        {
            await TestAsync(@"
class Program
{
    void M()
    {
        [|(var x, var y) = (1, 2);|]
        System.Console.Write(x + y);
    }
}",
@"
class Program
{
    void M()
    {
        int x, y;
        {|Rename:NewMethod|}(out x, out y);
        System.Console.Write(x + y);
    }

    private static void NewMethod(out int x, out int y)
    {
        (x, y) = (1, 2);
    }
}", TestOptions.Regular7_1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestIndexExpression()
        {
            await TestInRegularAndScriptAsync(TestSources.Index + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|^1|]);
    }
}", TestSources.Index + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine({|Rename:NewMethod|}());
    }

    private static System.Index NewMethod()
    {
        return ^1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestRangeExpression_Empty()
        {
            await TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|..|]);
    }
}", TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine({|Rename:NewMethod|}());
    }

    private static System.Range NewMethod()
    {
        return ..;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestRangeExpression_Left()
        {
            await TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|..1|]);
    }
}", TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine({|Rename:NewMethod|}());
    }

    private static System.Range NewMethod()
    {
        return ..1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestRangeExpression_Right()
        {
            await TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|1..|]);
    }
}", TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine({|Rename:NewMethod|}());
    }

    private static System.Range NewMethod()
    {
        return 1..;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestRangeExpression_Both()
        {
            await TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|1..2|]);
    }
}", TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine({|Rename:NewMethod|}());
    }

    private static System.Range NewMethod()
    {
        return 1..2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestAnnotatedNullableReturn()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string? M()
    {
        [|string? x = null;
        x?.ToString();|]

        return x;
    }
}",
@"#nullable enable

class C
{
    public string? M()
    {
        string? x = {|Rename:NewMethod|}();

        return x;
    }

    private static string? NewMethod()
    {
        string? x = null;
        x?.ToString();
        return x;
    }
}");


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestAnnotatedNullableParameters1()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string? M()
    {
        string? a = null;
        string? b = null;
        [|string? x = a?.Contains(b).ToString();|]

        return x;
    }
}",
@"#nullable enable

class C
{
    public string? M()
    {
        string? a = null;
        string? b = null;
        string? x = {|Rename:NewMethod|}(a, b);

        return x;
    }

    private static string? NewMethod(string? a, string? b)
    {
        return a?.Contains(b).ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestAnnotatedNullableParameters2()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = null;
        string? b = null;
        int c = 0;
        [|string x = (a + b + c).ToString();|]

        return x;
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = null;
        string? b = null;
        int c = 0;
        string x = {|Rename:NewMethod|}(a, b, c);

        return x;
    }

    private static string NewMethod(string? a, string? b, int c)
    {
        return (a + b + c).ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestAnnotatedNullableParameters3()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = null;
        string? b = null;
        int c = 0;
        return [|(a + b + c).ToString()|];
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = null;
        string? b = null;
        int c = 0;
        return {|Rename:NewMethod|}(a, b, c);
    }

    private static string NewMethod(string? a, string? b, int c)
    {
        return (a + b + c).ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestAnnotatedNullableParameters4()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string? M()
    {
        string? a = null;
        string? b = null;
        return [|a?.Contains(b).ToString()|];
    }
}",
@"#nullable enable

class C
{
    public string? M()
    {
        string? a = null;
        string? b = null;
        return {|Rename:NewMethod|}(a, b);
    }

    private static string? NewMethod(string? a, string? b)
    {
        return a?.Contains(b).ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters1()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        return [|(a + b + a).ToString()|];
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        return {|Rename:NewMethod|}(a, b);
    }

    private static string NewMethod(string a, string b)
    {
        return (a + b + a).ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters2()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string? M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        return [|(a + b + a).ToString()|];
    }
}",
@"#nullable enable

class C
{
    public string? M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        return {|Rename:NewMethod|}(a, b);
    }

    private static string NewMethod(string a, string b)
    {
        return (a + b + a).ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters3()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = null;
        string? b = null;
        return [|(a + b + a)?.ToString()|] ?? string.Empty;
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = null;
        string? b = null;
        return {|Rename:NewMethod|}(a, b) ?? string.Empty;
    }

    private static string? NewMethod(string? a, string? b)
    {
        return (a + b + a)?.ToString();
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters_MultipleStates()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        [|string? c = a + b;
        a = string.Empty;
        c += a;
        a = null;
        b = null;
        b = ""test"";
        c = a?.ToString();|]
        return c ?? string.Empty;
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        string? c = {|Rename:NewMethod|}(ref a, ref b);
        return c ?? string.Empty;
    }

    private static string? NewMethod(ref string? a, ref string? b)
    {
        string? c = a + b;
        a = string.Empty;
        c += a;
        a = null;
        b = null;
        b = ""test"";
        c = a?.ToString();
        return c;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters_MultipleStatesNonNullReturn()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        [|string? c = a + b;
        a = string.Empty;
        b = string.Empty;
        a = null;
        b = null;
        c = null;
        c = a + b;|]
        return c ?? string.Empty;
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        string? c = {|Rename:NewMethod|}(ref a, ref b);
        return c ?? string.Empty;
    }

    private static string NewMethod(ref string? a, ref string? b)
    {
        string? c = a + b;
        a = string.Empty;
        b = string.Empty;
        a = null;
        b = null;
        c = null;
        c = a + b;
        return c;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters_MultipleStatesNullReturn()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        [|string? c = a + b;
        a = string.Empty;
        b = string.Empty;
        a = null;
        b = null;
        c = a?.ToString();|]
        return c ?? string.Empty;
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        string? c = {|Rename:NewMethod|}(ref a, ref b);
        return c ?? string.Empty;
    }

    private static string? NewMethod(ref string? a, ref string? b)
    {
        string? c = a + b;
        a = string.Empty;
        b = string.Empty;
        a = null;
        b = null;
        c = a?.ToString();
        return c;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowStateNullableParameters_RefNotNull()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        [|var c = a + b;
        a = string.Empty;
        c += a;
        b = ""test"";
        c = a + b +c;|]
        return c;
    }
}",
@"#nullable enable

class C
{
    public string M()
    {
        string? a = string.Empty;
        string? b = string.Empty;
        string c = {|Rename:NewMethod|}(ref a, ref b);
        return c;
    }

    private static string NewMethod(ref string a, ref string b)
    {
        var c = a + b;
        a = string.Empty;
        c += a;
        b = ""test"";
        c = a + b + c;
        return c;
    }
}");

        // There's a case below where flow state correctly asseses that the variable
        // 'x' is non-null when returned. It's wasn't obvious when writing, but that's 
        // due to the fact the line above it being executed as 'x.ToString()' would throw
        // an exception and the return statement would never be hit. The only way the return
        // statement gets executed is if the `x.ToString()` call succeeds, thus suggesting 
        // that the value is indeed not null.
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowNullableReturn_NotNull1()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string? M()
    {
        [|string? x = null;
        x.ToString();|]

        return x;
    }
}",
@"#nullable enable

class C
{
    public string? M()
    {
        string? x = {|Rename:NewMethod|}();

        return x;
    }

    private static string NewMethod()
    {
        string? x = null;
        x.ToString();
        return x;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowNullableReturn_NotNull2()
            => TestInRegularAndScriptAsync(
@"#nullable enable

class C
{
    public string? M()
    {
        [|string? x = null;
        x?.ToString();
        x = string.Empty;|]

        return x;
    }
}",
@"#nullable enable

class C
{
    public string? M()
    {
        string? x = {|Rename:NewMethod|}();

        return x;
    }

    private static string NewMethod()
    {
        string? x = null;
        x?.ToString();
        x = string.Empty;
        return x;
    }
}");
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowNullable_Lambda()
            => TestInRegularAndScriptAsync(
@"#nullable enable

using System;

class C
{
    public string? M()
    {
        [|string? x = null;
        Action modifyXToNonNull = () =>
        {
            x += x;
        };

        modifyXToNonNull();|]

        return x;
    }
}",
@"#nullable enable

using System;

class C
{
    public string? M()
    {
        string? x = {|Rename:NewMethod|}();

        return x;
    }

    private static string NewMethod()
    {
        string? x = null;
        Action modifyXToNonNull = () =>
        {
            x += x;
        };

        modifyXToNonNull();
        return x;
    }
}");
    }
}
