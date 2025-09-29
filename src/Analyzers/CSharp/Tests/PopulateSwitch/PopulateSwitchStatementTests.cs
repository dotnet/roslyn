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
public sealed partial class PopulateSwitchStatementTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpPopulateSwitchStatementDiagnosticAnalyzer(), new CSharpPopulateSwitchStatementCodeFixProvider());

    [Fact]
    public Task OnlyOnFirstToken()
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
                        switch ([||]e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            default:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            default:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.FizzBuzz:
                                break;
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            default:
                                break;
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                        }
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
                        switch (e)
                        {
                            default:
                                break;
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                        }
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
                        [||]switch (e)
                        {
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                                break;
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                        }
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
                        [||]switch (e)
                        {
                        }
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
                        switch (e)
                        {
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                                break;
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case CreateNew:
                                break;
                            case Create:
                                break;
                            case Open:
                                break;
                            case OpenOrCreate:
                                break;
                            case Truncate:
                                break;
                            case Append:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case CreateNew:
                                break;
                            case OpenOrCreate:
                                break;
                            case Truncate:
                                break;
                            case Open:
                                break;
                            case Append:
                                break;
                            case Create:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case CreateNew:
                                break;
                            case Create:
                                break;
                            case Open:
                                break;
                            case OpenOrCreate:
                                break;
                            default:
                                break;
                        }
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
                        switch (e)
                        {
                            case CreateNew:
                                break;
                            case Create:
                                break;
                            case Open:
                                break;
                            case OpenOrCreate:
                                break;
                            case Truncate:
                                break;
                            case Append:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                        }
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
                        switch (e)
                        {
                            case CreateNew:
                                break;
                            case Create:
                                break;
                            case Open:
                                break;
                            case OpenOrCreate:
                                break;
                            case Truncate:
                                break;
                            case Append:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
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
                        [||]switch (e)
                        {
                            case "test1":
                            case "test1":
                            default:
                                break;
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
                    [||]switch (e)
                    {
                        case (MyEnum)0:
                        case (MyEnum)1:
                            break;
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
                    switch (e)
                    {
                        case (MyEnum)0:
                        case (MyEnum)1:
                            break;
                        case MyEnum.FizzBuzz:
                            break;
                        default:
                            break;
                    }
                }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13455")]
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
                [||]switch (e)
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
                switch (e)
                {
                    case MyEnum.Fizz:
                        break;
                }
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
                    Bar? bar;
                    [||]switch (bar)
                    {
                        case Bar.Option1:
                            break;
                        case Bar.Option2:
                            break;
                        case null:
                            break;
                    }
                }
            }

            public enum Bar
            {
                Option1, 
                Option2, 
                Option3,
            }
            """,
            """
            public class Program
            {
                void Main() 
                {
                    Bar? bar;
                    switch (bar)
                    {
                        case Bar.Option1:
                            break;
                        case Bar.Option2:
                            break;
                        case null:
                            break;
                        case Bar.Option3:
                            break;
                    }
                }
            }

            public enum Bar
            {
                Option1, 
                Option2, 
                Option3,
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61594")]
    public Task TestForNullableEnum_NullableEnabled()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    [||]switch (myEnumValue)
                    {
                    }
                }
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable enable

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    switch (myEnumValue)
                    {
                        case MyEnum.Value1:
                            break;
                        case MyEnum.Value2:
                            break;
                        case null:
                            break;
                    }
                }
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

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    [||]switch (myEnumValue)
                    {
                        case null:
                            throw null;
                    }
                }
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable enable

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    switch (myEnumValue)
                    {
                        case null:
                            throw null;
                        case MyEnum.Value1:
                            break;
                        case MyEnum.Value2:
                            break;
                    }
                }
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

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    [||]switch (myEnumValue)
                    {
                    }
                }
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable disable

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    switch (myEnumValue)
                    {
                        case MyEnum.Value1:
                            break;
                        case MyEnum.Value2:
                            break;
                        case null:
                            break;
                    }
                }
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

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    [||]switch (myEnumValue)
                    {
                        case null:
                            throw null;
                    }
                }
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """,
            """
            #nullable disable

            static class TestClass
            {
                public static void Test(MyEnum? myEnumValue)
                {
                    switch (myEnumValue)
                    {
                        case null:
                            throw null;
                        case MyEnum.Value1:
                            break;
                        case MyEnum.Value2:
                            break;
                    }
                }
            }

            enum MyEnum
            {
                Value1, Value2
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/61809")]
    public Task TestNotInSwitchWithUnknownType1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    switch[||]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/61809")]
    public Task TestNotInSwitchWithUnknownType2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var v = null switch[||]
                }
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
                    [||]switch (boolean)
                    {
                        case true: return "true";
                        case false: "false";
                    }
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
                    [||]switch (boolean)
                    {
                        case true: return "true";
                        case false: return "false";
                        case null: return "null";
                    }
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
                    [||]switch (boolean)
                    {
                        case true: return "true";
                    }
                }
            }
            """,
            """
            public class Sample
            {
                public string Method(bool boolean)
                {
                    switch (boolean)
                    {
                        case true: return "true";
                        default:
                            break;
                    }
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
                    [||]switch (boolean)
                    {
                        case true: return "true";
                        case false: return "false";
                    }
                }
            }
            """,
            """
            public class Sample
            {
                public string Method(bool? boolean)
                {
                    switch (boolean)
                    {
                        case true: return "true";
                        case false: return "false";
                        default:
                            break;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71281")]
    public Task NotWithPatternDefault1()
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            case var x:
                                break;
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71281")]
    public Task TestWithPatternDefault_NonConstantGuard1()
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
                    void Method(int i)
                    {
                        var e = MyEnum.Fizz;
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            case var x when i > 0:
                                break;
                        }
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
                    void Method(int i)
                    {
                        var e = MyEnum.Fizz;
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            case var x when i > 0:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71281")]
    public Task TestNotWithPatternDefault_ConstantGuard1()
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
                    void Method(int i)
                    {
                        var e = MyEnum.Fizz;
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            case var x when true:
                                break;
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71281")]
    public Task TestWithPatternDefault_NonConstantTrueGuard()
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
                    void Method(int i)
                    {
                        var e = MyEnum.Fizz;
                        [||]switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            case var x when false:
                                break;
                        }
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
                    void Method(int i)
                    {
                        var e = MyEnum.Fizz;
                        switch (e)
                        {
                            case MyEnum.Fizz:
                            case MyEnum.Buzz:
                            case MyEnum.FizzBuzz:
                                break;
                            case var x when false:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73245")]
    public Task NotAllMembersExist_NotDefault_OrPattern()
        => TestInRegularAndScriptAsync(
            """
            namespace ConsoleApplication1
            {
                enum MyEnum
                {
                    Fizz,
                    Buzz,
                    FizzBuzz,
                    FizzBuzzFizz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        [||]switch (e)
                        {
                            case MyEnum.Fizz or MyEnum.Buzz or MyEnum.FizzBuzz:
                                break;
                        }
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
                    FizzBuzz,
                    FizzBuzzFizz
                }

                class MyClass
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        switch (e)
                        {
                            case MyEnum.Fizz or MyEnum.Buzz or MyEnum.FizzBuzz:
                                break;
                            case MyEnum.FizzBuzzFizz:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            """, index: 2);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73245")]
    public Task NotAllMembersExist_WithDefault_OrPattern()
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
                        [||]switch (e)
                        {
                            case MyEnum.Fizz or MyEnum.Buzz:
                                break;
                            default:
                                break;
                        }
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
                        switch (e)
                        {
                            case MyEnum.Fizz or MyEnum.Buzz:
                                break;
                            case MyEnum.FizzBuzz:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("int")]
    [InlineData("int i")]
    public Task NullableValueTypeWithNullAndUnderlyingValueCases1(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                void M(int? x)
                {
                    [||]switch (x)
                    {
                        case null:
                            break;
                        case {{underlyingTypePattern}}:
                            break;
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("int")]
    [InlineData("int i")]
    public Task NullableValueTypeWithNullAndUnderlyingValueCases2(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(int? x)
                {
                    [||]switch (x)
                    {
                        case {{underlyingTypePattern}}:
                            break;
                        case null:
                            break;
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("int")]
    [InlineData("int i")]
    public Task NullableValueTypeWithNullAndUnderlyingValueCases3(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                void M(int? x)
                {
                    [||]switch (x)
                    {
                        case null:
                            break;
                        case 0:
                            break;
                        case {{underlyingTypePattern}}:
                            break;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    public Task NullableValueTypeWithNullAndUnderlyingValueCases4()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                int M(int? x)
                {
                    [||]switch (x)
                    {
                        case null:
                        case int:
                            break;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    public Task NullableValueTypeWithNullAndUnderlyingValueCases5()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                int M(int? x)
                {
                    [||]switch (x)
                    {
                        case int:
                        case null:
                            break;
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("string")]
    [InlineData("string s")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueCases1(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                void M(string? x)
                {
                    [||]switch (x)
                    {
                        case null:
                            break;
                        case {{underlyingTypePattern}}:
                            break;
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("string")]
    [InlineData("string s")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueCases2(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                int M(string? x)
                {
                    [||]switch (x)
                    {
                        case {{underlyingTypePattern}}:
                            break;
                        case null:
                            break;
                    }
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    [InlineData("string")]
    [InlineData("string s")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueCases3(string underlyingTypePattern)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                void M(string? x)
                {
                    [||]switch (x)
                    {
                        case null:
                            break;
                        case "":
                            break;
                        case {{underlyingTypePattern}}:
                            break;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueCases4()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                int M(string? x)
                {
                    [||]switch (x)
                    {
                        case null:
                        case string:
                            break;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50983")]
    public Task NullableReferenceTypeWithNullAndUnderlyingValueCases5()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                int M(string? x)
                {
                    [||]switch (x)
                    {
                        case string:
                        case null:
                            break;
                    }
                }
            }
            """);
}
