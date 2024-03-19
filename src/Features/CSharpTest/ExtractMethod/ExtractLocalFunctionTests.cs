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
using Roslyn.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CodeRefactorings.ExtractMethod.ExtractMethodCodeRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ExtractMethod
{
    public class ExtractLocalFunctionTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new ExtractMethodCodeRefactoringProvider();

        private static int CodeActionIndexWhenExtractMethodMissing => 0;

        private static int CodeActionIndex => 1;

        private const string EditorConfigNaming_CamelCase = """
            [*]
            # Naming rules

            dotnet_naming_rule.local_functions_should_be_camel_case.severity = suggestion
            dotnet_naming_rule.local_functions_should_be_camel_case.symbols = local_functions
            dotnet_naming_rule.local_functions_should_be_camel_case.style = camel_case

            # Symbol specifications

            dotnet_naming_symbols.local_functions.applicable_kinds = local_function
            dotnet_naming_symbols.local_functions.applicable_accessibilities = *

            # Naming styles

            dotnet_naming_style.camel_case.capitalization = camel_case
            """;

        private const string EditorConfigNaming_PascalCase = """
            [*]
            # Naming rules

            dotnet_naming_rule.local_functions_should_be_pascal_case.severity = suggestion
            dotnet_naming_rule.local_functions_should_be_pascal_case.symbols = local_functions
            dotnet_naming_rule.local_functions_should_be_pascal_case.style = pascal_case

            # Symbol specifications

            dotnet_naming_symbols.local_functions.applicable_kinds = local_function
            dotnet_naming_symbols.local_functions.applicable_accessibilities = *
            dotnet_naming_symbols.local_functions.required_modifiers = 

            # Naming styles

            dotnet_naming_style.pascal_case.capitalization = pascal_case
            """;

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionTrue()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b)
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionFalse()
        {
            await TestInRegularAndScript1Async(
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

                        bool NewMethod(bool b)
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionDefault()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b)
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CSharpCodeStyleOptions.PreferStaticLocalFunction.DefaultValue)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseExpressionBodyWhenPossible()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b) => b != true;
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b) => b != true;
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseExpressionWhenOnSingleLine_AndIsOnSingleLine2()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b) => b != true;
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b)
                        {
                            return b !=
                                        true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine2()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b)
                        {
                            return b !=/*
                */true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseExpressionWhenOnSingleLine_AndNotIsOnSingleLine3()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod()
                        {
                            return "" != @"
                ";
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestReadOfDataThatDoesNotFlowIn()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(int x, object y)
                        {
                            int s = true ? fun(x) : fun(y);
                        }
                    }

                    private static T fun<T>(T t)
                    {
                        return t;
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestOnStatementAfterUnconditionalGoto()
        {
            await TestInRegularAndScript1Async(
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

                            static int NewMethod(int x)
                            {
                                return x * x;
                            }
                        };
                    label2:
                        return;
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestOnNamespace()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod()
                        {
                            System.Console.WriteLine(4);
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestOnType()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod()
                        {
                            System.Console.WriteLine(4);
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestOnBase()
        {
            await TestInRegularAndScript1Async(
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

                        void NewMethod()
                        {
                            base.ToString();
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestOnActionInvocation()
        {
            await TestInRegularAndScript1Async(
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

                        static Action GetX()
                        {
                            return C.X;
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task DisambiguateCallSiteIfNecessary1()
        {
            await TestInRegularAndScript1Async(
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
                        Goo({|Rename:NewMethod|}(), y => (byte)0, z, z);

                        static Func<byte, byte> NewMethod()
                        {
                            return x => 0;
                        }
                    }

                    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
                    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task DisambiguateCallSiteIfNecessary2()
        {
            await TestInRegularAndScript1Async(
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
                        Goo({|Rename:NewMethod|}(), y => { return (byte)0; }, z, z);

                        static Func<byte, byte> NewMethod()
                        {
                            return x => 0;
                        }
                    }

                    static void Goo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
                    static void Goo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task DoNotOverParenthesize()
        {
            await TestAsync(
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
                        Outer(y => Inner(x => {|Rename:GetX|}(x).Ex(), y), (object)- -1);

                        static string GetX(string x)
                        {
                            return x;
                        }
                    }
                }

                static class E
                {
                    public static void Ex(this int x)
                    {
                    }
                }
                """,

parseOptions: TestOptions.Regular, index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task DoNotOverParenthesizeGenerics()
        {
            await TestAsync(
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
                        Outer(y => Inner(x => {|Rename:GetX|}(x).Ex<int>(), y), (object)- -1);

                        static string GetX(string x)
                        {
                            return x;
                        }
                    }
                }

                static class E
                {
                    public static void Ex<T>(this int x)
                    {
                    }
                }
                """,

parseOptions: TestOptions.Regular, index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task PreserveCommentsBeforeDeclaration_1()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(out Construct obj1, out Construct obj2)
                        {
                            obj1 = new Construct();
                            obj1.Do();
                            /* Interesting comment. */
                            obj2 = new Construct();
                            obj2.Do();
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task PreserveCommentsBeforeDeclaration_2()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(out Construct obj1, out Construct obj2, out Construct obj3)
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
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task PreserveCommentsBeforeDeclaration_3()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(out Construct obj1, out Construct obj2, out Construct obj3)
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
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTuple()
        {
            await TestInRegularAndScript1Async(
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

                        static (int, int) NewMethod()
                        {
                            return (1, 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleDeclarationWithNames()
        {
            await TestInRegularAndScript1Async(
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

                        static (int a, int b) NewMethod()
                        {
                            return (1, 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleDeclarationWithSomeNames()
        {
            await TestInRegularAndScript1Async(
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

                        static (int a, int) NewMethod()
                        {
                            return (1, 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleWith1Arity()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(ValueTuple<int> y)
                        {
                            y.Item1.ToString();
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleLiteralWithNames()
        {
            await TestInRegularAndScript1Async(
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

                        static (int, int) NewMethod()
                        {
                            return (a: 1, b: 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleDeclarationAndLiteralWithNames()
        {
            await TestInRegularAndScript1Async(
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

                        static (int a, int b) NewMethod()
                        {
                            return (c: 1, d: 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleIntoVar()
        {
            await TestInRegularAndScript1Async(
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

                        static (int c, int d) NewMethod()
                        {
                            return (c: 1, d: 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task RefactorWithoutSystemValueTuple()
        {
            await TestInRegularAndScript1Async(
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

                        static (int c, int d) NewMethod()
                        {
                            return (c: 1, d: 2);
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestTupleWithNestedNamedTuple()
        {
            // This is not the best refactoring, but this is an edge case
            await TestInRegularAndScript1Async(
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

                        static (int, int, int, int, int, int, int, string, string) NewMethod()
                        {
                            return new System.ValueTuple<int, int, int, int, int, int, int, (string a, string b)>(1, 2, 3, 4, 5, 6, 7, (a: "hello", b: "world"));
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestDeconstruction()
        {
            await TestInRegularAndScript1Async(
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

                        static (int, int) NewMethod()
                        {
                            return (1, 2);
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction), CompilerTrait(CompilerFeature.Tuples)]
        public async Task TestDeconstruction2()
        {
            await TestInRegularAndScript1Async(
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

                        static int NewMethod()
                        {
                            return 3;
                        }
                    }
                }
                """ + TestResources.NetFX.ValueTuple.tuplelib_cs, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [CompilerTrait(CompilerFeature.OutVar)]
        public async Task TestOutVar()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(int i, out int r, out int y)
                        {
                            r = M1(out y, i);
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task TestIsPattern()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(int i, out int r, out int y)
                        {
                            r = M1(3 is int {|Conflict:y|}, i);
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task TestOutVarAndIsPattern()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(out int r, out int y, out int z)
                        {
                            r = M1(out /*out*/  /*int*/ y /*y*/) + M2(3 is int {|Conflict:z|});
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task ConflictingOutVarLocals()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(out int r, out int y)
                        {
                            r = M1(out y);
                            {
                                M2(out int y);
                                System.Console.Write(y);
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [CompilerTrait(CompilerFeature.Patterns)]
        public async Task ConflictingPatternLocals()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(out int r, out int y)
                        {
                            r = M1(1 is int {|Conflict:y|});
                            {
                                M2(2 is int y);
                                System.Console.Write(y);
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestCancellationTokenGoesLast()
        {
            await TestInRegularAndScript1Async(
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

                        static void NewMethod(int v, CancellationToken ct)
                        {
                            if (true)
                            {
                                ct.ThrowIfCancellationRequested();
                                Console.WriteLine(v);
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseVar1()
        {
            await TestInRegularAndScript1Async(
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

                        static string NewMethod(int i)
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
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOption2.TrueWithSuggestionEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestUseVar2()
        {
            await TestInRegularAndScript1Async(
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

                        static string NewMethod(int i)
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
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSuggestionEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionCall()
        {
            var code = """
                class C
                {
                    public static void Main()
                    {
                        void Local() { }
                        [|Local();|]
                    }
                }
                """;
            var expectedCode = """
                class C
                {
                    public static void Main()
                    {
                        void Local() { }
                        {|Rename:NewMethod|}();

                        void NewMethod()
                        {
                            Local();
                        }
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_local_function]);
            await TestInRegularAndScript1Async(code, expectedCode, CodeActionIndexWhenExtractMethodMissing);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionCall_2()
        {
            await TestInRegularAndScript1Async("""
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

                        static void NewMethod()
                        {
                            void Local() { }
                            Local();
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39946"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionCall_3()
        {
            await TestInRegularAndScript1Async("""
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

                            static void NewMethod()
                            {
                                void Local() { }
                                Local();
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39946"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractFunctionUnderLocalFunctionCall()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    int Testing;

                    void ClassTest()
                    {
                        ExistingLocalFunction();
                        [|NewMethod();|]
                        Testing = 5;

                        void ExistingLocalFunction()
                        {

                        }
                    }

                    void NewMethod()
                    {

                    }
                }
                """, """
                class Test
                {
                    int Testing;

                    void ClassTest()
                    {
                        ExistingLocalFunction();
                        {|Rename:NewMethod1|}();
                        Testing = 5;

                        void ExistingLocalFunction()
                        {

                        }

                        void NewMethod1()
                        {
                            NewMethod();
                        }
                    }

                    void NewMethod()
                    {

                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionCallWithCapture()
        {
            var code = """
                class C
                {
                    public static void Main(string[] args)
                    {
                        bool Local() => args == null;
                        [|Local();|]
                    }
                }
                """;
            var expectedCode = """
                class C
                {
                    public static void Main(string[] args)
                    {
                        bool Local() => args == null;
                        {|Rename:NewMethod|}();

                        void NewMethod()
                        {
                            Local();
                        }
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_local_function]);
            await TestInRegularAndScript1Async(code, expectedCode, CodeActionIndexWhenExtractMethodMissing);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionInterior()
        {
            await TestInRegularAndScript1Async("""
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

                            static void NewMethod()
                            {
                                int x = 0;
                                x++;
                            }
                        }
                        Local();
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionWithinForLoop()
        {
            await TestInRegularAndScript1Async("""
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

                            static int NewMethod(int v, int i)
                            {
                                v = v + i;
                                return v;
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionWithinForLoop2()
        {
            await TestInRegularAndScript1Async("""
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

                            static int NewMethod(int v, int i)
                            {
                                return v + i;
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ExtractLocalFunctionWithinForLoop3()
        {
            await TestInRegularAndScript1Async("""
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

                            static int NewMethod(ref int v, int i)
                            {
                                return v = v + i;
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestTupleWithInferredNames()
        {
            await TestAsync("""
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
                        var t = {|Rename:GetT|}();
                        System.Console.Write(t.a);

                        (int a, int b) GetT()
                        {
                            return (a, b: 2);
                        }
                    }
                }
                """, TestOptions.Regular7_1, index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestDeconstruction4()
        {
            await TestAsync("""
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
                        {|Rename:NewMethod|}();
                        System.Console.Write(x + y);

                        void NewMethod()
                        {
                            var (x, y) = (1, 2);
                        }
                    }
                }
                """, TestOptions.Regular7_1, index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestDeconstruction5()
        {
            await TestAsync("""
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
                        {|Rename:NewMethod|}();
                        System.Console.Write(x + y);

                        void NewMethod()
                        {
                            (x, y) = (1, 2);
                        }
                    }
                }
                """, TestOptions.Regular7_1, index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestIndexExpression()
        {
            await TestInRegularAndScript1Async(TestSources.Index + """
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

        static System.Index NewMethod()
        {
            return ^1;
        }
    }
}
""", CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestRangeExpression_Empty()
        {
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + """
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

        static System.Range NewMethod()
        {
            return ..;
        }
    }
}
""", CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestRangeExpression_Left()
        {
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + """
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

        static System.Range NewMethod()
        {
            return ..1;
        }
    }
}
""", CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestRangeExpression_Right()
        {
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + """
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

        static System.Range NewMethod()
        {
            return 1..;
        }
    }
}
""", CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestRangeExpression_Both()
        {
            await TestInRegularAndScript1Async(TestSources.Index + TestSources.Range + """
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

        static System.Range NewMethod()
        {
            return 1..2;
        }
    }
}
""", CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestAnnotatedNullableReturn()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod()
                        {
                            string? x = null;
                            x?.ToString();
                            return x;
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestAnnotatedNullableParameters1()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod(string? a, string? b)
                        {
                            return a?.Contains(b).ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestAnnotatedNullableParameters2()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod(string? a, string? b, int c)
                        {
                            return (a + b + c).ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestAnnotatedNullableParameters3()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod(string? a, string? b, int c)
                        {
                            return (a + b + c).ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestAnnotatedNullableParameters4()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod(string? a, string? b)
                        {
                            return a?.Contains(b).ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters1()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod(string a, string b)
                        {
                            return (a + b + a).ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters2()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod(string a, string b)
                        {
                            return (a + b + a).ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters3()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod(string? a, string? b)
                        {
                            return (a + b + a)?.ToString();
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters_MultipleStates()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod(ref string? a, ref string? b)
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
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters_MultipleStatesNonNullReturn()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod(ref string? a, ref string? b)
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
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters_MultipleStatesNullReturn()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod(ref string? a, ref string? b)
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
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowStateNullableParameters_RefNotNull()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod(ref string a, ref string b)
                        {
                            var c = a + b;
                            a = string.Empty;
                            c += a;
                            b = "test";
                            c = a + b + c;
                            return c;
                        }
                    }
                }
                """, CodeActionIndex);

        // There's a case below where flow state correctly asseses that the variable
        // 'x' is non-null when returned. It's wasn't obvious when writing, but that's 
        // due to the fact the line above it being executed as 'x.ToString()' would throw
        // an exception and the return statement would never be hit. The only way the return
        // statement gets executed is if the `x.ToString()` call succeeds, thus suggesting 
        // that the value is indeed not null.
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowNullableReturn_NotNull1()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod()
                        {
                            string? x = null;
                            x.ToString();
                            return x;
                        }
                    }
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowNullableReturn_NotNull2()
            => TestInRegularAndScript1Async(
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

                        static string NewMethod()
                        {
                            string? x = null;
                            x?.ToString();
                            x = string.Empty;
                            return x;
                        }
                    }
                }
                """, CodeActionIndex);
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowNullable_Lambda()
            => TestInRegularAndScript1Async(
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

                        static string? NewMethod()
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
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestFlowNullable_LambdaWithReturn()
            => TestInRegularAndScript1Async(
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
                        string? x = {|Rename:NewMethod|}();

                        return x;

                        static string NewMethod()
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
                }
                """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestExtractReadOnlyMethod()
        {
            await TestInRegularAndScript1Async(
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

                        readonly void NewMethod()
                        {
                            int i = M1() + M1();
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestExtractReadOnlyMethodInReadOnlyStruct()
        {
            await TestInRegularAndScript1Async(
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

                        void NewMethod()
                        {
                            int i = M1() + M1();
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestExtractNonReadOnlyMethodInReadOnlyMethod()
        {
            await TestInRegularAndScript1Async(
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

                        void NewMethod()
                        {
                            int i = M1() + M1();
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNullableObjectWithExplicitCast()
        => TestInRegularAndScript1Async(
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

                    static object? GetO(object? o)
                    {
                        return o;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNotNullableObjectWithExplicitCast()
        => TestInRegularAndScript1Async(
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

                    static object GetO(object o)
                    {
                        return o;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNotNullableWithExplicitCast()
        => TestInRegularAndScript1Async(
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

                    static B GetB(B b)
                    {
                        return b;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNullableWithExplicitCast()
        => TestInRegularAndScript1Async(
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

                    static B? GetB(B? b)
                    {
                        return b;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNotNullableWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
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

                    static string GetS(object o)
                    {
                        return (string)o;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNullableWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
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

                    static string? GetS(object? o)
                    {
                        return (string?)o;
                    }
                }
            }
            """, CodeActionIndex);
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNullableNonNullFlowWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
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

                    static string? GetS(object o)
                    {
                        return (string?)o;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public Task TestExtractNullableToNonNullableWithExplicitCastSelected()
        => TestInRegularAndScript1Async(
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

                    static string? GetS(object? o)
                    {
                        return (string)o;
                    }
                }
            }
            """, CodeActionIndex);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestExtractLocalFunction_EnsureUniqueFunctionName()
        {
            await TestInRegularAndScript1Async(
                """
                class Test
                {
                    static void Main(string[] args)
                    {
                        [|var test = 1;|]

                        static void NewMethod()
                        {
                            var test = 1;
                        }
                    }
                }
                """,
                """
                class Test
                {
                    static void Main(string[] args)
                    {
                        {|Rename:NewMethod1|}();

                        static void NewMethod()
                        {
                            var test = 1;
                        }

                        static void NewMethod1()
                        {
                            var test = 1;
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestExtractLocalFunctionWithinLocalFunction_EnsureUniqueFunctionName()
        {
            await TestInRegularAndScript1Async(
                """
                class Test
                {
                    static void Main(string[] args)
                    {
                        static void NewMethod()
                        {
                            var NewMethod2 = 0;
                            [|var test = 1;|]

                            static void NewMethod1()
                            {
                                var test = 1;
                            }
                        }
                    }
                }
                """,
                """
                class Test
                {
                    static void Main(string[] args)
                    {
                        static void NewMethod()
                        {
                            var NewMethod2 = 0;
                            {|Rename:NewMethod3|}();

                            static void NewMethod1()
                            {
                                var test = 1;
                            }

                            static void NewMethod3()
                            {
                                var test = 1;
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestExtractNonStaticLocalMethod_WithDeclaration()
        {
            await TestInRegularAndScript1Async(
                """
                class Test
                {
                    static void Main(string[] args)
                    {
                        [|ExistingLocalFunction();

                        void ExistingLocalFunction()
                        {
                        }|]
                    }
                }
                """,
                """
                class Test
                {
                    static void Main(string[] args)
                    {
                        {|Rename:NewMethod|}();

                        static void NewMethod()
                        {
                            ExistingLocalFunction();

                            void ExistingLocalFunction()
                            {
                            }
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task ArgumentlessReturnWithConstIfExpression()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class Program
                {
                    void Test()
                    {
                        if (true)
                            [|if (true)
                                return;|]
                        Console.WriteLine();
                    }
                }
                """,
                """
                using System;

                class Program
                {
                    void Test()
                    {
                        if (true)
                        {
                            {|Rename:NewMethod|}();
                            return;
                        }
                        Console.WriteLine();

                        static void NewMethod()
                        {
                            if (true)
                                return;
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionTrue_EarlierCSharpVersionShouldBeNonStatic()
        {
            await TestInRegularAndScript1Async(
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
                        System.Console.WriteLine({|Rename:NewMethod|}() ? b = true : b = false);

                        bool NewMethod()
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_3)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionTrue_EarlierCSharpVersionShouldBeNonStatic2()
        {
            await TestInRegularAndScript1Async(
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
                        System.Console.WriteLine({|Rename:NewMethod|}() ? b = true : b = false);

                        bool NewMethod()
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionTrue_CSharp8AndLaterStaticSupported()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b)
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_StaticOptionTrue_CSharp8AndLaterStaticSupported2()
        {
            await TestInRegularAndScript1Async(
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

                        static bool NewMethod(bool b)
                        {
                            return b != true;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInPropertyInitializer_Get()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get { [|return _seconds / 3600;|] }
                        set 
                        { 
                            if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");
                            _seconds = value * 3600;
                        }
                    }
                }
                """,
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get
                        {
                            return {|Rename:NewMethod|}();

                            double NewMethod()
                            {
                                return _seconds / 3600;
                            }
                        }
                        set 
                        { 
                            if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");
                            _seconds = value * 3600;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInPropertyInitializer_Get2()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get { return [|_seconds / 3600;|] }
                        set 
                        { 
                            if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");
                            _seconds = value * 3600;
                        }
                    }
                }
                """,
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get
                        {
                            return {|Rename:NewMethod|}();

                            double NewMethod()
                            {
                                return _seconds / 3600;
                            }
                        }
                        set 
                        { 
                            if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");
                            _seconds = value * 3600;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInPropertyInitializer_Set()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get { return _seconds / 3600; }
                        set 
                        {
                            [|if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");|]
                            _seconds = value * 3600;
                        }
                    }
                }
                """,
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get { return _seconds / 3600; }
                        set
                        {
                            {|Rename:NewMethod|}(value);
                            _seconds = value * 3600;

                            static void NewMethod(double value)
                            {
                                if (value < 0 || value > 24)
                                    throw new ArgumentOutOfRangeException("test");
                            }
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInPropertyInitializer_Set2()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get { return _seconds / 3600; }
                        set 
                        {
                            if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");
                            [|_seconds = value * 3600;|]
                        }
                    }
                }
                """,
                """
                using System;

                class TimePeriod
                {
                    private double _seconds;

                    public double Hours
                    {
                        get { return _seconds / 3600; }
                        set
                        {
                            if (value < 0 || value > 24)
                                throw new ArgumentOutOfRangeException("test");
                            {|Rename:NewMethod|}(value);

                            void NewMethod(double value)
                            {
                                _seconds = value * 3600;
                            }
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInIndexer_Get()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            [|if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");|]
                            return testArr[index];
                        }
                        set
                        {
                            if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            testArr[index] = value;
                        }
                    }
                }
                """,
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            {|Rename:NewMethod|}(index);
                            return testArr[index];

                            void NewMethod(int index)
                            {
                                if (index < 0 && index >= testArr.Length)
                                    throw new IndexOutOfRangeException("test");
                            }
                        }
                        set
                        {
                            if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            testArr[index] = value;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInIndexer_Get2()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            [|return testArr[index];|]
                        }
                        set
                        {
                            if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            testArr[index] = value;
                        }
                    }
                }
                """,
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            return {|Rename:NewMethod|}(index);

                            string NewMethod(int index)
                            {
                                return testArr[index];
                            }
                        }
                        set
                        {
                            if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            testArr[index] = value;
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInIndexer_Set()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            return testArr[index];
                        }
                        set
                        {
                            [|if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");|]
                            testArr[index] = value;
                        }
                    }
                }
                """,
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            return testArr[index];
                        }
                        set
                        {
                            {|Rename:NewMethod|}(index);
                            testArr[index] = value;

                            void NewMethod(int index)
                            {
                                if (index < 0 || index >= testArr.Length)
                                    throw new IndexOutOfRangeException("test");
                            }
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInIndexer_Set2()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            return testArr[index];
                        }
                        set
                        {
                            if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            [|testArr[index] = value;|]
                        }
                    }
                }
                """,
                """
                using System;

                class Indexer
                {
                    private readonly string[] testArr = new string[1];

                    public string this[int index]
                    {
                        get
                        {
                            if (index < 0 && index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            return testArr[index];
                        }
                        set
                        {
                            if (index < 0 || index >= testArr.Length)
                                throw new IndexOutOfRangeException("test");
                            {|Rename:NewMethod|}(index, value);

                            void NewMethod(int index, string value)
                            {
                                testArr[index] = value;
                            }
                        }
                    }
                }
                """, CodeActionIndex, new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.TrueWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_CSharp5NotApplicable()
        {
            var code = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        bool b = true;
                        System.Console.WriteLine([|b != true|] ? b = true : b = false);
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method], new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestPartialSelection_CSharp6NotApplicable()
        {
            var code = """
                class Program
                {
                    static void Main(string[] args)
                    {
                        bool b = true;
                        System.Console.WriteLine([|b != true|] ? b = true : b = false);
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method], new TestParameters(parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestInIntegerExpression()
        {
            await TestInRegularAndScript1Async(
                """
                class MethodExtraction
                {
                    void TestMethod()
                    {
                        int a = [|1 + 1|];
                    }
                }
                """,
                """
                class MethodExtraction
                {
                    void TestMethod()
                    {
                        int a = {|Rename:GetA|}();

                        static int GetA()
                        {
                            return 1 + 1;
                        }
                    }
                }
                """, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40188"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestEditorconfigSetting_StaticLocalFunction_True()
        {
            var input = """
                <Workspace>
                    <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath = "z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        [|bool test = true;|]
                        System.Console.WriteLine(b != true ? b = true : b = false);
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_prefer_static_local_function = true:silent
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
                        {|Rename:NewMethod|}();
                        System.Console.WriteLine(b != true ? b = true : b = false);

                        static void NewMethod()
                        {
                            bool test = true;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_prefer_static_local_function = true:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40188"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestEditorconfigSetting_StaticLocalFunction_False()
        {
            var input = """
                <Workspace>
                    <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath = "z:\\file.cs">
                class Program1
                {
                    static void Main()
                    {
                        [|bool test = true;|]
                        System.Console.WriteLine(b != true ? b = true : b = false);
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_prefer_static_local_function = false:silent
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
                        {|Rename:NewMethod|}();
                        System.Console.WriteLine(b != true ? b = true : b = false);

                        void NewMethod()
                        {
                            bool test = true;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_prefer_static_local_function = false:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40188"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestEditorconfigSetting_ExpressionBodiedLocalFunction_True()
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
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_style_expression_bodied_local_functions = true:silent
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

                        static bool NewMethod() => true;
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_style_expression_bodied_local_functions = true:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40188"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestEditorconfigSetting_ExpressionBodiedLocalFunction_False()
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
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_style_expression_bodied_local_functions = false:silent
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

                        static bool NewMethod()
                        {
                            return true;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">[*.cs]
                csharp_style_expression_bodied_local_functions = false:silent
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestNaming_CamelCase()
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
                """ + EditorConfigNaming_CamelCase + """
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
                        bool b = {|Rename:newMethod|}();
                        System.Console.WriteLine(b != true ? b = true : b = false);

                        static bool newMethod()
                        {
                            return true;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
                """ + EditorConfigNaming_CamelCase + """
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestNaming_CamelCase_GetName()
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
                """ + EditorConfigNaming_CamelCase + """
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
                        int a = {|Rename:getA|}();

                        static int getA()
                        {
                            return 1 + 1;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
                """ + EditorConfigNaming_CamelCase + """
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestNaming_PascalCase()
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
                """ + EditorConfigNaming_PascalCase + """
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

                        static bool NewMethod()
                        {
                            return true;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
                """ + EditorConfigNaming_PascalCase + """
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestNaming_PascalCase_GetName()
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
                """ + EditorConfigNaming_PascalCase + """
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

                        static int GetA()
                        {
                            return 1 + 1;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
                """ + EditorConfigNaming_PascalCase + """
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40209"), Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestNaming_CamelCase_DoesntApply()
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
                """ + EditorConfigNaming_CamelCase + """
                dotnet_naming_symbols.local_functions.required_modifiers = static
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

                        bool NewMethod()
                        {
                            return true;
                        }
                    }
                }
                        </Document>
                        <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
                """ + EditorConfigNaming_CamelCase + """
                dotnet_naming_symbols.local_functions.required_modifiers = static
                </AnalyzerConfigDocument>
                    </Project>
                </Workspace>
                """;

            await TestInRegularAndScript1Async(input, expected, CodeActionIndex,
                new TestParameters(options: Option(CSharpCodeStyleOptions.PreferStaticLocalFunction, CodeStyleOption2.FalseWithSilentEnforcement)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40654")]
        public async Task TestOnInvalidUsingStatement_MultipleStatements()
        {
            var input = """
                class C
                {
                    void M()
                    {
                        [|var v = 0;
                        using System;|]
                    }
                }
                """;
            var expected = """
                class C
                {
                    void M()
                    {
                        {|Rename:NewMethod|}();

                        static void NewMethod()
                        {
                            var v = 0;
                            using System;
                        }
                    }
                }
                """;
            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40555")]
        public async Task TestOnLocalFunctionHeader_Parameter()
        {
            var input = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M(() =>
                        {
                            void F(int [|x|])
                            {
                            }
                        });
                    }
                }
                """;
            var expected = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M({|Rename:NewMethod|}());

                        static Action NewMethod()
                        {
                            return () =>
                            {
                                void F(int x)
                                {
                                }
                            };
                        }
                    }
                }
                """;
            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40555")]
        public async Task TestOnLocalFunctionHeader_Parameter_ExpressionBody()
        {
            var input = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M(() =>
                        {
                            int F(int [|x|]) => 1;
                        });
                    }
                }
                """;
            var expected = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M({|Rename:NewMethod|}());

                        static Action NewMethod()
                        {
                            return () =>
                            {
                                int F(int x) => 1;
                            };
                        }
                    }
                }
                """;
            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40555")]
        public async Task TestOnLocalFunctionHeader_Identifier()
        {
            var input = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M(() =>
                        {
                            void [|F|](int x)
                            {
                            }
                        });
                    }
                }
                """;
            var expected = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M({|Rename:NewMethod|}());

                        static Action NewMethod()
                        {
                            return () =>
                            {
                                void F(int x)
                                {
                                }
                            };
                        }
                    }
                }
                """;
            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40555")]
        public async Task TestOnLocalFunctionHeader_Identifier_ExpressionBody()
        {
            var input = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M(() =>
                        {
                            int [|F|](int x) => 1;
                        });
                    }
                }
                """;
            var expected = """
                using System;
                class C
                {
                    void M(Action a)
                    {
                        M({|Rename:NewMethod|}());

                        static Action NewMethod()
                        {
                            return () =>
                            {
                                int F(int x) => 1;
                            };
                        }
                    }
                }
                """;
            await TestInRegularAndScript1Async(input, expected, CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40654")]
        public async Task TestMissingOnUsingStatement()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        void L()
                        {
                            [|using System;|]
                        }
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInLocalFunctionDeclaration_ExpressionBody()
        {
            await TestMissingInRegularAndScriptAsync("""
                class C
                {
                    public static void Main(string[] args)
                    {
                        [|bool Local() => args == null;|]
                        Local();
                    }
                }
                """, new TestParameters(index: CodeActionIndex));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInLocalFunctionDeclaration()
        {
            await TestMissingInRegularAndScriptAsync("""
                class C
                {
                    public static void Main(string[] args)
                    {
                        [|bool Local()
                        {
                            return args == null;
                        }|]
                        Local();
                    }
                }
                """, new TestParameters(index: CodeActionIndex));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestOnGoto()
        {
            await TestInRegularAndScriptAsync(
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
                
                            static int NewMethod(int x)
                            {
                                goto label2;
                                return x * x;
                            }
                        };
                    label2:
                        return;
                    }
                }
                """, index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInFieldInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    [|int a = 10;|]
                    int b = 5;

                    static void Main(string[] args)
                    {
                    }
                }
                """, new TestParameters(index: CodeActionIndex));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInFieldInitializer_2()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int [|a = 10;|]
                    int b = 5;

                    static void Main(string[] args)
                    {
                    }
                }
                """, new TestParameters(index: CodeActionIndex));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInExpressionBodyProperty()
        {
            var code = """
                class Program
                {
                    int field;

                    public int Blah => [|this.field|];
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInExpressionBodyIndexer()
        {
            var code = """
                class Program
                {
                    int field;

                    public int this[int i] => [|this.field|];
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInExpressionBodyPropertyGetAccessor()
        {
            var code = """
                class Program
                {
                    int field;

                    public int Blah
                    {
                        get => [|this.field|];
                        set => field = value;
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInExpressionBodyPropertySetAccessor()
        {
            var code = """
                class Program
                {
                    int field;

                    public int Blah
                    {
                        get => this.field;
                        set => field = [|value|];
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInExpressionBodyIndexerGetAccessor()
        {
            var code = """
                class Program
                {
                    int field;

                    public int this[int i]
                    {
                        get => [|this.field|];
                        set => field = value;
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInExpressionBodyIndexerSetAccessor()
        {
            var code = """
                class Program
                {
                    int field;

                    public int this[int i]
                    {
                        get => this.field;
                        set => field = [|value|];
                    }
                }
                """;
            await TestExactActionSetOfferedAsync(code, [FeaturesResources.Extract_method]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInAttributeInitializer()
        {
            await TestMissingInRegularAndScriptAsync("""
                using System;

                class C
                {
                    [|[Serializable]|]
                    public class SampleClass
                    {
                        // Objects of this type can be serialized.
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInAttributeInitializerParameter()
        {
            await TestMissingInRegularAndScriptAsync("""
                using System.Runtime.InteropServices;

                class C
                {
                    [ComVisible([|true|])]
                    public class SampleClass
                    {
                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInThisConstructorCall()
        {
            await TestMissingInRegularAndScriptAsync("""
                class B
                {
                    protected B(string message)
                    {

                    }
                }

                class C : B
                {
                    public C(string message) : [|this("test", "test2")|]
                    {

                    }

                    public C(string message, string message2) : base(message)
                    {

                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingInBaseConstructorCall()
        {
            await TestMissingInRegularAndScriptAsync("""
                class B
                {
                    protected B(string message)
                    {

                    }
                }

                class C : B
                {
                    public C(string message) : this("test", "test2")
                    {

                    }

                    public C(string message, string message2) : [|base(message)|]
                    {

                    }
                }
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/22150")]
        public async Task ExtractLocalFunctionToLocalFunction()
        {
            var code = """
                class C
                {
                    static void Main(string[] args)
                    {
                        void Local() { }
                        [|Local();|]
                    }

                    static void Local() => System.Console.WriteLine();
                }
                """;
            var expected = """
                class C
                {
                    static void Main(string[] args)
                    {
                        void Local() { }
                        {|Rename:NewMethod|}();

                        void NewMethod()
                        {
                            Local();
                        }
                    }

                    static void Local() => System.Console.WriteLine();
                }
                """;

            await TestInRegularAndScript1Async(code, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/56969")]
        public async Task TopLevelStatement_FullStatement()
        {
            var code = """
                [|System.Console.WriteLine("string");|]
                """;
            var expected = """
                NewMethod();

                static void NewMethod()
                {
                    System.Console.WriteLine("string");
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_local_function),
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
            var expected = """
                System.Console.WriteLine("string");

                int x = NewMethod();

                System.Console.WriteLine(x);

                static int NewMethod()
                {
                    int x = int.Parse("0");
                    System.Console.WriteLine(x);
                    return x;
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_local_function),
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
            var expected = """
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
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_local_function),
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
            var expected = """
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
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionIndex = 0,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_local_function),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
        public async Task TopLevelStatement_ArgumentInInvocation()
        {
            var code = """
                System.Console.WriteLine([|"string"|]);
                """;
            var expected = """
                System.Console.WriteLine(NewMethod());

                static string NewMethod()
                {
                    return "string";
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_local_function),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
        public async Task TopLevelStatement_InBlock_ArgumentInInvocation()
        {
            var code = """
                {
                    System.Console.WriteLine([|"string"|]);
                }
                """;
            var expected = """
                {
                    System.Console.WriteLine(NewMethod());

                    static string NewMethod()
                    {
                        return "string";
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { code },
                    OutputKind = OutputKind.ConsoleApplication,
                },
                FixedCode = expected,
                LanguageVersion = LanguageVersion.CSharp9,
                CodeActionIndex = 1,
                CodeActionEquivalenceKey = nameof(FeaturesResources.Extract_local_function),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/44260")]
        public async Task TopLevelStatement_ArgumentInInvocation_InInteractive()
        {
            var code = """
                System.Console.WriteLine([|"string"|]);
                """;
            var expected =
                """
                {
                    System.Console.WriteLine({|Rename:NewMethod|}());

                    static string NewMethod()
                    {
                        return "string";
                    }
                }
                """;

            await TestAsync(code, expected, TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp9), index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        public async Task TestMissingOnExtractLocalFunctionInNamespace()
        {
            await TestMissingInRegularAndScriptAsync("""
                namespace C
                {
                    private bool TestMethod() => [|false|];
                }
                """, codeActionIndex: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/45422")]
        public async Task TestOnExtractLocalFunction()
        {
            await TestInRegularAndScriptAsync("""
                class C
                {
                    static void M()
                    {
                        if (true)
                        {
                            static void L()
                            {
                               [|
                                static void L2()
                                {
                                    var x = 1;
                                }|]
                            }
                        }
                    }
                }
                """, """
                class C
                {
                    static void M()
                    {
                        if (true)
                            {|Rename:NewMethod|}();

                        static void NewMethod()
                        {
                            static void L()
                            {

                                static void L2()
                                {
                                    var x = 1;
                                }
                            }
                        }
                    }
                }
                """, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractLocalFunction)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/45422")]
        public async Task TestExtractLocalFunctionWithExtraBrace()
        {
            await TestInRegularAndScript1Async("""
                class C
                {
                    static void M()
                    {
                        if (true)
                        {
                            static void L()
                            {
                                [|static void L2()
                                {
                                    var x = 1;
                                }
                            }|]
                        }
                    }
                }
                """, """
                class C
                {
                    static void M()
                    {
                        if (true)
                            {|Rename:NewMethod|}();

                        static void NewMethod()
                        {
                            static void L()
                            {
                                static void L2()
                                {
                                    var x = 1;
                                }
                            }
                        }
                    }
                }
                """, index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55031")]
        public async Task TestExtractLocalConst_CSharp7()
        {
            var code = """
                using NUnit.Framework;

                public class Tests
                {
                    public string SomeOtherMethod(int k)
                    {
                        return ";
                    }

                    int j = 2;
                    [Test]
                    public void Test1()
                    {
                        const string NAME = "SOMETEXT";
                        [|Assert.AreEqual(string.Format(NAME, 0, 0), SomeOtherMethod(j));|]
                    }
                }
                """;

            var expected = """
                using NUnit.Framework;

                public class Tests
                {
                    public string SomeOtherMethod(int k)
                    {
                        return ";
                    }

                    int j = 2;
                    [Test]
                    public void Test1()
                    {
                        const string NAME = "SOMETEXT";
                        {|Rename:NewMethod|}();

                        void NewMethod()
                        {
                            Assert.AreEqual(string.Format(NAME, 0, 0), SomeOtherMethod(j));
                        }
                    }
                }
                """;
            await TestAsync(code, expected, TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp7), index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55031")]
        public async Task TestExtractParameter_CSharp7()
        {
            var code = """
                using NUnit.Framework;

                public class Tests
                {
                    public string SomeOtherMethod(int k)
                    {
                        return ";
                    }

                    int j = 2;
                    [Test]
                    public void Test1(string NAME)
                    {
                        [|Assert.AreEqual(string.Format(NAME, 0, 0), SomeOtherMethod(j));|]
                    }
                }
                """;

            var expected = """
                using NUnit.Framework;

                public class Tests
                {
                    public string SomeOtherMethod(int k)
                    {
                        return ";
                    }

                    int j = 2;
                    [Test]
                    public void Test1(string NAME)
                    {
                        {|Rename:NewMethod|}();

                        void NewMethod()
                        {
                            Assert.AreEqual(string.Format(NAME, 0, 0), SomeOtherMethod(j));
                        }
                    }
                }
                """;
            await TestAsync(code, expected, TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp7), index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55031")]
        public async Task TestExtractLocal_CSharp7()
        {
            var code = """
                using NUnit.Framework;

                public class Tests
                {
                    public string SomeOtherMethod(int k)
                    {
                        return ";
                    }

                    int j = 2;
                    [Test]
                    public void Test1()
                    {
                        var NAME = "SOMETEXT";
                        [|Assert.AreEqual(string.Format(NAME, 0, 0), SomeOtherMethod(j));|]
                    }
                }
                """;

            var expected = """
                using NUnit.Framework;

                public class Tests
                {
                    public string SomeOtherMethod(int k)
                    {
                        return ";
                    }

                    int j = 2;
                    [Test]
                    public void Test1()
                    {
                        var NAME = "SOMETEXT";
                        {|Rename:NewMethod|}();

                        void NewMethod()
                        {
                            Assert.AreEqual(string.Format(NAME, 0, 0), SomeOtherMethod(j));
                        }
                    }
                }
                """;
            await TestAsync(code, expected, TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp7), index: CodeActionIndex);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractMethod)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55031")]
        public async Task TestExtractRangeVariable_CSharp7()
        {
            var code = """
                using System.Linq;

                public class Class
                {
                    public void M()
                    {
                        _ = from a in new object[0]
                            select [|a.ToString()|];
                    }
                }
                """;

            var expected = """
                using System.Linq;

                public class Class
                {
                    public void M()
                    {
                        _ = from a in new object[0]
                            select {|Rename:NewMethod|}(a);

                        string NewMethod(object a)
                        {
                            return a.ToString();
                        }
                    }
                }
                """;
            await TestAsync(code, expected, TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp7), index: CodeActionIndex);
        }

        [Fact]
        public async Task TestExtractLocalFunction_MissingInBaseInitializer()
        {
            var code = """
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
                """;

            await TestMissingAsync(code, codeActionIndex: CodeActionIndex);
        }

        [Fact]
        public async Task TestExtractLocalFunction_MissingInThisInitializer()
        {
            var code = """
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
                """;

            await TestMissingAsync(code, codeActionIndex: CodeActionIndex);
        }

        [Fact]
        public async Task TestExtractLocalFunction_LambdaBlockInitializer()
        {
            var code = """
                class C
                {
                    public C(int y)
                        : this(y, (x) =>
                        {
                            return [|x + 1|];
                        })
                    {
                    }
                
                    public C(int x, System.Func<int, int> modX)
                    {
                    }
                }
                """;

            var expected = """
                class C
                {
                    public C(int y)
                        : this(y, (x) =>
                        {
                            return {|Rename:NewMethod|}(x);

                            static int NewMethod(int x)
                            {
                                return x + 1;
                            }
                        })
                    {
                    }
                
                    public C(int x, System.Func<int, int> modX)
                    {
                    }
                }
                """;

            await TestAsync(code, expected, TestOptions.Regular, index: CodeActionIndex);
        }
    }
}
