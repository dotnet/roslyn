// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.PreferFrameworkType;

public partial class PreferFrameworkTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInDocument_DeclarationContext()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                static int F(int x, System.Int16 y)
                {
                    {|FixAllInDocument:int|} i1 = 0;
                    System.Int16 s1 = 0;
                    int i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                static System.Int32 F(System.Int32 x, System.Int16 y)
                {
                    System.Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    System.Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program2
            {
                static System.Int32 F(System.Int32 x, System.Int16 y)
                {
                    System.Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    System.Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                static Int32 F(Int32 x, System.Int16 y)
                {
                    Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                static System.Int32 F(System.Int32 x, System.Int16 y)
                {
                    System.Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    System.Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program2
            {
                static System.Int32 F(System.Int32 x, System.Int16 y)
                {
                    System.Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    System.Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, new(options: FrameworkTypeEverywhere));

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInProject_DeclarationContext()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                static int F(int x, System.Int16 y)
                {
                    {|FixAllInProject:int|} i1 = 0;
                    System.Int16 s1 = 0;
                    int i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                static int F(int x, System.Int16 y)
                {
                    int i1 = 0;
                    System.Int16 s1 = 0;
                    int i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program2
            {
                static System.Int32 F(System.Int32 x, System.Int16 y)
                {
                    System.Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    System.Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                static Int32 F(Int32 x, System.Int16 y)
                {
                    Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                static Int32 F(Int32 x, System.Int16 y)
                {
                    Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program2
            {
                static System.Int32 F(System.Int32 x, System.Int16 y)
                {
                    System.Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    System.Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, new(options: FrameworkTypeEverywhere));

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInSolution_DeclarationContext()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                static int F(int x, System.Int16 y)
                {
                    {|FixAllInSolution:int|} i1 = 0;
                    System.Int16 s1 = 0;
                    int i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                static int F(int x, System.Int16 y)
                {
                    int i1 = 0;
                    System.Int16 s1 = 0;
                    int i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program2
            {
                static int F(int x, System.Int16 y)
                {
                    int i1 = 0;
                    System.Int16 s1 = 0;
                    int i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;

            class Program
            {
                static Int32 F(Int32 x, System.Int16 y)
                {
                    Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;

            class Program2
            {
                static Int32 F(Int32 x, System.Int16 y)
                {
                    Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;

            class Program2
            {
                static Int32 F(Int32 x, System.Int16 y)
                {
                    Int32 i1 = 0;
                    System.Int16 s1 = 0;
                    Int32 i2 = 0;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, new(options: FrameworkTypeEverywhere));

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseFrameworkType)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInSolution_MemberAccessContext()
        => TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            class ProgramA
            {
                private int x = 0;
                private int y = 0;
                private int z = 0;

                private System.Int32 F(System.Int32 p1, System.Int16 p2)
                {
                    System.Int32 i1 = this.x;
                    System.Int16 s1 = this.y;
                    System.Int32 i2 = this.z;
                    {|FixAllInSolution:int|}.Parse(i1 + s1 + i2);
                    int.Parse(i1 + s1 + i2);
                    System.Exception ex = null;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;
            class ProgramA2
            {
                private int x = 0;
                private int y = 0;
                private int z = 0;

                private System.Int32 F(System.Int32 p1, System.Int16 p2)
                {
                    System.Int32 i1 = this.x;
                    System.Int16 s1 = this.y;
                    System.Int32 i2 = this.z;
                    int.Parse(i1 + s1 + i2);
                    int.Parse(i1 + s1 + i2);
                    System.Exception ex = null;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            class ProgramA3
            {
                private int x = 0;
                private int y = 0;
                private int z = 0;

                private System.Int32 F(System.Int32 p1, System.Int16 p2)
                {
                    System.Int32 i1 = this.x;
                    System.Int16 s1 = this.y;
                    System.Int32 i2 = this.z;
                    int.Parse(i1 + s1 + i2);
                    int.Parse(i1 + s1 + i2);
                    System.Exception ex = null;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            class ProgramA
            {
                private Int32 x = 0;
                private Int32 y = 0;
                private Int32 z = 0;

                private System.Int32 F(System.Int32 p1, System.Int16 p2)
                {
                    System.Int32 i1 = this.x;
                    System.Int16 s1 = this.y;
                    System.Int32 i2 = this.z;
                    Int32.Parse(i1 + s1 + i2);
                    Int32.Parse(i1 + s1 + i2);
                    System.Exception ex = null;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                    <Document>
            using System;
            class ProgramA2
            {
                private Int32 x = 0;
                private Int32 y = 0;
                private Int32 z = 0;

                private System.Int32 F(System.Int32 p1, System.Int16 p2)
                {
                    System.Int32 i1 = this.x;
                    System.Int16 s1 = this.y;
                    System.Int32 i2 = this.z;
                    Int32.Parse(i1 + s1 + i2);
                    Int32.Parse(i1 + s1 + i2);
                    System.Exception ex = null;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            using System;
            class ProgramA3
            {
                private Int32 x = 0;
                private Int32 y = 0;
                private Int32 z = 0;

                private System.Int32 F(System.Int32 p1, System.Int16 p2)
                {
                    System.Int32 i1 = this.x;
                    System.Int16 s1 = this.y;
                    System.Int32 i2 = this.z;
                    Int32.Parse(i1 + s1 + i2);
                    Int32.Parse(i1 + s1 + i2);
                    System.Exception ex = null;
                    return i1 + s1 + i2;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, new(options: FrameworkTypeEverywhere));
}
