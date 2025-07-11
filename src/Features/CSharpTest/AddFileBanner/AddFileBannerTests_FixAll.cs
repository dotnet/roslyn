// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddFileBanner;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddFileBanner)]
[Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
public sealed partial class AddFileBannerTests
{
    [Fact]
    public Task FixAllInProject()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>{|FixAllInProject:|}using System;

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
                    <Document>
            class Program3
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
                    <Document>// This is the banner


            class Program3
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task FixAllInSolution()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>{|FixAllInSolution:|}using System;

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
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            class Program3
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
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>// This is the banner


            class Program3
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task FixAll_AlreadyHasBanner()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>{|FixAllInProject:|}using System;

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
                    <Document>
            class Program3
            {
            }
                    </Document>
                    <Document>// This is the banner
            // It goes over multiple lines

            class Program4
            {
            }
                    </Document>
                    <Document>/** This is the banner
            * It goes over multiple lines
            */

            class Program5
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
                    <Document>// This is the banner


            class Program3
            {
            }
                    </Document>
                    <Document>// This is the banner
            // It goes over multiple lines

            class Program4
            {
            }
                    </Document>
                    <Document>/** This is the banner
            * It goes over multiple lines
            */

            class Program5
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    public Task FixAll_UpdatedFileNameInBanner()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="Bar1.cs">{|FixAllInProject:|}using System;

            class Program1
            {
                static void Main()
                {
                }
            }
                    </Document>
                    <Document FilePath="Bar2.cs">// This is the banner in Bar2.cs
            // It goes over multiple lines.  This line has Baz.cs
            // The last line includes Bar2.cs

            class Program2
            {
            }
                    </Document>
                    <Document FilePath="Bar3.cs">
            class Program3
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="Bar1.cs">// This is the banner in Bar1.cs
            // It goes over multiple lines.  This line has Baz.cs
            // The last line includes Bar1.cs

            using System;

            class Program1
            {
                static void Main()
                {
                }
            }
                    </Document>
                    <Document FilePath="Bar2.cs">// This is the banner in Bar2.cs
            // It goes over multiple lines.  This line has Baz.cs
            // The last line includes Bar2.cs

            class Program2
            {
            }
                    </Document>
                    <Document FilePath="Bar3.cs">// This is the banner in Bar3.cs
            // It goes over multiple lines.  This line has Baz.cs
            // The last line includes Bar3.cs


            class Program3
            {
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Theory]
    [InlineData(FixAllScope.ContainingMember)]
    [InlineData(FixAllScope.ContainingType)]
    [InlineData(FixAllScope.Document)]
    public async Task FixAllScopes_NotApplicable(FixAllScope fixAllScope)
    {
        var fixAllScopeString = $"FixAllIn{fixAllScope}";

        await TestMissingInRegularAndScriptAsync(
            $$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>{|{{fixAllScopeString}}:|}using System;

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
}
