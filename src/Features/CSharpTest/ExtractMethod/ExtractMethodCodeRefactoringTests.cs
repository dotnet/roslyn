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
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ExtractMethod;

using VerifyCS = CSharpCodeRefactoringVerifier<
    ExtractMethodCodeRefactoringProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
public sealed class ExtractMethodCodeRefactoringTests : AbstractCSharpCodeActionTest_NoEditor
{
    private const string SystemThreadingTasks = "System.Threading.Tasks";
    private const string SystemThreadingTasksTask = $"{SystemThreadingTasks}.Task";
    private const string SystemThreadingTasksUsing = $"using {SystemThreadingTasks};";

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ExtractMethodCodeRefactoringProvider();

    private const string EditorConfigNaming_LocalFunctions_CamelCase = """
        [*]
        # Naming rules

        dotnet_naming_rule.local_functions_should_be_camel_case.severity = suggestion
        dotnet_naming_rule.local_functions_should_be_camel_case.symbols = local_functions
        dotnet_naming_rule.local_functions_should_be_camel_case.style = camel_case

        # Symbol specifications

        dotnet_naming_symbols.local_functions.applicable_kinds = local_function
        dotnet_naming_symbols.local_functions.applicable_accessibilities = *
        dotnet_naming_symbols.local_functions.required_modifiers = 

        # Naming styles

        dotnet_naming_style.camel_case.capitalization = camel_case
        """;

    private static readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private static readonly CodeStyleOption2<bool> offWithInfo = new(false, NotificationOption2.Suggestion);

