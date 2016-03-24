using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch
{
    public class PopulateSwitchTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpPopulateSwitchDiagnosticAnalyzer(), new PopulateSwitchCodeFixProvider());
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
            switch (e)
            {
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                default:
                    break;|]
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
            switch (e)
            {
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                    break;|]
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
            switch (e)
            {
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;|]
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
            switch (e)
            {
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;
                default:
                    break;|]
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
                default:
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
        public async Task NoMembersExist()
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
            switch (e)
            {
                [||]
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
");
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
            switch (e)
            {
                [|case CreateNew:
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
                    break;|]
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
            switch (e)
            {
                [|case CreateNew:
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
                    break;|]
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
            switch (e)
            {
                [|case CreateNew:
                    break;
                case Create:
                    break;
                case Open:
                    break;
                case OpenOrCreate:
                    break;
                default:
                    break;|]
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
                default:
                    break;
                case Truncate:
                    break;
                case Append:
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
            switch (e)
            {[||]
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
        public async Task NotAllMembersExist_SwitchIsFlags()
        {
            await TestMissingAsync(
            @"
using System;
namespace ConsoleApplication1
{
    [Flags]
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
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;|]
            }
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_SwitchIsFlagsAttribute()
        {
            await TestMissingAsync(
            @"
using System;
namespace ConsoleApplication1
{
    [FlagsAttribute]
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
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;|]
            }
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_SwitchIsFullyQualifiedSystemFlags()
        {
            await TestMissingAsync(
            @"
namespace ConsoleApplication1
{
    [System.Flags]
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
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;|]
            }
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_SwitchIsFullyQualifiedSystemFlagsAttribute()
        {
            await TestMissingAsync(
            @"
namespace ConsoleApplication1
{
    [System.FlagsAttribute]
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
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                default:
                    break;|]
            }
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_SwitchHasNonFlagsAttribute()
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
            switch (e)
            {
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;|]
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
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        public async Task NotAllMembersExist_NotDefault_SwitchIsNested()
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
            switch (e)
            {
                [|case MyEnum.Fizz:
                case MyEnum.Buzz:
                    break;|]
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
");
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
            switch (e)
            {
                [|case ""test1"":
                case ""test1"":
                default:
                    break;|]
            }
        }
    }
}
");
        }
    }
}
