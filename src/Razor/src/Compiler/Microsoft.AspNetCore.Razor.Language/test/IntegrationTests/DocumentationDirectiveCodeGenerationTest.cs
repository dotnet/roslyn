// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Xunit;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class DocumentationDirectiveCodeGenerationTest()
    : RazorBaselineIntegrationTestBase(TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => Path.Combine("TestFiles", "IntegrationTests", nameof(DocumentationDirectiveCodeGenerationTest), testName);

    [Fact]
    public void Documentation()
    {
        VerifyBaseline("""
            @attribute [System.Serializable]
            @documentation {
                <summary>
                Hello from <see cref="System.String"/>.
                </summary>
                <remarks>More documentation.</remarks>
            }
            <p>Rendered content</p>
            """);
    }

    private CompileToCSharpResult VerifyBaseline(
        string source,
        RazorConfiguration? configuration = null,
        [CallerMemberName] string testName = "")
    {
        var result = CompileToCSharp(source, configuration: configuration);
        AssertSyntaxTreeMatchesBaseline(result.CodeDocument, testName);
        AssertDocumentNodeMatchesBaseline(result.CodeDocument, testName);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument, testName: testName);
        CompileToAssembly(result);
        return result;
    }
}