    // specify all options explicitly to override defaults.
    private OptionsCollection ImplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private OptionsCollection ExplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ImplicitForBuiltInTypes()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, onWithInfo },
        };

    private OptionsCollection NoBraces()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferBraces, new CodeStyleOption2<PreferBracesPreference>(PreferBracesPreference.None, NotificationOption2.Suggestion) }
        };

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39946")]
    public Task LocalFuncExtract()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540799")]
    public Task TestPartialSelection()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine([|b != true|] ? b = true : b = false);
                }
            }
            """,
            """
            class Program
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
            }
            """);

    [Fact]
    public Task TestSelectionOfSwitchExpressionArm()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                int Goo(int x) => x switch
                {
                    1 => 1,
                    _ => [|1 + x|]
                };
            }
            """,
            """
            class Program
            {
                int Goo(int x) => x switch
                {
                    1 => 1,
                    _ => {|Rename:NewMethod|}(x)
                };
                private static int NewMethod(int x) => 1 + x;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact]
    public Task TestSelectionOfSwitchExpressionArmContainingVariables()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionBodyWhenPossible()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine([|b != true|] ? b = true : b = false);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
                }

                private static bool NewMethod(bool b) => b != true;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine([|b != true|] ? b = true : b = false);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine({|Rename:NewMethod|}(b) ? b = true : b = false);
                }

                private static bool NewMethod(bool b) => b != true;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine(

                        [|b != true|]
                            ? b = true : b = false);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine(

                        {|Rename:NewMethod|}(b)
                            ? b = true : b = false);
                }

                private static bool NewMethod(bool b) => b != true;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine([|b != 
                        true|] ? b = true : b = false);
                }
            }
            """,
            """
            class Program
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
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine([|b !=/*
            */true|] ? b = true : b = false);
                }
            }
            """,
            """
            class Program
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
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestExtractMethodInCtorInit()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
                public Goo(int a, int b){}
                public Goo(int i) : this([|i * 10 + 2|], 2)
                {}
            }
            """,
            """
            class Goo
            {
                public Goo(int a, int b){}
                public Goo(int i) : this({|Rename:NewMethod|}(i), 2)
                { }

                private static int NewMethod(int i) => i * 10 + 2;
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestExtractMethodInCtorInitWithOutVar()
        => TestInRegularAndScriptAsync(
            """
            class Goo
            {
                public Goo(int a, int b){}
                public Goo(int i, out int q) : this([|i * 10 + (q = 2)|], 2)
                {}
            }
            """,
            """
            class Goo
            {
                public Goo(int a, int b){}
                public Goo(int i, out int q) : this({|Rename:NewMethod|}(i, out q), 2)
                { }

                private static int NewMethod(int i, out int q) => i * 10 + (q = 2);
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestExtractMethodInCtorInitWithByRefVar()
        => TestInRegularAndScriptAsync(
            """
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
                        Console.WriteLine("begin base ctor");

                        s = 42;
                        Console.WriteLine(sx);
                        Console.WriteLine(r);
                        Console.WriteLine(inRef);

                        r = 777;
                        Console.WriteLine(inRef);
                        Console.WriteLine(r);

                        Console.WriteLine("end base ctor");
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
                        Console.WriteLine($"in ctor {x}");
                    }

                    static void Main()
                    {
                        int val = 33;
                        var x = new X(out var f, ref val);
                        Console.WriteLine(val);
                    }
                }
            }
            """,
            """
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
                        Console.WriteLine("begin base ctor");

                        s = 42;
                        Console.WriteLine(sx);
                        Console.WriteLine(r);
                        Console.WriteLine(inRef);

                        r = 777;
                        Console.WriteLine(inRef);
                        Console.WriteLine(r);

                        Console.WriteLine("end base ctor");
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
                        Console.WriteLine($"in ctor {x}");
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
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact]
    public Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine3()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine([|"" != @"
            "|] ? b = true : b = false);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    bool b = true;
                    System.Console.WriteLine({|Rename:NewMethod|}() ? b = true : b = false);
                }

                private static bool NewMethod()
                {
                    return "" != @"
            ";
                }
            }
            """,
            new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540796")]
    public Task TestReadOfDataThatDoesNotFlowIn()
        => TestInRegularAndScriptAsync(
            """
            class Program
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
            }
            """,
            """
            class Program
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540819")]
    public Task TestOnGoto()
        => TestInRegularAndScriptAsync(
            """
            delegate int del(int i);

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
            }
            """,
            """
            delegate int del(int i);

            class C
            {
                static void Main(string[] args)
                {
                    del q = x =>
                    {
                        return {|Rename:NewMethod|}(x);
                    };
                label2:
                    return;
                }

                private static int NewMethod(int x)
                {
                    goto label2;
                    return x * x;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540819")]
    public Task TestOnStatementAfterUnconditionalGoto()
        => TestInRegularAndScriptAsync(
            """
            delegate int del(int i);

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
            }
            """,
            """
            delegate int del(int i);

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
            }
            """);

    [Fact]
    public Task TestMissingOnNamespace()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|System|].Console.WriteLine(4);
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    {|Rename:NewMethod|}();
                }

                private static void NewMethod()
                {
                    System.Console.WriteLine(4);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnType()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|System.Console|].WriteLine(4);
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    {|Rename:NewMethod|}();
                }

                private static void NewMethod()
                {
                    System.Console.WriteLine(4);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnBase()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    [|base|].ToString();
                }
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    {|Rename:NewMethod|}();
                }

                private void NewMethod()
                {
                    base.ToString();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545623")]
    public Task TestOnActionInvocation()
        => TestInRegularAndScriptAsync(
            """
            using System;

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
            }
            """,
            """
            using System;

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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529841"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
    public Task DisambiguateCallSiteIfNecessary1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    byte z = 0;
                    Goo([|x => 0|], y => 0, z, z);
                }

                static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
                static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
            }
            """,

            """
            using System;

            class Program
            {
                static void Main()
                {
                    byte z = 0;
                    Goo({|Rename:NewMethod|}(), y => 0, z, z);
                }

                private static Func<byte, byte> NewMethod()
                {
                    return x => 0;
                }

                static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
                static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529841"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
    public Task DisambiguateCallSiteIfNecessary2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    byte z = 0;
                    Goo([|x => 0|], y => { return 0; }, z, z);
                }

                static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
                static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
            }
            """,

            """
            using System;

            class Program
            {
                static void Main()
                {
                    byte z = 0;
                    Goo({|Rename:NewMethod|}(), y => { return 0; }, z, z);
                }

                private static Func<byte, byte> NewMethod()
                {
                    return x => 0;
                }

                static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
                static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530709")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632182")]
    public Task DoNotOverparenthesize()
        => TestAsync(
            """
            using System;

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
            }
            """,

            """
            using System;

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
                    Outer(y => Inner(x => {|Rename:GetX|}(x).Ex(), y), - -1);
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
            }
            """,
            new(parseOptions: TestOptions.Regular));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632182")]
    public Task DoNotOverparenthesizeGenerics()
        => TestAsync(
            """
            using System;

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
            }
            """,

            """
            using System;

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
                    Outer(y => Inner(x => {|Rename:GetX|}(x).Ex<int>(), y), - -1);
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
            }
            """,
            new(parseOptions: TestOptions.Regular));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
    public Task PreserveCommentsBeforeDeclaration_1()
        => TestInRegularAndScriptAsync(
            """
            class Construct
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
            }
            """,

            """
            class Construct
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
    public Task PreserveCommentsBeforeDeclaration_2()
        => TestInRegularAndScriptAsync(
            """
            class Construct
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
            }
            """,

            """
            class Construct
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/984831")]
    public Task PreserveCommentsBeforeDeclaration_3()
        => TestInRegularAndScriptAsync(
            """
            class Construct
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
            }
            """,

            """
            class Construct
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
            }
            """);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTuple()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|(int, int) x = (1, 2);|]
                    System.Console.WriteLine(x.Item1);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTupleDeclarationWithNames()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|(int a, int b) x = (1, 2);|]
                    System.Console.WriteLine(x.a);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTupleDeclarationWithSomeNames()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|(int a, int) x = (1, 2);|]
                    System.Console.WriteLine(x.a);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18311")]
    public Task TestTupleWith1Arity()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                static void Main(string[] args)
                {
                    ValueTuple<int> y = ValueTuple.Create(1);
                    [|y.Item1.ToString();|]
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            using System;
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTupleLiteralWithNames()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|(int, int) x = (a: 1, b: 2);|]
                    System.Console.WriteLine(x.Item1);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTupleDeclarationAndLiteralWithNames()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|(int a, int b) x = (c: 1, d: 2);|]
                    System.Console.WriteLine(x.a);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTupleIntoVar()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|var x = (c: 1, d: 2);|]
                    System.Console.WriteLine(x.c);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task RefactorWithoutSystemValueTuple()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|var x = (c: 1, d: 2);|]
                    System.Console.WriteLine(x.c);
                }
            }
            """,
            """
            class Program
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
            }
            """);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/11196")]
    public Task TestTupleWithNestedNamedTuple()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    [|var x = new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: "hello", b: "world"));|]
                    System.Console.WriteLine(x.c);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    (int, int, int, int, int, int, int, string, string) x = {|Rename:NewMethod|}();
                    System.Console.WriteLine(x.c);
                }

                private static (int, int, int, int, int, int, int, string, string) NewMethod()
                {
                    return new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: "hello", b: "world"));
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TestDeconstruction()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var (x, y) = [|(1, 2)|];
                    System.Console.WriteLine(x);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact, CompilerTrait(CompilerFeature.Tuples)]
    public Task TestDeconstruction2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var (x, y) = (1, 2);
                    var z = [|3;|]
                    System.Console.WriteLine(z);
                }
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs,
            """
            class Program
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
            }
            """ + TestResources.NetFX.ValueTuple.tuplelib_cs);

    [Fact]
    [CompilerTrait(CompilerFeature.OutVar)]
    public Task TestOutVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void M(int i)
                {
                    int r;
                    [|r = M1(out int y, i);|]
                    System.Console.WriteLine(r + y);
                }
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    [CompilerTrait(CompilerFeature.Patterns)]
    public Task TestIsPattern()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void M(int i)
                {
                    int r;
                    [|r = M1(3 is int y, i);|]
                    System.Console.WriteLine(r + y);
                }
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    [CompilerTrait(CompilerFeature.Patterns)]
    public Task TestOutVarAndIsPattern()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    int r;
                    [|r = M1(out /*out*/ int /*int*/ y /*y*/) + M2(3 is int z);|]
                    System.Console.WriteLine(r + y + z);
                }
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    [CompilerTrait(CompilerFeature.Patterns)]
    public Task ConflictingOutVarLocals()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact]
    [CompilerTrait(CompilerFeature.Patterns)]
    public Task ConflictingPatternLocals()
        => TestInRegularAndScriptAsync(
            """
            class C
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
            }
            """,
            """
            class C
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15218")]
    public Task TestCancellationTokenGoesLast()
        => TestInRegularAndScriptAsync(
            """
            using System;
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
            }
            """,
            """
            using System;
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15219")]
    public Task TestUseVar1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo(int i)
                {
                    [|var v = (string)null;

                    switch (i)
                    {
                        case 0: v = "0"; break;
                        case 1: v = "1"; break;
                    }|]

                    Console.WriteLine(v);
                }
            }
            """,
            """
            using System;

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
                        case 0: v = "0"; break;
                        case 1: v = "1"; break;
                    }

                    return v;
                }
            }
            """, new TestParameters(options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOption2.TrueWithSuggestionEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15219")]
    public Task TestUseVar2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo(int i)
                {
                    [|var v = (string)null;

                    switch (i)
                    {
                        case 0: v = "0"; break;
                        case 1: v = "1"; break;
                    }|]

                    Console.WriteLine(v);
                }
            }
            """,
            """
            using System;

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
                        case 0: v = "0"; break;
                        case 1: v = "1"; break;
                    }

                    return v;
                }
            }
            """, new TestParameters(options: Option(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSuggestionEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15532")]
    public Task ExtractLocalFunctionCall()
        => TestExactActionSetOfferedAsync("""
            class C
            {
                public static void Main()
                {
                    void Local() { }
                    [|Local();|]
                }
            }
            """, [FeaturesResources.Extract_local_function]);

    [Fact]
    public Task ExtractLocalFunctionCall_2()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public static void Main()
                {
                    [|void Local() { }
                    Local();|]
                }
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15532")]
    public Task ExtractLocalFunctionCallWithCapture()
        => TestExactActionSetOfferedAsync("""
            class C
            {
                public static void Main(string[] args)
                {
                    bool Local() => args == null;
                    [|Local();|]
                }
            }
            """, [FeaturesResources.Extract_local_function]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15532")]
    public Task ExtractLocalFunctionDeclaration()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                public static void Main()
                {
                    [|bool Local() => args == null;|]
                    Local();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15532")]
    public Task ExtractLocalFunctionInterior()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
    public Task Bug3790()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
    public Task Bug3790_1()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538229")]
    public Task Bug3790_2()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
    public Task TestExpressionBodyProperty()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int field;

                public int Blah => [|this.field|];
            }
            """,
            """
            class Program
            {
                int field;

                public int Blah => {|Rename:GetField|}();

                private int GetField()
                {
                    return this.field;
                }
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
    public Task TestExpressionBodyIndexer()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int field;

                public int this[int i] => [|this.field|];
            }
            """,
            """
            class Program
            {
                int field;

                public int this[int i] => {|Rename:GetField|}();

                private int GetField()
                {
                    return this.field;
                }
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
    public Task TestExpressionBodyPropertyGetAccessor()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int field;

                public int Blah
                {
                    get => [|this.field|];
                    set => field = value;
                }
            }
            """,
            """
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
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
    public Task TestExpressionBodyPropertySetAccessor()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int field;

                public int Blah
                {
                    get => this.field;
                    set => field = [|value|];
                }
            }
            """,
            """
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
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
    public Task TestExpressionBodyIndexerGetAccessor()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int field;

                public int this[int i]
                {
                    get => [|this.field|];
                    set => field = value;
                }
            }
            """,
            """
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
            }
            """);

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=392560")]
    public Task TestExpressionBodyIndexerSetAccessor()
        => TestInRegularAndScriptAsync("""
            class Program
            {
                int field;

                public int this[int i]
                {
                    get => this.field;
                    set => field = [|value|];
                }
            }
            """,
            """
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
            }
            """);

    [Fact]
    public Task TestTupleWithInferredNames()
        => TestAsync("""
            class Program
            {
                void M()
                {
                    int a = 1;
                    var t = [|(a, b: 2)|];
                    System.Console.Write(t.a);
                }
            }
            """,
            """
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
            }
            """, new(TestOptions.Regular7_1));

    [Fact]
    public Task TestDeconstruction4()
        => TestAsync("""
            class Program
            {
                void M()
                {
                    [|var (x, y) = (1, 2);|]
                    System.Console.Write(x + y);
                }
            }
            """,
            """
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
            }
            """, new(TestOptions.Regular7_1));

    [Fact]
    public Task TestDeconstruction5()
        => TestAsync("""
            class Program
            {
                void M()
                {
                    [|(var x, var y) = (1, 2);|]
                    System.Console.Write(x + y);
                }
            }
            """,
            """
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
            }
            """, new(TestOptions.Regular7_1));

    [Fact]
    public Task TestIndexExpression()
        => TestInRegularAndScriptAsync(TestSources.Index + """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine([|^1|]);
                }
            }
            """,
TestSources.Index +
"""
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
}
""");

    [Fact]
    public Task TestRangeExpression_Empty()
        => TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine([|..|]);
                }
            }
            """,
