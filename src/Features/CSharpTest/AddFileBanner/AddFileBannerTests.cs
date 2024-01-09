// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.AddFileBanner;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddFileBanner
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
    public partial class AddFileBannerTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new CSharpAddFileBannerCodeRefactoringProvider();

        [Fact]
        public async Task TestBanner1()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>// This is the banner

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>// This is the banner

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>// This is the banner

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact]
        public async Task TestMultiLineBanner1()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>// This is the banner
                // It goes over multiple lines

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>// This is the banner
                // It goes over multiple lines

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>// This is the banner
                // It goes over multiple lines

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
        public async Task TestSingleLineDocCommentBanner()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>/// This is the banner
                /// It goes over multiple lines

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>/// This is the banner
                /// It goes over multiple lines

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>/// This is the banner
                /// It goes over multiple lines

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
        public async Task TestMultiLineDocCommentBanner()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>/** This is the banner
                * It goes over multiple lines
                */

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>/** This is the banner
                * It goes over multiple lines
                */

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>/** This is the banner
                * It goes over multiple lines
                */

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact]
        public async Task TestMissingWhenAlreadyThere()
        {
            await TestMissingAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]// I already have a banner

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>// This is the banner

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Theory]
        [InlineData("", 1)]
        [InlineData("file_header_template =", 1)]
        [InlineData("file_header_template = unset", 1)]
        [InlineData("file_header_template = defined file header", 0)]
        public async Task TestMissingWhenHandledByAnalyzer(string fileHeaderTemplate, int expectedActionCount)
        {
            var initialMarkup = $@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""/0/Test0.cs"">[||]using System;

class Program1
{{
    static void Main()
    {{
    }}
}}
        </Document>
        <Document FilePath=""/0/Test1.cs"">/// This is the banner
/// It goes over multiple lines

class Program2
{{
}}
        </Document>
        <AnalyzerConfigDocument FilePath=""/.editorconfig"">
root = true

[*]
{fileHeaderTemplate}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            await TestActionCountAsync(initialMarkup, expectedActionCount);
        }

        [Fact]
        public async Task TestMissingIfOtherFileDoesNotHaveBanner()
        {
            await TestMissingAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact]
        public async Task TestMissingIfOtherFileIsAutoGenerated()
        {
            await TestMissingAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>[||]

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document>// &lt;autogenerated /&gt;

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32792")]
        public async Task TestUpdateFileNameInComment()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">// This is the banner in Bar.cs
                // It goes over multiple lines.  This line has Baz.cs
                // The last line includes Bar.cs

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">// This is the banner in Goo.cs
                // It goes over multiple lines.  This line has Baz.cs
                // The last line includes Goo.cs

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">// This is the banner in Bar.cs
                // It goes over multiple lines.  This line has Baz.cs
                // The last line includes Bar.cs

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32792")]
        public async Task TestUpdateFileNameInComment2()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">/* This is the banner in Bar.cs
                 It goes over multiple lines.  This line has Baz.cs
                 The last line includes Bar.cs */

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">/* This is the banner in Goo.cs
                 It goes over multiple lines.  This line has Baz.cs
                 The last line includes Goo.cs */

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">/* This is the banner in Bar.cs
                 It goes over multiple lines.  This line has Baz.cs
                 The last line includes Bar.cs */

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
        public async Task TestUpdateFileNameInComment3()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">/** This is the banner in Bar.cs
                 It goes over multiple lines.  This line has Baz.cs
                 The last line includes Bar.cs */

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">/** This is the banner in Goo.cs
                 It goes over multiple lines.  This line has Baz.cs
                 The last line includes Goo.cs */

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">/** This is the banner in Bar.cs
                 It goes over multiple lines.  This line has Baz.cs
                 The last line includes Bar.cs */

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
        public async Task TestUpdateFileNameInComment4()
        {
            await TestInRegularAndScriptAsync(
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">[||]using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">/// This is the banner in Bar.cs
                /// It goes over multiple lines.  This line has Baz.cs
                /// The last line includes Bar.cs

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """,
                """
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document FilePath="Goo.cs">/// This is the banner in Goo.cs
                /// It goes over multiple lines.  This line has Baz.cs
                /// The last line includes Goo.cs

                using System;

                class Program1
                {
                    static void Main()
                    {
                    }
                }
                        </Document>
                        <Document FilePath="Bar.cs">/// This is the banner in Bar.cs
                /// It goes over multiple lines.  This line has Baz.cs
                /// The last line includes Bar.cs

                class Program2
                {
                }
                        </Document>
                    </Project>
                </Workspace>
                """);
        }
    }
}
