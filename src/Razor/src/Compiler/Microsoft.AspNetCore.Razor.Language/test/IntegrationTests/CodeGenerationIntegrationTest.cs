// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class CodeGenerationIntegrationTest : IntegrationTestBase
{
    private readonly bool designTime;

    public CodeGenerationIntegrationTest(bool designTime = false)
        : base(layer: TestProject.Layer.Compiler)
    {
        this.designTime = designTime;
        var testTagHelpers = CSharpCompilation.Create(
            assemblyName: "Microsoft.AspNetCore.Razor.Language.Test",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(TestTagHelperDescriptors.Code),
            ],
            references: ReferenceUtil.AspNetLatestAll,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        BaseCompilation = BaseCompilation.AddReferences(testTagHelpers.VerifyDiagnostics().EmitToImageReference());
    }

    [IntegrationTestFact]
    public void SingleLineControlFlowStatements() => RunTest();

    [IntegrationTestFact]
    public void CSharp8()
    {
        // C# 8 features are not available in .NET Framework without polyfills
        // so the C# diagnostics would be different between .NET Framework and .NET Core.
        SkipVerifyingCSharpDiagnostics = ExecutionConditionUtil.IsDesktop;

        NullableEnable = true;

        RunTest();
    }

    [IntegrationTestFact]
    public void IncompleteDirectives() => RunTest();

    [IntegrationTestFact]
    public void CSharp7() => RunTest();

    [IntegrationTestFact]
    public void UnfinishedExpressionInCode() => RunTest();

    [IntegrationTestFact]
    public void Templates() => RunTest();

    [IntegrationTestFact]
    public void Markup_InCodeBlocks() => RunTest();

    [IntegrationTestFact]
    public void Markup_InCodeBlocksWithTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void StringLiterals() => RunTest();

    [IntegrationTestFact]
    public void SimpleUnspacedIf() => RunTest();

    [IntegrationTestFact]
    public void Sections() => RunTest();

    [IntegrationTestFact]
    public void RazorComments() => RunTest();

    [IntegrationTestFact]
    public void ParserError() => RunTest();

    [IntegrationTestFact]
    public void OpenedIf() => RunTest();

    [IntegrationTestFact]
    public void NullConditionalExpressions() => RunTest();

    [IntegrationTestFact]
    public void NoLinePragmas() => RunTest();

    [IntegrationTestFact]
    public void NestedCSharp() => RunTest();

    [IntegrationTestFact]
    public void NestedCodeBlocks() => RunTest();

    [IntegrationTestFact]
    public void MarkupInCodeBlock() => RunTest();

    [IntegrationTestFact]
    public void Instrumented() => RunTest();

    [IntegrationTestFact]
    public void InlineBlocks() => RunTest();

    [IntegrationTestFact]
    public void Inherits() => RunTest();

    [IntegrationTestFact]
    public void Usings() => RunTest();

    [IntegrationTestFact]
    public void Usings_OutOfOrder() => RunTest();

    [IntegrationTestFact]
    public void ImplicitExpressionAtEOF() => RunTest();

    [IntegrationTestFact]
    public void ImplicitExpression() => RunTest();

    [IntegrationTestFact]
    public void HtmlCommentWithQuote_Double() => RunTest();

    [IntegrationTestFact]
    public void HtmlCommentWithQuote_Single() => RunTest();

    [IntegrationTestFact]
    public void HiddenSpansInCode() => RunTest();

    [IntegrationTestFact]
    public void FunctionsBlock() => RunTest();

    [IntegrationTestFact]
    public void FunctionsBlockMinimal() => RunTest();

    [IntegrationTestFact]
    public void ExpressionsInCode() => RunTest();

    [IntegrationTestFact]
    public void ExplicitExpressionWithMarkup() => RunTest();

    [IntegrationTestFact]
    public void ExplicitExpressionAtEOF() => RunTest();

    [IntegrationTestFact]
    public void ExplicitExpression() => RunTest();

    [IntegrationTestFact]
    public void EmptyImplicitExpressionInCode() => RunTest();

    [IntegrationTestFact]
    public void EmptyImplicitExpression() => RunTest();

    [IntegrationTestFact]
    public void EmptyExplicitExpression() => RunTest();

    [IntegrationTestFact]
    public void EmptyCodeBlock() => RunTest();

    [IntegrationTestFact]
    public void ConditionalAttributes() => RunTest();

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttributes2()
    {
        if (designTime)
        {
            // An error scenario: tag helper + C# dynamic content (a razor error is reported,
            // so it is fine there is a missing mapping for the C# dynamic content).
            ExpectedMissingSourceMappings = new()
            {
                { new(base.GetTestFileName() + ".cshtml", 328, 11, 8), "s" }
            };
        }

        RunTest();
    }

    [IntegrationTestFact]
    public void CodeBlockWithTextElement() => RunTest();

    [IntegrationTestFact]
    public void CodeBlockAtEOF() => RunTest();

    [IntegrationTestFact]
    public void CodeBlock() => RunTest();

    [IntegrationTestFact]
    public void Blocks() => RunTest();

    [IntegrationTestFact]
    public void Await() => RunTest();

    [IntegrationTestFact]
    public void Tags() => RunTest();

    [IntegrationTestFact]
    public void SimpleTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void TagHelpersWithBoundAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void TagHelpersWithBoundAttributesAndRazorComment() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void TagHelpersWithPrefix() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void NestedTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void SingleTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void SingleTagHelperWithNewlineBeforeAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void TagHelpersWithWeirdlySpacedAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void IncompleteTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void BasicTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void BasicTagHelpers_Prefixed() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void BasicTagHelpers_RemoveTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void CssSelectorTagHelperAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.CssSelectorTagHelperDescriptors);

    [IntegrationTestFact]
    public void ComplexTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void EmptyAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void EscapedTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void DuplicateTargetTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DuplicateTargetTagHelperDescriptors);

    [IntegrationTestFact]
    public void AttributeTargetingTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.AttributeTargetingTagHelperDescriptors);

    [IntegrationTestFact]
    public void PrefixedAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.PrefixedAttributeTagHelperDescriptors);

    [IntegrationTestFact]
    public void DuplicateAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void DynamicAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DynamicAttributeTagHelpers_Descriptors);

    [IntegrationTestFact]
    public void TransitionsInTagHelperAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void MinimizedTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.MinimizedTagHelpers_Descriptors);

    [IntegrationTestFact]
    public void NestedScriptTagTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [IntegrationTestFact]
    public void SymbolBoundAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.SymbolBoundTagHelperDescriptors);

    [IntegrationTestFact]
    public void EnumTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.EnumTagHelperDescriptors);

    [IntegrationTestFact]
    public void TagHelpersInSection() => RunTagHelpersTest(TestTagHelperDescriptors.TagHelpersInSectionDescriptors);

    [IntegrationTestFact]
    public void TagHelpersWithTemplate() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void TagHelpersWithDataDashAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact]
    public void Implements() => RunTest();

    [IntegrationTestFact]
    public void Implements_Multiple() => RunTest();

    [IntegrationTestFact]
    public void AttributeDirective() => RunTest();

    [IntegrationTestFact]
    public void SwitchExpression_RecursivePattern()
    {
        // System.Index is not available in .NET Framework without polyfills
        // so the C# diagnostics would be different between .NET Framework and .NET Core.
        SkipVerifyingCSharpDiagnostics = ExecutionConditionUtil.IsDesktop;

        RunTest();
    }

    [IntegrationTestFact]
    public new void DesignTime() => RunTest();

    [IntegrationTestFact]
    public void RemoveTagHelperDirective() => RunTest();

    [IntegrationTestFact]
    public void AddTagHelperDirective() => RunTest();

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void EscapedIdentifier() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [IntegrationTestFact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void EscapedExpression() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    public override string GetTestFileName([CallerMemberName] string? testName = null)
    {
        return base.GetTestFileName(testName) + (designTime ? "_DesignTime" : "_Runtime");
    }

    private void RunTest([CallerMemberName] string testName = "")
    {
        if (designTime)
        {
            DesignTimeTest(testName);
        }
        else
        {
            RunTimeTest(testName);
        }
    }

    private void DesignTimeTest(string testName)
    {
        // Arrange
        var projectEngine = CreateProjectEngine(RazorExtensions.Register);

        var projectItem = CreateProjectItemFromFile(testName: testName);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode(), testName);
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(codeDocument), testName);
        AssertCSharpDocumentMatchesBaseline(codeDocument.GetRequiredCSharpDocument(), testName);
        AssertSourceMappingsMatchBaseline(codeDocument, testName);
        AssertLinePragmas(codeDocument);
        AssertCSharpDiagnosticsMatchBaseline(codeDocument, testName);
    }

    private void RunTimeTest(string testName)
    {
        // Arrange
        var projectEngine = CreateProjectEngine(RazorExtensions.Register);

        var projectItem = CreateProjectItemFromFile(testName: testName);

        // Act
        var codeDocument = projectEngine.Process(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode(), testName);
        AssertCSharpDocumentMatchesBaseline(codeDocument.GetRequiredCSharpDocument(), testName);
        AssertLinePragmas(codeDocument);
        AssertCSharpDiagnosticsMatchBaseline(codeDocument, testName);
    }

    private void RunTagHelpersTest(TagHelperCollection tagHelpers, [CallerMemberName] string testName = "")
    {
        if (designTime)
        {
            RunDesignTimeTagHelpersTest(tagHelpers, testName);
        }
        else
        {
            RunRuntimeTagHelpersTest(tagHelpers, testName);
        }
    }

    private void RunRuntimeTagHelpersTest(TagHelperCollection tagHelpers, string testName)
    {
        // Arrange
        var projectEngine = CreateProjectEngine(RazorExtensions.Register);

        var projectItem = CreateProjectItemFromFile(testName: testName);
        var imports = GetImports(projectEngine, projectItem);

        AddTagHelperStubs(tagHelpers);

        // Act
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, imports, tagHelpers);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode(), testName);
        AssertCSharpDocumentMatchesBaseline(codeDocument.GetRequiredCSharpDocument(), testName);
        AssertCSharpDiagnosticsMatchBaseline(codeDocument, testName);
    }

    private void RunDesignTimeTagHelpersTest(TagHelperCollection tagHelpers, string testName)
    {
        // Arrange
        var projectEngine = CreateProjectEngine(RazorExtensions.Register);

        var projectItem = CreateProjectItemFromFile(testName: testName);
        var imports = GetImports(projectEngine, projectItem);

        AddTagHelperStubs(tagHelpers);

        // Act
        var codeDocument = projectEngine.ProcessDesignTime(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, imports, tagHelpers);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode(), testName);
        AssertCSharpDocumentMatchesBaseline(codeDocument.GetRequiredCSharpDocument(), testName);
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(codeDocument), testName);
        AssertSourceMappingsMatchBaseline(codeDocument, testName);
        AssertCSharpDiagnosticsMatchBaseline(codeDocument, testName);
    }

    private static ImmutableArray<RazorSourceDocument> GetImports(RazorProjectEngine projectEngine, RazorProjectItem projectItem)
    {
        using var result = new PooledArrayBuilder<RazorSourceDocument>();

        foreach (var import in projectEngine.GetImports(projectItem, static i => i.Exists))
        {
            result.Add(RazorSourceDocument.ReadFrom(import));
        }

        return result.ToImmutable();
    }

    private void AddTagHelperStubs(TagHelperCollection tagHelpers)
    {
        var tagHelperClasses = tagHelpers.Select(descriptor =>
        {
            var typeName = descriptor.TypeName;
            var namespaceSeparatorIndex = typeName.LastIndexOf('.');
            if (namespaceSeparatorIndex >= 0)
            {
                var ns = typeName[..namespaceSeparatorIndex];
                var c = typeName[(namespaceSeparatorIndex + 1)..];

                return $$"""
                    namespace {{ns}}
                    {
                        class {{c}} {{getTagHelperBody(descriptor)}}
                    }
                    """;
            }

            return $$"""
                class {{typeName}} {{getTagHelperBody(descriptor)}}
                """;

            static string getTagHelperBody(TagHelperDescriptor descriptor)
            {
                var attributes = descriptor.BoundAttributes.Select(attribute => $$"""
                    public {{attribute.TypeName}} {{attribute.PropertyName}}
                    {
                        get => throw new System.NotImplementedException();
                        set { }
                    }
                    """);

                return $$"""
                    : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
                    {
                        {{string.Join("\n", attributes)}}
                    }
                    """;
            }
        });

        AddCSharpSyntaxTree(string.Join("\n", tagHelperClasses));
    }

    [IntegrationTestFact]
    public void Utf8StringLiterals() => RunTest();
}
