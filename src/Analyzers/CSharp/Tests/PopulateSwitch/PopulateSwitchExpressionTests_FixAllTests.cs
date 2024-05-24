// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PopulateSwitch;

[Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
public partial class PopulateSwitchExpressionTests
{
    [Fact]
    public async Task TestFixAllInDocument()
    {
        var input = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                        _ = e {|FixAllInDocument:|}switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
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
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace ConsoleApplication1
            {
                class MyClass3
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        var expected = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                            _ => throw new System.NotImplementedException(),
                        };
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
                    </Document>
                    <Document>
            namespace ConsoleApplication1
            {
                class MyClass2
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace ConsoleApplication1
            {
                class MyClass3
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact]
    public async Task TestFixAllInProject()
    {
        var input = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                        _ = e {|FixAllInProject:|}switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
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
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace ConsoleApplication1
            {
                class MyClass3
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        var expected = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                    </Document>
                    <Document>
            namespace ConsoleApplication1
            {
                class MyClass2
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
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            namespace ConsoleApplication1
            {
                class MyClass3
                {
                    void Method()
                    {
                        var e = MyEnum.Fizz;
                        _ = e switch
                        {
                            MyEnum.Fizz => 1,
                            MyEnum.Buzz => 2,
                            MyEnum.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact]
    public async Task TestFixAllInSolution()
    {
        var input = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                        _ = e {|FixAllInSolution:|}switch
                        {
                            MyEnum1.Fizz => 1,
                            MyEnum1.Buzz => 2,
                            MyEnum1.FizzBuzz => 3,
                        };
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
                        _ = e switch
                        {
                            MyEnum2.Fizz => 1,
                            MyEnum2.Buzz => 2,
                            MyEnum2.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
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
                        _ = e switch
                        {
                            MyEnum3.Fizz => 1,
                            MyEnum3.Buzz => 2,
                            MyEnum3.FizzBuzz => 3,
                        };
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        var expected = """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
                        _ = e switch
                        {
                            MyEnum1.Fizz => 1,
                            MyEnum1.Buzz => 2,
                            MyEnum1.FizzBuzz => 3,
                            _ => throw new System.NotImplementedException(),
                        };
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
                        _ = e switch
                        {
                            MyEnum2.Fizz => 1,
                            MyEnum2.Buzz => 2,
                            MyEnum2.FizzBuzz => 3,
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
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
                        _ = e switch
                        {
                            MyEnum3.Fizz => 1,
                            MyEnum3.Buzz => 2,
                            MyEnum3.FizzBuzz => 3,
                            _ => throw new System.NotImplementedException(),
                        };
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        await TestInRegularAndScriptAsync(input, expected);
    }
}
