// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class DocumentationDirectiveCodeGenerationTest_Legacy()
    : IntegrationTestBase(TestProject.Layer.Compiler)
{
    private RazorConfiguration _configuration =
        RazorConfiguration.Default with { LanguageVersion = RazorLanguageVersion.Preview };

    protected override RazorConfiguration Configuration => _configuration;

    protected override CSharpParseOptions CSharpParseOptions { get; } =
        new(LanguageVersion.Preview, documentationMode: DocumentationMode.Diagnose);

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        => RazorExtensions.Register(builder);

    [Fact]
    public void Documentation()
    {
        var result = VerifyBaselineWithSourceMappings();

        var compiled = CompileToAssembly(result);
        var @namespace = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryNamespace();
        var @class = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryClass();
        Assert.NotNull(@namespace);
        Assert.NotNull(@class);
        var type = compiled.Compilation.GetTypeByMetadataName($"{@namespace.Name}.{@class.Name}");
        Assert.NotNull(type);
        Assert.Contains("<remarks>More documentation.</remarks>", type.GetDocumentationCommentXml());
    }

    private CompiledCSharpCode VerifyBaseline([CallerMemberName] string testName = "")
    {
        var result = CompileToCSharp(CreateProjectItemFromFile(testName: testName));
        AssertSyntaxTreeMatchesBaseline(result.CodeDocument, testName);
        AssertDocumentNodeMatchesBaseline(result.CodeDocument.GetRequiredDocumentNode(), testName);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument.GetRequiredImplCSharpDocument(), testName);

        return result;
    }

    private CompiledCSharpCode VerifyBaselineWithSourceMappings([CallerMemberName] string testName = "")
    {
        var result = VerifyBaseline(testName);
        AssertSourceMappingsMatchBaseline(result.CodeDocument, testName);
        return result;
    }
}
