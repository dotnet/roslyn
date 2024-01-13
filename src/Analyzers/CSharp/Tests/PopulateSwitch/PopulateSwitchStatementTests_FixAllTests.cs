// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch
{
    public partial class PopulateSwitchStatementTests
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
            {|FixAllInDocument:|}switch (e)
            {
                case MyEnum.Fizz:
                case MyEnum.Buzz:
                case MyEnum.FizzBuzz:
                    break;
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

            await TestInRegularAndScriptAsync(input, expected);
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
            {|FixAllInProject:|}switch (e)
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

            await TestInRegularAndScriptAsync(input, expected);
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
            {|FixAllInSolution:|}switch (e)
            {
                case MyEnum1.Fizz:
                case MyEnum1.Buzz:
                case MyEnum1.FizzBuzz:
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

            await TestInRegularAndScriptAsync(input, expected);
        }
    }
}
