// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class RazorDirectivesTest() : ParserTestBase(layer: TestProject.Layer.Compiler, validateSpanEditHandlers: true, useLegacyTokenizer: true)
{
    [Fact]
    public void DirectiveDescriptor_FileScopedMultipleOccurring_CanHaveDuplicates()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.AddTypeToken();
            });

        // Act & Assert
        ParseDocumentTest(
@"@custom System.Text.Encoding.ASCIIEncoding
@custom System.Text.Encoding.UTF8Encoding",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_FileScopedSinglyOccurring_ErrorsIfDuplicate()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                builder.AddTypeToken();
            });

        // Act & Assert
        ParseDocumentTest(
@"@custom System.Text.Encoding.ASCIIEncoding
@custom System.Text.Encoding.UTF8Encoding",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_FileScoped_CanBeBeneathOtherDirectives()
    {
        // Arrange
        var customDescriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                builder.AddTypeToken();
            });
        var somethingDescriptor = DirectiveDescriptor.CreateDirective(
            "something",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.AddMemberToken();
            });

        // Act & Assert
        ParseDocumentTest(
@"@custom System.Text.Encoding.ASCIIEncoding
@something Else",
            [customDescriptor, somethingDescriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_FileScoped_CanBeBeneathOtherWhiteSpaceCommentsAndDirectives()
    {
        // Arrange
        var customDescriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                builder.AddTypeToken();
            });
        var somethingDescriptor = DirectiveDescriptor.CreateDirective(
            "something",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.AddMemberToken();
            });

        // Act & Assert
        ParseDocumentTest(
@"@* There are two directives beneath this *@
@custom System.Text.Encoding.ASCIIEncoding

@something Else

<p>This is extra</p>",
            [customDescriptor, somethingDescriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_TokensMustBeSeparatedBySpace()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken().AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"string1\"\"string2\"",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_CanHandleEOFIncompleteNamespaceTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddNamespaceToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom System.",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_CanHandleEOFInvalidNamespaceTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddNamespaceToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom System<",
            [descriptor]);
    }
    [Fact]
    public void DirectiveDescriptor_CanHandleIncompleteNamespaceTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddNamespaceToken());

        // Act & Assert
        ParseDocumentTest("""
            @custom System.

            """,
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_CanHandleInvalidNamespaceTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddNamespaceToken());

        // Act & Assert
        ParseDocumentTest("""
            @custom System<

            """,
            [descriptor]);
    }

    [Fact]
    public void ExtensibleDirectiveDoesNotErorrIfNotAtStartOfLineBecauseOfWhitespace()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddTypeToken());

        // Act & Assert
        ParseDocumentTest("""

              @custom System.Text.Encoding.ASCIIEncoding
            """,
            [descriptor]);
    }

    [Fact]
    public void BuiltInDirectiveDoesNotErorrIfNotAtStartOfLineBecauseOfWhitespace()
    {
        // Act & Assert
        ParseDocumentTest("""

              @addTagHelper "*, Foo"
            """);
    }

    [Fact]
    public void BuiltInDirectiveErrorsIfNotAtStartOfLine()
    {
        // Act & Assert
        ParseDocumentTest("""
            {  @addTagHelper "*, Foo"
            }
            """);
    }

    [Fact]
    public void ExtensibleDirectiveErrorsIfNotAtStartOfLine()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddTypeToken());

        // Act & Assert
        ParseDocumentTest(
"""
            {  @custom System.Text.Encoding.ASCIIEncoding
            }
            """,
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsTypeTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddTypeToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom System.Text.Encoding.ASCIIEncoding",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsMemberTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddMemberToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom Some_Member",
            [descriptor]);
    }

    [Fact]
    public void Parser_ParsesNamespaceDirectiveToken_WithSingleSegment()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddNamespaceToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom BaseNamespace",
            [descriptor]);
    }

    [Fact]
    public void Parser_ParsesNamespaceDirectiveToken_WithMultipleSegments()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddNamespaceToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom BaseNamespace.Foo.Bar",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsStringTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"AString\"",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_StringToken_ParserErrorForUnquotedValue()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom AString",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_StringToken_ParserErrorForNonStringValue()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom {foo?}",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_StringToken_ParserErrorForSingleQuotedValue()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom 'AString'",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_StringToken_ParserErrorForPartialQuotedValue()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom AString\"",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsMultipleTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddTypeToken().AddMemberToken().AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom System.Text.Encoding.ASCIIEncoding Some_Member \"AString\"",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsRazorBlocks()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.RazorBlock,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"Header\" { <p>F{o}o</p> }",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsCodeBlocks()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.CodeBlock,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"Name\" { foo(); bar(); }",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_AllowsWhiteSpaceAroundTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddTypeToken().AddMemberToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom    System.Text.Encoding.ASCIIEncoding       Some_Member    ",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_ErrorsForInvalidMemberTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddMemberToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom -Some_Member",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_NoErrorsSemicolonAfterDirective()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"hello\" ;  ",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_AllowsNullableTypes()
    {
        // Arrange
        var variants = new[]
        {
                "string?",
                "string?[]",
                "global::System.Int32?",
                "KeyValuePair<string, string>?",
                "KeyValuePair<string, string>?[]",
                "global::System.Collections.Generic.KeyValuePair<string, string>?[]",
            };

        var directiveName = "custom";
        var source = $"@{directiveName}";
        var descriptor = DirectiveDescriptor.CreateDirective(
            directiveName,
            DirectiveKind.SingleLine,
            b =>
            {
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
            });

        for (var i = 0; i < variants.Length; i++)
        {
            source += $" {variants[i]}";
        }

        // Act & Assert
        ParseDocumentTest(source, [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_AllowsTupleTypes()
    {
        // Arrange
        var variants = new[]
        {
                "(bool, int)",
                "(int aa, string bb)?",
                "(  int?   q   ,  bool   w   )",
                "( int  ?  q, bool ?w ,(long ?  [])) ?",
                "(List<(int, string)?> aa, string bb)",
                "(string ss, (int u, List<(string, int)> k, (Char c, bool b, List<int> l)), global::System.Int32[] a)",
            };

        var directiveName = "custom";
        var source = $"@{directiveName}";
        var descriptor = DirectiveDescriptor.CreateDirective(
            directiveName,
            DirectiveKind.SingleLine,
            b =>
            {
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
                b.AddTypeToken();
            });

        for (var i = 0; i < variants.Length; i++)
        {
            source += $" {variants[i]}";
        }

        // Act & Assert
        ParseDocumentTest(source, [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_AllowsTupleTypes_IgnoresTrailingWhitespace()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddTypeToken());

        // Act & Assert
        ParseDocumentTest(
            $"@custom (bool, int?)   ",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_ErrorsExtraContentAfterDirective()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"hello\" \"world\"",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_ErrorsWhenExtraContentBeforeBlockStart()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.CodeBlock,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"Hello\" World { foo(); bar(); }",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_ErrorsWhenEOFBeforeDirectiveBlockStart()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.CodeBlock,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"Hello\"",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_ErrorsWhenMissingEndBrace()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.CodeBlock,
            b => b.AddStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"Hello\" {",
            [descriptor]);
    }

    [Fact]
    public void TagHelperPrefixDirective_NoValueSucceeds()
    {
        ParseDocumentTest("@tagHelperPrefix \"\"");
    }

    [Fact]
    public void TagHelperPrefixDirective_Succeeds()
    {
        ParseDocumentTest("@tagHelperPrefix Foo");
    }

    [Fact]
    public void TagHelperPrefixDirective_WithQuotes_Succeeds()
    {
        ParseDocumentTest("@tagHelperPrefix \"Foo\"");
    }

    [Fact]
    public void TagHelperPrefixDirective_RequiresValue()
    {
        ParseDocumentTest("@tagHelperPrefix ");
    }

    [Fact]
    public void TagHelperPrefixDirective_StartQuoteRequiresDoubleQuotesAroundValue()
    {
        ParseDocumentTest("@tagHelperPrefix \"Foo");
    }

    [Fact]
    public void TagHelperPrefixDirective_EndQuoteRequiresDoubleQuotesAroundValue()
    {
        ParseDocumentTest("@tagHelperPrefix Foo   \"");
    }

    [Fact]
    public void RemoveTagHelperDirective_NoValue_Invalid()
    {
        ParseDocumentTest("@removeTagHelper \"\"");
    }

    [Fact]
    public void RemoveTagHelperDirective_InvalidLookupText_AddsError()
    {
        ParseDocumentTest("@removeTagHelper Foo");
    }

    [Fact]
    public void RemoveTagHelperDirective_SingleQuotes_AddsError()
    {
        ParseDocumentTest("@removeTagHelper '*, Foo'");
    }

    [Fact]
    public void RemoveTagHelperDirective_WithQuotes_InvalidLookupText_AddsError()
    {
        ParseDocumentTest("@removeTagHelper \"Foo\"");
    }

    [Fact]
    public void RemoveTagHelperDirective_SupportsSpaces()
    {
        ParseDocumentTest("@removeTagHelper     Foo,   Bar    ");
    }

    [Fact]
    public void RemoveTagHelperDirective_RequiresValue()
    {
        ParseDocumentTest("@removeTagHelper ");
    }

    [Fact]
    public void RemoveTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
    {
        // Arrange
        ParseDocumentTest("@removeTagHelper \"Foo");
    }

    [Fact]
    public void RemoveTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
    {
        ParseDocumentTest("@removeTagHelper Foo\"");
    }

    [Fact]
    public void AddTagHelperDirective_NoValue_Invalid()
    {
        ParseDocumentTest("@addTagHelper \"\"");
    }

    [Fact]
    public void AddTagHelperDirective_InvalidLookupText_AddsError()
    {
        ParseDocumentTest("@addTagHelper Foo");
    }

    [Fact]
    public void AddTagHelperDirective_WithQuotes_InvalidLookupText_AddsError()
    {
        ParseDocumentTest("@addTagHelper \"Foo\"");
    }

    [Fact]
    public void AddTagHelperDirective_SingleQuotes_AddsError()
    {
        ParseDocumentTest("@addTagHelper '*, Foo'");
    }

    [Fact]
    public void AddTagHelperDirective_SupportsSpaces()
    {
        ParseDocumentTest("@addTagHelper     Foo,   Bar    ");
    }

    [Fact]
    public void AddTagHelperDirective_RequiresValue()
    {
        ParseDocumentTest("@addTagHelper ");
    }

    [Fact]
    public void AddTagHelperDirective_StartQuoteRequiresDoubleQuotesAroundValue()
    {
        ParseDocumentTest("@addTagHelper \"Foo");
    }

    [Fact]
    public void AddTagHelperDirective_EndQuoteRequiresDoubleQuotesAroundValue()
    {
        ParseDocumentTest("@addTagHelper Foo\"");
    }

    [Fact]
    public void InheritsDirectiveSupportsArrays()
    {
        ParseDocumentTest(
            "@inherits string[[]][]",
            [InheritsDirective.Directive]);
    }

    [Fact]
    public void InheritsDirectiveSupportsNestedGenerics()
    {
        ParseDocumentTest(
            "@inherits System.Web.Mvc.WebViewPage<IEnumerable<MvcApplication2.Models.RegisterModel>>",
            [InheritsDirective.Directive]);
    }

    [Fact]
    public void InheritsDirectiveSupportsTypeKeywords()
    {
        ParseDocumentTest(
            "@inherits string",
            [InheritsDirective.Directive]);
    }

    [Fact]
    public void Parse_FunctionsDirective()
    {
        ParseDocumentTest(
            "@functions { foo(); bar(); }",
            [FunctionsDirective.Directive]);
    }

    [Fact]
    public void EmptyFunctionsDirective()
    {
        ParseDocumentTest(
            "@functions { }",
            [FunctionsDirective.Directive]);
    }

    [Fact]
    public void Parse_SectionDirective()
    {
        ParseDocumentTest(
            "@section Header { <p>F{o}o</p> }",
            [SectionDirective.Directive]);
    }

    [Fact]
    public void TypeParam_WithSemicolon()
    {
        ParseDocumentTest($$"""
@typeparam TItem;
<ul>
</ul>
@code {
    // something
}
""",
            [ComponentConstrainedTypeParamDirective.Directive]);
    }

    [Fact]
    public void TypeParam_WithoutSemicolon()
    {
        ParseDocumentTest($$"""
@typeparam TItem
<ul>
</ul>
@code {
    // something
}
""",
            [ComponentConstrainedTypeParamDirective.Directive]);
    }

    [Fact]
    public void OptionalDirectiveTokens_AreSkipped()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddOptionalStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom ",
            [descriptor]);
    }

    [Fact]
    public void OptionalDirectiveTokens_WithSimpleTokens_AreParsed()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddOptionalStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"simple-value\"",
            [descriptor]);
    }

    [Fact]
    public void OptionalDirectiveTokens_WithBraces_AreParsed()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddOptionalStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"{formaction}?/{id}?\"",
            [descriptor]);
    }

    [Fact]
    public void OptionalDirectiveTokens_WithMultipleOptionalTokens_AreParsed()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddOptionalStringToken().AddOptionalTypeToken());

        // Act & Assert
        ParseDocumentTest(
            "@custom \"{formaction}?/{id}?\" System.String",
            [descriptor]);
    }

    [Fact]
    public void OptionalMemberTokens_WithMissingMember_IsParsed()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "TestDirective",
            DirectiveKind.SingleLine,
            b => b.AddOptionalMemberToken().AddOptionalStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@TestDirective ",
            [descriptor]);
    }

    [Fact]
    public void OptionalMemberTokens_WithMemberSpecified_IsParsed()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "TestDirective",
            DirectiveKind.SingleLine,
            b => b.AddOptionalMemberToken().AddOptionalStringToken());

        // Act & Assert
        ParseDocumentTest(
            "@TestDirective PropertyName",
            [descriptor]);
    }

    [Fact]
    public void Directives_CanUseReservedWord_Class()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "class",
            DirectiveKind.SingleLine);

        // Act & Assert
        ParseDocumentTest(
            "@class",
            [descriptor]);
    }

    [Fact]
    public void Directives_CanUseReservedWord_Namespace()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "namespace",
            DirectiveKind.SingleLine);

        // Act & Assert
        ParseDocumentTest(
            "@namespace",
            [descriptor]);
    }

    [Fact]
    public void Directives_ReservedWordInsideCodeBlock()
    {
        ParseDocumentTest("@{ class }");
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsGenericConstraintsToken()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b =>
            {
                b.AddMemberToken();
                b.AddOptionalGenericTypeConstraintToken("name", "description");
            });

        // Act & Assert
        ParseDocumentTest(@"
@custom TSomething where TSomething : class
",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_GenericConstraintTokenWorksWhenAtEndOfFile()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b =>
            {
                b.AddMemberToken();
                b.AddOptionalGenericTypeConstraintToken("name", "description");
            });

        // Act & Assert
        ParseDocumentTest(@"
@custom TSomething where TSomething : class",
            directives: [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_GenericConstraintTokenProducesErrorWhenFirstTokenIsNotWhereKeyword()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b =>
            {
                b.AddMemberToken();
                b.AddOptionalGenericTypeConstraintToken("name", "description");
            });

        // Act & Assert
        ParseDocumentTest(@"
@custom TSomething maybe TSomething : class",
            directives: [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_GenericConstraintErrorsWhenConstraintIdentifierIsDifferent()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b =>
            {
                b.AddMemberToken();
                b.AddOptionalGenericTypeConstraintToken("name", "description");
            });

        // Act & Assert
        ParseDocumentTest(@"
@custom TSomething where TElse : class",
            directives: [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_UnderstandsAttributeTokens()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddAttributeToken());

        // Act & Assert
        ParseDocumentTest(@"
@custom [Serializable]
@custom [DllImport(""user32.dll"", SetLastError=false, ExactSpelling=false)]
",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_AttributeToken_BalancesBrackets()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddAttributeToken());

        // Act & Assert
        ParseDocumentTest(@"
@custom [SomeCustom(new int[] { 1, 2, 3 }
",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_MultilineAttributeToken_BalancesBrackets()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddAttributeToken());

        // Act & Assert
        ParseDocumentTest(@"
@custom [SomeCustom(new int[]
    {
        1,
        2,
        3
    }]
",
            [descriptor]);
    }

    [Fact]
    public void DirectiveDescriptor_AttributeToken_ErrorsIfDoesNotStartWithOpenBracket()
    {
        // Arrange
        var descriptor = DirectiveDescriptor.CreateDirective(
            "custom",
            DirectiveKind.SingleLine,
            b => b.AddAttributeToken());

        // Act & Assert
        ParseDocumentTest("@custom Serializable]",
            [descriptor]);
    }
}
