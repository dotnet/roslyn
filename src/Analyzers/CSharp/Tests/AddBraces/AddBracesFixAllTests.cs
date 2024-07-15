// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddBraces
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
    public partial class AddBracesTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument1()
        {
            var input = """
                class Program1
                {
                    static void Main()
                    {
                        {|FixAllInDocument:if|} (true) if (true) return;
                    }
                }
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument2()
        {
            var input = """
                class Program1
                {
                    static void Main()
                    {
                        if (true) {|FixAllInDocument:if|} (true) return;
                    }
                }
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = """
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var input = """
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var input = """
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInContainingMember()
        {
            var input = """
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInContainingType_AcrossSingleFile()
        {
            var input = """
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInContainingType_AcrossMultipleFiles()
        {
            var input = """
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Theory]
        [InlineData("FixAllInContainingMember")]
        [InlineData("FixAllInContainingType")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddBraces)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInContainingMemberAndType_TopLevelStatements(string fixAllScope)
        {
            var input = $$"""
                {|{{fixAllScope}}:if|} (true) if (true) return;

                if (false) if (false) return;

                class OtherType
                {
                    void OtherMethod()
                    {
                        if (true) if (true) return;
                    }
                }
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Theory]
        [InlineData("FixAllInContainingMember")]
        [InlineData("FixAllInContainingType")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInContainingMemberAndType_TopLevelStatements_02(string fixAllScope)
        {
            var input = $$"""
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
                """;

            var expected = """
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
                """;

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Theory]
        [InlineData("FixAllInContainingMember")]
        [InlineData("FixAllInContainingType")]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInContainingMemberAndType_TopLevelStatements_ErrorCase(string fixAllScope)
        {
            // Error case: Global statements should precede non-global statements.
            var input = $$"""
                class OtherType
                {
                    void OtherMethod()
                    {
                        if (true) if (true) return;
                    }
                }

                {|{{fixAllScope}}:if|} (true) if (true) return;

                if (false) if (false) return;
                """;

            await TestMissingInRegularAndScriptAsync(input);
        }
    }
}
