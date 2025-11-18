// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.AddFileBanner;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddFileBanner;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
public sealed partial class AddFileBannerTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpAddFileBannerCodeRefactoringProvider();

    [Fact]
    public Task TestBanner1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLineBanner1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public Task TestSingleLineDocCommentBanner()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public Task TestMultiLineDocCommentBanner()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingWhenAlreadyThere()
        => TestMissingAsync(
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

    [Theory]
    [InlineData("", 1)]
    [InlineData("file_header_template =", 1)]
    [InlineData("file_header_template = unset", 1)]
    [InlineData("file_header_template = defined file header", 0)]
    public Task TestMissingWhenHandledByAnalyzer(string fileHeaderTemplate, int expectedActionCount)
        => TestActionCountAsync($$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="/0/Test0.cs">[||]using System;

            class Program1
            {
                static void Main()
                {
                }
            }
                    </Document>
                    <Document FilePath="/0/Test1.cs">/// This is the banner
            /// It goes over multiple lines

            class Program2
            {
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath="/.editorconfig">
            root = true

            [*]
            {{fileHeaderTemplate}}
                    </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """, expectedActionCount);

    [Fact]
    public Task TestMissingIfOtherFileDoesNotHaveBanner()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingIfOtherFileIsAutoGenerated()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32792")]
    public Task TestUpdateFileNameInComment()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32792")]
    public Task TestUpdateFileNameInComment2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public Task TestUpdateFileNameInComment3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")]
    public Task TestUpdateFileNameInComment4()
        => TestInRegularAndScriptAsync(
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
