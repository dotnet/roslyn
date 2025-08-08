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

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
public sealed class RemoveDocCommentNodeCodeFixProviderTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    private static readonly CSharpParseOptions Regular = new(kind: SourceCodeKind.Regular);

    public RemoveDocCommentNodeCodeFixProviderTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpRemoveDocCommentNodeCodeFixProvider());

    private async Task TestAsync(string initial, string expected)
    {
        var parseOptions = Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        await TestAsync(initial, expected, new(parseOptions: parseOptions));
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param [|name="value"|]></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag_OnlyParamTags()
        => TestAsync("""
            class Program
            {
                /// <param name="value"></param>
                /// <param [|name="value"|]></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag_TagBelowOffendingParamTag()
        => TestAsync("""
            class Program
            {
                /// <param name="value"></param>
                /// <param [|name="value"|]></param>
                /// <returns></returns>
                public int Fizz(int value) { return 0; }
            }
            """, """
            class Program
            {
                /// <param name="value"></param>
                /// <returns></returns>
                public int Fizz(int value) { return 0; }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_DocCommentTagBetweenThem()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>    /// <param [|name="value"|]></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_WhitespaceBetweenThem()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>     <param [|name="value"|]></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag_BothParamTagsOnSameLine_NothingBetweenThem1()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param><param [|name="value"|]></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13436")]
    public Task RemovesTag_BothParamTagsOnSameLine_NothingBetweenThem2()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param [|name="a"|]></param><param name="value"></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/13436")]
    public Task RemovesTag_TrailingTextAfterTag()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param [|name="a"|]></param> a
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                ///  a
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateParamTag_RawTextBeforeAndAfterNode()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// some comment<param [|name="value"|]></param>out of the XML nodes
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// some commentout of the XML nodes
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesDuplicateTypeparamTag()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <typeparam name="T"></typeparam>
                /// <typeparam [|name="T"|]></typeparam>
                public void Fizz<T>() { }
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <typeparam name="T"></typeparam>
                public void Fizz<T>() { }
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesParamTagWithNoMatchingParameter()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="[|val|]"></param>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// 
                /// </summary>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesParamTag_NestedInSummaryTag()
        => TestAsync("""
            class Program
            {
                /// <summary>
                /// <param name="value"></param>
                /// <param [|name="value"|]></param>
                /// </summary>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                /// <param name="value"></param>
                /// </summary>
                public void Fizz(int value) {}
            }
            """);

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)]
    public Task RemovesParamTag_NestedInSummaryTag_WithChildren()
        => TestAsync("""
            class Program
            {
                /// <summary>
                ///   <param name="value"></param>
                ///   <param [|name="value"|]>
                ///     <xmlnode></xmlnode>
                ///   </param>
                /// </summary>
                public void Fizz(int value) {}
            }
            """, """
            class Program
            {
                /// <summary>
                ///   <param name="value"></param>
                /// </summary>
                public void Fizz(int value) {}
            }
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllTypeparamInDocument_DoesNotFixDuplicateParamTags()
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                /// <typeparam name="T"></typeparam>
                /// <typeparam {|FixAllInDocument:name="T"|}></typeparam>
                /// <typeparam name="U"></typeparam>
                public void Fizz<T, U>(int value) {}

                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                /// <typeparam name="T"></typeparam>
                /// <typeparam name="T"></typeparam>
                /// <typeparam name="U"></typeparam>
                /// <returns></returns>
                public int Buzz<T, U>(int value) { returns 0; }
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                /// <typeparam name="T"></typeparam>
                /// <typeparam name="U"></typeparam>
                public void Fizz<T, U>(int value) {}

                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                /// <typeparam name="T"></typeparam>
                /// <typeparam name="U"></typeparam>
                /// <returns></returns>
                public int Buzz<T, U>(int value) { returns 0; }
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInDocument()
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param {|FixAllInDocument:name="value"|}></param>
                public void Fizz(int value) {}

                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                /// <returns></returns>
                public int Buzz(int value) { returns 0; }
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                public void Fizz(int value) {}

                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <returns></returns>
                public int Buzz(int value) { returns 0; }
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInProject()
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param {|FixAllInProject:name="value"|}></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public Task TestFixAllInSolution()
        => TestAsync("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" DocumentationMode="Diagnose">
                    <Document>
            <![CDATA[
            class Program1
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param {|FixAllInSolution:name="value"|}></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                    <Document>
            <![CDATA[
            class Program2
            {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="value"></param>
                public void Fizz(int value) {}
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
                /// <param name="value"></param>
                public void Fizz(int value) {}
            }]]>
                    </Document>
                </Project>
            </Workspace>
            """);
}
