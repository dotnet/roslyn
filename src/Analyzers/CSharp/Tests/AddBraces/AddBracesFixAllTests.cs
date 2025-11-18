// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddBraces;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
public sealed partial class AddBracesTests
{
    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInDocument1()
        => TestInRegularAndScriptAsync("""
            class Program1
            {
                static void Main()
                {
                    {|FixAllInDocument:if|} (true) if (true) return;
                }
            }
            """, """
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }
                }
            }
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInDocument2()
        => TestInRegularAndScriptAsync("""
            class Program1
            {
                static void Main()
                {
                    if (true) {|FixAllInDocument:if|} (true) return;
                }
            }
            """, """
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }
                }
            }
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInDocument()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class Program1
            {
                static void Main()
                {
                    {|FixAllInDocument:if|} (true) return;
                    if (true) return;
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }

                    if (true)
                    {
                        return;
                    }
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInProject()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class Program1
            {
                static void Main()
                {
                    {|FixAllInProject:if|} (true) return;
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInSolution()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class Program1
            {
                static void Main()
                {
                    {|FixAllInSolution:if|} (true) return;
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                static void Main()
                {
                    if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
            {
                static void Main()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingMember()
        => TestInRegularAndScriptAsync("""
            class Program1
            {
                static void Main()
                {
                    {|FixAllInContainingMember:if|} (true) if (true) return;

                    if (false) if (false) return;
                }

                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }

            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
            """, """
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }

                    if (false)
                    {
                        if (false)
                        {
                            return;
                        }
                    }
                }

                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }

            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingType_AcrossSingleFile()
        => TestInRegularAndScriptAsync("""
            class Program1
            {
                static void Main()
                {
                    {|FixAllInContainingType:if|} (true) if (true) return;

                    if (false) if (false) return;
                }

                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }

            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
            """, """
            class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }

                    if (false)
                    {
                        if (false)
                        {
                            return;
                        }
                    }
                }

                void OtherMethod()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }
                }
            }

            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingType_AcrossMultipleFiles()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            partial class Program1
            {
                static void Main()
                {
                    {|FixAllInContainingType:if|} (true) if (true) return;

                    if (false) if (false) return;
                }

                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
                    </Document>
                    <Document>
            partial class Program1
            {
                void OtherFileMethod()
                {
                    if (true) if (true) return;
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                void OtherTypeMethod()
                {
                    if (true) if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            partial class Program1
            {
                static void Main()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }

                    if (false)
                    {
                        if (false)
                        {
                            return;
                        }
                    }
                }

                void OtherMethod()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }
                }
            }
                    </Document>
                    <Document>
            partial class Program1
            {
                void OtherFileMethod()
                {
                    if (true)
                    {
                        if (true)
                        {
                            return;
                        }
                    }
                }
            }
                    </Document>
                    <Document>
            class Program2
            {
                void OtherTypeMethod()
                {
                    if (true) if (true) return;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Theory]
    [InlineData("FixAllInContainingMember")]
    [InlineData("FixAllInContainingType")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingMemberAndType_TopLevelStatements(string fixAllScope)
        => TestInRegularAndScriptAsync($$"""
            {|{{fixAllScope}}:if|} (true) if (true) return;

            if (false) if (false) return;

            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
            """, """
            if (true)
            {
                if (true)
                {
                    return;
                }
            }

            if (false)
            {
                if (false)
                {
                    return;
                }
            }

            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }
            """);

    [Theory]
    [InlineData("FixAllInContainingMember")]
    [InlineData("FixAllInContainingType")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingMemberAndType_TopLevelStatements_02(string fixAllScope)
        => TestInRegularAndScriptAsync($$"""
            using System;

            {|{{fixAllScope}}:if|} (true) if (true) return;

            if (false) if (false) return;

            namespace N
            {
                class OtherType
                {
                    void OtherMethod()
                    {
                        if (true) if (true) return;
                    }
                }
            }
            """, """
            using System;

            if (true)
            {
                if (true)
                {
                    return;
                }
            }

            if (false)
            {
                if (false)
                {
                    return;
                }
            }

            namespace N
            {
                class OtherType
                {
                    void OtherMethod()
                    {
                        if (true) if (true) return;
                    }
                }
            }
            """);

    [Theory]
    [InlineData("FixAllInContainingMember")]
    [InlineData("FixAllInContainingType")]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInContainingMemberAndType_TopLevelStatements_ErrorCase(string fixAllScope)
        => TestMissingInRegularAndScriptAsync($$"""
            class OtherType
            {
                void OtherMethod()
                {
                    if (true) if (true) return;
                }
            }

            {|{{fixAllScope}}:if|} (true) if (true) return;

            if (false) if (false) return;
            """);
}
