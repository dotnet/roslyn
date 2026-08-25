// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class CodeGenerationIntegrationTest : IntegrationTestBase
{
    public CodeGenerationIntegrationTest()
        : base(layer: TestProject.Layer.Compiler)
    {
        var testTagHelpers = CSharpCompilation.Create(
            assemblyName: "Microsoft.AspNetCore.Razor.Language.Test",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(Microsoft.CodeAnalysis.Text.SourceText.From(TestTagHelperDescriptors.Code, System.Text.Encoding.UTF8)),
            ],
            references: ReferenceUtil.AspNetLatestAll,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        BaseCompilation = BaseCompilation.AddReferences(testTagHelpers.VerifyDiagnostics().EmitToImageReference());
    }

    [Fact]
    public void SingleLineControlFlowStatements() => RunTest();

    [Fact]
    public void CSharp8()
    {
        // C# 8 features are not available in .NET Framework without polyfills
        // so the C# diagnostics would be different between .NET Framework and .NET Core.
        SkipVerifyingCSharpDiagnostics = ExecutionConditionUtil.IsDesktop;

        NullableEnable = true;

        RunTest();
    }

    [Fact]
    public void IncompleteDirectives() => RunTest();

    [Fact]
    public void CSharp7() => RunTest();

    [Fact]
    public void UnfinishedExpressionInCode() => RunTest();

    [Fact]
    public void Templates() => RunTest();

    [Fact]
    public void Markup_InCodeBlocks() => RunTest();

    [Fact]
    public void Markup_InCodeBlocksWithTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void StringLiterals() => RunTest();

    [Fact]
    public void SimpleUnspacedIf() => RunTest();

    [Fact]
    public void Sections() => RunTest();

    [Fact]
    public void RazorComments() => RunTest();

    [Fact]
    public void ParserError() => RunTest();

    [Fact]
    public void OpenedIf() => RunTest();

    [Fact]
    public void NullConditionalExpressions() => RunTest();

    [Fact]
    public void NoLinePragmas() => RunTest();

    [Fact]
    public void NestedCSharp() => RunTest();

    [Fact]
    public void NestedCodeBlocks() => RunTest();

    [Fact]
    public void MarkupInCodeBlock() => RunTest();

    [Fact]
    public void Instrumented() => RunTest();

    [Fact]
    public void InlineBlocks() => RunTest();

    [Fact]
    public void Inherits() => RunTest();

    [Fact]
    public void Usings() => RunTest();

    [Fact]
    public void Usings_OutOfOrder() => RunTest();

    [Fact]
    public void ImplicitExpressionAtEOF() => RunTest();

    [Fact]
    public void ImplicitExpression() => RunTest();

    [Fact]
    public void HtmlCommentWithQuote_Double() => RunTest();

    [Fact]
    public void HtmlCommentWithQuote_Single() => RunTest();

    [Fact]
    public void HiddenSpansInCode() => RunTest();

    [Fact]
    public void FunctionsBlock() => RunTest();

    [Fact]
    public void FunctionsBlockMinimal() => RunTest();

    [Fact]
    public void ExpressionsInCode() => RunTest();

    [Fact]
    public void ExplicitExpressionWithMarkup() => RunTest();

    [Fact]
    public void ExplicitExpressionAtEOF() => RunTest();

    [Fact]
    public void ExplicitExpression() => RunTest();

    [Fact]
    public void EmptyImplicitExpressionInCode() => RunTest();

    [Fact]
    public void EmptyImplicitExpression() => RunTest();

    [Fact]
    public void EmptyExplicitExpression() => RunTest();

    [Fact]
    public void EmptyCodeBlock() => RunTest();

    [Fact]
    public void ConditionalAttributes() => RunTest();

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10586")]
    public void ConditionalAttributes2()
    {
        RunTest();
    }

    [Fact]
    public void CodeBlockWithTextElement() => RunTest();

    [Fact]
    public void CodeBlockAtEOF() => RunTest();

    [Fact]
    public void CodeBlock() => RunTest();

    [Fact]
    public void Blocks() => RunTest();

    [Fact]
    public void Await() => RunTest();

    [Fact]
    public void Tags() => RunTest();

    [Fact]
    public void SimpleTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void TagHelpersWithBoundAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/13188")]
    public void StringLiteralAttributeOnUnionTagHelperProperty()
    {
        AddCSharpSyntaxTree("""
            #nullable enable

            namespace System.Runtime.CompilerServices
            {
                public interface IUnion
                {
                    object? Value { get; }
                }

                public class UnionAttribute : System.Attribute
                {
                }
            }
            """);

        AddCSharpSyntaxTree("""
            using Microsoft.AspNetCore.Razor.TagHelpers;

            namespace Test
            {
                public union SlotContent(string, int);

                public class SlotTagHelper : TagHelper
                {
                    public SlotContent Content { get; set; }
                }
            }
            """);

        RunDiscoveredTagHelpersTest(static tagHelpers =>
        {
            Assert.Contains(tagHelpers, static tagHelper => tagHelper.TypeName == "Test.SlotTagHelper");
        });
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/12261")]
    public void TagHelpersWithBoundAttributesAndRazorComment() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void TagHelpersWithPrefix() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void NestedTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void SingleTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void SingleTagHelperWithNewlineBeforeAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void TagHelpersWithWeirdlySpacedAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void IncompleteTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void BasicTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void BasicTagHelpers_Prefixed() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void BasicTagHelpers_RemoveTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void CssSelectorTagHelperAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.CssSelectorTagHelperDescriptors);

    [Fact]
    public void ComplexTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void EmptyAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void EscapedTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void DuplicateTargetTagHelper() => RunTagHelpersTest(TestTagHelperDescriptors.DuplicateTargetTagHelperDescriptors);

    [Fact]
    public void AttributeTargetingTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.AttributeTargetingTagHelperDescriptors);

    [Fact]
    public void PrefixedAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.PrefixedAttributeTagHelperDescriptors);

    [Fact]
    public void DuplicateAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void DynamicAttributeTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DynamicAttributeTagHelpers_Descriptors);

    [Fact]
    public void TransitionsInTagHelperAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void MinimizedTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.MinimizedTagHelpers_Descriptors);

    [Fact]
    public void NestedScriptTagTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.DefaultPAndInputTagHelperDescriptors);

    [Fact]
    public void SymbolBoundAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.SymbolBoundTagHelperDescriptors);

    [Fact]
    public void EnumTagHelpers() => RunTagHelpersTest(TestTagHelperDescriptors.EnumTagHelperDescriptors);

    [Fact]
    public void TagHelpersInSection() => RunTagHelpersTest(TestTagHelperDescriptors.TagHelpersInSectionDescriptors);

    [Fact]
    public void TagHelpersWithTemplate() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void TagHelpersWithDataDashAttributes() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact]
    public void Implements() => RunTest();

    [Fact]
    public void Implements_Multiple() => RunTest();

    [Fact]
    public void AttributeDirective() => RunTest();

    [Fact]
    public void SwitchExpression_RecursivePattern()
    {
        // System.Index is not available in .NET Framework without polyfills
        // so the C# diagnostics would be different between .NET Framework and .NET Core.
        SkipVerifyingCSharpDiagnostics = ExecutionConditionUtil.IsDesktop;

        RunTest();
    }

    [Fact]
    public void DesignTime() => RunTest();

    [Fact]
    public void RemoveTagHelperDirective() => RunTest();

    [Fact]
    public void AddTagHelperDirective() => RunTest();

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10186")]
    public void EscapedIdentifier() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10426")]
    public void EscapedExpression() => RunTagHelpersTest(TestTagHelperDescriptors.SimpleTagHelperDescriptors);

    private void RunTest([CallerMemberName] string testName = "")
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

    private void RunDiscoveredTagHelpersTest(Action<TagHelperCollection> verifyTagHelpers, [CallerMemberName] string testName = "")
    {
        // Arrange
        var projectEngine = CreateProjectEngine(static builder =>
        {
            RazorExtensions.Register(builder);
            builder.RegisterDefaultTagHelperProducer();
        });

        var projectItem = CreateProjectItemFromFile(testName: testName);
        var imports = GetImports(projectEngine, projectItem);
        var compilation = BaseCompilation.AddSyntaxTrees(CSharpSyntaxTrees);
        var tagHelpers = projectEngine.Engine.Features.OfType<ITagHelperDiscoveryService>().Single().GetTagHelpers(compilation);

        verifyTagHelpers(tagHelpers);

        // Act
        var codeDocument = projectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), RazorFileKind.Legacy, imports, tagHelpers);

        // Assert
        AssertDocumentNodeMatchesBaseline(codeDocument.GetRequiredDocumentNode(), testName);
        AssertCSharpDocumentMatchesBaseline(codeDocument.GetRequiredCSharpDocument(), testName);
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

    [Fact]
    public void Utf8StringLiterals() => RunTest();
}
