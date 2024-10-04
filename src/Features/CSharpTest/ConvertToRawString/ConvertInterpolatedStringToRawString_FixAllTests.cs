// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertToRawString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRawString;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToRawString)]
[Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
public class ConvertInterpolatedStringToRawString_FixAllTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ConvertStringToRawStringCodeRefactoringProvider();

    [Fact]
    public async Task FixAllInDocument_SingleLine()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var singleLine1 = {|FixAllInDocument:|}$"a";
                    var singleLine2 = @$"goo""bar";

                    var multiLine1 = $"goo\r\nbar";
                    var multiLine2 = @$"goo
            bar";

                    var multiLineWithoutLeadingWhitespace1 = @$"
            from x in y
            where x > 0
            select x";
                    var multiLineWithoutLeadingWhitespace2 = @$"
            from x2 in y2
            where x2 > 0
            select x2";
                }

                void M2()
                {
                    var singleLine1 = $"a";
                    var singleLine2 = @$"goo""bar";

                    var multiLine1 = $"goo\r\nbar";
                    var multiLine2 = @$"goo
            bar";

                    var multiLineWithoutLeadingWhitespace1 = @$"
            from x in y
            where x > 0
            select x";
                    var multiLineWithoutLeadingWhitespace2 = @$"
            from x2 in y2
            where x2 > 0
            select x2";
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    var singleLine1 = $"""a""";
                    var singleLine2 = $"""goo"bar""";

                    var multiLine1 = $"goo\r\nbar";
                    var multiLine2 = @$"goo
            bar";

                    var multiLineWithoutLeadingWhitespace1 = @$"
            from x in y
            where x > 0
            select x";
                    var multiLineWithoutLeadingWhitespace2 = @$"
            from x2 in y2
            where x2 > 0
            select x2";
                }

                void M2()
                {
                    var singleLine1 = $"""a""";
                    var singleLine2 = $"""goo"bar""";

                    var multiLine1 = $"goo\r\nbar";
                    var multiLine2 = @$"goo
            bar";

                    var multiLineWithoutLeadingWhitespace1 = @$"
            from x in y
            where x > 0
            select x";
                    var multiLineWithoutLeadingWhitespace2 = @$"
            from x2 in y2
            where x2 > 0
            select x2";
                }
            }
            """");
    }

    [Fact]
    public async Task FixAllInDocument_MultiLine()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = {|FixAllInDocument:|}$"goo\r\nbar";
                var multiLine2 = @$"goo
        bar";

                var multiLineWithoutLeadingWhitespace1 = @$"
        from x in y
        where x > 0
        select x";
                var multiLineWithoutLeadingWhitespace2 = @$"
        from x2 in y2
        where x2 > 0
        select x2";
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"goo\r\nbar";
                var multiLine2 = @$"goo
        bar";

                var multiLineWithoutLeadingWhitespace1 = @$"
        from x in y
        where x > 0
        select x";
                var multiLineWithoutLeadingWhitespace2 = @$"
        from x2 in y2
        where x2 > 0
        select x2";
            }
        }
        """,
        """"
        class C
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"""
                    goo
                    bar
                    """;
                var multiLine2 = $"""
                    goo
                    bar
                    """;

                var multiLineWithoutLeadingWhitespace1 = $"""

                    from x in y
                    where x > 0
                    select x
                    """;
                var multiLineWithoutLeadingWhitespace2 = $"""

                    from x2 in y2
                    where x2 > 0
                    select x2
                    """;
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"""
                    goo
                    bar
                    """;
                var multiLine2 = $"""
                    goo
                    bar
                    """;

                var multiLineWithoutLeadingWhitespace1 = $"""

                    from x in y
                    where x > 0
                    select x
                    """;
                var multiLineWithoutLeadingWhitespace2 = $"""

                    from x2 in y2
                    where x2 > 0
                    select x2
                    """;
            }
        }
        """");
    }

    [Fact]
    public async Task FixAllInDocument_MultiLineWithoutLeadingWhitespace()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"goo\r\nbar";
                var multiLine2 = @$"goo
        bar";

                var multiLineWithoutLeadingWhitespace1 = {|FixAllInDocument:|}@$"
        from x in y
        where x > 0
        select x";
                var multiLineWithoutLeadingWhitespace2 = @$"
        from x2 in y2
        where x2 > 0
        select x2";
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"goo\r\nbar";
                var multiLine2 = @$"goo
        bar";

                var multiLineWithoutLeadingWhitespace1 = @$"
        from x in y
        where x > 0
        select x";
                var multiLineWithoutLeadingWhitespace2 = @$"
        from x2 in y2
        where x2 > 0
        select x2";
            }
        }
        """,
        """"
        class C
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"""
                    goo
                    bar
                    """;
                var multiLine2 = $"""
                    goo
                    bar
                    """;

                var multiLineWithoutLeadingWhitespace1 = $"""
                    from x in y
                    where x > 0
                    select x
                    """;
                var multiLineWithoutLeadingWhitespace2 = $"""
                    from x2 in y2
                    where x2 > 0
                    select x2
                    """;
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";

                var multiLine1 = $"""
                    goo
                    bar
                    """;
                var multiLine2 = $"""
                    goo
                    bar
                    """;

                var multiLineWithoutLeadingWhitespace1 = $"""
                    from x in y
                    where x > 0
                    select x
                    """;
                var multiLineWithoutLeadingWhitespace2 = $"""
                    from x2 in y2
                    where x2 > 0
                    select x2
                    """;
            }
        }
        """", index: 1);
    }

    [Fact]
    public async Task FixAllInProject()
    {
        await TestInRegularAndScriptAsync(
        """
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document>
        class Program1
        {
            void M1()
            {
                var singleLine1 = {|FixAllInProject:|}$"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
                <Document>
        class Program2
        {
            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                <Document>
        class Program3
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
        </Workspace>
        """,
        """"
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document>
        class Program1
        {
            void M1()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
                </Document>
                <Document>
        class Program2
        {
            void M2()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                <Document>
        class Program3
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
        </Workspace>
        """");
    }

    [Fact]
    public async Task FixAllInSolution()
    {
        await TestInRegularAndScriptAsync(
        """
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document>
        class Program1
        {
            void M1()
            {
                var singleLine1 = {|FixAllInSolution:|}$"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
                <Document>
        class Program2
        {
            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                <Document>
        class Program3
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
        </Workspace>
        """,
        """"
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document>
        class Program1
        {
            void M1()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
                </Document>
                <Document>
        class Program2
        {
            void M2()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                <Document>
        class Program3
        {
            void M3()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
                </Document>
            </Project>
        </Workspace>
        """");
    }

    [Fact]
    public async Task FixAllInContainingMember()
    {
        await TestInRegularAndScriptAsync(
        """
        class C
        {
            void M()
            {
                var singleLine1 = {|FixAllInContainingMember:|}$"a";
                var singleLine2 = @$"goo""bar";
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }

        class C2
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
        """,
        """"
        class C
        {
            void M()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }

        class C2
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
        """");
    }

    [Fact]
    public async Task FixAllInContainingType()
    {
        await TestInRegularAndScriptAsync(
        """
        partial class C
        {
            void M()
            {
                var singleLine1 = {|FixAllInContainingType:|}$"a";
                var singleLine2 = @$"goo""bar";
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }

        class C2
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }

        partial class C
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
        """,
        """"
        partial class C
        {
            void M()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }

            void M2()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }

        class C2
        {
            void M()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }

        partial class C
        {
            void M3()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
        """");
    }

    [Fact]
    public async Task FixAllInContainingType_AcrossFiles()
    {
        await TestInRegularAndScriptAsync(
        """
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document>
        partial class Program1
        {
            void M1()
            {
                var singleLine1 = {|FixAllInContainingType:|}$"a";
                var singleLine2 = @$"goo""bar";
            }

            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
                <Document>
        partial class Program1
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }

        class Program2
        {
            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                <Document>
        class Program3
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
        </Workspace>
        """,
        """"
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                <Document>
        partial class Program1
        {
            void M1()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }

            void M2()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }
                </Document>
                <Document>
        partial class Program1
        {
            void M3()
            {
                var singleLine1 = $"""a""";
                var singleLine2 = $"""goo"bar""";
            }
        }

        class Program2
        {
            void M2()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
            <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                <Document>
        class Program3
        {
            void M3()
            {
                var singleLine1 = $"a";
                var singleLine2 = @$"goo""bar";
            }
        }
                </Document>
            </Project>
        </Workspace>
        """");
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"class X
            {{
            }}",
            @$"class Y
            {{
            }}");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                        }
                        """,
                        $$"""
                        class Y
                        {
                        }
                        """);
                }
            }
            """",
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern1_B()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"class X
            {{
            }}", @$"class Y
            {{
            }}");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                        }
                        """, $$"""
                        class Y
                        {
                        }
                        """);
                }
            }
            """",
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern2()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"
            class X
            {{
            }}",
            @$"
            class Y
            {{
            }}");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                        }
                        """,
                        $$"""
                        class Y
                        {
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern2_B()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"
            class X
            {{
            }}", @$"
            class Y
            {{
            }}");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                        }
                        """, $$"""
                        class Y
                        {
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern3()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"
            class X
            {{
            }}
            ",
            @$"
            class Y
            {{
            }}
            ");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                        }
                        """,
                        $$"""
                        class Y
                        {
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern3_B()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"
            class X
            {{
            }}
            ", @$"
            class Y
            {{
            }}
            ");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                        }
                        """, $$"""
                        class Y
                        {
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern4()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"
            class X
            {{
                {0}
            }}
            ", @$"
            class Y
            {{
                {1}
            }}
            ");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                            {{0}}
                        }
                        """, $$"""
                        class Y
                        {
                            {{1}}
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern5()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@$"
            class X
            {{
                {0}
            }}
            ", @"
            class Y
            {
            }
            ");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        $$"""
                        class X
                        {
                            {{0}}
                        }
                        """, """
                        class Y
                        {
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }

    [Fact]
    public async Task FixAllCommonRoslynTestPattern6()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    await TestAsync(
            {|FixAllInDocument:|}@"
            class X
            {
            }
            ", @$"
            class Y
            {{
                {0}
            }}
            ");
                }
            }
            """,
            """"
            class C
            {
                void M()
                {
                    await TestAsync(
                        """
                        class X
                        {
                        }
                        """, $$"""
                        class Y
                        {
                            {{0}}
                        }
                        """);
                }
            }
            """", index: 1,
            new TestParameters(treatPositionIndicatorsAsCode: true));
    }
}
