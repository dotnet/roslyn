// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.PopulateSwitch;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch;

[Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
public sealed partial class PopulateSwitchExpressionTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpPopulateSwitchExpressionDiagnosticAnalyzer(), new CSharpPopulateSwitchExpressionCodeFixProvider());

    [Fact]
    public Task NotOnRangeToken()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = [||]e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            _ => 3,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task AllMembersAndDefaultExist()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                            _ => 4,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task AllMembersExist_NotDefault()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task AllMembersExist_NotDefault_NoComma()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                            _ => throw new System.NotImplementedException()
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task NotAllMembersExist_NotDefault()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task NotAllMembersExist_WithDefault()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            _ => 3,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => 3,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task NotAllMembersExist_NotDefault_EnumHasExplicitType()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum : long
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum : long
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task NotAllMembersExist_WithMembersAndDefaultInSection_NewValuesAboveDefaultSection()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            _ => 3,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => 3,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task NotAllMembersExist_WithMembersAndDefaultInSection_AssumesDefaultIsInLastSection()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            _ => 1,
                            MyEnum.Fizz => 2,
                            MyEnum.Buzz => 3,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            _ => 1,
                            MyEnum.Fizz => 2,
                            MyEnum.Buzz => 3,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task NoMembersExist0()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => throw new System.NotImplementedException(),
                            MyEnum.Buzz => throw new System.NotImplementedException(),
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 0);

    [Fact]
    public Task NoMembersExist1()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 1);

    [Fact]
    public Task NoMembersExist2()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => throw new System.NotImplementedException(),
                            MyEnum.Buzz => throw new System.NotImplementedException(),
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task UsingStaticEnum_AllMembersExist()
        => TestMissingInRegularAndScriptAsync(
            """
            using static System.IO.FileMode;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = Append;
                        _ = e [||]switch
                        {
                            CreateNew => 1,
                            Create => 2,
                            Open => 3,
                            OpenOrCreate => 4,
                            Truncate => 5,
                            Append => 6,
                            _ => 7,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task UsingStaticEnum_AllMembersExist_OutOfDefaultOrder()
        => TestMissingInRegularAndScriptAsync(
            """
            using static System.IO.FileMode;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = Append;
                        _ = e [||]switch
                        {
                            CreateNew => 1,
                            OpenOrCreate => 2,
                            Truncate => 3,
                            Open => 4,
                            Append => 5,
                            Create => 6,
                            _ => 7,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task UsingStaticEnum_MembersExist()
        => TestInRegularAndScriptAsync(
            """
            using static System.IO.FileMode;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = Append;
                        _ = e [||]switch
                        {
                            CreateNew => 1,
                            Create => 2,
                            Open => 3,
                            OpenOrCreate => 4,
                            _ => 5,
                        };
                    }
                }
            }
            """,
            """
            using static System.IO.FileMode;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = Append;
                        _ = e switch
                        {
                            CreateNew => 1,
                            Create => 2,
                            Open => 3,
                            OpenOrCreate => 4,
                            Truncate => throw new System.NotImplementedException(),
                            Append => throw new System.NotImplementedException(),
                            _ => 5,
                        };
                    }
                }
            }
            """);

    [Fact]
    public Task UsingStaticEnum_NoMembersExist()
        => TestInRegularAndScriptAsync(
            """
            using static System.IO.FileMode;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = Append;
                        _ = e [||]switch
                        {
                        };
                    }
                }
            }
            """,
            """
            using static System.IO.FileMode;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = Append;
                        _ = e switch
                        {
                            CreateNew => throw new System.NotImplementedException(),
                            Create => throw new System.NotImplementedException(),
                            Open => throw new System.NotImplementedException(),
                            OpenOrCreate => throw new System.NotImplementedException(),
                            Truncate => throw new System.NotImplementedException(),
                            Append => throw new System.NotImplementedException(),
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task NotAllMembersExist_NotDefault_EnumHasNonFlagsAttribute()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                [System.Obsolete]
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                [System.Obsolete]
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task NotAllMembersExist_NotDefault_EnumIsNested()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                class MyClass
                {
                    enum MyEnum
                    {
                        Fizz,
                        Buzz,
                        FizzBuzz
                    }

                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e [||]switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                        };
                    }
                }
            }
            """,
            """
            namespace ConsoleApplication1
            {
                class MyClass
                {
                    enum MyEnum
                    {
                        Fizz,
                        Buzz,
                        FizzBuzz
                    }

                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task NotAllMembersExist_SwitchIsNotEnum()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace ConsoleApplication1
            {
                class MyClass
                {
                    void Method()
                    {
                        var e = "test";
                        _ = e [||]switch
                        {
                            "test1" => 1,
                            "test2" => 2,
                            _ => 3,
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task NotAllMembersExist_NotDefault_UsingConstants()
        => TestInRegularAndScriptAsync(
            """
            enum MyEnum
            {
                Fizz,
                Buzz,
                FizzBuzz
            }

            class MyClass
            {
                void Method()
                {
                    var e = MyEnum.Fizz;
                    _ = e [||]switch
                    {
                        (MyEnum)0 => 1,
                        (MyEnum)1 => 2,
                    }
                }
            }
            """,
            """
            enum MyEnum
            {
                Fizz,
                Buzz,
                FizzBuzz
            }

            class MyClass
            {
                void Method()
                {
                    var e = MyEnum.Fizz;
                    _ = e switch
                    {
                        (MyEnum)0 => 1,
                        (MyEnum)1 => 2,
                        MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                        _ => throw new System.NotImplementedException(),
                    }
                }
            }
            """, index: 2);

    [Fact]
    public Task NotAllMembersExist_NotDefault_WithMismatchingConstantType()
        => TestInRegularAndScriptAsync(
            """
            enum MyEnum
            {
                Fizz,
                Buzz,
                FizzBuzz
            }

            class MyClass
            {
                void Method()
                {
                    var e = MyEnum.Fizz;
                    _ = e [||]switch
                    {
                        (MyEnum)0 => 1,
                        (MyEnum)1 => 2,
                        "Mismatching constant" => 3,
                    }
                }
            }
            """,
            """
            enum MyEnum
            {
                Fizz,
                Buzz,
                FizzBuzz
            }

            class MyClass
            {
                void Method()
                {
                    var e = MyEnum.Fizz;
                    _ = e switch
                    {
                        (MyEnum)0 => 1,
                        (MyEnum)1 => 2,
                        "Mismatching constant" => 3,
                        MyEnum.FizzBuzz => throw new System.NotImplementedException(),
                    }
                }
            }
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/40399")]
    public Task AllMissingTokens()
        => TestInRegularAndScriptAsync(
        """
        enum MyEnum
        {
            Fizz
        }
        class MyClass
        {
            void Method()
            {
                var e = MyEnum.Fizz;
                _ = e [||]switch
            }
        }
        """,
        """
        enum MyEnum
        {
            Fizz
        }
        class MyClass
        {
            void Method()
            {
                var e = MyEnum.Fizz;
                _ = e switch
                {
                    MyEnum.Fizz => throw new System.NotImplementedException(),
                };
            }
        }
        """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40240")]
    public Task TestAddMissingCasesForNullableEnum()
        => TestInRegularAndScriptAsync(
            """
            public class Program
            {
                void Main() 
                {
                    var bar = Bar.Option1;
                    var b = bar [||]switch
                    {
                        Bar.Option1 => 1,
                        Bar.Option2 => 2,
                        null => null,
                    };
                }

                public enum Bar
            {
                Option1, 
                Option2, 
                Option3,
            }
            }
            """,
            """
            public class Program
            {
                void Main() 
                {
                    var bar = Bar.Option1;
                    var b = bar switch
                    {
                        Bar.Option1 => 1,
                        Bar.Option2 => 2,
                        null => null,
                        Bar.Option3 => throw new System.NotImplementedException(),
                    };
                }

                public enum Bar
            {
                Option1, 
                Option2, 
                Option3,
            }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public Task TestOrPatternIsHandled()
        => TestInRegularAndScriptAsync(
            """
            public static class C
            {
                static bool IsValidValue(E e) 
                {
                    return e [||]switch
                    {
                        E.A or E.B or E.C => true,
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """,
            """
            public static class C
            {
                static bool IsValidValue(E e) 
                {
                    return e [||]switch
                    {
                        E.A or E.B or E.C => true,
                        E.D => throw new System.NotImplementedException(),
                        E.E => throw new System.NotImplementedException(),
                        E.F => throw new System.NotImplementedException(),
                        E.G => throw new System.NotImplementedException(),
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public Task TestOrPatternIsHandled_AllEnumValuesAreHandled_NoDiagnostic()
        => TestMissingInRegularAndScriptAsync(
            """
            public static class C
            {
                static bool IsValidValue(E e) 
                {
                    return e [||]switch
                    {
                        (E.A or E.B) or (E.C or E.D) => true,
                        (E.E or E.F) or (E.G) => true,
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public Task TestMixingOrWithAndPatterns()
        => TestInRegularAndScriptAsync(
            """
            public static class C
            {
                static bool M(E e) 
                {
                    return e [||]switch
                    {
                        (E.A or E.B) and (E.C or E.D) => true,
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """,
            """
            public static class C
            {
                static bool M(E e) 
                {
                    return e [||]switch
                    {
                        (E.A or E.B) and (E.C or E.D) => true,
                        E.A => throw new System.NotImplementedException(),
                        E.B => throw new System.NotImplementedException(),
                        E.C => throw new System.NotImplementedException(),
                        E.D => throw new System.NotImplementedException(),
                        E.E => throw new System.NotImplementedException(),
                        E.F => throw new System.NotImplementedException(),
                        E.G => throw new System.NotImplementedException(),
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """
);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public Task TestMixingOrWithAndPatterns2()
        => TestInRegularAndScriptAsync(
            """
            public static class C
            {
                static bool M(E e) 
                {
                    return e [||]switch
                    {
                        (E.A or E.B) or (E.C and E.D) => true,
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """,
            """
            public static class C
            {
                static bool M(E e) 
                {
                    return e [||]switch
                    {
                        (E.A or E.B) or (E.C and E.D) => true,
                        E.C => throw new System.NotImplementedException(),
                        E.D => throw new System.NotImplementedException(),
                        E.E => throw new System.NotImplementedException(),
                        E.F => throw new System.NotImplementedException(),
                        E.G => throw new System.NotImplementedException(),
                        _ = false
                    };
                }

                public enum E
                {
                    A,
                    B,
                    C,
                    D,
                    E,
                    F,
                    G,
                }
            }
            """
);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public Task NotOnOrPatternWhichAlwaysSucceeds1()
        => TestMissingInRegularAndScriptAsync(
            """
            enum Greeting
            {
                Hello,
                Goodbye
            };

            class C
            {
                void M()
                {
                    Greeting greeting = Greeting.Hello;
                    string message = greeting [||]switch
                    {
                        Greeting.Hello => "Hey!",
                        Greeting.Goodbye or _ => "Not sure what to say 🤔"
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public Task NotOnOrPatternWhichAlwaysSucceeds2()
        => TestMissingInRegularAndScriptAsync(
            """
            enum Greeting
            {
                Hello,
                Goodbye
            };

            class C
            {
                void M()
                {
                    Greeting greeting = Greeting.Hello;
                    string message = greeting [||]switch
                    {
                        Greeting.Hello => "Hey!",
                        _ or Greeting.Goodbye => "Not sure what to say 🤔"
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public Task NotOnOrPatternWhichAlwaysSucceeds3()
        => TestMissingInRegularAndScriptAsync(
            """
            enum Greeting
            {
                Hello,
                Goodbye
            };

            class C
            {
                void M()
                {
                    Greeting greeting = Greeting.Hello;
                    string message = greeting [||]switch
                    {
                        Greeting.Hello => "Hey!",
                        Greeting.Goodbye => "Bye!",
                        _ and var v => "Not sure what to say 🤔"
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public Task NotOnOrPatternWhichAlwaysSucceeds4()
        => TestMissingInRegularAndScriptAsync(
            """
            enum Greeting
            {
                Hello,
                Goodbye
            };

            class C
            {
                void M()
                {
                    Greeting greeting = Greeting.Hello;
                    string message = greeting [||]switch
                    {
                        Greeting.Hello => "Hey!",
                        Greeting.Goodbye => "Bye!",
                        var x and var y => "Not sure what to say 🤔"
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public Task TestForNullableEnum_NullableEnabled()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue [||]switch
                    {
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable enable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue switch
                    {
                        MyEnum.Value1 => throw new System.NotImplementedException(),
                        MyEnum.Value2 => throw new System.NotImplementedException(),
                        null => throw new System.NotImplementedException(),
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public Task TestForNullableEnum_NullableEnabled_NotGenerateNullArmIfItAlreadyExists()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue [||]switch
                    {
                        null => throw null,
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable enable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue switch
                    {
                        null => throw null,
                        MyEnum.Value1 => throw new System.NotImplementedException(),
                        MyEnum.Value2 => throw new System.NotImplementedException(),
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public Task TestForNullableEnum_NullableDisabled()
        => TestInRegularAndScriptAsync(
            """
            #nullable disable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue [||]switch
                    {
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable disable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue switch
                    {
                        MyEnum.Value1 => throw new System.NotImplementedException(),
                        MyEnum.Value2 => throw new System.NotImplementedException(),
                        null => throw new System.NotImplementedException(),
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public Task TestForNullableEnum_NullableDisabled_NotGenerateNullArmIfItAlreadyExists()
        => TestInRegularAndScriptAsync(
            """
            #nullable disable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue [||]switch
                    {
                        null => throw null,
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable disable

            static class MyEnumExtensions
            {
                public static string ToAnotherEnum(this MyEnum? myEnumValue)
                    => myEnumValue switch
                    {
                        null => throw null,
                        MyEnum.Value1 => throw new System.NotImplementedException(),
                        MyEnum.Value2 => throw new System.NotImplementedException(),
                    };
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public Task NotOnCompleteBoolean1()
        => TestMissingAsync(
            """
            public class Sample
            {
                public string Method(bool boolean)
                {
                    return boolean [||]switch
                    {
                        true => "true",
                        false => "false",
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public Task NotOnCompleteBoolean2()
        => TestMissingAsync(
            """
            public class Sample
            {
                public string Method(bool? boolean)
                {
                    return boolean [||]switch
                    {
                        true => "true",
                        false => "false",
                        null => "null",
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public Task OnIncompleteBoolean1()
        => TestInRegularAndScriptAsync(
            """
            public class Sample
            {
                public string Method(bool boolean)
                {
                    return boolean [||]switch
                    {
                        true => "true",
                    };
                }
            }
            """,
            """
            public class Sample
            {
                public string Method(bool boolean)
                {
                    return boolean switch
                    {
                        true => "true",
                        _ => throw new System.NotImplementedException(),
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public Task OnIncompleteBoolean2()
        => TestInRegularAndScriptAsync(
            """
            public class Sample
            {
                public string Method(bool? boolean)
                {
                    return boolean [||]switch
                    {
                        true => "true",
                        false => "false",
                    };
                }
            }
            """,
            """
            public class Sample
            {
                public string Method(bool? boolean)
                {
                    return boolean switch
                    {
                        true => "true",
                        false => "false",
                        _ => throw new System.NotImplementedException(),
                    };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("int")]
    [InlineData("int i")]
    public Task NullableValueTypeWithNullAndUnderlyingValueArms1(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(int? x)
                {
                    return x [||]switch
                    {
                        null => -1,
                        {{underlyingTypePattern}} => 0,
                    };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("int")]
    [InlineData("int i")]
    public Task NullableValueTypeWithNullAndUnderlyingValueArms2(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(int? x)
                {
                    return x [||]switch
                    {
                        {{underlyingTypePattern}} => 0,
                        null => -1,
                    };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("int")]
    [InlineData("int i")]
    public Task NullableValueTypeWithNullAndUnderlyingValueArms3(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(int? x)
                {
                    return x [||]switch
                    {
                        null => -1,
                        0 => 0,
                        {{underlyingTypePattern}} => 1,
                    };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("string")]
    [InlineData("string s")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueArms1(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(string? x)
                {
                    return x [||]switch
                    {
                        null => -1,
                        {{underlyingTypePattern}} => 0,
                    };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("string")]
    [InlineData("string s")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueArms2(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(string? x)
                {
                    return x [||]switch
                    {
                        {{underlyingTypePattern}} => 0,
                        null => -1,
                    };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("string")]
    [InlineData("string s")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueArms3(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(string? x)
                {
                    return x [||]switch
                    {
                        null => -1,
                        "" => 0,
                        {{underlyingTypePattern}} => 1,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74977")]
    public Task TestMissingWithExhaustiveSwitchExpression1()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                void M(bool showDiagnosticMessages, string assemblyDisplayName, bool noColor)
                {
                    var prefix = (showDiagnosticMessages, assemblyDisplayName, noColor) [||]switch
                    {
                        (false, _, _) => null,
                        (true, null, false) => "",
                        (true, null, true) => "[D]",
                        (true, _, false) => "No color output",
                        (true, _, true) => "Color output enabled",
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74977")]
    public Task TestMissingWithExhaustiveSwitchExpression2()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                uint[] GetUnlockableRows(uint row1, uint row2)
                {
                    return (row1, row2) [||]switch
                    {
                        (0, _) => [],
                        (uint i, 0) => [i],
                        (uint i, uint j) => [i, j],
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60784")]
    public Task TestMissingWithExhaustiveSwitchExpression3()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                void GetUnlockableRows(bool FirstBoolean, bool SecondBoolean)
                {
                    var v = (FirstBoolean, SecondBoolean) [||]switch
                    {
                        (true, true) => 1,
                        (true, false) => 2,
                        (false, true) => 3,
                        (false, false) => 4
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66433")]
    public Task TestMissingWithExhaustiveSwitchExpression4()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                static string TestSwitch(int goo)
                    => goo [||]switch
                    {
                        0 => "zero",
                        < 0 => "negative",
                        > 0 => "positive",
                    };
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76080")]
    public Task TestMissingWithExhaustiveSwitchExpression5()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                static void M()
                {
                    char c = 'x';
                    bool i = false;

                    var x = (c, i) [||]switch
                    {
                        ('L', _) => '#',
                        ('#', true) => 'L',
                        ('#', false) => 'M',
                        (var z, _) => z,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76080")]
    public Task TestMissingWithExhaustiveSwitchExpression6()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                static void M()
                {
                    char c = 'x';
                    bool i = false;

                    var y = (c, i) [||]switch
                    {
                        ('L', _) => '#',
                        ('#', true) => 'L',
                        ('#', false) => 'M',
                        (var z, _) => z,
                        _ => ' ',
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57523")]
    public Task TestMissingWithExhaustiveSwitchExpression7()
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                public static string ToApiString(this bool? value)
                    => value [||]switch
                    {
                        { } val => val.ToApiString(),
                        null => "Unknown",
                    };
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57523")]
    public Task TestMissingWithExhaustiveSwitchExpression8()
        => TestMissingInRegularAndScriptAsync($$"""
            public class Program
            {
                public static int Main() => GetCuts(Shape.Circle) is null ? -1 : 0;

                static int? GetCuts(Shape? shape)
                    => shape [||]switch
                    {
                        Shape.Triangle => 3,
                        Shape.Square or Shape.Rectangle => 4,
                        Shape.Circle => 0,
                        _ => throw new System.NotSupportedException(),
                    };
            }

            public enum Shape
            {
                Circle,
                Square,
                Rectangle,
                Triangle,
            }
            """);
}
