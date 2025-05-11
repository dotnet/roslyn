// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeLens;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeLens;

[Trait(Traits.Feature, Traits.Features.CodeLens)]
public sealed class CSharpCodeLensTests : AbstractCodeLensTest
{
    [Fact]
    public async Task TestCount()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public class A
            {
                {|0: public void B()
                {
                    C();
                }|}

                {|2: public void C()
                {
                    D();
                }|}

                {|1: public void D()
                {
                    C();
                }|}
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunCountTest(input);
    }

    [Fact]
    public async Task TestCapping()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public class A
            {
                {|0: public void B()
                {
                    C();
                }|}

                {|capped1: public void C()
                {
                    D();
                }|}

                {|1: public void D()
                {
                    C();
                }|}
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;

        await RunCountTest(input, 1);
    }

    [Fact]
    public async Task TestLinkedFiles()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public class A
            {
                {|0: public void B()
                {
                    C();
                }|}

                {|3: public void C()
                {
                    D();
                }|}

                {|3: public void D()
                {
                    C();
                }|}
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.cs"/>
                    <Document FilePath="AdditionalDocument.cs"><![CDATA[
            class E
            {
                void F()
                {
                    A.C();
                    A.D();
                    A.D();
                }
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;

        await RunReferenceTest(input);
    }

    [Fact]
    public async Task TestDisplay()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public class A
            {
                {|0: public void B()
                {
                    C();
                }|}

                {|2: public void C()
                {
                    D();
                }|}

                {|1: public void D()
                {
                    C();
                }|}
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;

        await RunReferenceTest(input);
    }

    [Fact]
    public async Task TestMethodReferences()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public class A
            {
                {|0: public void B()
                {
                    C();
                }|}

                {|2: public void C()
                {
                    D();
                }|}

                {|1: public void D()
                {
                    C();
                }|}
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunMethodReferenceTest(input);
    }

    [Fact]
    public async Task TestMethodReferencesWithDocstrings()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public class A
            {
                /// <summary>
                ///     <see cref="A.C"/>
                /// </summary>
                {|0: public void B()
                {
                    C();
                }|}

                {|2: public void C()
                {
                    D();
                }|}

                {|1: public void D()
                {
                    C();
                }|}
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunMethodReferenceTest(input);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public async Task TestFullyQualifiedName(string typeKind)
    {
        var input = $@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
public {typeKind} A
{{
    {{|A.C: public void C()
    {{
        C();
    }}|}}

    public {typeKind} B
    {{
        {{|A+B.C: public void C()
        {{
            C();
        }}|}}

        public {typeKind} D
        {{
            {{|A+B+D.C: public void C()
            {{
                C();
            }}|}}
        }}
    }}
}}
]]>
        </Document>
    </Project>
</Workspace>";
        await RunFullyQualifiedNameTest(input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public async Task TestExplicitParameterlessConstructor()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            {|2:public class Foo|}
            {
                public Foo() { }
            }
            public class B
            {
                private void Test()
                {
                    var foo = new Foo();
                }
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunReferenceTest(input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public async Task TestExplicitParameterlessConstructor_TwoCalls()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            {|3:public class Foo|}
            {
                public Foo() { }
            }
            public class B
            {
                private void Test()
                {
                    var foo1 = new Foo();
                    var foo2 = new Foo();
                }
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunReferenceTest(input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public async Task TestImplicitParameterlessConstructor()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            {|1:public class Foo|}
            {
            }
            public class B
            {
                private void Test()
                {
                    var foo = new Foo();
                }
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunReferenceTest(input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public async Task TestImplicitParameterlessConstructor_TwoCalls()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            {|2:public class Foo|}
            {
            }
            public class B
            {
                private void Test()
                {
                    var foo1 = new Foo();
                    var foo2 = new Foo();
                }
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """;
        await RunReferenceTest(input);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51633")]
    public async Task TestMethodRefSourceGeneratedDocument()
    {
        const string input = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="Program.cs"><![CDATA[
            namespace ConsoleSample
            {
                class Program
                {
                    {|1:public Program()
                    {
                    }|}
                }
            }]]>
                    </Document>
                    <DocumentFromSourceGenerator><![CDATA[
            namespace ConsoleSample
            {
                internal partial class Program
                {
                    public static CreateProgram() => new Program();
                }
            }]]>
                    </DocumentFromSourceGenerator>
                </Project>
            </Workspace>
            """;
        await RunMethodReferenceTest(input);
    }
}
