﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
    public partial class PopulateSwitchExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
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
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task AllMembersAndDefaultExist()
        {
            await TestMissingInRegularAndScriptAsync(
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task AllMembersExist_NotDefault()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task AllMembersExist_NotDefault_NoComma()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task NotAllMembersExist_NotDefault()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 2);
        }

        [Fact]
        public async Task NotAllMembersExist_WithDefault()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task NotAllMembersExist_NotDefault_EnumHasExplicitType()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 2);
        }

        [Fact]
        public async Task NotAllMembersExist_WithMembersAndDefaultInSection_NewValuesAboveDefaultSection()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task NotAllMembersExist_WithMembersAndDefaultInSection_AssumesDefaultIsInLastSection()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}");
        }

        [Fact]
        public async Task NoMembersExist0()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 0);
        }

        [Fact]
        public async Task NoMembersExist1()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 1);
        }

        [Fact]
        public async Task NoMembersExist2()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 2);
        }

        [Fact]
        public async Task UsingStaticEnum_AllMembersExist()
        {
            await TestMissingInRegularAndScriptAsync(
@"using static System.IO.FileMode;

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
}");
        }

        [Fact]
        public async Task UsingStaticEnum_AllMembersExist_OutOfDefaultOrder()
        {
            await TestMissingInRegularAndScriptAsync(
@"using static System.IO.FileMode;

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
}");
        }

        [Fact]
        public async Task UsingStaticEnum_MembersExist()
        {
            await TestInRegularAndScript1Async(
@"using static System.IO.FileMode;

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
}",
@"using static System.IO.FileMode;

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
}");
        }

        [Fact]
        public async Task UsingStaticEnum_NoMembersExist()
        {
            await TestInRegularAndScript1Async(
@"using static System.IO.FileMode;

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
}",
@"using static System.IO.FileMode;

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
}", index: 2);
        }

        [Fact]
        public async Task NotAllMembersExist_NotDefault_EnumHasNonFlagsAttribute()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 2);
        }

        [Fact]
        public async Task NotAllMembersExist_NotDefault_EnumIsNested()
        {
            await TestInRegularAndScript1Async(
@"namespace ConsoleApplication1
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
}",
@"namespace ConsoleApplication1
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
}", index: 2);
        }

        [Fact]
        public async Task NotAllMembersExist_SwitchIsNotEnum()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

namespace ConsoleApplication1
{
    class MyClass
    {
        void Method()
        {
            var e = ""test"";
            _ = e [||]switch
            {
                ""test1"" => 1,
                ""test2"" => 2,
                _ => 3,
            }
        }
    }
}");
        }

        [Fact]
        public async Task NotAllMembersExist_NotDefault_UsingConstants()
        {
            await TestInRegularAndScript1Async(
@"enum MyEnum
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
}",
@"enum MyEnum
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
}", index: 2);
        }

        [Fact]
        public async Task NotAllMembersExist_NotDefault_WithMismatchingConstantType()
        {
            await TestInRegularAndScript1Async(
@"enum MyEnum
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
            ""Mismatching constant"" => 3,
        }
    }
}",
@"enum MyEnum
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
            ""Mismatching constant"" => 3,
            _ => throw new System.NotImplementedException(),
        }
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/40399")]
        public async Task AllMissingTokens()
        {
            await TestInRegularAndScript1Async(
            @"
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
",
            @"
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
}");
        }

        [Fact]
        [WorkItem(40240, "https://github.com/dotnet/roslyn/issues/40240")]
        public async Task TestAddMissingCasesForNullableEnum()
        {
            await TestInRegularAndScript1Async(
@"public class Program
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
",
@"public class Program
{
    void Main() 
    {
        var bar = Bar.Option1;
        var b = bar switch
        {
            Bar.Option1 => 1,
            Bar.Option2 => 2,
            null => null,
            _ => throw new System.NotImplementedException(),
        };
    }

    public enum Bar
{
    Option1, 
    Option2, 
    Option3,
}
}
");
        }
    }
}
