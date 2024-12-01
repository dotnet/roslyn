// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseDeconstruction;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseDeconstruction;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseDeconstructionDiagnosticAnalyzer,
    CSharpUseDeconstructionCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseDeconstruction)]
public class UseDeconstructionTests
{
    [Fact]
    public async Task TestVar()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    var [|t1|] = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    var (name, age) = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotIfNameInInnerScope()
    {
        var code = """
            class C
            {
                void M()
                {
                    var t1 = GetPerson();
                    {
                        int age;
                    }
                }

                (string name, int age) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotIfNameInOuterScope()
    {
        var code = """
            class C
            {
                int age;

                void M()
                {
                    var t1 = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestUpdateReference()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    var [|t1|] = GetPerson();
                    System.Console.WriteLine(t1.name + " " + t1.age);
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    var (name, age) = GetPerson();
                    System.Console.WriteLine(name + " " + age);
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestTupleType()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    (string name, int age) [|t1|] = GetPerson();
                    System.Console.WriteLine(t1.name + " " + t1.age);
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    (string name, int age) = GetPerson();
                    System.Console.WriteLine(name + " " + age);
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestVarInForEach()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    foreach (var [|t1|] in GetPeople())
                        System.Console.WriteLine(t1.name + " " + t1.age);
                }

                IEnumerable<(string name, int age)> GetPeople() => default;
            }
            """, """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    foreach (var (name, age) in GetPeople())
                        System.Console.WriteLine(name + " " + age);
                }

