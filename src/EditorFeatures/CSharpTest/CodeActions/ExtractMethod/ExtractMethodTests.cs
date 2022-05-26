// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod.ExtractMethodCodeRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ExtractMethod
{
    public class ExtractMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ExtractMethodCodeRefactoringProvider();

        private const string EditorConfigNaming_LocalFunctions_CamelCase = @"[*]
# Naming rules

dotnet_naming_rule.local_functions_should_be_camel_case.severity = suggestion
dotnet_naming_rule.local_functions_should_be_camel_case.symbols = local_functions
dotnet_naming_rule.local_functions_should_be_camel_case.style = camel_case

# Symbol specifications

dotnet_naming_symbols.local_functions.applicable_kinds = local_function
dotnet_naming_symbols.local_functions.applicable_accessibilities = *
dotnet_naming_symbols.local_functions.required_modifiers = 

# Naming styles

dotnet_naming_style.camel_case.capitalization = camel_case";

        [Fact]
        [WorkItem(39946, "https://github.com/dotnet/roslyn/issues/39946")]
        public async Task LocalFuncExtract()
        {
            await TestInRegularAndScript1Async(@"
class C
{
    int Testing;

    void M()
    {
        local();

        [|NewMethod();|]
        
        Testing = 5;

        void local()
        { }
    }

    void NewMethod()
    {
    }
}", @"
class C
{
    int Testing;

    void M()
    {
        local();
        {|Rename:NewMethod1|}();

        Testing = 5;

        void local()
        { }
    }

    private void NewMethod1()
    {
        NewMethod();
    }

    void NewMethod()
    {
    }
}");
        }

        [WorkItem(540799, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540799")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestPartialSelection()
        {
            await TestInRegularAndScript1Async(
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
        public async Task TestSelectionOfSwitchExpressionArm()
        {
            await TestInRegularAndScript1Async(
@"class Program
{
    int Foo(int x) => x switch
    {
        1 => 1,
        _ => [|1 + x|]
    };
}",
@"class Program
{
    int Foo(int x) => x switch
    {
        1 => 1,
        _ => {|Rename:NewMethod|}(x)
    };
    private static int NewMethod(int x) => 1 + x;
}",
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestSelectionOfSwitchExpressionArmContainingVariables()
        {
            await TestInRegularAndScript1Async(
@"using System;
using System.Collections.Generic;

class TestClass
{
    public static T RecursiveExample<T>(IEnumerable<T> sequence) =>
    sequence switch
    {
        Array { Length: 0 } => default(T),
        Array { Length: 1 } array => [|(T)array.GetValue(0)|],
        Array { Length: 2 } array => (T)array.GetValue(1),
        Array array => (T)array.GetValue(2),
        _ => throw new NotImplementedException(),
    };
}",
@"using System;
using System.Collections.Generic;

class TestClass
{
    public static T RecursiveExample<T>(IEnumerable<T> sequence) =>
    sequence switch
    {
        Array { Length: 0 } => default(T),
        Array { Length: 1 } array => {|Rename:NewMethod|}<T>(array),
        Array { Length: 2 } array => (T)array.GetValue(1),
        Array array => (T)array.GetValue(2),
        _ => throw new NotImplementedException(),
    };
    private static T NewMethod<T>(Array array) => (T)array.GetValue(0);
}",
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionBodyWhenPossible()
        {
            await TestInRegularAndScript1Async(
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
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine()
        {
            await TestInRegularAndScript1Async(
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
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine2()
        {
            await TestInRegularAndScript1Async(
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
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine()
        {
            await TestInRegularAndScript1Async(
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
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine2()
        {
            await TestInRegularAndScript1Async(
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
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractMethodInCtorInit()
        {
            await TestInRegularAndScript1Async(
@"
class Foo
{
    public Foo(int a, int b){}
    public Foo(int i) : this([|i * 10 + 2|], 2)
    {}
}",
@"
class Foo
{
    public Foo(int a, int b){}
    public Foo(int i) : this({|Rename:NewMethod|}(i), 2)
    { }

    private static int NewMethod(int i) => i * 10 + 2;
}",
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractMethodInCtorInitWithOutVar()
        {
            await TestInRegularAndScript1Async(
@"
class Foo
{
    public Foo(int a, int b){}
    public Foo(int i, out int q) : this([|i * 10 + (q = 2)|], 2)
    {}
}",
@"
class Foo
{
    public Foo(int a, int b){}
    public Foo(int i, out int q) : this({|Rename:NewMethod|}(i, out q), 2)
    { }

    private static int NewMethod(int i, out int q) => i * 10 + (q = 2);
}",
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractMethodInCtorInitWithByRefVar()
        {
            await TestInRegularAndScript1Async(
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Test
{
    public class BaseX
    {
        public BaseX(out int s, int sx, ref int r, in int inRef)
        {
            Console.WriteLine(""begin base ctor"");

            s = 42;
            Console.WriteLine(sx);
            Console.WriteLine(r);
            Console.WriteLine(inRef);

            r = 777;
            Console.WriteLine(inRef);
            Console.WriteLine(r);

            Console.WriteLine(""end base ctor"");
        }
    }

    public class X : BaseX
    {
        static int PrintX(int i)
        {
            Console.WriteLine(i);
            return i;
        }


        public X(out int x, ref int r) :
            base(out x, x = PrintX(x = 12), ref r, [|r++|])
        {
            Console.WriteLine($""in ctor {x}"");
        }

        static void Main()
        {
            int val = 33;
            var x = new X(out var f, ref val);
            Console.WriteLine(val);
        }
    }
}
",
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Test
{
    public class BaseX
    {
        public BaseX(out int s, int sx, ref int r, in int inRef)
        {
            Console.WriteLine(""begin base ctor"");

            s = 42;
            Console.WriteLine(sx);
            Console.WriteLine(r);
            Console.WriteLine(inRef);

            r = 777;
            Console.WriteLine(inRef);
            Console.WriteLine(r);

            Console.WriteLine(""end base ctor"");
        }
    }

    public class X : BaseX
    {
        static int PrintX(int i)
        {
            Console.WriteLine(i);
            return i;
        }


        public X(out int x, ref int r) :
            base(out x, x = PrintX(x = 12), ref r, {|Rename:NewMethod|}(ref r))
        {
            Console.WriteLine($""in ctor {x}"");
        }

        private static int NewMethod(ref int r) => r++;

        static void Main()
        {
            int val = 33;
            var x = new X(out var f, ref val);
            Console.WriteLine(val);
        }
    }
}
",
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine3()
        {
            await TestInRegularAndScript1Async(
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
new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [WorkItem(540796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540796")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestReadOfDataThatDoesNotFlowIn()
        {
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTuple()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationWithNames()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationWithSomeNames()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(18311, "https://github.com/dotnet/roslyn/issues/18311")]
        public async Task TestTupleWith1Arity()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleLiteralWithNames()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleDeclarationAndLiteralWithNames()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleIntoVar()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task RefactorWithoutSystemValueTuple()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        [WorkItem(11196, "https://github.com/dotnet/roslyn/issues/11196")]
        public async Task TestTupleWithNestedNamedTuple()
        {
            // This is not the best refactoring, but this is an edge case
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestDeconstruction()
        {
            await TestInRegularAndScript1Async(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestDeconstruction2()
        {
            await TestInRegularAndScript1Async(
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
        [CompilerTrait(CompilerFeature.OutVar)]
        public async Task TestOutVar()
        {
            await TestInRegularAndScript1Async(
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
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task TestIsPattern()
        {
            await TestInRegularAndScript1Async(
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
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task TestOutVarAndIsPattern()
        {
            await TestInRegularAndScript1Async(
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
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task ConflictingOutVarLocals()
        {
            await TestInRegularAndScript1Async(
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
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task ConflictingPatternLocals()
        {
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
            await TestInRegularAndScript1Async(
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
}", new TestParameters(options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOptions2.TrueWithSuggestionEnforcement)));
        }

        [WorkItem(15219, "https://github.com/dotnet/roslyn/issues/15219")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestUseVar2()
        {
            await TestInRegularAndScript1Async(
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
}", new TestParameters(options: Option(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOptions2.TrueWithSuggestionEnforcement)));
        }

        [Fact]
        [WorkItem(15532, "https://github.com/dotnet/roslyn/issues/15532")]
        public async Task ExtractLocalFunctionCall()
        {
            var code = @"
class C
{
    public static void Main()
    {
        void Local() { }
        [|Local();|]
    }
}";
            await TestExactActionSetOfferedAsync(code, new[] { FeaturesResources.Extract_local_function });
        }

        [Fact]
        public async Task ExtractLocalFunctionCall_2()
        {
            await TestInRegularAndScript1Async(@"
class C
{
    public static void Main()
    {
        [|void Local() { }
        Local();|]
    }
}", @"
class C
{
    public static void Main()
    {
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        void Local() { }
        Local();
    }
}");
        }

        [Fact]
        [WorkItem(15532, "https://github.com/dotnet/roslyn/issues/15532")]
        public async Task ExtractLocalFunctionCallWithCapture()
        {
            var code = @"
class C
{
    public static void Main(string[] args)
    {
        bool Local() => args == null;
        [|Local();|]
    }
}";
            await TestExactActionSetOfferedAsync(code, new[] { FeaturesResources.Extract_local_function });
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
            await TestInRegularAndScript1Async(@"
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
        {
            {|Rename:NewMethod|}();
        }
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task Bug3790()
        {
            await TestInRegularAndScript1Async(@"
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
        v = v + i;
        return v;
    }
}");
        }

        [WorkItem(538229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task Bug3790_1()
        {
            await TestInRegularAndScript1Async(@"
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
                v = {|Rename:NewMethod|}(v, i);
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task Bug3790_2()
        {
            await TestInRegularAndScript1Async(@"
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
                i = {|Rename:NewMethod|}(ref v, i);
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExpressionBodyProperty()
        {
            await TestInRegularAndScript1Async(@"
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExpressionBodyIndexer()
        {
            await TestInRegularAndScript1Async(@"
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExpressionBodyPropertyGetAccessor()
        {
            await TestInRegularAndScript1Async(@"
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExpressionBodyPropertySetAccessor()
        {
            await TestInRegularAndScript1Async(@"
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExpressionBodyIndexerGetAccessor()
        {
            await TestInRegularAndScript1Async(@"
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
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExpressionBodyIndexerSetAccessor()
        {
            await TestInRegularAndScript1Async(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
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
            await TestInRegularAndScript1Async(TestSources.Index + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|^1|]);
    }
}",
TestSources.Index +
@"
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
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|..|]);
    }
}",
TestSources.Index +
TestSources.Range + @"
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
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|..1|]);
    }
}",
TestSources.Index +
TestSources.Range + @"
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
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|1..|]);
    }
}",
TestSources.Index +
TestSources.Range + @"
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
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine([|1..2|]);
    }
}",
TestSources.Index +
TestSources.Range + @"
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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
            => TestInRegularAndScript1Async(
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

    private static string? NewMethod()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestFlowNullable_LambdaWithReturn()
            => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    public string? M()
    {
        [|string? x = null;
        Func<string?> returnNull = () =>
        {
            return null;
        };

        x = returnNull() ?? string.Empty;|]

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
        Func<string?> returnNull = () =>
        {
            return null;
        };

        x = returnNull() ?? string.Empty;
        return x;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractReadOnlyMethod()
        {
            await TestInRegularAndScript1Async(
@"struct S1
{
    readonly int M1() => 42;
    void Main()
    {
        [|int i = M1() + M1()|];
    }
}",
@"struct S1
{
    readonly int M1() => 42;
    void Main()
    {
        {|Rename:NewMethod|}();
    }

    private readonly void NewMethod()
    {
        int i = M1() + M1();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractReadOnlyMethodInReadOnlyStruct()
        {
            await TestInRegularAndScript1Async(
@"readonly struct S1
{
    int M1() => 42;
    void Main()
    {
        [|int i = M1() + M1()|];
    }
}",
@"readonly struct S1
{
    int M1() => 42;
    void Main()
    {
        {|Rename:NewMethod|}();
    }

    private void NewMethod()
    {
        int i = M1() + M1();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractNonReadOnlyMethodInReadOnlyMethod()
        {
            await TestInRegularAndScript1Async(
@"struct S1
{
    int M1() => 42;
    readonly void Main()
    {
        [|int i = M1() + M1()|];
    }
}",
@"struct S1
{
    int M1() => 42;
    readonly void Main()
    {
        {|Rename:NewMethod|}();
    }

    private void NewMethod()
    {
        int i = M1() + M1();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNullableObjectWithExplicitCast()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = null;
        var s = (string?)[|o|];
        Console.WriteLine(s);
    }
}",
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = null;
        var s = (string?){|Rename:GetO|}(o);
        Console.WriteLine(s);
    }

    private static object? GetO(object? o)
    {
        return o;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNotNullableObjectWithExplicitCast()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = new object();
        var s = (string)[|o|];
        Console.WriteLine(s);
    }
}",
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = new object();
        var s = (string){|Rename:GetO|}(o);
        Console.WriteLine(s);
    }

    private static object GetO(object o)
    {
        return o;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNotNullableWithExplicitCast()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class A
{
}

class B : A 
{
}

class C
{
    void M()
    {
        B? b = new B();
        var s = (A)[|b|];
    }
}",
@"#nullable enable

using System;

class A
{
}

class B : A 
{
}

class C
{
    void M()
    {
        B? b = new B();
        var s = (A){|Rename:GetB|}(b);
    }

    private static B GetB(B b)
    {
        return b;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNullableWithExplicitCast()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class A
{
}

class B : A 
{
}

class C
{
    void M()
    {
        B? b = null;
        var s = (A)[|b|];
    }
}",
@"#nullable enable

using System;

class A
{
}

class B : A 
{
}

class C
{
    void M()
    {
        B? b = null;
        var s = (A){|Rename:GetB|}(b);
    }

    private static B? GetB(B? b)
    {
        return b;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNotNullableWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = new object();
        var s = [|(string)o|];
        Console.WriteLine(s);
    }
}",
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = new object();
        var s = {|Rename:GetS|}(o);
        Console.WriteLine(s);
    }

    private static string GetS(object o)
    {
        return (string)o;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNullableWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = null;
        var s = [|(string?)o|];
        Console.WriteLine(s);
    }
}",
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = null;
        var s = {|Rename:GetS|}(o);
        Console.WriteLine(s);
    }

    private static string? GetS(object? o)
    {
        return (string?)o;
    }
}");
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNullableNonNullFlowWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = new object();
        var s = [|(string?)o|];
        Console.WriteLine(s);
    }
}",
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = new object();
        var s = {|Rename:GetS|}(o);
        Console.WriteLine(s);
    }

    private static string? GetS(object o)
    {
        return (string?)o;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public Task TestExtractNullableToNonNullableWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = null;
        var s = [|(string)o|];
        Console.WriteLine(s);
    }
}",
@"#nullable enable

using System;

class C
{
    void M()
    {
        object? o = null;
        var s = {|Rename:GetS|}(o);
        Console.WriteLine(s);
    }

    private static string? GetS(object? o)
    {
        return (string)o;
    }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task EnsureStaticLocalFunctionOptionHasNoEffect()
        {
            await TestInRegularAndScript1Async(
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
}", new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOptions2.FalseWithSuggestionEnforcement)));
        }

        [Fact, WorkItem(39946, "https://github.com/dotnet/roslyn/issues/39946"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task ExtractLocalFunctionCallAndDeclaration()
        {
            await TestInRegularAndScript1Async(@"
class C
{
    public static void Main()
    {
        static void LocalParent()
        {
            [|void Local() { }
            Local();|]
        }
    }
}", @"
class C
{
    public static void Main()
    {
        static void LocalParent()
        {
            {|Rename:NewMethod|}();
        }
    }

    private static void NewMethod()
    {
        void Local() { }
        Local();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingWhenOnlyLocalFunctionCallSelected()
        {
            var code = @"
class Program
{
    static void Main(string[] args)
    {
        [|Local();|]
        static void Local()
        {
        }
    }
}";
            await TestExactActionSetOfferedAsync(code, new[] { FeaturesResources.Extract_local_function });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestOfferedWhenBothLocalFunctionCallAndDeclarationSelected()
        {
            await TestInRegularAndScript1Async(@"
class Program
{
    static void Main(string[] args)
    {
        [|Local();
        var test = 5;
        static void Local()
        {
        }|]
    }
}", @"
class Program
{
    static void Main(string[] args)
    {
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        Local();
        var test = 5;
        static void Local()
        {
        }
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractNonAsyncMethodWithAsyncLocalFunction()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M() 
    {
        [|F();
        async void F() => await Task.Delay(0);|]
    }
}",
@"class C
{
    void M()
    {
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        F();
        async void F() => await Task.Delay(0);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitFalse()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(false)|];
    }
}",
@"class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
    }

    private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
    {
        return await Task.Delay(duration).ConfigureAwait(false);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitFalseNamedParameter()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(continueOnCapturedContext: false)|];
    }
}",
@"class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
    }

    private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
    {
        return await Task.Delay(duration).ConfigureAwait(continueOnCapturedContext: false);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitFalseOnNonTask()
        {
            await TestInRegularAndScript1Async(
@"using System.Threading.Tasks

class C
{
    async Task MyDelay() 
    {
        [|await new ValueTask<int>(0).ConfigureAwait(false)|];
    }
}",
@"using System.Threading.Tasks

class C
{
    async Task MyDelay()
    {
        await {|Rename:NewMethod|}().ConfigureAwait(false);
    }

    private static async Task<object> NewMethod()
    {
        return await new ValueTask<int>(0).ConfigureAwait(false);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitTrue()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(true)|];
    }
}",
@"class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration);
    }

    private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
    {
        return await Task.Delay(duration).ConfigureAwait(true);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitNonLiteral()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(M())|];
    }
}",
@"class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration);
    }

    private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
    {
        return await Task.Delay(duration).ConfigureAwait(M());
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithNoConfigureAwait()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration)|];
    }
}",
@"class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration);
    }

    private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
    {
        return await Task.Delay(duration);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitFalseInLambda()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Run(async () => await Task.Delay(duration).ConfigureAwait(false))|];
    }
}",
@"class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration);
    }

    private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
    {
        return await Task.Run(async () => await Task.Delay(duration).ConfigureAwait(false));
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitFalseInLocalMethod()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Run(F());
        async Task F() => await Task.Delay(duration).ConfigureAwait(false);|]
    }
}",
@"using System.Threading.Tasks;

class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration);
    }

    private static async Task NewMethod(TimeSpan duration)
    {
        await Task.Run(F());
        async Task F() => await Task.Delay(duration).ConfigureAwait(false);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitMixture1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(false);
        await Task.Delay(duration).ConfigureAwait(true);|]
    }
}",
@"using System.Threading.Tasks;

class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
    }

    private static async Task NewMethod(TimeSpan duration)
    {
        await Task.Delay(duration).ConfigureAwait(false);
        await Task.Delay(duration).ConfigureAwait(true);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitMixture2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(true);
        await Task.Delay(duration).ConfigureAwait(false);|]
    }
}",
@"using System.Threading.Tasks;

class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
    }

    private static async Task NewMethod(TimeSpan duration)
    {
        await Task.Delay(duration).ConfigureAwait(true);
        await Task.Delay(duration).ConfigureAwait(false);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitMixture3()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        [|await Task.Delay(duration).ConfigureAwait(M());
        await Task.Delay(duration).ConfigureAwait(false);|]
    }
}",
@"using System.Threading.Tasks;

class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
    }

    private static async Task NewMethod(TimeSpan duration)
    {
        await Task.Delay(duration).ConfigureAwait(M());
        await Task.Delay(duration).ConfigureAwait(false);
    }
}");
        }

        [Fact, WorkItem(38529, "https://github.com/dotnet/roslyn/issues/38529"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestExtractAsyncMethodWithConfigureAwaitFalseOutsideSelection()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    async Task MyDelay(TimeSpan duration) 
    {
        await Task.Delay(duration).ConfigureAwait(false);
        [|await Task.Delay(duration).ConfigureAwait(true);|]
    }
}",
@"using System.Threading.Tasks;

class C
{
    async Task MyDelay(TimeSpan duration)
    {
        await Task.Delay(duration).ConfigureAwait(false);
        await {|Rename:NewMethod|}(duration);
    }

    private static async Task NewMethod(TimeSpan duration)
    {
        await Task.Delay(duration).ConfigureAwait(true);
    }
}");
        }

        [Fact, WorkItem(40188, "https://github.com/dotnet/roslyn/issues/40188"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestEditorconfigSetting_ExpressionBodiedLocalFunction_True()
        {
            var input = @"
<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath = ""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        [|bool b = true;|]
        System.Console.WriteLine(b != true ? b = true : b = false);
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">[*.cs]
csharp_style_expression_bodied_methods = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        bool b = {|Rename:NewMethod|}();
        System.Console.WriteLine(b != true ? b = true : b = false);
    }

    private static bool NewMethod() => true;
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">[*.cs]
csharp_style_expression_bodied_methods = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            await TestInRegularAndScript1Async(input, expected);
        }

        [Fact, WorkItem(40188, "https://github.com/dotnet/roslyn/issues/40188"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestEditorconfigSetting_ExpressionBodiedLocalFunction_False()
        {
            var input = @"
<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath = ""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        [|bool b = true;|]
        System.Console.WriteLine(b != true ? b = true : b = false);
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">[*.cs]
csharp_style_expression_bodied_methods = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        bool b = {|Rename:NewMethod|}();
        System.Console.WriteLine(b != true ? b = true : b = false);
    }

    private static bool NewMethod()
    {
        return true;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">[*.cs]
csharp_style_expression_bodied_methods = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            await TestInRegularAndScript1Async(input, expected);
        }

        [Fact, WorkItem(40209, "https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestNaming_CamelCase_VerifyLocalFunctionSettingsDontApply()
        {
            var input = @"
<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath = ""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        [|bool b = true;|]
        System.Console.WriteLine(b != true ? b = true : b = false);
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">" + EditorConfigNaming_LocalFunctions_CamelCase + @"
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
class Program1
{
    static void Main()
    {
        bool b = {|Rename:NewMethod|}();
        System.Console.WriteLine(b != true ? b = true : b = false);
    }

    private static bool NewMethod()
    {
        return true;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">" + EditorConfigNaming_LocalFunctions_CamelCase + @"
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            await TestInRegularAndScript1Async(input, expected);
        }

        [Fact, WorkItem(40209, "https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestNaming_CamelCase_VerifyLocalFunctionSettingsDontApply_GetName()
        {
            var input = @"
<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath = ""z:\\file.cs"">
class MethodExtraction
{
    void TestMethod()
    {
        int a = [|1 + 1|];
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">" + EditorConfigNaming_LocalFunctions_CamelCase + @"
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.cs"">
class MethodExtraction
{
    void TestMethod()
    {
        int a = {|Rename:GetA|}();
    }

    private static int GetA()
    {
        return 1 + 1;
    }
}
        </Document>
        <AnalyzerConfigDocument FilePath = ""z:\\.editorconfig"">" + EditorConfigNaming_LocalFunctions_CamelCase + @"
</AnalyzerConfigDocument>
    </Project>
</Workspace>";

            await TestInRegularAndScript1Async(input, expected);
        }

        [WorkItem(40654, "https://github.com/dotnet/roslyn/issues/40654")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestOnInvalidUsingStatement_MultipleStatements()
        {
            var input = @"
class C
{
    void M()
    {
        [|var v = 0;
        using System;|]
    }
}";
            var expected = @"
class C
{
    void M()
    {
        {|Rename:NewMethod|}();
    }

    private static void NewMethod()
    {
        var v = 0;
        using System;
    }
}";
            await TestInRegularAndScript1Async(input, expected);
        }

        [WorkItem(40654, "https://github.com/dotnet/roslyn/issues/40654")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMissingOnInvalidUsingStatement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        [|using System;|]
    }
}");
        }

        [Fact, WorkItem(19461, "https://github.com/dotnet/roslyn/issues/19461")]
        public async Task TestLocalFunction()
        {
            await TestInRegularAndScript1Async(@"
using System;

class Program
{
    void M()
    {
        int y = 0;
        [|var x = local();
        
        int local()
        {
            return y;
        }|]
    }
}", @"
using System;

class Program
{
    void M()
    {
        int y = 0;
        {|Rename:NewMethod|}(y);
    }

    private static void NewMethod(int y)
    {
        var x = local();

        int local()
        {
            return y;
        }
    }
}");
        }

        [Fact, WorkItem(43834, "https://github.com/dotnet/roslyn/issues/43834")]
        public async Task TestRecursivePatternRewrite()
        {
            await TestInRegularAndScript1Async(@"
using System;
namespace N
{
    class Context
    {
    }
    class C
    {
        public void DoAction(Action<Context> action)
        {
        }
        private void Recursive(object context)
        {
            DoAction(context =>
            {
                if (context is Context { })
                {
                    DoAction(
                        [|context =>|] context.ToString());
                }
            });
        }
    }
}", @"
using System;
namespace N
{
    class Context
    {
    }
    class C
    {
        public void DoAction(Action<Context> action)
        {
        }
        private void Recursive(object context)
        {
            DoAction(context =>
            {
                if (context is Context { })
                {
                    DoAction(
                        {|Rename:NewMethod|}());
                }
            });
        }

        private static Action<Context> NewMethod()
        {
            return context => context.ToString();
        }
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess1()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?.[|ToString|]();
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static string NewMethod(List<int> b)
    {
        return b?.ToString();
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess2()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?.[|ToString|]().Length;
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static int? NewMethod(List<int> b)
    {
        return b?.ToString().Length;
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess3()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?.Count.[|ToString|]();
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static string NewMethod(List<int> b)
    {
        return b?.Count.ToString();
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess4()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?.[|Count|].ToString();
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static string NewMethod(List<int> b)
    {
        return b?.Count.ToString();
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess5()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?.[|ToString|]()?.ToString();
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static string NewMethod(List<int> b)
    {
        return b?.ToString()?.ToString();
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess6()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?.ToString()?.[|ToString|]();
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static string NewMethod(List<int> b)
    {
        return b?.ToString()?.ToString();
    }
}");
        }

        [Fact, WorkItem(41895, "https://github.com/dotnet/roslyn/issues/41895")]
        public async Task TestConditionalAccess7()
        {
            await TestInRegularAndScript1Async(@"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = b?[|[0]|];
    }
}", @"
using System;
using System.Collections.Generic;
class C
{
    void Test()
    {
        List<int> b = null;
        b?.Clear();
        _ = {|Rename:NewMethod|}(b);
    }

    private static int? NewMethod(List<int> b)
    {
        return b?[0];
    }
}");
        }

        [WorkItem(48453, "https://github.com/dotnet/roslyn/issues/48453")]
        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [InlineData("record")]
        [InlineData("record class")]
        public async Task TestInRecord(string record)
        {
            await TestInRegularAndScript1Async($@"
{record} Program
{{
    int field;

    public int this[int i] => [|this.field|];
}}",
$@"
{record} Program
{{
    int field;

    public int this[int i] => {{|Rename:GetField|}}();

    private int GetField()
    {{
        return this.field;
    }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestInRecordStruct()
        {
            await TestInRegularAndScript1Async(@"
record struct Program
{
    int field;

    public int this[int i] => [|this.field|];
}",
@"
record struct Program
{
    int field;

    public int this[int i] => {|Rename:GetField|}();

    private readonly int GetField()
    {
        return this.field;
    }
}");
        }

        [WorkItem(53031, "https://github.com/dotnet/roslyn/issues/53031")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMethodInNamespace()
        {
            await TestMissingInRegularAndScriptAsync(@"
namespace TestNamespace
{
    private bool TestMethod() => [|false|];
}");
        }

        [WorkItem(53031, "https://github.com/dotnet/roslyn/issues/53031")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestMethodInInterface()
        {
            await TestInRegularAndScript1Async(@"
interface TestInterface
{
    bool TestMethod() => [|false|];
}",
@"
interface TestInterface
{
    bool TestMethod() => {|Rename:NewMethod|}();

    bool NewMethod()
    {
        return false;
    }
}");
        }

        [WorkItem(53031, "https://github.com/dotnet/roslyn/issues/53031")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TestStaticMethodInInterface()
        {
            await TestInRegularAndScript1Async(@"
interface TestInterface
{
    static bool TestMethod() => [|false|];
}",
@"
interface TestInterface
{
    static bool TestMethod() => {|Rename:NewMethod|}();

    static bool NewMethod()
    {
        return false;
    }
}");
        }

        [WorkItem(56969, "https://github.com/dotnet/roslyn/issues/56969")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TopLevelStatement_FullStatement()
        {
            var code = @"
[|System.Console.WriteLine(""string"");|]
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_method),
            }.RunAsync();
        }

        [WorkItem(56969, "https://github.com/dotnet/roslyn/issues/56969")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TopLevelStatement_MultipleStatements()
        {
            var code = @"
System.Console.WriteLine(""string"");

[|int x = int.Parse(""0"");
System.Console.WriteLine(x);|]

System.Console.WriteLine(x);
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_method),
            }.RunAsync();
        }

        [WorkItem(56969, "https://github.com/dotnet/roslyn/issues/56969")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TopLevelStatement_MultipleStatementsWithUsingAndClass()
        {
            var code = @"
using System;

Console.WriteLine(""string"");

[|int x = int.Parse(""0"");
Console.WriteLine(x);|]

Console.WriteLine(x);

class Ignored { }
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_method),
            }.RunAsync();
        }

        [WorkItem(56969, "https://github.com/dotnet/roslyn/issues/56969")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TopLevelStatement_MultipleStatementsWithInvalidOrdering()
        {
            var code = @"
using System;

Console.WriteLine(""string"");

class Ignored { }

[|{|CS8803:int x = int.Parse(""0"");|}
Console.WriteLine(x);|]

Console.WriteLine(x);

class Ignored2 { }
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_method),
            }.RunAsync();
        }

        [WorkItem(56969, "https://github.com/dotnet/roslyn/issues/58013")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task TopLevelMethod_StaticMethod()
        {
            await TestInRegularAndScript1Async(@"
static void X(string s)
{
    [|s = s.Trim();|]
}",
@"
static void X(string s)
{
    s = {|Rename:NewMethod|}(s);
}

static string NewMethod(string s)
{
    s = s.Trim();
    return s;
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));
        }

        [WorkItem(56969, "https://github.com/dotnet/roslyn/issues/58013")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task StaticMethod_ExtractStatementContainingParameter()
        {
            await TestInRegularAndScript1Async(@"
public class Class
{
    static void X(string s)
    {
        [|s = s.Trim();|]
    }
}",
@"
public class Class
{
    static void X(string s)
    {
        s = {|Rename:NewMethod|}(s);
    }

    private static string NewMethod(string s)
    {
        s = s.Trim();
        return s;
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));
        }

        [WorkItem(57428, "https://github.com/dotnet/roslyn/issues/57428")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        public async Task AttributeArgumentWithLambdaBody()
        {
            await TestInRegularAndScript1Async(
@"using System.Runtime.InteropServices;
class Program
{
    static void F([DefaultParameterValue(() => { return [|null|]; })] object obj)
    {
    }
}",
@"using System.Runtime.InteropServices;
class Program
{
    static void F([DefaultParameterValue(() => { return {|Rename:NewMethod|}(); })] object obj)
    {
    }

    private static object NewMethod()
    {
        return null;
    }
}");
        }
    }
}
