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
public sealed class ConvertInterpolatedStringToRawString_FixAllTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new ConvertStringToRawStringCodeRefactoringProvider();

    [Fact]
    public Task FixAllInDocument_SingleLine()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllInDocument_MultiLine()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllInDocument_MultiLineWithoutLeadingWhitespace()
        => TestInRegularAndScriptAsync(
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

                var multiLine1 = $"goo\r\nbar";
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

                var multiLine1 = $"goo\r\nbar";
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

    [Fact]
    public Task FixAllInProject()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllInSolution()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllInContainingMember()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllInContainingType()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllInContainingType_AcrossFiles()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern1_B()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern2_B()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern3_B()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task FixAllCommonRoslynTestPattern6()
        => TestInRegularAndScriptAsync(
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
