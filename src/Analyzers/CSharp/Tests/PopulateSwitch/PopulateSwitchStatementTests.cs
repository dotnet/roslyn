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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch
{
    public partial class PopulateSwitchStatementTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public PopulateSwitchStatementTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpPopulateSwitchStatementDiagnosticAnalyzer(), new CSharpPopulateSwitchStatementCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task OnlyOnFirstToken()
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
            switch ([||]e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task AllMembersExist_NotDefault()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                    break;
            }
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_WithDefault()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_EnumHasExplicitType()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_WithMembersAndDefaultInSection_NewValuesAboveDefaultSection()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;
            }
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_WithMembersAndDefaultInSection_AssumesDefaultIsInLastSection()
        {
            await TestInRegularAndScriptAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NoMembersExist0()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
            }
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
}", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NoMembersExist1()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
            }
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
            switch (e)
            {
                default:
                    break;
            }
        }
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NoMembersExist2()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
            }
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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task UsingStaticEnum_MembersExist()
        {
            await TestInRegularAndScriptAsync(
@"using static System.IO.FileMode;

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
}",
@"using static System.IO.FileMode;

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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task UsingStaticEnum_NoMembersExist()
        {
            await TestInRegularAndScriptAsync(
@"using static System.IO.FileMode;

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
}",
@"using static System.IO.FileMode;

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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_EnumHasNonFlagsAttribute()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_EnumIsNested()
        {
            await TestInRegularAndScriptAsync(
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
            [||]switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
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
            [||]switch (e)
            {
                case ""test1"":
                case ""test1"":
                default:
                    break;
            }
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_UsingConstants()
        {
            await TestInRegularAndScriptAsync(
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
        [||]switch (e)
        {
            case (MyEnum)0:
            case (MyEnum)1:
                break;
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
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        [WorkItem(13455, "https://github.com/dotnet/roslyn/issues/13455")]
        public async Task AllMissingTokens()
        {
            await TestInRegularAndScriptAsync(
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
        [||]switch (e)
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
        switch (e)
        {
            case MyEnum.Fizz:
                break;
        }
    }
}");
        }

        [Fact]
        [WorkItem(40240, "https://github.com/dotnet/roslyn/issues/40240")]
        public async Task TestAddMissingCasesForNullableEnum()
        {
            await TestInRegularAndScriptAsync(
@"public class Program
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
",
@"public class Program
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
");
        }
    }
}
