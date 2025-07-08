// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
public sealed class AddDocCommentNodesCodesFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    private static readonly CSharpParseOptions Regular = new(kind: SourceCodeKind.Regular);

    public AddDocCommentNodesCodesFixProviderTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpAddDocCommentNodesCodeFixProvider());

    private async Task TestAsync(string initial, string expected)
    {
        var parseOptions = Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        await TestAsync(initial, expected, parseOptions: parseOptions);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_NoNodesBefore()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                public void Fizz(int [|i|], int j, int k) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_NoNodesAfter()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int [|k|]) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_NodesBeforeAndAfter()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int [|j|], int k) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_NodesBeforeAndAfter_RawTextInComment()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// text
                /// <param name="k"></param>
                public void Fizz(int i, int [|j|], int k) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// text
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_NodesBeforeAndAfter_WithContent()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i">Parameter <paramref name="i"/> does something</param>
                /// <param name="k">Parameter <paramref name="k"/> does something else</param>
                public void Fizz(int i, int [|j|], int k) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i">Parameter <paramref name="i"/> does something</param>
                /// <param name="j"></param>
                /// <param name="k">Parameter <paramref name="k"/> does something else</param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_NestedInSummaryTag()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// <param name="j"></param>
                /// </summary>
                public void Fizz(int i, int j, int [|k|]) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// </summary>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_BeforeNode_EverythingOnOneLine()
    {
        await TestAsync("""
            class Program
            {
                /// <summary></summary> <param name="j"></param>
                public void Fizz(int [|i|], int j, int k) {}
            }
            """, """
            class Program
            {
                /// <summary></summary>
                /// <param name="i"></param> <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_AfterNode_EverythingOnOneLine()
    {
        await TestAsync("""
            class Program
            {
                /// <summary></summary> <param name="j"></param>
                public void Fizz(int i, int j, int [|k|]) {}
            }
            """, """
            class Program
            {
                /// <summary></summary>
                /// <param name="i"></param> <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_BeforeNode_JustParamNode()
    {
        await TestAsync("""
            class Program
            {
                /// <param name="j"></param>
                public void Fizz(int [|i|], int j, int k) {}
            }
            """, """
            class Program
            {
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_AfterNode_JustParamNode()
    {
        await TestAsync("""
            class Program
            {
                /// <param name="j"></param>
                public void Fizz(int i, int j, int [|k|]) {}
            }
            """, """
            class Program
            {
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_MultipleDocComments()
    {
        await TestAsync("""
            class Program
            {
                /// <summary></summary>
                // ...
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                public void Fizz(int [|i|], int j, int k) {}
            }
            """, """
            class Program
            {
                /// <summary></summary>
                // ...
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_Ctor()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                public Program(int [|i|], int j, int k) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public Program(int i, int j, int k) {}
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_Delegate()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                public delegate int Goo(int [|i|], int j, int k);
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public delegate int Goo(int [|i|], int j, int k);
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_Operator()
    {
        await TestAsync("""
            public struct MyStruct
            {
                public int Val { get; }

                public MyStruct(int val)
                {
                    Val = val;
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="s1"></param>
                /// <returns></returns>
                public static MyStruct operator +(MyStruct s1, MyStruct [|s2|])
                {
                    return new MyStruct(s1.Val + s2.Val);
                }
            }
            """, """
            public struct MyStruct
            {
                public int Val { get; }

                public MyStruct(int val)
                {
                    Val = val;
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="s1"></param>
                /// <param name="s2"></param>
                /// <returns></returns>
                public static MyStruct operator +(MyStruct s1, MyStruct s2)
                {
                    return new MyStruct(s1.Val + s2.Val);
                }
            }
            """);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInDocument_MultipleParamNodesInVariousPlaces()
    {
        await TestAsync("""
            class Program
            {
                /// <summary>
                /// <param name="i"></param>
                /// </summary>
                /// <param name="k"></param>
                public void Fizz(int i, int {|FixAllInDocument:j|}, int k, int l) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// <param name="i"></param>
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <param name="l"></param>
                public void Fizz(int i, int j, int k, int l) {}
            }
            """);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInDocument()
    {
        await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, {|FixAllInDocument:int k|}) {}

                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <returns></returns>
                public int Buzz(int i, int j, int k) { returns 0; }
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program3
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}

                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <returns></returns>
                public int Buzz(int i, int j, int k) { returns 0; }
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program3
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInProject()
    {
        await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, {|FixAllInProject:int k|}) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <returns></returns>
                public int Buzz(int i, int j, int k) { returns 0; }
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program3
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <returns></returns>
                public int Buzz(int i, int j, int k) { returns 0; }
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <returns></returns>
                public int Buzz(int i, int j, int k) { returns 0; }
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document>
            <![CDATA[
            class Program3
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                /// <param name="k"></param>
                /// <returns></returns>
                public int Buzz(int i, int j, int k) { returns 0; }
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInSolution()
    {
        await TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, {|FixAllInSolution:int k|}) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program3
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program3
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="i"></param>
                /// <param name="j"></param>
                /// <param name="k"></param>
                public void Fizz(int i, int j, int k) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/52738")]
    public async Task AddsParamTag_Record()
    {
        await TestAsync("""
            /// <summary>
            /// 
            /// </summary>
            /// <param name="Second"></param>
            record R(int [|First|], int Second, int Third);
            """, """
            /// <summary>
            /// 
            /// </summary>
            /// <param name="First"></param>
            /// <param name="Second"></param>
            /// <param name="Third"></param>
            record R(int First, int Second, int Third);
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_Class()
    {
        await TestAsync("""
            /// <summary>
            /// 
            /// </summary>
            /// <param name="Second"></param>
            class R(int [|First|], int Second, int Third);
            """, """
            /// <summary>
            /// 
            /// </summary>
            /// <param name="First"></param>
            /// <param name="Second"></param>
            /// <param name="Third"></param>
            class R(int First, int Second, int Third);
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddDocCommentNodes)]
    public async Task AddsParamTag_Struct()
    {
        await TestAsync("""
            /// <summary>
            /// 
            /// </summary>
            /// <param name="Second"></param>
            struct R(int [|First|], int Second, int Third);
            """, """
            /// <summary>
            /// 
            /// </summary>
            /// <param name="First"></param>
            /// <param name="Second"></param>
            /// <param name="Third"></param>
            struct R(int First, int Second, int Third);
            """);
    }
}