                IEnumerable<(string name, int age)> GetPeople() => default;
            }
            """);
    }

    [Fact]
    public async Task TestTupleTypeInForEach()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    foreach ((string name, int age) [|t1|] in GetPeople())
                        System.Console.WriteLine(t1.name + " " + t1.age);
                }

                IEnumerable<(string name, int age)> GetPeople() => default;
            }
            """, """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    foreach ((string name, int age) in GetPeople())
                        System.Console.WriteLine(name + " " + age);
                }

                IEnumerable<(string name, int age)> GetPeople() => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixAll1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    var [|t1|] = GetPerson();
                    var [|t2|] = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    var (name, age) = GetPerson();
                    var t2 = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixAll2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    var [|t1|] = GetPerson();
                }

                void M2()
                {
                    var [|t2|] = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    var (name, age) = GetPerson();
                }

                void M2()
                {
                    var (name, age) = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixAll3()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    (string name1, int age1) [|t1|] = GetPerson();
                    (string name2, int age2) [|t2|] = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    (string name1, int age1) = GetPerson();
                    (string name2, int age2) = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestFixAll4()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    (string name, int age) [|t1|] = GetPerson();
                    (string name, int age) [|t2|] = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    (string name, int age) = GetPerson();
                    (string name, int age) t2 = GetPerson();
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotIfDefaultTupleNameWithVar()
    {
        var code = """
            class C
            {
                void M()
                {
                    var t1 = GetPerson();
                }

                (string, int) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestWithUserNamesThatMatchDefaultTupleNameWithVar1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    var [|t1|] = GetPerson();
                }

                (string Item1, int Item2) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    var (Item1, Item2) = GetPerson();
                }

                (string Item1, int Item2) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithUserNamesThatMatchDefaultTupleNameWithVar2()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    var [|t1|] = GetPerson();
                    System.Console.WriteLine(t1.Item1);
                }

                (string Item1, int Item2) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    var (Item1, Item2) = GetPerson();
                    System.Console.WriteLine(Item1);
                }

                (string Item1, int Item2) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestNotIfDefaultTupleNameWithTupleType()
    {
        var code = """
            class C
            {
                void M()
                {
                    (string, int) t1 = GetPerson();
                }

                (string, int) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotIfTupleIsUsed()
    {
        var code = """
            class C
            {
                void M()
                {
                    var t1 = GetPerson();
                    System.Console.WriteLine(t1);
                }

                (string name, int age) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotIfTupleMethodIsUsed()
    {
        var code = """
            class C
            {
                void M()
                {
                    var t1 = GetPerson();
                    System.Console.WriteLine(t1.ToString());
                }

                (string name, int age) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotIfTupleDefaultElementNameUsed()
    {
        var code = """
            class C
            {
                void M()
                {
                    var t1 = GetPerson();
                    System.Console.WriteLine(t1.Item1);
                }

                (string name, int age) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestNotIfTupleRandomNameUsed()
    {
        var code = """
            class C
            {
                void M()
                {
                    var t1 = GetPerson();
                    System.Console.WriteLine(t1.{|CS1061:Unknown|});
                }

                (string name, int age) GetPerson() => default;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    /*1*/(/*2*/string/*3*/ name, /*4*/int/*5*/ age)/*6*/ [|t1|] = GetPerson();
                    System.Console.WriteLine(/*7*/t1.name/*8*/ + " " + /*9*/t1.age/*10*/);
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    /*1*/(/*2*/string/*3*/ name, /*4*/int/*5*/ age)/*6*/ = GetPerson();
                    System.Console.WriteLine(/*7*/name/*8*/ + " " + /*9*/age/*10*/);
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25260")]
    public async Task TestNotWithDefaultLiteralInitializer()
    {
        await new VerifyCS.Test()
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        (string name, int age) person = default;
                        System.Console.WriteLine(person.name + " " + person.age);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp7_1
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithDefaultExpressionInitializer()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    (string name, int age) [|person|] = default((string, int));
                    System.Console.WriteLine(person.name + " " + person.age);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    (string name, int age) = default((string, int));
                    System.Console.WriteLine(name + " " + age);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithImplicitConversionFromNonTuple()
    {
        var code = """
            class C
            {
                class Person
                {
                    public static implicit operator (string, int)(Person person) => default;
                }

                void M()
                {
                    (string name, int age) person = new Person();
                    System.Console.WriteLine(person.name + " " + person.age);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestWithExplicitImplicitConversionFromNonTuple()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                class Person
                {
                    public static implicit operator (string, int)(Person person) => default;
                }

                void M()
                {
                    (string name, int age) [|person|] = ((string, int))new Person();
                    System.Console.WriteLine(person.name + " " + person.age);
                }
            }
            """, """
            class C
            {
                class Person
                {
                    public static implicit operator (string, int)(Person person) => default;
                }

                void M()
                {
                    (string name, int age) = ((string, int))new Person();
                    System.Console.WriteLine(name + " " + age);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithImplicitConversionFromNonTupleInForEach()
    {
        var code = """
            class C
            {
                class Person
                {
                    public static implicit operator (string, int)(Person person) => default;
                }

                void M()
                {
                    foreach ((string name, int age) person in new Person[] { })
                        System.Console.WriteLine(person.name + " " + person.age);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestWithExplicitImplicitConversionFromNonTupleInForEach()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System.Linq;
            class C
            {
                class Person
                {
                    public static implicit operator (string, int)(Person person) => default;
                }

                void M()
                {
                    foreach ((string name, int age) [|person|] in new Person[] { }.Cast<(string, int)>())
                        System.Console.WriteLine(person.name + " " + person.age);
                }
            }
            """, """
            using System.Linq;
            class C
            {
                class Person
                {
                    public static implicit operator (string, int)(Person person) => default;
                }

                void M()
                {
                    foreach ((string name, int age) in new Person[] { }.Cast<(string, int)>())
                        System.Console.WriteLine(name + " " + age);
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithTupleLiteralConversion()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    (object name, double age) [|person|] = (null, 0);
                    System.Console.WriteLine(person.name + " " + person.age);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    (object name, double age) = (null, 0);
                    System.Console.WriteLine(name + " " + age);
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithImplicitTupleConversion()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    (object name, double age) [|person|] = GetPerson();
                    System.Console.WriteLine(person.name + " " + person.age);
                }

                (string name, int age) GetPerson() => default;
            }
            """, """
            class C
            {
                void M()
                {
                    (object name, double age) = GetPerson();
                    System.Console.WriteLine(name + " " + age);
                }

                (string name, int age) GetPerson() => default;
            }
            """);
    }

    [Fact]
    public async Task TestWithImplicitTupleConversionInForEach()
    {
        await VerifyCS.VerifyCodeFixAsync("""
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    foreach ((object name, double age) [|person|] in GetPeople())
                        System.Console.WriteLine(person.name + " " + person.age);
                }

                IEnumerable<(string name, int age)> GetPeople() => default;
            }
            """, """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    foreach ((object name, double age) in GetPeople())
                        System.Console.WriteLine(name + " " + age);
                }

                IEnumerable<(string name, int age)> GetPeople() => default;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27251")]
    public async Task TestEscapedContextualKeywordAsTupleName()
    {
        await new VerifyCS.Test()
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    void M()
                    {
                        var collection = new List<(int position, int @delegate)>();
                        foreach (var [|item|] in collection)
                        {
                            // Do something
                        }
                    }

                    IEnumerable<(string name, int age)> GetPeople() => default;
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                class C
                {
                    void M()
                    {
                        var collection = new List<(int position, int @delegate)>();
                        foreach (var (position, @delegate) in collection)
                        {
                            // Do something
                        }
                    }

                    IEnumerable<(string name, int age)> GetPeople() => default;
                }
                """,
            CodeActionValidationMode = Testing.CodeActionValidationMode.None
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42770")]
    public async Task TestPreserveAwait()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                class Program
                {
                    static async Task Main(string[] args)
                    {
                        {|CS7014:[Goo]|}
                        await foreach (var [|t|] in Sequence())
                        {
                            Console.WriteLine(t.x + t.y);
                        }
                    }

                    static async IAsyncEnumerable<(int x, int y)> Sequence()
                    {
                        yield return (0, 0);
                        await Task.Yield();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;

                class Program
                {
                    static async Task Main(string[] args)
                    {
                        {|CS7014:[Goo]|}
                        await foreach (var (x, y) in Sequence())
                        {
                            Console.WriteLine(x + y);
                        }
                    }

                    static async IAsyncEnumerable<(int x, int y)> Sequence()
                    {
                        yield return (0, 0);
                        await Task.Yield();
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66994")]
    public async Task TestTopLevelDeconstruct1()
    {
        await new VerifyCS.Test()
        {
            TestCode = """
                (int A, int B) ints = (1, 1);
                M(ints);

                void M((int, int) i)
                {

                }
                """,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66994")]
    public async Task TestTopLevelDeconstruct2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                (int A, int B) [|ints|] = (1, 1);
                M(ints.A, ints.B);

                void M(int x, int y)
                {

                }
                """,
            FixedCode = """
                (int A, int B) = (1, 1);
                M(A, B);

                void M(int x, int y)
                {

                }
                """,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
            FixedState =
            {
                OutputKind = OutputKind.ConsoleApplication
            },
            LanguageVersion = LanguageVersion.CSharp9
        }.RunAsync();
    }
}
