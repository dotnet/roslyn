// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.PopulateSwitch;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch;

[Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
public partial class PopulateSwitchExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public PopulateSwitchExpressionTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpPopulateSwitchExpressionDiagnosticAnalyzer(), new CSharpPopulateSwitchExpressionCodeFixProvider());

    [Fact]
    public async Task NotOnRangeToken()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task AllMembersAndDefaultExist()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task AllMembersExist_NotDefault()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task AllMembersExist_NotDefault_NoComma()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_NotDefault()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_WithDefault()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_NotDefault_EnumHasExplicitType()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_WithMembersAndDefaultInSection_NewValuesAboveDefaultSection()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_WithMembersAndDefaultInSection_AssumesDefaultIsInLastSection()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NoMembersExist0()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NoMembersExist1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NoMembersExist2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task UsingStaticEnum_AllMembersExist()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task UsingStaticEnum_AllMembersExist_OutOfDefaultOrder()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task UsingStaticEnum_MembersExist()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task UsingStaticEnum_NoMembersExist()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_NotDefault_EnumHasNonFlagsAttribute()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_NotDefault_EnumIsNested()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_SwitchIsNotEnum()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task NotAllMembersExist_NotDefault_UsingConstants()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task NotAllMembersExist_NotDefault_WithMismatchingConstantType()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/40399")]
    public async Task AllMissingTokens()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40240")]
    public async Task TestAddMissingCasesForNullableEnum()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public async Task TestOrPatternIsHandled()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public async Task TestOrPatternIsHandled_AllEnumValuesAreHandled_NoDiagnostic()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public async Task TestMixingOrWithAndPatterns()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50982")]
    public async Task TestMixingOrWithAndPatterns2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public async Task NotOnOrPatternWhichAlwaysSucceeds1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public async Task NotOnOrPatternWhichAlwaysSucceeds2()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public async Task NotOnOrPatternWhichAlwaysSucceeds3()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58468")]
    public async Task NotOnOrPatternWhichAlwaysSucceeds4()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public async Task TestForNullableEnum_NullableEnabled()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public async Task TestForNullableEnum_NullableEnabled_NotGenerateNullArmIfItAlreadyExists()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public async Task TestForNullableEnum_NullableDisabled()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public async Task TestForNullableEnum_NullableDisabled_NotGenerateNullArmIfItAlreadyExists()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public async Task NotOnCompleteBoolean1()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public async Task NotOnCompleteBoolean2()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public async Task OnIncompleteBoolean1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48876")]
    public async Task OnIncompleteBoolean2()
    {
        await TestInRegularAndScript1Async(
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
    }
}
