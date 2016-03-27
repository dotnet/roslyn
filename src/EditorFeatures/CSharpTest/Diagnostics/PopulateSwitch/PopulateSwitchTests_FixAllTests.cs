using Roslyn.Test.Utilities;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch
{
    public partial class PopulateSwitchTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass1
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch (e)
            {
                {|FixAllInDocument:case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                    break;|}
            }
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
        </Document>
        <Document>
namespace ConsoleApplication1
{
    class MyClass2
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
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    class MyClass3
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
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass1
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
        </Document>
        <Document>
namespace ConsoleApplication1
{
    class MyClass2
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
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    class MyClass3
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
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass1
    {
        void Method()
        {
            var e = MyEnum.Fizz;
            switch (e)
            {
                {|FixAllInProject:case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                    break;|}
            }
        }
    }
}
        </Document>
        <Document>
namespace ConsoleApplication1
{
    class MyClass2
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
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    class MyClass3
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
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass1
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
        </Document>
        <Document>
namespace ConsoleApplication1
{
    class MyClass2
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
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    class MyClass3
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
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsPopulateSwitch)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum1
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass1
    {
        void Method()
        {
            var e = MyEnum1.Fizz;
            switch (e)
            {
                {|FixAllInSolution:case MyEnum1.Fizz:
                case MyEnum1.Buzz:
                case MyEnum1.FizzBuzz:
                    break;|}
            }
        }
    }
}
        </Document>
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum2
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass2
    {
        void Method()
        {
            var e = MyEnum2.Fizz;
            switch (e)
            {
                case MyEnum2.Fizz:
                case MyEnum2.Buzz:
                case MyEnum2.FizzBuzz:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication2
{
    enum MyEnum3
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass3
    {
        void Method()
        {
            var e = MyEnum3.Fizz;
            switch (e)
            {
                case MyEnum3.Fizz:
                case MyEnum3.Buzz:
                case MyEnum3.FizzBuzz:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum1
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass1
    {
        void Method()
        {
            var e = MyEnum1.Fizz;
            switch (e)
            {
                case MyEnum1.Fizz:
                case MyEnum1.Buzz:
                case MyEnum1.FizzBuzz:
                    break;
                default:
                    break;
            }
        }
    }
}
        </Document>
        <Document>
namespace ConsoleApplication1
{
    enum MyEnum2
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass2
    {
        void Method()
        {
            var e = MyEnum2.Fizz;
            switch (e)
            {
                case MyEnum2.Fizz:
                case MyEnum2.Buzz:
                case MyEnum2.FizzBuzz:
                    break;
                default:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
namespace ConsoleApplication2
{
    enum MyEnum3
    {
        Fizz, Buzz, FizzBuzz
    }
    class MyClass3
    {
        void Method()
        {
            var e = MyEnum3.Fizz;
            switch (e)
            {
                case MyEnum3.Fizz:
                case MyEnum3.Buzz:
                case MyEnum3.FizzBuzz:
                    break;
                default:
                    break;
            }
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false);
        }
    }
}
