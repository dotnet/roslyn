// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

[Trait(Traits.Feature, Traits.Features.SignatureHelp)]
public class TupleConstructionSignatureHelpProviderTests : AbstractCSharpSignatureHelpProviderTests
{
    internal override Type GetSignatureHelpProviderType()
        => typeof(TupleConstructionSignatureHelpProvider);

    [Fact]
    public async Task InvocationAfterOpenParen()
    {
        var markup = """
            class C
            {
                (int, int) y = [|($$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, int)", currentParameterIndex: 0, parameterDocumentation: "")
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task InvocationWithNullableReferenceTypes()
    {
        var markup = """
            class C
            {
                (string?, string) y = [|($$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(string?, string)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/655607")]
    public async Task TestMissingTupleElement()
    {
        var markup = """
            class C
            {
                void M()
                {
                    (a, ) = [|($$
            |]  }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(object a, object)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task InvocationAfterOpenParen2()
    {
        var markup = """
            class C
            {
                (int, int) y = [|($$)|]
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, int)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task InvocationAfterComma1()
    {
        var markup = """
            class C
            {
                (int, int) y = [|(1,$$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, int)", currentParameterIndex: 1, parameterDocumentation: "")
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task InvocationAfterComma2()
    {
        var markup = """
            class C
            {
                (int, int) y = [|(1,$$)|]
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, int)", currentParameterIndex: 1)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task ParameterIndexWithNameTyped()
    {
        var markup = """
            class C
            {
                (int a, int b) y = [|(b: $$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            // currentParameterIndex only considers the position in the argument list 
            // and not names, hence passing 0 even though the controller will highlight
            // "int b" in the actual display
            new SignatureHelpTestItem("(int a, int b)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/14277")]
    public async Task NestedTuple()
    {
        var markup = """
            class C
            {
                (int a, (int b, int c)) y = [|(1, ($$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int b, int c)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task NestedTupleWhenNotInferred()
    {
        var markup = """
            class C
            {
                (int, object) y = [|(1, ($$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, object)", currentParameterIndex: 1)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task NestedTupleWhenNotInferred2()
    {
        var markup = """
            class C
            {
                (int, object) y = [|(1, (2,$$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, object)", currentParameterIndex: 1)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task NestedTupleWhenNotInferred3()
    {
        var markup = """
            class C
            {
                (int, object) y = [|(1, ($$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, object)", currentParameterIndex: 1)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task NestedTupleWhenNotInferred4()
    {
        var markup = """
            class C
            {
                (object, object) y = [|(($$
            |]}
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(object, object)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact]
    public async Task MultipleOverloads()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    Do1([|($$)|])
                }

                static void Do1((int, int) i) { }
                static void Do1((string, string) s) { }
            }
            """;

        var expectedOrderedItems = new List<SignatureHelpTestItem>
        {
            new SignatureHelpTestItem("(int, int)", currentParameterIndex: 0),
            new SignatureHelpTestItem("(string, string)", currentParameterIndex: 0)
        };

        await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14793")]
    public async Task DoNotCrashInLinkedFile()
    {
        var markup = """
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO">
                    <Document FilePath="SourceDocument"><![CDATA[
            class C
            {
            #if GOO
                void bar()
                {
                }
            #endif
                void goo()
                {
                    (int, string) x = ($$
                }
            }
            ]]>
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="Proj2">
                    <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="SourceDocument"/>
                </Project>
            </Workspace>
            """;
        var expectedDescription = new SignatureHelpTestItem($"(int, string)", currentParameterIndex: 0);
        await VerifyItemWithReferenceWorkerAsync(markup, new[] { expectedDescription }, false);
    }
}