TestSources.Index +
TestSources.Range + """
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
}
""");

    [Fact]
    public Task TestRangeExpression_Left()
        => TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine([|..1|]);
                }
            }
            """,
TestSources.Index +
TestSources.Range + """
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
}
""");

    [Fact]
    public Task TestRangeExpression_Right()
        => TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine([|1..|]);
                }
            }
            """,
TestSources.Index +
TestSources.Range + """
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
}
""");

    [Fact]
    public Task TestRangeExpression_Both()
        => TestInRegularAndScriptAsync(TestSources.Index + TestSources.Range + """
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine([|1..2|]);
                }
            }
            """,
TestSources.Index +
TestSources.Range + """
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
}
""");

    [Fact]
    public Task TestAnnotatedNullableReturn()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string? M()
                {
                    [|string? x = null;
                    x?.ToString();|]

                    return x;
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestAnnotatedNullableParameters1()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string? M()
                {
                    string? a = null;
                    string? b = null;
                    [|string? x = a?.Contains(b).ToString();|]

                    return x;
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestAnnotatedNullableParameters2()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

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
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestAnnotatedNullableParameters3()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string M()
                {
                    string? a = null;
                    string? b = null;
                    int c = 0;
                    return [|(a + b + c).ToString()|];
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestAnnotatedNullableParameters4()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string? M()
                {
                    string? a = null;
                    string? b = null;
                    return [|a?.Contains(b).ToString()|];
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters1()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string M()
                {
                    string? a = string.Empty;
                    string? b = string.Empty;
                    return [|(a + b + a).ToString()|];
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters2()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string? M()
                {
                    string? a = string.Empty;
                    string? b = string.Empty;
                    return [|(a + b + a).ToString()|];
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters3()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string M()
                {
                    string? a = null;
                    string? b = null;
                    return [|(a + b + a)?.ToString()|] ?? string.Empty;
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters_MultipleStates()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

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
                    b = "test";
                    c = a?.ToString();|]
                    return c ?? string.Empty;
                }
            }
            """,
            """
            #nullable enable

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
                    b = "test";
                    c = a?.ToString();
                    return c;
                }
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters_MultipleStatesNonNullReturn()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

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
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters_MultipleStatesNullReturn()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

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
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowStateNullableParameters_RefNotNull()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string M()
                {
                    string? a = string.Empty;
                    string? b = string.Empty;
                    [|var c = a + b;
                    a = string.Empty;
                    c += a;
                    b = "test";
                    c = a + b +c;|]
                    return c;
                }
            }
            """,
            """
            #nullable enable

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
                    b = "test";
                    c = a + b + c;
                    return c;
                }
            }
            """);

    // There's a case below where flow state correctly asseses that the variable
    // 'x' is non-null when returned. It's wasn't obvious when writing, but that's 
    // due to the fact the line above it being executed as 'x.ToString()' would throw
    // an exception and the return statement would never be hit. The only way the return
    // statement gets executed is if the `x.ToString()` call succeeds, thus suggesting 
    // that the value is indeed not null.
    [Fact]
    public Task TestFlowNullableReturn_NotNull1()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string? M()
                {
                    [|string? x = null;
                    x.ToString();|]

                    return x;
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowNullableReturn_NotNull2()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            class C
            {
                public string? M()
                {
                    [|string? x = null;
                    x?.ToString();
                    x = string.Empty;|]

                    return x;
                }
            }
            """,
            """
            #nullable enable

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
            }
            """);
    [Fact]
    public Task TestFlowNullable_Lambda()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

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
            }
            """,
            """
            #nullable enable

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
            }
            """);

    [Fact]
    public Task TestFlowNullable_LambdaWithReturn()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

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
            }
            """,
            """
            #nullable enable

            using System;

            class C
            {
                public string? M()
                {
                    string x = {|Rename:NewMethod|}();

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
            }
            """);

    [Fact]
    public Task TestExtractReadOnlyMethod()
        => TestInRegularAndScriptAsync(
            """
            struct S1
            {
                readonly int M1() => 42;
                void Main()
                {
                    [|int i = M1() + M1()|];
                }
            }
            """,
            """
            struct S1
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
            }
            """);

    [Fact]
    public Task TestExtractReadOnlyMethodInReadOnlyStruct()
        => TestInRegularAndScriptAsync(
            """
            readonly struct S1
            {
                int M1() => 42;
                void Main()
                {
                    [|int i = M1() + M1()|];
                }
            }
            """,
            """
            readonly struct S1
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
            }
            """);

    [Fact]
    public Task TestExtractNonReadOnlyMethodInReadOnlyMethod()
        => TestInRegularAndScriptAsync(
            """
            struct S1
            {
                int M1() => 42;
                readonly void Main()
                {
                    [|int i = M1() + M1()|];
                }
            }
            """,
            """
            struct S1
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
            }
            """);

    [Fact]
    public Task TestExtractNullableObjectWithExplicitCast()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

        using System;

        class C
        {
            void M()
            {
                object? o = null;
                var s = (string?)[|o|];
                Console.WriteLine(s);
            }
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact]
    public Task TestExtractNotNullableObjectWithExplicitCast()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

        using System;

        class C
        {
            void M()
            {
                object? o = new object();
                var s = (string)[|o|];
                Console.WriteLine(s);
            }
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact]
    public Task TestExtractNotNullableWithExplicitCast()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

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
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact]
    public Task TestExtractNullableWithExplicitCast()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

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
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact]
    public Task TestExtractNotNullableWithExplicitCastSelected()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

        using System;

        class C
        {
            void M()
            {
                object? o = new object();
                var s = [|(string)o|];
                Console.WriteLine(s);
            }
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact]
    public Task TestExtractNullableWithExplicitCastSelected()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

        using System;

        class C
        {
            void M()
            {
                object? o = null;
                var s = [|(string?)o|];
                Console.WriteLine(s);
            }
        }
        """,
        """
        #nullable enable

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
        }
        """);
    [Fact]
    public Task TestExtractNullableNonNullFlowWithExplicitCastSelected()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

        using System;

        class C
        {
            void M()
            {
                object? o = new object();
                var s = [|(string?)o|];
                Console.WriteLine(s);
            }
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact]
    public Task TestExtractNullableToNonNullableWithExplicitCastSelected()
    => TestInRegularAndScriptAsync(
        """
        #nullable enable

        using System;

        class C
        {
            void M()
            {
                object? o = null;
                var s = [|(string)o|];
                Console.WriteLine(s);
            }
        }
        """,
        """
        #nullable enable

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
        }
        """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38127")]
    public Task TestNestedNullability_Async()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            using System;
            using System.Threading.Tasks;

            class C
            {
                private Task<string> DoSomethingAsync() => Task.FromResult("");

                public async Task<string?> M()
                {
                    [|string? x = await DoSomethingAsync();|]
                    x = null;
                    return x;
                }
            }
            """,
            """
            #nullable enable

            using System;
            using System.Threading.Tasks;

            class C
            {
                private Task<string> DoSomethingAsync() => Task.FromResult("");

                public async Task<string?> M()
                {
                    string? x = await {|Rename:NewMethod|}();
                    x = null;
                    return x;
                }

                private async Task<string?> NewMethod()
                {
                    return await DoSomethingAsync();
                }
            }
            """);

    [Fact]
    public Task EnsureStaticLocalFunctionOptionHasNoEffect()
        => TestInRegularAndScriptAsync(
"""
class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        System.Console.WriteLine([|b != true|] ? b = true : b = false);
    }
}
""",
"""
class Program
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
}
""", new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.FalseWithSuggestionEnforcement)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39946")]
    public Task ExtractLocalFunctionCallAndDeclaration()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact]
    public Task TestMissingWhenOnlyLocalFunctionCallSelected()
        => TestExactActionSetOfferedAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    [|Local();|]
                    static void Local()
                    {
                    }
                }
            }
            """, [FeaturesResources.Extract_local_function]);

    [Fact]
    public Task TestOfferedWhenBothLocalFunctionCallAndDeclarationSelected()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractNonAsyncMethodWithAsyncLocalFunction()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M() 
                {
                    [|F();
                    async void F() => await Task.Delay(0);|]
                }
            }
            """,
            """
            class C
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitFalse()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(false)|];
                }
            }
            """,
            """
            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
                }

                private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
                {
                    return await Task.Delay(duration).ConfigureAwait(false);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitFalseNamedParameter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(continueOnCapturedContext: false)|];
                }
            }
            """,
            """
            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
                }

                private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
                {
                    return await Task.Delay(duration).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitFalseOnNonTask()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks

            class C
            {
                async Task MyDelay() 
                {
                    [|await new ValueTask<int>(0).ConfigureAwait(false)|];
                }
            }
            """,
            """
            using System.Threading.Tasks

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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitTrue()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(true)|];
                }
            }
            """,
            """
            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration);
                }

                private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
                {
                    return await Task.Delay(duration).ConfigureAwait(true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitNonLiteral()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(M())|];
                }
            }
            """,
            """
            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration);
                }

                private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
                {
                    return await Task.Delay(duration).ConfigureAwait(M());
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithNoConfigureAwait()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration)|];
                }
            }
            """,
            """
            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration);
                }

                private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
                {
                    return await Task.Delay(duration);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitFalseInLambda()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Run(async () => await Task.Delay(duration).ConfigureAwait(false))|];
                }
            }
            """,
            """
            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration);
                }

                private static async System.Threading.Tasks.Task<object> NewMethod(TimeSpan duration)
                {
                    return await Task.Run(async () => await Task.Delay(duration).ConfigureAwait(false));
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitFalseInLocalMethod(bool includeUsing)
        => TestInRegularAndScriptAsync(
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Run(F());
                    async Task F() => await Task.Delay(duration).ConfigureAwait(false);|]
                }
            }
            """,
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration);
                }

                private static async {{(includeUsing ? "Task" : SystemThreadingTasksTask)}} NewMethod(TimeSpan duration)
                {
                    await Task.Run(F());
                    async Task F() => await Task.Delay(duration).ConfigureAwait(false);
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitMixture1(bool includeUsing)
        => TestInRegularAndScriptAsync(
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(false);
                    await Task.Delay(duration).ConfigureAwait(true);|]
                }
            }
            """,
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
                }

                private static async {{(includeUsing ? "Task" : SystemThreadingTasksTask)}} NewMethod(TimeSpan duration)
                {
                    await Task.Delay(duration).ConfigureAwait(false);
                    await Task.Delay(duration).ConfigureAwait(true);
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitMixture2(bool includeUsing)
        => TestInRegularAndScriptAsync(
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(true);
                    await Task.Delay(duration).ConfigureAwait(false);|]
                }
            }
            """,
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
                }

                private static async {{(includeUsing ? "Task" : SystemThreadingTasksTask)}} NewMethod(TimeSpan duration)
                {
                    await Task.Delay(duration).ConfigureAwait(true);
                    await Task.Delay(duration).ConfigureAwait(false);
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitMixture3(bool includeUsing)
        => TestInRegularAndScriptAsync(
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    [|await Task.Delay(duration).ConfigureAwait(M());
                    await Task.Delay(duration).ConfigureAwait(false);|]
                }
            }
            """,
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await {|Rename:NewMethod|}(duration).ConfigureAwait(false);
                }

                private static async {{(includeUsing ? "Task" : SystemThreadingTasksTask)}} NewMethod(TimeSpan duration)
                {
                    await Task.Delay(duration).ConfigureAwait(M());
                    await Task.Delay(duration).ConfigureAwait(false);
                }
            }
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/38529")]
    public Task TestExtractAsyncMethodWithConfigureAwaitFalseOutsideSelection(bool includeUsing)
        => TestInRegularAndScriptAsync(
            $$"""
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration) 
                {
                    await Task.Delay(duration).ConfigureAwait(false);
                    [|await Task.Delay(duration).ConfigureAwait(true);|]
                }
            }
            """,
            $$"""            
            {{(includeUsing ? SystemThreadingTasksUsing : "")}}

            class C
            {
                async Task MyDelay(TimeSpan duration)
                {
                    await Task.Delay(duration).ConfigureAwait(false);
                    await {|Rename:NewMethod|}(duration);
                }

                private static async {{(includeUsing ? "Task" : SystemThreadingTasksTask)}} NewMethod(TimeSpan duration)
                {
                    await Task.Delay(duration).ConfigureAwait(true);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40188")]
    public Task TestEditorconfigSetting_ExpressionBodiedLocalFunction_True()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            class Program1
            {
                static void Main()
                {
                    [|bool b = true;|]
                    System.Console.WriteLine(b != true ? b = true : b = false);
                }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
            csharp_style_expression_bodied_methods = true:silent
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                     <Document FilePath="z:\\file.cs">
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
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
            csharp_style_expression_bodied_methods = true:silent
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40188")]
    public Task TestEditorconfigSetting_ExpressionBodiedLocalFunction_False()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            class Program1
            {
                static void Main()
                {
                    [|bool b = true;|]
                    System.Console.WriteLine(b != true ? b = true : b = false);
                }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
            csharp_style_expression_bodied_methods = false:silent
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                     <Document FilePath="z:\\file.cs">
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
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
            csharp_style_expression_bodied_methods = false:silent
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209")]
    public async Task TestNaming_CamelCase_VerifyLocalFunctionSettingsDoNotApply()
    {
        var input = """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            class Program1
            {
                static void Main()
                {
                    [|bool b = true;|]
                    System.Console.WriteLine(b != true ? b = true : b = false);
                }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            """ + EditorConfigNaming_LocalFunctions_CamelCase + """
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        var expected = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                     <Document FilePath="z:\\file.cs">
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
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            """ + EditorConfigNaming_LocalFunctions_CamelCase + """
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209")]
    public async Task TestNaming_CamelCase_VerifyLocalFunctionSettingsDoNotApply_GetName()
    {
        var input = """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            class MethodExtraction
            {
                void TestMethod()
                {
                    int a = [|1 + 1|];
                }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            """ + EditorConfigNaming_LocalFunctions_CamelCase + """
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        var expected = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                     <Document FilePath="z:\\file.cs">
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
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            """ + EditorConfigNaming_LocalFunctions_CamelCase + """
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """;

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40654")]
    public Task TestOnInvalidUsingStatement_MultipleStatements()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M()
                {
                    [|var v = 0;
                    using System;|]
                }
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40654")]
    public Task TestMissingOnInvalidUsingStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    [|using System;|]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19461")]
    public Task TestLocalFunction()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43834")]
    public Task TestRecursivePatternRewrite()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess1()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess2()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess3()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess4()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess5()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess6()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41895")]
    public Task TestConditionalAccess7()
        => TestInRegularAndScriptAsync("""
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
            }
            """, """
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
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/48453")]
    [InlineData("record")]
    [InlineData("record class")]
    public Task TestInRecord(string record)
        => TestInRegularAndScriptAsync($$"""
            {{record}} Program
            {
                int field;

                public int this[int i] => [|this.field|];
            }
            """,
            $$"""
            {{record}} Program
            {
                int field;

                public int this[int i] => {|Rename:GetField|}();

                private int GetField()
                {
                    return this.field;
                }
            }
            """);

    [Fact]
    public Task TestInRecordStruct()
        => TestInRegularAndScriptAsync("""
            record struct Program
            {
                int field;

                public int this[int i] => [|this.field|];
            }
            """,
            """
            record struct Program
            {
                int field;

                public int this[int i] => {|Rename:GetField|}();

                private readonly int GetField()
                {
                    return this.field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53031")]
    public Task TestMethodInNamespace()
        => TestMissingInRegularAndScriptAsync("""
            namespace TestNamespace
            {
                private bool TestMethod() => [|false|];
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53031")]
    public Task TestMethodInInterface()
        => TestInRegularAndScriptAsync("""
            interface TestInterface
            {
                bool TestMethod() => [|false|];
            }
            """,
            """
            interface TestInterface
            {
                bool TestMethod() => {|Rename:NewMethod|}();

                bool NewMethod()
                {
                    return false;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53031")]
    public Task TestStaticMethodInInterface()
        => TestInRegularAndScriptAsync("""
            interface TestInterface
            {
                static bool TestMethod() => [|false|];
            }
            """,
            """
            interface TestInterface
            {
                static bool TestMethod() => {|Rename:NewMethod|}();

                static bool NewMethod()
                {
                    return false;
                }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/56969")]
    public async Task TopLevelStatement_FullStatement()
    {
        var code = """
            [|System.Console.WriteLine("string");|]
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedCode = """
            NewMethod();
            
            static void NewMethod()
            {
                System.Console.WriteLine("string");
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/56969")]
    public async Task TopLevelStatement_MultipleStatements()
    {
        var code = """
            System.Console.WriteLine("string");

            [|int x = int.Parse("0");
            System.Console.WriteLine(x);|]

            System.Console.WriteLine(x);
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedCode = """
            System.Console.WriteLine("string");

            int x = NewMethod();

            System.Console.WriteLine(x);
            
            static int NewMethod()
            {
                int x = int.Parse("0");
                System.Console.WriteLine(x);
                return x;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/56969")]
    public async Task TopLevelStatement_MultipleStatementsWithUsingAndClass()
    {
        var code = """
            using System;

            Console.WriteLine("string");

            [|int x = int.Parse("0");
            Console.WriteLine(x);|]

            Console.WriteLine(x);

            class Ignored { }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedCode = """
                using System;
            
                Console.WriteLine("string");
            
                int x = NewMethod();
            
                Console.WriteLine(x);

                static int NewMethod()
                {
                    int x = int.Parse("0");
                    Console.WriteLine(x);
                    return x;
                }
                
                class Ignored { }
                """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/56969")]
    public async Task TopLevelStatement_MultipleStatementsWithInvalidOrdering()
    {
        var code = """
            using System;

            Console.WriteLine("string");

            class Ignored { }

            [|{|CS8803:int x = int.Parse("0");|}
            Console.WriteLine(x);|]

            Console.WriteLine(x);

            class Ignored2 { }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedCode = """
            using System;

            Console.WriteLine("string");

            class Ignored { }

            {|CS8803:int x = NewMethod();|}

            Console.WriteLine(x);

            static int NewMethod()
            {
                int x = int.Parse("0");
                Console.WriteLine(x);
                return x;
            }
            
            class Ignored2 { }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();
    }

    [Fact, WorkItem(56969, "https://github.com/dotnet/roslyn/issues/58013")]
    public Task TopLevelMethod_StaticMethod()
        => TestInRegularAndScriptAsync("""
            static void X(string s)
            {
                [|s = s.Trim();|]
            }
            """,
            """
            static void X(string s)
            {
                s = {|Rename:NewMethod|}(s);
            }

            static string NewMethod(string s)
            {
                s = s.Trim();
                return s;
            }
            """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem(56969, "https://github.com/dotnet/roslyn/issues/58013")]
    public Task StaticMethod_ExtractStatementContainingParameter()
        => TestInRegularAndScriptAsync("""
            public class Class
            {
                static void X(string s)
                {
                    [|s = s.Trim();|]
                }
            }
            """,
            """
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
            }
            """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57428")]
    public Task AttributeArgumentWithLambdaBody()
        => TestInRegularAndScriptAsync(
            """
            using System.Runtime.InteropServices;
            class Program
            {
                static void F([DefaultParameterValue(() => { return [|null|]; })] object obj)
                {
                }
            }
            """,
            """
            using System.Runtime.InteropServices;
            class Program
            {
                static void F([DefaultParameterValue(() => { return {|Rename:NewMethod|}(); })] object obj)
                {
                }

                private static object NewMethod()
                {
                    return null;
                }
            }
            """);

    [Fact]
    public Task ExtractMethod_InsideBaseInitializer()
        => TestInRegularAndScriptAsync(
            """
            class Base
            {
                private readonly int _x;
                public Base(int x)
                {
                    _x = x;
                }
            }

            class C : Base
            {
                public C(int y)
                    : base([|y + 1|])
                {
                }
            }
            """,
            """
            class Base
            {
                private readonly int _x;
                public Base(int x)
                {
                    _x = x;
                }
            }

            class C : Base
            {
                public C(int y)
                    : base({|Rename:NewMethod|}(y))
                {
                }

                private static int NewMethod(int y)
                {
                    return y + 1;
                }
            }
            """);

    [Fact]
    public Task ExtractMethod_InsideThisInitializer()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public C(int y)
                    : this(y, [|y + 1|])
                {
                }

                public C(int x, int y)
                {
                }
            }
            """,
            """
            class C
            {
                public C(int y)
                    : this(y, {|Rename:NewMethod|}(y))
                {
                }

                private static int NewMethod(int y)
                {
                    return y + 1;
                }
        
                public C(int x, int y)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8439")]
    public Task TestRefReturn1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public ref int M()
                {
                    return [|ref M()|];
                }
            }
            """,
            """
            class Program
            {
                public ref int M()
                {
                    return ref {|Rename:NewMethod|}();
                }

                private ref int NewMethod()
                {
                    return ref M();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33618")]
    public Task TestPreferThisPreference_NotForInstanceMethodWhenOff()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                int i;

                public void M()
                {
                    [|Console.WriteLine(i);|]
                }
            }
            """,
            """
            using System;

            class Program
            {
                int i;

                public void M()
                {
                    {|Rename:NewMethod|}();
                }

                private void NewMethod()
                {
                    Console.WriteLine(i);
                }
            }
            """,
            new(options: new(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.QualifyMethodAccess, CodeStyleOption2.FalseWithSilentEnforcement },
            }));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/33618")]
    public async Task TestPreferThisPreference_ForInstanceMethodWhenOn(ReportDiagnostic diagnostic)
    {
        if (diagnostic is ReportDiagnostic.Default)
            return;

        await TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                int i;

                public void M()
                {
                    [|Console.WriteLine(i);|]
                }
            }
            """,
            """
            using System;

            class Program
            {
                int i;

                public void M()
                {
                    this.{|Rename:NewMethod|}();
                }

                private void NewMethod()
                {
                    Console.WriteLine(i);
                }
            }
            """,
            new(options: new(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.QualifyMethodAccess, new CodeStyleOption2<bool>(true, new(diagnostic, true)) },
            }));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33618")]
    public Task TestPreferThisPreference_NotForStaticMethodWhenOn()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public void M()
                {
                    [|Console.WriteLine();|]
                }
            }
            """,
            """
            using System;

            class Program
            {
                public void M()
                {
                    {|Rename:NewMethod|}();
                }

                private static void NewMethod()
                {
                    Console.WriteLine();
                }
            }
            """,
            new(options: new(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.QualifyMethodAccess, CodeStyleOption2.TrueWithSilentEnforcement },
            }));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33618")]
    public Task TestPreferThisPreference_NotForLocalFunctionWhenOn()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public void M()
                {
                    [|Console.WriteLine();|]
                }
            }
            """,
            """
            using System;

            class Program
            {
                public void M()
                {
                    {|Rename:NewMethod|}();

                    static void NewMethod()
                    {
                        Console.WriteLine();
                    }
                }
            }
            """,
            index: 1,
            new(options: new(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.QualifyMethodAccess, CodeStyleOption2.TrueWithSilentEnforcement },
            }));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleOutTuple1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Customer
            {
                public int Id;
            }

            class Repository
            {
                private static readonly Repository _repository = new();
                public Task<Customer> GetValue(int i) => null!;

                public static async Task Goo(string value)
                {
                    [|var anotherValue = "GooBar";
                    var customer = await _repository.GetValue(value.Length);|]

                    Console.Write(customer.Id);
                    Console.Write(anotherValue);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Customer
            {
                public int Id;
            }
            
            class Repository
            {
                private static readonly Repository _repository = new();
                public Task<Customer> GetValue(int i) => null!;
            
                public static async Task Goo(string value)
                {
                    var (anotherValue, customer) = await {|Rename:NewMethod|}(value);
            
                    Console.Write(customer.Id);
                    Console.Write(anotherValue);
                }

                private static async Task<(string anotherValue, Customer customer)> NewMethod(string value)
                {
                    var anotherValue = "GooBar";
                    var customer = await _repository.GetValue(value.Length);
                    return (anotherValue, customer);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleOutTuple_ExplicitEverywhere()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Customer
            {
                public int Id;
            }

            class Repository
            {
                private static readonly Repository _repository = new();
                public Task<Customer> GetValue(int i) => null!;

                public static async Task Goo(string value)
                {
                    [|var anotherValue = "GooBar";
                    var customer = await _repository.GetValue(value.Length);|]

                    Console.Write(customer.Id);
                    Console.Write(anotherValue);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Customer
            {
                public int Id;
            }
            
            class Repository
            {
                private static readonly Repository _repository = new();
                public Task<Customer> GetValue(int i) => null!;
            
                public static async Task Goo(string value)
                {
                    (string anotherValue, Customer customer) = await {|Rename:NewMethod|}(value);
            
                    Console.Write(customer.Id);
                    Console.Write(anotherValue);
                }

                private static async Task<(string anotherValue, Customer customer)> NewMethod(string value)
                {
                    var anotherValue = "GooBar";
                    var customer = await _repository.GetValue(value.Length);
                    return (anotherValue, customer);
                }
            }
            """,
            new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleOutTuple_ImplicitForBuiltInTypes()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Customer
            {
                public int Id;
            }

            class Repository
            {
                private static readonly Repository _repository = new();
                public Task<Customer> GetValue(int i) => null!;

                public static async Task Goo(string value)
                {
                    [|var anotherValue = "GooBar";
                    var customer = await _repository.GetValue(value.Length);|]

                    Console.Write(customer.Id);
                    Console.Write(anotherValue);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Customer
            {
                public int Id;
            }
            
            class Repository
            {
                private static readonly Repository _repository = new();
                public Task<Customer> GetValue(int i) => null!;
            
                public static async Task Goo(string value)
                {
                    (var anotherValue, Customer customer) = await {|Rename:NewMethod|}(value);
            
                    Console.Write(customer.Id);
                    Console.Write(anotherValue);
                }

                private static async Task<(string anotherValue, Customer customer)> NewMethod(string value)
                {
                    var anotherValue = "GooBar";
                    var customer = await _repository.GetValue(value.Length);
                    return (anotherValue, customer);
                }
            }
            """,
            new(options: ImplicitForBuiltInTypes()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleRefCapture()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Repository
            {
                public async Task Goo()
                {
                    int a = 0;
                    int b = 0;

                    [|a++;
                    b++;
                    await Goo();|]

                    Console.Write(a);
                    Console.Write(b);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Repository
            {
                public async Task Goo()
                {
                    int a = 0;
                    int b = 0;
                    (a, b) = await {|Rename:NewMethod|}(a, b);
            
                    Console.Write(a);
                    Console.Write(b);
                }

                private async Task<(int a, int b)> NewMethod(int a, int b)
                {
                    a++;
                    b++;
                    await Goo();
                    return (a, b);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleRefCapture_PartialCapture()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Repository
            {
                public async Task Goo()
                {
                    int a = 0;
                    [|int b = 0;

                    a++;
                    b++;
                    await Goo();|]

                    Console.Write(a);
                    Console.Write(b);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Repository
            {
                public async Task Goo()
                {
                    int a = 0;
                    (a, var b) = await {|Rename:NewMethod|}(a);
            
                    Console.Write(a);
                    Console.Write(b);
                }

                private async Task<(int a, int b)> NewMethod(int a)
                {
                    int b = 0;

                    a++;
                    b++;
                    await Goo();
                    return (a, b);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleRefCapture_PartialCapture_InitializedInside()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Repository
            {
                public async Task Goo()
                {
                    int a = 0;
                    [|int b;

                    b = 0;
                    a++;
                    b++;
                    await Goo();|]

                    Console.Write(a);
                    Console.Write(b);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Repository
            {
                public async Task Goo()
                {
                    int a = 0;
                    (a, var b) = await {|Rename:NewMethod|}(a);
            
                    Console.Write(a);
                    Console.Write(b);
                }

                private async Task<(int a, int b)> NewMethod(int a)
                {
                    int b = 0;
                    a++;
                    b++;
                    await Goo();
                    return (a, b);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64597")]
    public Task TestMultipleRefCapture_PartialCapture_InitializedInside2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Repository
            {
                public async Task Goo()
                {
                    int a;
                    [|int b;

                    a = 0;
                    b = 0;
                    a++;
                    b++;
                    await Goo();|]

                    Console.Write(a);
                    Console.Write(b);
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;
            
            class Repository
            {
                public async Task Goo()
                {
                    int a;
                    (a, var b) = await {|Rename:NewMethod|}();
            
                    Console.Write(a);
                    Console.Write(b);
                }

                private async Task<(int a, int b)> NewMethod()
                {
                    int a;
                    int b;

                    a = 0;
                    b = 0;
                    a++;
                    b++;
                    await Goo();
                    return (a, b);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61555")]
    public async Task TestKnownNotNullParameter()
        => await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                public class C
                {
                    public void M(C? c)
                    {
                        if (c == null)
                        {
                            return;
                        }

                        [|c.ToString();|]
                    }
                }
                """,
            FixedCode = """
                #nullable enable
            
                public class C
                {
                    public void M(C? c)
                    {
                        if (c == null)
                        {
                            return;
                        }
            
                        NewMethod(c);
                    }

                    private static void NewMethod(C c)
                    {
                        c.ToString();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61555")]
    public async Task TestKnownNotNullParameter_AssignedNull()
        => await new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                public class C
                {
                    public void M(C? c)
                    {
                        if (c == null)
                        {
                            return;
                        }

                        [|c.ToString();
                        c = null;
                        c?.ToString();|]
                    }
                }
                """,
            FixedCode = """
                #nullable enable
            
                public class C
                {
                    public void M(C? c)
                    {
                        if (c == null)
                        {
                            return;
                        }
            
                        c = NewMethod(c);
                    }

                    private static C? NewMethod(C? c)
                    {
                        {|CS8602:c|}.ToString();
                        c = null;
                        c?.ToString();
                        return c;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/67017")]
    public async Task TestPrimaryConstructorBaseList(bool withBody)
    {
        var source = $$"""
            class C1(int p1);
            class C2(S1 a10000, int a20000) : C1([|a10000.F1|]){{(withBody ? "{}" : ";")}}

            struct S1
            {
                public int F1;
            }
            """;

        // Only want 'extract method' not 'extract local function' here.
        await TestActionCountAsync(source, 1);
        await TestInRegularAndScriptAsync(
            source,
            """
            class C1(int p1);
            class C2(S1 a10000, int a20000) : C1({|Rename:GetF1|}(a10000))
            {
                private static int GetF1(S1 a10000)
                {
                    return a10000.F1;
                }
            }
            
            struct S1
            {
                public int F1;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38087")]
    public Task TestPartialSelectionOfArithmeticExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private void Repro()
                {
                    int i = 1, j = 2;
                    int k = [|i + j|] + 1;
                }
            }
            """,
            """
            class C
            {
                private void Repro()
                {
                    int i = 1, j = 2;
                    int k = {|Rename:NewMethod|}(i, j) + 1;
                }

                private static int NewMethod(int i, int j)
                {
                    return i + j;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndBreak()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        {|Rename:NewMethod|}(v);
                        break;
                    }
            
                    return 0;
                }

                private static void NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }

                    return;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinue()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndReturn()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            return value;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    return (flowControl: true, value: 1);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndFallThrough()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndBreak()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return true;
                    }

                    return false;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndContinue()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        {|Rename:NewMethod|}(v);
                        continue;
                    }
            
                    return 0;
                }

                private static void NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }

                    return;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndReturn()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    return (flowControl: true, value: 1);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndFallThrough()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            continue;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndBreak()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            return value;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: true, value: 1);
                    }

                    return (flowControl: false, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndContinue()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: true, value: 1);
                    }

                    return (flowControl: false, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndReturn()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 2;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        return {|Rename:NewMethod|}(v);
                    }
            
                    return 0;
                }

                private static int NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return 2;
                    }

                    return 1;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndFallThrough()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: 1);
                    }

                    return (flowControl: true, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndReturn()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        if (v == 1)
                        {
                            continue;
                        }

                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool? flowControl, int value) = {|Rename:NewMethod|}(v);
                        if (flowControl == false)
                        {
                            break;
                        }
                        else if (flowControl == true)
                        {
                            continue;
                        }
                        else
                        {
                            return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool? flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    if (v == 1)
                    {
                        return (flowControl: true, value: default);
                    }

                    return (flowControl: null, value: 1);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndFallThrough()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        if (v == 1)
                        {
                            continue;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool? flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl == false)
                        {
                            break;
                        }
                        else if (flowControl == true)
                        {
                            continue;
                        }
                    }
            
                    return 0;
                }

                private static bool? NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    if (v == 1)
                    {
                        return true;
                    }

                    return null;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndReturnAndFallThrough()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        if (v == 1)
                        {
                            return 1;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool? flowControl, int value) = {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool? flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    if (v == 1)
                    {
                        return (flowControl: true, value: 1);
                    }

                    return (flowControl: null, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndReturnAndFallThrough()
        => TestInRegularAndScriptAsync(
            """
        class C
        {
            private string Repro(int[] x)
            {
                foreach (var v in x)
                {
                    [|if (v == 0)
                    {
                        break;
                    }
                        
                    if (v == 1)
                    {
                        continue;
                    }

                    if (v == 2)
                    {
                        return "";
                    }|]
                }

                return "x";
            }
        }
        """,
            """
        class C
        {
            private string Repro(int[] x)
            {
                foreach (var v in x)
                {
                    (int flowControl, string value) = {|Rename:NewMethod|}(v);
                    if (flowControl == 0)
                    {
                        break;
                    }
                    else if (flowControl == 1)
                    {
                        continue;
                    }
                    else if (flowControl == 2)
                    {
                        return value;
                    }
                }

                return "x";
            }

            private static (int flowControl, string value) NewMethod(int v)
            {
                if (v == 0)
                {
                    return (flowControl: 0, value: null);
                }

                if (v == 1)
                {
                    return (flowControl: 1, value: null);
                }

                if (v == 2)
                {
                    return (flowControl: 2, value: "");
                }

                return (flowControl: 3, value: null);
            }
        }
        """);

    [Fact]
    public Task TestFlowControl_BreakAndBreak_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        {|Rename:NewMethod|}(v);
                        break;
                    }
            
                    return 0;
                }

                private static void NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }

                    return;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndContinue_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndReturn_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            return value;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    return (flowControl: true, value: 1);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndFallThrough_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var flowControl = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ContinueAndBreak_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return true;
                    }

                    return false;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ContinueAndContinue_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        {|Rename:NewMethod|}(v);
                        continue;
                    }
            
                    return 0;
                }

                private static void NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }

                    return;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ContinueAndReturn_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    return (flowControl: true, value: 1);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ContinueAndFallThrough_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var flowControl = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            continue;
                        }
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ReturnAndBreak_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            return value;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: true, value: 1);
                    }

                    return (flowControl: false, value: default);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ReturnAndContinue_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: true, value: 1);
                    }

                    return (flowControl: false, value: default);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ReturnAndReturn_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 2;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        return {|Rename:NewMethod|}(v);
                    }
            
                    return 0;
                }

                private static int NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return 2;
                    }

                    return 1;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ReturnAndFallThrough_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: 1);
                    }

                    return (flowControl: true, value: default);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndReturn_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        if (v == 1)
                        {
                            continue;
                        }

                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        if (flowControl == false)
                        {
                            break;
                        }
                        else if (flowControl == true)
                        {
                            continue;
                        }
                        else
                        {
                            return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool? flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    if (v == 1)
                    {
                        return (flowControl: true, value: default);
                    }

                    return (flowControl: null, value: 1);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndFallThrough_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        if (v == 1)
                        {
                            continue;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl == false)
                        {
                            break;
                        }
                        else if (flowControl == true)
                        {
                            continue;
                        }
                    }
            
                    return 0;
                }

                private static bool? NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    if (v == 1)
                    {
                        return true;
                    }

                    return null;
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_ContinueAndReturnAndFallThrough_AllowVar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        
                        if (v == 1)
                        {
                            return 1;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static (bool? flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    if (v == 1)
                    {
                        return (flowControl: true, value: 1);
                    }

                    return (flowControl: null, value: default);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndReturnAndFallThrough_AllowVar()
        => TestInRegularAndScriptAsync("""
            class C
            {
                private string Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        if (v == 1)
                        {
                            continue;
                        }

                        if (v == 2)
                        {
                            return "";
                        }|]
                    }

                    return "x";
                }
            }
            """,
            """
            class C
            {
                private string Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        var (flowControl, value) = {|Rename:NewMethod|}(v);
                        if (flowControl == 0)
                        {
                            break;
                        }
                        else if (flowControl == 1)
                        {
                            continue;
                        }
                        else if (flowControl == 2)
                        {
                            return value;
                        }
                    }

                    return "x";
                }

                private static (int flowControl, string value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: 0, value: null);
                    }

                    if (v == 1)
                    {
                        return (flowControl: 1, value: null);
                    }

                    if (v == 2)
                    {
                        return (flowControl: 2, value: "");
                    }

                    return (flowControl: 3, value: null);
                }
            }
            """,
            new(options: ImplicitTypeEverywhere()));

    [Fact]
    public Task TestFlowControl_BreakAndBreak_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        await {|Rename:NewMethod|}(v);
                        break;
                    }
            
                    return 0;
                }

                private static async Task NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }
                    await Task.Delay(0);
                    return;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinue_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = await {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static async Task<bool> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }
                    await Task.Delay(0);
                    return true;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndReturn_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = await {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            return value;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }
                    await Task.Delay(0);
                    return (flowControl: true, value: 1);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndFallThrough_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = await {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static async Task<bool> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }
                    await Task.Delay(0);
                    return true;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndBreak_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        await Task.Delay(0);
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = await {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static async Task<bool> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return true;
                    }
                    await Task.Delay(0);
                    return false;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndContinue_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        await Task.Delay(0);
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        await {|Rename:NewMethod|}(v);
                        continue;
                    }
            
                    return 0;
                }

                private static async Task NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }
                    await Task.Delay(0);
                    return;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndReturn_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        await Task.Delay(0);
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = await {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }
                    await Task.Delay(0);
                    return (flowControl: true, value: 1);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndFallThrough_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        await Task.Delay(0);|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = await {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            continue;
                        }
                    }
            
                    return 0;
                }

                private static async Task<bool> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }
                    await Task.Delay(0);
                    return true;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndBreak_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        await Task.Delay(0);
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = await {|Rename:NewMethod|}(v);
                        if (flowControl)
                        {
                            return value;
                        }
                        else
                        {
                            break;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: true, value: 1);
                    }
                    await Task.Delay(0);
                    return (flowControl: false, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndContinue_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        await Task.Delay(0);
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = await {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: true, value: 1);
                    }
                    await Task.Delay(0);
                    return (flowControl: false, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndReturn_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 2;
                        }
                        await Task.Delay(0);
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        return await {|Rename:NewMethod|}(v);
                    }
            
                    return 0;
                }

                private static async Task<int> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return 2;
                    }
                    await Task.Delay(0);
                    return 1;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ReturnAndFallThrough_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            return 1;
                        }
                        await Task.Delay(0);|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = await {|Rename:NewMethod|}(v);
                        if (!flowControl)
                        {
                            return value;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: 1);
                    }
                    await Task.Delay(0);
                    return (flowControl: true, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndReturn_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);
                        if (v == 1)
                        {
                            continue;
                        }
                        await Task.Delay(1);
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool? flowControl, int value) = await {|Rename:NewMethod|}(v);
                        if (flowControl == false)
                        {
                            break;
                        }
                        else if (flowControl == true)
                        {
                            continue;
                        }
                        else
                        {
                            return value;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool? flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }
                    await Task.Delay(0);
                    if (v == 1)
                    {
                        return (flowControl: true, value: default);
                    }
                    await Task.Delay(1);
                    return (flowControl: null, value: 1);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndFallThrough_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);
                        if (v == 1)
                        {
                            continue;
                        }
                        await Task.Delay(1);|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool? flowControl = await {|Rename:NewMethod|}(v);
                        if (flowControl == false)
                        {
                            break;
                        }
                        else if (flowControl == true)
                        {
                            continue;
                        }
                    }
            
                    return 0;
                }

                private static async Task<bool?> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }
                    await Task.Delay(0);
                    if (v == 1)
                    {
                        return true;
                    }
                    await Task.Delay(1);
                    return null;
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_ContinueAndReturnAndFallThrough_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            continue;
                        }
                        await Task.Delay(0);
                        if (v == 1)
                        {
                            return 1;
                        }
                        await Task.Delay(1);|]
                    }

                    return 0;
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<int> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool? flowControl, int value) = await {|Rename:NewMethod|}(v);
                        switch (flowControl)
                        {
                            case false: continue;
                            case true: return value;
                        }
                    }
            
                    return 0;
                }

                private static async Task<(bool? flowControl, int value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }
                    await Task.Delay(0);
                    if (v == 1)
                    {
                        return (flowControl: true, value: 1);
                    }
                    await Task.Delay(1);
                    return (flowControl: null, value: default);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndContinueAndReturnAndFallThrough_Async()
        => TestInRegularAndScriptAsync(
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<string> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        await Task.Delay(0);
                        if (v == 1)
                        {
                            continue;
                        }
                        await Task.Delay(1);
                        if (v == 2)
                        {
                            return "";
                        }
                        await Task.Delay(2);|]
                    }

                    return "x";
                }
            }
            """,
            """
            using System.Threading.Tasks;
            class C
            {
                private async Task<string> Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (int flowControl, string value) = await {|Rename:NewMethod|}(v);
                        if (flowControl == 0)
                        {
                            break;
                        }
                        else if (flowControl == 1)
                        {
                            continue;
                        }
                        else if (flowControl == 2)
                        {
                            return value;
                        }
                    }

                    return "x";
                }

                private static async Task<(int flowControl, string value)> NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: 0, value: null);
                    }
                    await Task.Delay(0);
                    if (v == 1)
                    {
                        return (flowControl: 1, value: null);
                    }
                    await Task.Delay(1);
                    if (v == 2)
                    {
                        return (flowControl: 2, value: "");
                    }
                    await Task.Delay(2);
                    return (flowControl: 3, value: null);
                }
            }
            """);

    [Fact]
    public Task TestFlowControl_BreakAndBreak_NoBraces()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        break;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        {|Rename:NewMethod|}(v);
                        break;
                    }
            
                    return 0;
                }

                private static void NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return;
                    }

                    return;
                }
            }
            """,
            new(options: NoBraces()));

    [Fact]
    public Task TestFlowControl_BreakAndContinue_NoBraces()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        continue;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = {|Rename:NewMethod|}(v);
                        if (flowControl)
                            continue;
                        else
                            break;
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """,
            new(options: NoBraces()));

    [Fact]
    public Task TestFlowControl_BreakAndReturn_NoBraces()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }
                        
                        return 1;|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        (bool flowControl, int value) = {|Rename:NewMethod|}(v);
                        if (flowControl)
                            return value;
                        else
                            break;
                    }
            
                    return 0;
                }

                private static (bool flowControl, int value) NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return (flowControl: false, value: default);
                    }

                    return (flowControl: true, value: 1);
                }
            }
            """,
            new(options: NoBraces()));

    [Fact]
    public Task TestFlowControl_BreakAndFallThrough_NoBraces()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        [|if (v == 0)
                        {
                            break;
                        }|]
                    }

                    return 0;
                }
            }
            """,
            """
            class C
            {
                private int Repro(int[] x)
                {
                    foreach (var v in x)
                    {
                        bool flowControl = {|Rename:NewMethod|}(v);
                        if (!flowControl)
                            break;
                    }
            
                    return 0;
                }

                private static bool NewMethod(int v)
                {
                    if (v == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            """,
            new(options: NoBraces()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22597")]
    public Task TestFullyExtractedTypeParameter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                private void Test()
                {
                    [|void Goo<T>(T bar) => Console.WriteLine(bar);
                    Goo(3);|]
                }
            }
            """,
            """
            class C
            {
                private void Test()
                {
                    {|Rename:NewMethod|}();
                }

                private static void NewMethod()
                {
                    void Goo<T>(T bar) => Console.WriteLine(bar);
                    Goo(3);
                }
            }
            """);
}
