// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryCast;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryCast)]
public sealed partial class RemoveUnnecessaryCastTests_AsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public RemoveUnnecessaryCastTests_AsTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnnecessaryCastDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryCastCodeFixProvider());

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545979")]
    public Task DoNotRemoveCastToErrorType()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    object s = ";
                    foreach (object x in ([|s as ErrorType|]))
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545146")]
    public Task ParenthesizeToKeepParseTheSame2()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class C
        {
            static void Main()
            {
                Action a = Console.WriteLine;
                ([|a as Action|])();
            }
        }
        """,

        """
        using System;

        class C
        {
            static void Main()
            {
                Action a = Console.WriteLine;
                a();
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545138")]
    public Task DoNotRemoveTypeParameterCastToObject()
        => TestMissingInRegularAndScriptAsync(
            """
            class Ð¡
            {
                void Goo<T>(T obj)
            {
                int x = (int)([|obj as object|]);
            }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545139")]
    public Task DoNotRemoveCastInIsTest()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Ð¡
            {
                static void Main()
            {
                DayOfWeek[] a = {
                };
                Console.WriteLine([|a as object|] is int[]);
            }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545142")]
    public Task DoNotRemoveCastNeedForUserDefinedOperator()
        => TestMissingInRegularAndScriptAsync(
            """
            class A
            {
                public static implicit operator A(string x)
                {
                    return new A();
                }
            }

            class Program
            {
                static void Main()
                {
                    A x = [|null as string|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545143")]
    public Task DoNotRemovePointerCast1()
        => TestMissingInRegularAndScriptAsync(
            """
            unsafe class C
            {
                static unsafe void Main()
                {
                    var x = (int)([|null as int*|]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545144")]
    public Task DoNotRemoveCastToObjectFromDelegateComparison()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Action a = Console.WriteLine;
                    Action b = Console.WriteLine;
                    Console.WriteLine(a == ([|b as object|]));
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545145")]
    public Task DoNotRemoveCastToAnonymousMethodWhenOnLeftOfAsCast()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    var x = [|delegate {
                    } as Action|]

                    as Action;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545157")]
    public Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    Goo(x => [|x as string|]);
                }

                static void Goo(Func<int, object> x)
                {
                }

                static void Goo(Func<string, object> x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
    public Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var x = [|1 as IComparable<int>|];
                    Goo(x);
                }

                static void Goo(IComparable<int> x)
                {
                }

                static void Goo(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545158")]
    public Task DoNotRemoveIdentityCastWhichAffectsOverloadResolution3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    var x = [|1 as IComparable<int>|];
                    var y = x;
                    Goo(y);
                }

                static void Goo(IComparable<int> x)
                {
                }

                static void Goo(int x)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545747")]
    public Task DoNotRemoveCastWhichChangesTypeOfInferredLocal()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void Main()
                {
                    var x = [|"" as object|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545159")]
    public Task DoNotRemoveNeededCastToIListOfObject()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void Main()
                {
                    Action<object>[] x = {
                    };
                    Goo(x);
                }

                static void Goo<T>(Action<T>[] x)
                {
                    var y = (IList<Action<object>>)([|x as IList<object>|]);
                    Console.WriteLine(y.Count);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545287"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
    public Task RemoveUnneededCastInParameterDefaultValue()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            static void M1(string i1 = [|null as string|])
            {
            }
        }
        """,

        """
        class Program
        {
            static void M1(string i1 = null)
            {
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545289")]
    public Task RemoveUnneededCastInReturnStatement()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            static string M2()
            {
                return [|"" as string|];
            }
        }
        """,

        """
        class Program
        {
            static string M2()
            {
                return "";
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public Task RemoveUnneededCastInLambda1()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = () => [|"" as string|];
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = () => "";
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public Task RemoveUnneededCastInLambda2()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = () => { return [|"" as string|]; };
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = () => { return ""; };
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public Task RemoveUnneededCastInLambda3()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = _ => { return [|"" as string|]; };
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = _ => { return ""; };
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545288")]
    public Task RemoveUnneededCastInLambda4()
        => TestInRegularAndScriptAsync(
        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = _ => [|"" as string|];
            }
        }
        """,

        """
        using System;
        class Program
        {
            static void M1()
            {
                Func<string> f1 = _ => "";
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public Task RemoveUnneededCastInConditionalExpression1()
        => TestInRegularAndScriptAsync(
        """
        class Test
        {
            public static void Main()
            {
                int b = 5;

                string f1 = (b == 5) ? [|"a" as string|] : "b" as string;
            }
        }
        """,

        """
        class Test
        {
            public static void Main()
            {
                int b = 5;

                string f1 = (b == 5) ? "a" : "b" as string;
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public Task RemoveUnneededCastInConditionalExpression2()
        => TestInRegularAndScriptAsync(
        """
        class Test
        {
            public static void Main()
            {
                int b = 5;

                string f1 = (b == 5) ? "a" as string : [|"b" as string|];
            }
        }
        """,

        """
        class Test
        {
            public static void Main()
            {
                int b = 5;

                string f1 = (b == 5) ? "a" as string : "b";
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545291")]
    public Task DoNotRemoveNeededCastInConditionalExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            class Test
            {
                public static void Main()
                {
                    int b = 5;
                    var f1 = (b == 5) ? "" : [|"" as object|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545459")]
    public Task DoNotRemoveIllegalAsCastInsideADelegateConstructor()
        => TestMissingAsync(
        """
        using System;
        class Test
        {
            delegate void D(int x);

            static void Main(string[] args)
            {
                var cd1 = new D([|M1 as Action<int>|]);
            }

            public static void M1(int i) { }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545422")]
    public Task RemoveUnneededCastInsideCaseLabel()
        => TestInRegularAndScriptAsync(
        """
        class Test
        {
            static void Main()
            {
                switch ("")
                {
                    case [|"" as string|]:
                        break;
                }
            }
        }
        """,

        """
        class Test
        {
            static void Main()
            {
                switch ("")
                {
                    case "":
                        break;
                }
            }
        }
        """);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545595")]
    [Fact(Skip = "529787")]
    public Task RemoveUnneededCastInCollectionInitializer()
        => TestInRegularAndScriptAsync(
        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                var z = new List<string> { [|"" as string|] };
            }
        }
        """,

        """
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                var z = new List<string> { "" };
            }
        }
        """);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
    [Fact(Skip = "529787")]
    public Task DoNotRemoveNecessaryCastWhichInCollectionInitializer1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class X : List<int>
            {
                void Add(object x)
                {
                    Console.WriteLine(1);
                }

                void Add(string x)
                {
                    Console.WriteLine(2);
                }

                static void Main()
                {
                    var z = new X { [|"" as object|] };
                }
            }
            """);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529787")]
    [Fact(Skip = "529787")]
    public Task DoNotRemoveNecessaryCastWhichInCollectionInitializer2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class X : List<int>
            {
                void Add(object x)
                {
                    Console.WriteLine(1);
                }

                void Add(string x)
                {
                    Console.WriteLine(2);
                }

                static void Main()
                {
                    X z = new X { [|"" as object|] };
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545607")]
    public Task RemoveUnneededCastInArrayInitializer()
        => TestInRegularAndScriptAsync(
        """
        class X
        {
            static void Goo()
            {
                string x = ";
                var s = new object[] { [|x as object|] };
            }
        }
        """,

        """
        class X
        {
            static void Goo()
            {
                string x = ";
                var s = new object[] { x };
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545608")]
    public Task DoNotRemoveNecessaryCastWithImplicitUserDefinedConversion()
        => TestMissingInRegularAndScriptAsync(
            """
            class X
            {
                static void Goo()
                {
                    X x = null;
                    object y = [|x as string|];
                }

                public static implicit operator string(X x)
                {
                    return ";
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
    public Task DoNotRemoveNecessaryCastWithImplicitConversionInThrow()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class E
            {
                public static implicit operator Exception(E e)
                {
                    return new Exception();
                }

                static void Main()
                {
                    throw [|new E() as Exception|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545981")]
    public Task DoNotRemoveNecessaryCastInThrow()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    object ex = new Exception();
                    throw [|ex as Exception|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545941")]
    public Task RemoveUnnecessaryCastInThrow()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class E
        {
            static void Main()
            {
                throw [|new Exception() as Exception|];
            }
        }
        """,

        """
        using System;

        class E
        {
            static void Main()
            {
                throw new Exception();
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545945")]
    public Task DoNotRemoveNecessaryDowncast()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void Goo(object y)
                {
                    var x = [|y as string|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545606")]
    public Task DoNotRemoveNecessaryCastFromNullToTypeParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class X
            {
                static void Goo<T, S>() where T : class, S
                {
                    S y = [|null as T|];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545744")]
    public Task DoNotRemoveNecessaryCastInImplicitlyTypedArray()
        => TestMissingInRegularAndScriptAsync(
            """
            class X
            {
                static void Goo()
                {
                    string x = ";
                    var s = new[] { [|x as object|] };
                    s[0] = 1;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545750")]
    public Task RemoveUnnecessaryCastToBaseType()
        => TestInRegularAndScriptAsync(
        """
        class X
        {
            static void Main()
            {
                var s = ([|new X() as object|]).ToString();
            }

            public override string ToString()
            {
                return ";
            }
        }
        """,

        """
        class X
        {
            static void Main()
            {
                var s = new X().ToString();
            }

            public override string ToString()
            {
                return ";
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545855")]
    public Task DoRemoveIllegalAsCastOnLambda()
        => TestMissingInRegularAndScriptAsync(
        """
        using System;
        using System.Collections.Generic;
        using System.Reflection;

        static class Program
        {
            static void Main()
            {
                FieldInfo[] fields = typeof(Exception).GetFields();
                Console.WriteLine(fields.Any([|(field => field.IsStatic) as Func<FieldInfo, bool>|]));
            }

            static bool Any<T>(this IEnumerable<T> s, Func<T, bool> predicate)
            {
                return false;
            }

            static bool Any<T>(this ICollection<T> s, Func<T, bool> predicate)
            {
                return true;
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
    public Task RemoveUnnecessaryCastInQueryExpression()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class A
        {
            int Select(Func<int, string> x) { return 1; }

            static void Main()
            {
                Console.WriteLine(from y in new A() select [|"" as string|]);
            }
        }
        """,

        """
        using System;

        class A
        {
            int Select(Func<int, string> x) { return 1; }

            static void Main()
            {
                Console.WriteLine(from y in new A() select "");
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529816")]
    public Task DoNotRemoveNecessaryCastInQueryExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class A
            {
                int Select(Func<int, string> x)
                {
                    return 1;
                }

                int Select(Func<int, object> x)
                {
                    return 2;
                }

                static void Main()
                {
                    Console.WriteLine(from y in new A()
                                      select [|"" as object|]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
    public Task DoNotRemoveNecessaryCastFromTypeParameterToInterface()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            interface IIncrementable
            {
                int Value { get; }

                void Increment();
            }

            struct S : IIncrementable
            {
                public int Value { get; private set; }

                public void Increment()
                {
                    Value++;
                }
            }

            class C : IIncrementable
            {
                public int Value { get; private set; }

                public void Increment()
                {
                    Value++;
                }
            }

            static class Program
            {
                static void Main()
                {
                    Goo(new S(), new C());
                }

                static void Goo<TAny, TClass>(TAny x, TClass y)
                    where TAny : IIncrementable
                    where TClass : class, IIncrementable
                {
                    ([|x as IIncrementable|]).Increment(); // False Unnecessary Cast
                    ((IIncrementable)y).Increment(); // Unnecessary Cast - OK

                    Console.WriteLine(x.Value);
                    Console.WriteLine(y.Value);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529831")]
    public Task RemoveUnnecessaryCastFromTypeParameterToInterface()
        => TestInRegularAndScriptAsync(
        """
        using System;

        interface IIncrementable
        {
            int Value { get; }
            void Increment();
        }

        struct S : IIncrementable
        {
            public int Value { get; private set; }
            public void Increment() { Value++; }
        }

        class C: IIncrementable
        {
            public int Value { get; private set; }
            public void Increment() { Value++; }
        }

        static class Program
        {
            static void Main()
            {
                Goo(new S(), new C());
            }

            static void Goo<TAny, TClass>(TAny x, TClass y) 
                where TAny : IIncrementable
                where TClass : class, IIncrementable
            {
                ((IIncrementable)x).Increment(); // False Unnecessary Cast
                ([|y as IIncrementable|]).Increment(); // Unnecessary Cast - OK

                Console.WriteLine(x.Value);
                Console.WriteLine(y.Value);
            }
        }
        """,
        """
         using System;

         interface IIncrementable
         {
             int Value { get; }
             void Increment();
         }

         struct S : IIncrementable
         {
             public int Value { get; private set; }
             public void Increment() { Value++; }
         }

         class C: IIncrementable
         {
             public int Value { get; private set; }
             public void Increment() { Value++; }
         }

         static class Program
         {
             static void Main()
             {
                 Goo(new S(), new C());
             }

             static void Goo<TAny, TClass>(TAny x, TClass y) 
                 where TAny : IIncrementable
                 where TClass : class, IIncrementable
             {
                 ((IIncrementable)x).Increment(); // False Unnecessary Cast
                 y.Increment(); // Unnecessary Cast - OK

                 Console.WriteLine(x.Value);
                 Console.WriteLine(y.Value);
             }
         }
         """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545877")]
    public Task DoNotCrashOnIncompleteMethodDeclaration()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class A
            {
                static void Main()
                {
                    string
                    Goo(x => 1, [|"" as string|]);
                }

                static void Goo<T, S>(T x, )
                {
                }
            }
            """,
            """
            using System;

            class A
            {
                static void Main()
                {
                    string
                    Goo(x => 1, "");
                }

                static void Goo<T, S>(T x, )
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529791")]
    public Task RemoveUnnecessaryCastToNullable1()
        => TestInRegularAndScriptAsync(
        """
        class X
        {
            static void Goo()
            {
                object x = (string)null;
                object y = [|null as int?|];
            }
        }
        """,

        """
        class X
        {
            static void Goo()
            {
                object x = (string)null;
                object y = null;
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545850")]
    public Task RemoveSurroundingParentheses()
        => TestInRegularAndScriptAsync(
        """
        class Program
        {
            static void Main()
            {
                string x = "";
                ([|x as string|]).ToString();
            }
        }
        """,

        """
        class Program
        {
            static void Main()
            {
                string x = "";
                x.ToString();
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
    public Task DoNotRemoveNecessaryCastFromTypeParameterToObject()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void Goo<T>(T x, object y)
                {
                    if (([|x as object|]) == y)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545858")]
    public Task DoNotRemoveNecessaryCastFromDelegateTypeToMulticastDelegate()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    Action x = Console.WriteLine;
                    Action y = Console.WriteLine;
                    Console.WriteLine(([|x as MulticastDelegate|]) == y);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529842")]
    public Task DoNotRemoveNecessaryCastInTernaryExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class X
            {
                public static implicit operator string(X x)
                {
                    return x.ToString();
                }

                static void Main()
                {
                    bool b = true;
                    X x = new X();
                    Console.WriteLine(b ? [|null as string|] : x);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
    public Task RemoveCastInConstructorInitializer1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                C(string x) { }
                C() : this([|"" as string|]) { }
            }
            """,

            """
            class C
            {
                C(string x) { }
                C() : this("") { }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545958"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/880752")]
    public Task RemoveCastInConstructorInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections;

            class C
            {
                C(int x) { }
                C(object x) { }
                C() : this([|"" as IEnumerable|]) { }
            }
            """,

            """
            using System.Collections;

            class C
            {
                C(int x) { }
                C(object x) { }
                C() : this("") { }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545957")]
    public Task DoNotRemoveCastInConstructorInitializer3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                C(string x)
                {
                }

                C() : this([|"" as object|])
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545842")]
    public Task RemoveCastToNullableInArithmeticExpression()
        => TestInRegularAndScriptAsync(
            """
            static class C
            {
                static void Main()
                {
                    int? x = 1;
                    long y = 2;
                    long? z = x + ([|y as long?|]);
                }
            }
            """,

            """
            static class C
            {
                static void Main()
                {
                    int? x = 1;
                    long y = 2;
                    long? z = x + y;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545942")]
    public Task DoNotRemoveCastFromStringTypeToObjectInReferenceEquality()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    object x = "";
                    Console.WriteLine(x == ([|"" as object|]));
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545962")]
    public Task DoNotRemoveCastWhenExpressionDoesntBind()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    ([|x as IDisposable|]).Dispose();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545944")]
    public Task DoNotRemoveNecessaryCastBeforePointerDereference1()
        => TestMissingInRegularAndScriptAsync(
            """
            unsafe class C
            {
                int x = *([|null as int*|]);
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545978")]
    public Task DoNotRemoveNecessaryCastBeforePointerDereference2()
        => TestMissingInRegularAndScriptAsync(
            """
            unsafe class C
            {
                static void Main()
                {
                    void* p = null;
                    int x = *([|p as int*|]);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public Task DoNotRemoveCastToByteFromIntInConditionalExpression_CSharp8()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                object M1(bool b)
                {
                    return [|b ? (1 as byte?) : (0 as byte?)|];
                }
            }
            """, new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26640")]
    public Task DoNotRemoveCastToByteFromIntInConditionalExpression_CSharp9()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                object M1(bool b)
                {
                    return [|b ? (1 as byte?) : (0 as byte?)|];
                }
            }
            """, new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    #region Interface Casts

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545889")]
    public Task DoNotRemoveCastToInterfaceForUnsealedType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class X : IDisposable
            {
                static void Main()
                {
                    X x = new Y();
                    ([|x as IDisposable|]).Dispose();
                }

                public void Dispose()
                {
                    Console.WriteLine("X.Dispose");
                }
            }

            class Y : X, IDisposable
            {
                void IDisposable.Dispose()
                {
                    Console.WriteLine("Y.Dispose");
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    public Task DoRemoveCastToInterfaceForSealedType1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            interface I
            {
                void Goo(int x = 0);
            }

            sealed class C : I
            {
                public void Goo(int x = 0)
                {
                    Console.WriteLine(x);
                }

                static void Main()
                {
                    ([|new C() as I|]).Goo();
                }
            }
            """,
            """
            using System;

            interface I
            {
                void Goo(int x = 0);
            }

            sealed class C : I
            {
                public void Goo(int x = 0)
                {
                    Console.WriteLine(x);
                }

                static void Main()
                {
                    new C().Goo();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    public Task DoRemoveCastToInterfaceForSealedType2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            interface I
            {
                string Goo { get; }
            }

            sealed class C : I
            {
                public string Goo
                {
                    get
                    {
                        return "Nikov Rules";
                    }
                }

                static void Main()
                {
                    Console.WriteLine(([|new C() as I|]).Goo);
                }
            }
            """,
            """
            using System;

            interface I
            {
                string Goo { get; }
            }

            sealed class C : I
            {
                public string Goo
                {
                    get
                    {
                        return "Nikov Rules";
                    }
                }

                static void Main()
                {
                    Console.WriteLine(new C().Goo);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    public Task DoNotRemoveCastToInterfaceForSealedType3()
        => TestMissingAsync(
            """
            using System;

            interface I
            {
                string Goo { get; }
            }

            sealed class C : I
            {
                public C Instance { get { return new C(); } }

                public string Goo
                {
                    get
                    {
                        return "Nikov Rules";
                    }
                }

                static void Main()
                {
                    Console.WriteLine(([|Instance as I|]).Goo);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    public Task DoNotRemoveCastToInterfaceForSealedType4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            interface I
            {
                void Goo(int x = 0);
            }

            sealed class C : I
            {
                public void Goo(int x = 1)
                {
                    Console.WriteLine(x);
                }

                static void Main()
                {
                    ([|new C() as I|]).Goo();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545890")]
    public Task DoRemoveCastToInterfaceForSealedTypeWhenDefaultValuesAreDifferentButParameterIsPassed()
        => TestInRegularAndScriptAsync(
            """
            using System;

            interface I
            {
                void Goo(int x = 0);
            }

            sealed class C : I
            {
                public void Goo(int x = 1)
                {
                    Console.WriteLine(x);
                }

                static void Main()
                {
                    ([|new C() as I|]).Goo(2);
                }
            }
            """,
            """
            using System;

            interface I
            {
                void Goo(int x = 0);
            }

            sealed class C : I
            {
                public void Goo(int x = 1)
                {
                    Console.WriteLine(x);
                }

                static void Main()
                {
                    new C().Goo(2);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public Task DoNotRemoveCastToInterfaceForSealedType6()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            interface I
            {
                void Goo(int x = 0, int y = 0);
            }

            sealed class C : I
            {
                public void Goo(int y = 0, int x = 0)
                {
                    Console.WriteLine(x);
                }

                static void Main()
                {
                    ([|new C() as I|]).Goo(x: 1);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public Task DoNotRemoveCastToInterfaceForSealedType7()
        => TestMissingAsync(
            """
            using System;

            interface I
            {
                int this[int x = 0, int y = 0] { get; }
            }

            sealed class C : I
            {
                public int this[int x = 0, int y = 0]
                {
                    get
                    {
                        return x * 2;
                    }
                }

                static void Main()
                {
                    Console.WriteLine(([|new C() as I|])[x: 1]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545888")]
    public Task DoNotRemoveCastToInterfaceForSealedType8()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            interface I
            {
                int this[int x = 0, int y = 0] { get; }
            }

            sealed class C : I
            {
                public int this(int y = 0, int x = 0)
                {
                    get
                    {
                        return x * 2;
                    }
                }

                static void Main()
                {
                    Console.WriteLine(([|new C() as I|])[x: 1]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545883")]
    public Task DoNotRemoveCastToInterfaceForSealedType9()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.IO;

            sealed class C : MemoryStream
            {
                static void Main()
                {
                    C s = new C();
                    ([|s as IDisposable|]).Dispose();
                }

                new public void Dispose()
                {
                    Console.WriteLine("new Dispose()");
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545887")]
    public Task DoNotRemoveCastToInterfaceForStruct1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            interface IIncrementable
            {
                int Value { get; }

                void Increment();
            }

            struct S : IIncrementable
            {
                public int Value { get; private set; }

                public void Increment()
                {
                    Value++;
                }

                static void Main()
                {
                    var s = new S();
                    ([|s as IIncrementable|]).Increment();
                    Console.WriteLine(s.Value);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545834")]
    public Task RemoveCastToInterfaceForStruct2()
        => TestInRegularAndScriptAsync(
        """
        using System;
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                ([|GetEnumerator() as IDisposable|]).Dispose();
            }

            static List<int>.Enumerator GetEnumerator()
            {
                var x = new List<int> { 1, 2, 3 };
                return x.GetEnumerator();
            }
        }
        """,

        """
        using System;
        using System.Collections.Generic;

        class Program
        {
            static void Main()
            {
                GetEnumerator().Dispose();
            }

            static List<int>.Enumerator GetEnumerator()
            {
                var x = new List<int> { 1, 2, 3 };
                return x.GetEnumerator();
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544655")]
    public Task RemoveCastToICloneableForDelegate()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class C
        {
            static void Main()
            {
                Action a = () => { };
                var c = ([|a as ICloneable|]).Clone();
            }
        }
        """,

        """
        using System;

        class C
        {
            static void Main()
            {
                Action a = () => { };
                var c = a.Clone();
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545926")]
    public Task RemoveCastToICloneableForArray()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class C
        {
            static void Main()
            {
                var a = new[] { 1, 2, 3 };
                var c = ([|a as ICloneable|]).Clone(); 
            }
        }
        """,

        """
        using System;

        class C
        {
            static void Main()
            {
                var a = new[] { 1, 2, 3 };
                var c = a.Clone(); 
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529897")]
    public Task RemoveCastToIConvertibleForEnum()
        => TestInRegularAndScriptAsync(
        """
        using System;

        class Program
        {
            static void Main()
            {
                Enum e = DayOfWeek.Monday;
                var y = ([|e as IConvertible|]).GetTypeCode();
            }
        }
        """,

        """
        using System;

        class Program
        {
            static void Main()
            {
                Enum e = DayOfWeek.Monday;
                var y = e.GetTypeCode();
            }
        }
        """);

    #endregion

    #region ParamArray Parameter Casts

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545141")]
    public Task DoNotRemoveCastToObjectInParamArrayArg1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    Goo([|null as object|]);
                }

                static void Goo(params object[] x)
                {
                    Console.WriteLine(x.Length);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task DoNotRemoveCastToIntArrayInParamArrayArg2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    Goo([|null as int[]|]);
                }

                static void Goo(params object[] x)
                {
                    Console.WriteLine(x.Length);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task DoNotRemoveCastToObjectArrayInParamArrayArg3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void Main()
                {
                    Goo([|null as object[]|]);
                }

                static void Goo(params object[][] x)
                {
                    Console.WriteLine(x.Length);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task RemoveCastToObjectArrayInParamArrayArg1()
        => TestInRegularAndScriptAsync(
        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo([|null as object[]|]);
            }
        }
        """,

        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo(null);
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task RemoveCastToStringArrayInParamArrayArg2()
        => TestInRegularAndScriptAsync(
        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo([|null as string[]|]);
            }
        }
        """,

        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo(null);
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task RemoveCastToIntArrayInParamArrayArg3()
        => TestInRegularAndScriptAsync(
        """
        class C
        {
            static void Goo(params int[] x) { }

            static void Main()
            {
                Goo([|null as int[]|]);
            }
        }
        """,

        """
        class C
        {
            static void Goo(params int[] x) { }

            static void Main()
            {
                Goo(null);
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task RemoveCastToObjectArrayInParamArrayArg4()
        => TestInRegularAndScriptAsync(
        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo([|null as object[]|], null);
            }
        }
        """,

        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo(null, null);
            }
        }
        """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529911")]
    public Task RemoveCastToObjectInParamArrayArg5()
        => TestInRegularAndScriptAsync(
        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo([|null as object|], null);
            }
        }
        """,

        """
        class C
        {
            static void Goo(params object[] x) { }

            static void Main()
            {
                Goo(null, null);
            }
        }
        """);

    [Fact]
    public Task RemoveCastToObjectArrayInParamArrayWithNamedArgument()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void Main()
                {
                    Goo(x: [|null as object[]|]);
                }

                static void Goo(params object[] x) { }
            }
            """,
            """
            class C
            {
                static void Main()
                {
                    Goo(x: null);
                }

                static void Goo(params object[] x) { }
            }
            """);

    #endregion

    #region ForEach Statements

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public Task DoNotRemoveNecessaryCastInForEach1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections;

            class Program
            {
                static void Main()
                {
                    object s = ";
                    foreach (object x in [|s as IEnumerable|])
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public Task DoNotRemoveNecessaryCastInForEach2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class Program
            {
                static void Main()
                {
                    object s = ";
                    foreach (object x in [|s as IEnumerable<char>|])
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public Task DoNotRemoveNecessaryCastInForEach3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections;

            class D
            {
                public IEnumerator GetEnumerator()
                {
                    yield return 1;
                }
            }

            class C
            {
                public static implicit operator D(C c)
                {
                    return new D();
                }

                static void Main()
                {
                    object s = ";
                    foreach (object x in [|new C() as D|])
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public Task DoNotRemoveNecessaryCastInForEach4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;

            class D
            {
                public IEnumerator GetEnumerator()
                {
                    yield return 1;
                }
            }

            class C
            {
                public IEnumerator GetEnumerator()
                {
                    yield return 2;
                }

                public static implicit operator D(C c)
                {
                    return new D();
                }

                static void Main()
                {
                    object s = ";
                    foreach (object x in [|new C() as D|])
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545961")]
    public Task DoNotRemoveNecessaryCastInForEach5()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    string[] s = {
                        "A"
                    };
                    foreach (var x in [|s as Array|])
                    {
                        var y = x;
                        y = 1;
                    }
                }
            }
            """);

    #endregion

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
    public Task DoNotRemoveCastIfOverriddenMethodHasIncompatibleParameterList()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            abstract class Y
            {
                public abstract void Goo(int x = 1);
            }

            class X : Y
            {
                static void Main()
                {
                    ([|new X() as Y|]).Goo();
                }

                public override void Goo(int x = 2)
                {
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545925")]
    public Task RemoveCastIfOverriddenMethodHaveCompatibleParameterList()
        => TestInRegularAndScriptAsync(
            """
            using System;

            abstract class Y
            {
                public abstract void Goo(int x = 1);
            }

            class X : Y
            {
                static void Main()
                {
                    ([|new X() as Y|]).Goo();
                }

                public override void Goo(int x = 1)
                {
                    Console.WriteLine(x);
                }
            }
            """,

            """
            using System;

            abstract class Y
            {
                public abstract void Goo(int x = 1);
            }

            class X : Y
            {
                static void Main()
                {
                    new X().Goo();
                }

                public override void Goo(int x = 1)
                {
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529916")]
    public Task RemoveCastInReceiverForMethodGroup()
        => TestInRegularAndScriptAsync(
            """
            using System;

            static class Program
            {
                static void Main()
                {
                    Action a = ([|"" as string|]).Goo;
                }

                static void Goo(this string x) { }
            }
            """,

            """
            using System;

            static class Program
            {
                static void Main()
                {
                    Action a = "".Goo;
                }

                static void Goo(this string x) { }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609497")]
    public Task Bugfix_609497()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                static void Main()
                {
                    Goo().Wait();
                }

                static async Task Goo()
                {
                    Task task = Task.FromResult(0);
                    Console.WriteLine(await ([|task as dynamic|]));
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624252")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608180")]
    public Task DoNotRemoveCastIfArgumentIsRestricted_TypedReference()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                }

                static void v(dynamic x)
                {
                    var y = default(TypedReference);
                    dd([|x as object|], y);
                }

                static void dd(object obj, TypedReference d)
                {
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    C<string>.InvokeGoo(0);
                }
            }

            class C<T>
            {
                public static void InvokeGoo(dynamic x)
                {
                    Console.WriteLine(Goo(x, [|"" as object|], ""));
                }

                static void Goo(int x, string y, T z)
                {
                }

                static bool Goo(int x, object y, object z)
                {
                    return true;
                }
            
                static bool Goo(long x, object y, object z)
                {
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments_Bracketed()
        => TestMissingInRegularAndScriptAsync(
            """
            class C<T>
            {
                int this[int x, T s, string d = "abc"]
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                    }
                }

                int this[int x, object s, object d]
                {
                    get
                    {
                        return 0;
                    }

                    set
                    {
                    }
                }
            
                int this[long x, object s, string d]
                {
                    get
                    {
                        return 0;
                    }
            
                    set
                    {
                    }
                }
            
                void Goo(dynamic xx)
                {
                    var y = this[x: xx, s: "", d: [|"" as object|]];
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d([|"" as object|]);
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d.goo([|"" as object|]);
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d.goo.bar.goo([|"" as object|]);
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithDynamicReceiverOpt_3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static bool Goo(dynamic d)
                {
                    d.goo().bar().goo([|"" as object|]);
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627107")]
    public Task DoNotRemoveCastOnArgumentsWithOtherDynamicArguments_1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main()
                {
                    C<string>.InvokeGoo(0);
                }
            }

            class C<T>
            {
                public static void InvokeGoo(dynamic x)
                {
                    Console.WriteLine(Goo([|"" as object|], x, ""));
                }

                static void Goo(string y, int x, T z)
                {
                }

                static bool Goo(object y, int x, object z)
                {
                    return true;
                }

                static bool Goo(object y, long x, object z)
                {
                    return true;
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529846")]
    public Task DoNotUnnecessaryCastFromTypeParameterToObject()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                static void Goo<T>(T x, object y)
                {
                    if (([|x as object|]) == y)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/640136")]
    public Task RemoveUnnecessaryCastAndParseCorrect()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo(Task<Action> x)
                {
                    (([|x as Task<Action>|]).Result)();
                }
            }
            """,

            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                void Goo(Task<Action> x)
                {
                    (x.Result)();
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/626026")]
    public Task DoNotRemoveCastIfUserDefinedExplicitCast()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    B bar = new B();
                    A a = [|bar as A|];
                }
            }

            public class A
            {
                public static explicit operator A(B b)
                {
                    return new A();
                }
            }

            public struct B
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770187")]
    public Task DoNotRemoveNecessaryCastInSwitchExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace ConsoleApplication23
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        int goo = 0;
                        switch ([|goo as E?|])
                        {
                            case E.A:
                            case E.B:
                                return;
                        }
                    }
                }

                enum E
                {
                    A,
                    B,
                    C
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2761")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844482")]
    public Task DoNotRemoveCastFromBaseToDerivedWithExplicitReference()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    C x = null;
                    C y = null;
                    y = [|x as D|];
                }
            }

            class C
            {
            }

            class D : C
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3254")]
    public Task DoNotRemoveCastToTypeParameterWithExceptionConstraint()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : Exception
                {
                    if (!condition)
                    {
                        throw [|Activator.CreateInstance(typeof(TException), messageOnFalseCondition) as TException|];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3254")]
    public Task DoNotRemoveCastToTypeParameterWithExceptionSubTypeConstraint()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                private static void RequiresCondition<TException>(bool condition, string messageOnFalseCondition) where TException : ArgumentException
                {
                    if (!condition)
                    {
                        throw [|Activator.CreateInstance(typeof(TException), messageOnFalseCondition) as TException|];
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8111")]
    public Task DoNotRemoveCastThatChangesShapeOfAnonymousTypeObject()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(object o)
                {
                    object thing = new { shouldBeAString = [|o as string|] };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8111")]
    public Task RemoveCastThatDoesntChangeShapeOfAnonymousTypeObject()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string o)
                {
                    object thing = new { shouldBeAString = [|o as string|] };
                }
            }
            """,

            """
            class Program
            {
                static void Main(string o)
                {
                    object thing = new { shouldBeAString = o };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18978")]
    public Task DoNotRemoveCastOnCallToMethodWithParamsArgs()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    var takesArgs = new[] { "Hello", "World" };
                    TakesParams([|takesArgs as object|]);
                }

                private static void TakesParams(params object[] goo)
                {
                    Console.WriteLine(goo.Length);
                }
            }
            """);

    [Fact]
    public Task DoRemoveCastOnCallToMethodWithIncorrectParamsArgs()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    TakesParams([|null as string|]);
                }

                private static void TakesParams(params string wrongDefined)
                {
                    Console.WriteLine(wrongDefined.Length);
                }
            }
            """,
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    TakesParams(null);
                }

                private static void TakesParams(params string wrongDefined)
                {
                    Console.WriteLine(wrongDefined.Length);
                }
            }
            """);

    [Fact]
    public Task DoNotRemoveCastOnCallToMethodWithCorrectParamsArgs()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    TakesParams([|null as string|]);
                }

                private static void TakesParams(params string[] wrongDefined)
                {
                    Console.WriteLine(wrongDefined.Length);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18978")]
    public Task RemoveCastOnCallToMethodWithParamsArgsIfImplicitConversionExists()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    var takesArgs = new[] { "Hello", "World" };
                    TakesParams([|takesArgs as System.IComparable[]|]);
                }

                private static void TakesParams(params object[] goo)
                {
                    System.Console.WriteLine(goo.Length);
                }
            }
            """,
            """
            class Program
            {
                public static void Main(string[] args)
                {
                    var takesArgs = new[] { "Hello", "World" };
                    TakesParams(takesArgs);
                }

                private static void TakesParams(params object[] goo)
                {
                    System.Console.WriteLine(goo.Length);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task DoNotRemoveCastOnCallToAttributeWithParamsArgs()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Reflection;

            sealed class MarkAttribute : Attribute
            {
              public readonly string[] Arr;

              public MarkAttribute(params string[] arr)
              {
                Arr = arr;
              }
            }
            [Mark([|null as string|])]   // wrong instance of: IDE0004 Cast is redundant.
            static class Program
            {
              static void Main()
              {
              }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29264")]
    public Task RemoveCastOnDictionaryIndexer()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Reflection;
            using System.Collections.Generic;

            static class Program
            {
                static void Main()
                {
                    Dictionary<string, string> Icons = new Dictionary<string, string>
                    {
                        [[|"" as string|]] = null,
                    };
                }
            }
            """,
            """
            using System;
            using System.Reflection;
            using System.Collections.Generic;

            static class Program
            {
                static void Main()
                {
                    Dictionary<string, string> Icons = new Dictionary<string, string>
                    {
                        [""] = null,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task DoNotRemoveCastOnCallToAttributeWithParamsArgsAndProperty()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(params string[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark([|null as string|], Prop = 1)] 
            static class Program
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task DoNotRemoveCastOnCallToAttributeWithParamsArgsPropertyAndOtherArg()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params string[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(true, [|null as string|], Prop = 1)] 
            static class Program
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task DoNotRemoveCastOnCallToAttributeWithParamsArgsNamedArgsAndProperty()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params string[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(arr: [|null as string|], otherArg: true, Prop = 1)]
            static class Program
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task DoRemoveCastOnCallToAttributeWithInvalidParamsArgs()
        => TestInRegularAndScriptAsync(
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params string wrongDefined)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(true, [|null as string|], Prop = 1)]
            static class Program
            {
            }
            """,
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params string wrongDefined)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(true, null, Prop = 1)]
            static class Program
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task DoNotRemoveCastOfNullToParamsArg()
        => TestMissingAsync(
            """
            using System;
            using System.Reflection;

            class MarkAttribute : Attribute
            {
              public readonly string[] Arr;

              public MarkAttribute(params string[] arr)
              {
                Arr = arr;
              }
            }

            [Mark([|(string)|]null)]
            static class Program
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20630")]
    public Task RemoveCastOnCallToAttributeWithParamsArgsWithImplicitCast()
        => TestInRegularAndScriptAsync(
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params object[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(arr: [|new[] { "Hello", "World" } as object[]|], otherArg: true, Prop = 1)]
            static class Program
            {
            }
            """,
            """
            using System;
            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool otherArg, params object[] arr)
                {
                }
                public int Prop { get; set; }
            }

            [Mark(arr: new[] { "Hello", "World" }, otherArg: true, Prop = 1)]
            static class Program
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25456#issuecomment-373549735")]
    public Task DoNotIntroduceDefaultLiteralInSwitchCase()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    switch ("")
                    {
                        case [|default as string|]:
                            break;
                    }
                }
            }
            """, parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));

    [Fact]
    public Task DoNotIntroduceDefaultLiteralInSwitchCase_CastInsideParentheses()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    switch ("")
                    {
                        case ([|default as string|]):
                            break;
                    }
                }
            }
            """, parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));

    [Fact]
    public Task DoNotIntroduceDefaultLiteralInSwitchCase_DefaultInsideParentheses()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    switch ("")
                    {
                        case [|(default) as string|]:
                            break;
                    }
                }
            }
            """, parameters: new TestParameters(new CSharpParseOptions(LanguageVersion.CSharp7_1)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27239")]
    public Task DoNotOfferToRemoveCastWhereNoConversionExists()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    object o = null;
                    TypedReference r2 = [|o as TypedReference|];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28412")]
    public Task DoNotOfferToRemoveCastWhenAccessingHiddenProperty()
        => TestMissingInRegularAndScriptAsync("""
            using System.Collections.Generic;
            class Fruit
            {
                public IDictionary<string, object> Properties { get; set; }
            }
            class Apple : Fruit
            {
                public new IDictionary<string, object> Properties { get; }
            }
            class Tester
            {
                public void Test()
                {
                    var a = new Apple();
                    ([|a as Fruit|]).Properties["Color"] = "Red";
                }
            }
            """);
}
