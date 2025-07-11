// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertIfToSwitch;

[Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
public sealed class ConvertIfToSwitchFixAllTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpConvertIfToSwitchCodeRefactoringProvider();

    [Fact]
    public Task ConvertIfToSwitchStatement_FixAllInDocument()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int M(int i)
                {
                    {|FixAllInDocument:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }

                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }

                int M2(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
            """);

    [Fact]
    public Task ConvertIfToSwitchExpression_FixAllInDocument()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int M(int i)
                {
                    {|FixAllInDocument:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }

                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                int M(int i)
                {
                    return i switch
                    {
                        3 => 0,
                        6 => 1,
                        7 => 1,
                        _ => 0
                    };
                }

                int M2(int i)
                {
                    return i switch
                    {
                        3 => 0,
                        6 => 1,
                        7 => 1,
                        _ => 0
                    };
                }
            }
            """, index: 1);

    [Fact]
    public Task ConvertIfToSwitchStatement_Nested_FixAllInDocument()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int M(int i, int j)
                {
                    {|FixAllInDocument:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (j == 6)
                        {
                            if (i == 6) return 1;
                            if (i == 7) return 2;
                            return 3;
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                int M(int i, int j)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        default:
                            if (j == 6)
                            {
                                switch (i)
                                {
                                    case 6:
                                        return 1;
                                    case 7:
                                        return 2;
                                    default:
                                        return 3;
                                }
                            }
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task ConvertIfToSwitchStatement_FixAllInProject()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class C
            {
                int M(int i)
                {
                    {|FixAllInProject:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                    <Document>
            class C2
            {
                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class C3
            {
                int M3(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
                    </Document>
                    <Document>
            class C2
            {
                int M2(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class C3
            {
                int M3(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task ConvertIfToSwitchStatement_FixAllInSolution()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class C
            {
                int M(int i)
                {
                    {|FixAllInSolution:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                    <Document>
            class C2
            {
                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class C3
            {
                int M3(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
                    </Document>
                    <Document>
            class C2
            {
                int M2(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class C3
            {
                int M3(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task ConvertIfToSwitchStatement_FixAllInContainingMember()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int M(int i)
                {
                    {|FixAllInContainingMember:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }

                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }

                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """);

    [Fact]
    public Task ConvertIfToSwitchStatement_FixAllInContainingType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int M(int i)
                {
                    {|FixAllInContainingType:|}if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }

                int M2(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }

            class C2
            {
                int M3(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """,
            """
            class C
            {
                int M(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }

                int M2(int i)
                {
                    switch (i)
                    {
                        case 3:
                            return 0;
                        case 6:
                            return 1;
                        case 7:
                            return 1;
                        default:
                            return 0;
                    }
                }
            }

            class C2
            {
                int M3(int i)
                {
                    if (i == 3)
                    {
                        return 0;
                    }
                    else
                    {
                        if (i == 6) return 1;
                        if (i == 7) return 1;
                        return 0;
                    }
                }
            }
            """);
}
