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
    public Task TestCount()
        => RunCountTest("""
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
            """);

    [Fact]
    public Task TestCapping()
        => RunCountTest("""
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
            """, 1);

    [Fact]
    public Task TestLinkedFiles()
        => RunReferenceTest("""
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
            """);

    [Fact]
    public Task TestDisplay()
        => RunReferenceTest("""
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
            """);

    [Fact]
    public Task TestMethodReferences()
        => RunMethodReferenceTest("""
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
            """);

    [Fact]
    public Task TestMethodReferencesWithDocstrings()
        => RunMethodReferenceTest("""
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
            """);

    [Theory]
    [InlineData("class")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public Task TestFullyQualifiedName(string typeKind)
        => RunFullyQualifiedNameTest($$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="CurrentDocument.cs"><![CDATA[
            public {{typeKind}} A
            {
                {|A.C: public void C()
                {
                    C();
                }|}

                public {{typeKind}} B
                {
                    {|A+B.C: public void C()
                    {
                        C();
                    }|}

                    public {{typeKind}} D
                    {
                        {|A+B+D.C: public void C()
                        {
                            C();
                        }|}
                    }
                }
            }
            ]]>
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public Task TestExplicitParameterlessConstructor()
        => RunReferenceTest("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public Task TestExplicitParameterlessConstructor_TwoCalls()
        => RunReferenceTest("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public Task TestImplicitParameterlessConstructor()
        => RunReferenceTest("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49636")]
    public Task TestImplicitParameterlessConstructor_TwoCalls()
        => RunReferenceTest("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51633")]
    public Task TestMethodRefSourceGeneratedDocument()
        => RunMethodReferenceTest("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64592")]
    public Task TestFileScopedTypes()
        => RunReferenceTest("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1">
                    <Document FilePath="File1.cs"><![CDATA[
            namespace TestNamespace
            {
                {|1:file class C|}
                {
                    public C ()
                    {
                    }

                    void M()
                    {
                        var t1 = new T();
                        var t2 = new T();
                    }
                }

                {|2:file class T|}
                {
                }
            }]]>
                    </Document>
                    <Document FilePath="File2.cs"><![CDATA[
            namespace TestNamespace
            {
                {|0:file class C|}
                {
                    void M()
                    {
                        var t1 = new T();
                        var t2 = new T();
                    }
                }
            
                {|2:file class T|}
                {
                }
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67956")]
    public Task TestConstructorReferencesInOtherProject()
        => RunReferenceTest("""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                    <Document FilePath="Class1.cs"><![CDATA[
            namespace ClassLibrary1
            {
                public class Class1
                {
                    {|2:public Class1()|}
                    {
                    }
                }
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary2">
                    <ProjectReference>ClassLibrary1</ProjectReference>
                    <Document FilePath="Class2.cs"><![CDATA[
            using ClassLibrary1;

            namespace ClassLibrary2;

            public class Class2
            {
                static Class1 x = new Class1();
                static Class1 y = new();
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);
}
