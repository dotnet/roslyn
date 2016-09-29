// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PopulateSwitch;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch
{
    public partial class PopulateSwitchTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new PopulateSwitchDiagnosticAnalyzer(), new PopulateSwitchCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task AllMembersAndDefaultExist()
        {
            await TestMissingAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task AllMembersExist_NotDefault()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                    break;
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_WithDefault()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
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
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_EnumHasExplicitType()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum : long
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum : long
    {
        Fizz, Buzz, FizzBuzz
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_WithMembersAndDefaultInSection_NewValuesAboveDefaultSection()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_WithMembersAndDefaultInSection_AssumesDefaultIsInLastSection()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
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
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NoMembersExist0()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
", index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NoMembersExist1()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NoMembersExist2()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task UsingStaticEnum_AllMembersExist()
        {
            await TestMissingAsync(
            @"
using static System.IO.FileMode;
namespace ConsoleApplication1
{
    class MyClass
    {
        void Method()
        {
            var e = Append;
            switch ([|e|])
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task UsingStaticEnum_AllMembersExist_OutOfDefaultOrder()
        {
            await TestMissingAsync(
            @"
using static System.IO.FileMode;
namespace ConsoleApplication1
{
    class MyClass
    {
        void Method()
        {
            var e = Append;
            switch ([|e|])
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task UsingStaticEnum_MembersExist()
        {
            await TestAsync(
            @"
using static System.IO.FileMode;
namespace ConsoleApplication1
{
    class MyClass
    {
        void Method()
        {
            var e = Append;
            switch ([|e|])
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
",
                        @"
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task UsingStaticEnum_NoMembersExist()
        {
            await TestAsync(
            @"
using static System.IO.FileMode;
namespace ConsoleApplication1
{
    class MyClass
    {
        void Method()
        {
            var e = Append;
            switch ([|e|])
            {
            }
        }
    }
}
",
                        @"
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_EnumHasNonFlagsAttribute()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    [System.Obsolete]
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    [System.Obsolete]
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_EnumIsNested()
        {
            await TestAsync(
            @"
namespace ConsoleApplication1
{
    class MyClass
    {
        enum MyEnum
        {
            Fizz, Buzz, FizzBuzz
        }

        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
            }
        }
    }
}
",
                        @"
namespace ConsoleApplication1
{
    class MyClass
    {
        enum MyEnum
        {
            Fizz, Buzz, FizzBuzz
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_SwitchIsNotEnum()
        {
            await TestMissingAsync(
            @"
using System;
namespace ConsoleApplication1
{
    class MyClass
    {
        void Method()
        {
            var e = ""test"";
            switch ([|e|])
            {
                case ""test1"":
                case ""test1"":
                default:
                    break;
            }
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_UsingConstants()
        {
            await TestAsync(
            @"
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch ([|e|])
            {
                case (MyEnum)0:
                case (MyEnum)1:
                    break;
            }
        }
    }
",
                        @"
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
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
", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        [WorkItem(13455, "https://github.com/dotnet/roslyn/issues/13455")]
        public async Task AllMissingTokens()
        {
            await TestAsync(
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
        switch ([|e|])
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
}", compareTokens: false);
        }
    }
}