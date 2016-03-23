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
            switch (e)
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
            switch (e)
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
    }
}
