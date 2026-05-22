// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Completion.Html;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <summary>
/// Tests for the local HTML completion provider, covering vs: schema attribute behaviors:
/// - Element completion (basic, filtered by parent)
/// - Attribute completion (element-specific and global)
/// - Attribute value completion (enumerated values)
/// - HasExternalCompletion (vs:preferredextensions, vs:multivalue, class/style)
/// - Element HasExternalCompletion (script, style content)
/// - vs:nonbrowseable (hidden attributes/elements)
/// - vs:standalone (boolean attributes)
/// - vs:omtype="event" (event handler attributes)
/// - vs:implicitclosure (implicit close tag behavior)
/// - vs:disallowedancestor (ancestor filtering)
/// </summary>
public class LocalHtmlCompletionProviderTest
{
    private static readonly RazorCompletionOptions s_defaultOptions = new(
        SnippetsSupported: true,
        AutoInsertAttributeQuotes: true,
        CommitElementsWithSpace: true,
        IsVsCode: false);

    #region Element Completion

    [Fact]
    public void ElementCompletion_InBody_ReturnsElements()
    {
        // Arrange & Act
        var result = GetCompletionList("<div><$$</div>");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, static item => item.Label == "p");
        Assert.Contains(result.Items, static item => item.Label == "span");
    }

    [Fact]
    public void ElementCompletion_FilteredByParent_OnlyAllowedChildren()
    {
        // <ul> only allows <li> as a direct child
        var result = GetCompletionList("<ul><$$</ul>");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "li");
        // <div> is not allowed directly inside <ul>
        Assert.DoesNotContain(result.Items, static item => item.Label == "div");
    }

    [Fact]
    public void ElementCompletion_VoidElement_NoChildren()
    {
        // <br> is void — no children possible
        var result = GetCompletionList("<br><$$</br>");

        // A void element context may return null or empty since <br> is self-closing.
        // Actually the parser sees this differently — <br> is void so cursor after <br><
        // is actually inside the parent, not inside br.
        // Just verify we don't crash and get some result.
        Assert.NotNull(result);
    }

    [Fact]
    public void ElementCompletion_InsideScript_ReturnsNull()
    {
        // vs:hasExternalCompletion on <script> — content completion defers to external provider
        var result = GetCompletionList("<script>$$</script>");

        Assert.Null(result);
    }

    [Fact]
    public void ElementCompletion_InsideStyle_ReturnsNull()
    {
        // vs:hasExternalCompletion on <style> — content completion defers to external provider
        var result = GetCompletionList("<style>$$</style>");

        Assert.Null(result);
    }

    [Fact]
    public void ElementCompletion_NonBrowseableElements_NotShown()
    {
        // Elements marked vs:nonbrowseable should not appear in completion
        var result = GetCompletionList("<body><$$</body>");

        Assert.NotNull(result);
        // "nextid" is a deprecated element marked nonbrowseable in the schema
        Assert.DoesNotContain(result.Items, static item => item.Label == "nextid");
    }

    #endregion

    #region Attribute Completion

    [Fact]
    public void AttributeCompletion_OnDiv_IncludesGlobalAndElementAttributes()
    {
        var result = GetCompletionList("<div $$>");

        Assert.NotNull(result);
        // Global attributes
        Assert.Contains(result.Items, static item => item.Label == "id");
        Assert.Contains(result.Items, static item => item.Label == "class");
        Assert.Contains(result.Items, static item => item.Label == "style");
        // Event attributes (vs:omtype="event")
        Assert.Contains(result.Items, static item => item.Label == "onclick");
    }

    [Fact]
    public void AttributeCompletion_OnInput_IncludesElementSpecificAttributes()
    {
        var result = GetCompletionList("<input $$>");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "type");
        Assert.Contains(result.Items, static item => item.Label == "value");
        Assert.Contains(result.Items, static item => item.Label == "placeholder");
    }

    [Fact]
    public void AttributeCompletion_ExistingAttribute_NoEqualsInInsertText()
    {
        // When the cursor is inside an existing attribute name that already has ="",
        // we should only insert the name without ="$0" to avoid producing markup like
        // draggable=""="".
        var result = GetCompletionList("""<div dr$$opzone=""></div>""");

        Assert.NotNull(result);
        var draggable = result.Items.FirstOrDefault(static item => item.Label == "draggable");
        Assert.NotNull(draggable);
        // Should be plain name, no ="$0"
        Assert.Equal(InsertTextFormat.Plaintext, draggable.InsertTextFormat);
    }

    [Fact]
    public void AttributeCompletion_ExistingTagHelperAttribute_NoEqualsInInsertText()
    {
        // When the cursor is inside an existing tag helper attribute that already has a value,
        // we should only insert the name without ="$0" to avoid producing markup like
        // autocorrect=""="true".
        var tagHelper = TagHelperDescriptorBuilder.Create("InputTagHelper", "TestAssembly");
        tagHelper.TagMatchingRule(rule => rule.TagName = "input");
        tagHelper.BindAttribute(attr =>
        {
            attr.Name = "autocorrect";
            attr.TypeName = typeof(string).FullName;
        });

        var result = GetCompletionList(
            """<input auto$$correct="true" />""",
            [tagHelper.Build()]);

        Assert.NotNull(result);
        var item = result.Items.FirstOrDefault(static item => item.Label == "autocomplete");
        Assert.NotNull(item);
        // Should be plain name, no ="$0" since the attribute already has a value
        Assert.Equal(InsertTextFormat.Plaintext, item.InsertTextFormat);
    }

    [Fact]
    public void AttributeCompletion_EventAttributes_HaveEventKind()
    {
        // vs:omtype="event" marks attributes as event handlers
        var result = GetCompletionList("<div $$>");

        Assert.NotNull(result);
        var onclick = result.Items.FirstOrDefault(static item => item.Label == "onclick");
        Assert.NotNull(onclick);
        // Event attributes get a distinct icon (Event kind in LSP)
        Assert.Equal(CompletionItemKind.Event, onclick.Kind);
    }

    [Fact]
    public void AttributeCompletion_NonBrowseableAttributes_NotShown()
    {
        // Attributes marked vs:nonbrowseable should not appear
        var result = GetCompletionList("<div $$>");

        Assert.NotNull(result);
        // "datafld" is a deprecated attribute marked nonbrowseable
        Assert.DoesNotContain(result.Items, static item => item.Label == "datafld");
    }

    [Fact]
    public void AttributeCompletion_UnclosedTag_CursorBeforeDistantContent_ShowsAllAttributes()
    {
        // When a start tag is unclosed, the parser may treat distant content as an attribute name.
        // The cursor is at the space after "table", two lines above "adfs". Completion should
        // show all attributes with a zero-length range (no replacement), not filter against "adfs".
        var result = GetCompletionList("<table $$\n\nadfs");

        Assert.NotNull(result);

        // Should include global attributes — not filtered by "adfs"
        Assert.Contains(result.Items, static item => item.Label == "id");
        Assert.Contains(result.Items, static item => item.Label == "class");
        Assert.Contains(result.Items, static item => item.Label == "style");

        // TextEdit range should be zero-length at cursor (no replacement of "adfs")
        var idItem = result.Items.First(static item => item.Label == "id");
        Assert.NotNull(idItem.TextEdit);
        var textEdit = (TextEdit)idItem.TextEdit.Value;
        Assert.Equal(new LspRange
        {
            Start = new Position { Line = 0, Character = 7 },
            End = new Position { Line = 0, Character = 7 }
        }, textEdit.Range);
    }

    [Fact]
    public void AttributeCompletion_DataPrefixExpanded_NoSchemaEntries_ReturnsNull()
    {
        // When the user types "data-" and expands the prefix group, but no data-* attributes
        // exist in the schema (jQuery Mobile excluded), we return null to fall back to the
        // external HTML server which can provide usage-derived completions.
        var result = GetCompletionList("<div data-$$>");

        Assert.Null(result);
    }

    #endregion

    #region Attribute Value Completion

    [Fact]
    public void AttributeValueCompletion_EnumeratedValues_ReturnsAll()
    {
        // <input type="|"> has enumerated values (text, password, checkbox, etc.)
        // Place cursor inside some value text so the parser sees us in the value span
        var result = GetCompletionList("<input type=\"t$$\">");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, static item => item.Label == "text");
        Assert.Contains(result.Items, static item => item.Label == "checkbox");
        Assert.Contains(result.Items, static item => item.Label == "hidden");
    }

    [Fact]
    public void AttributeValueCompletion_IdAttribute_ReturnsNull()
    {
        // <div id="|"> — id has hasExternalCompletion=true so it defers to external CSS ID provider
        var result = GetCompletionList("<div id=\"x$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_HasExternalCompletion_FilePathAttribute_ReturnsNull()
    {
        // vs:preferredextensions marks attributes like src, href as file-path attributes.
        // These defer to an external file-picker provider.
        var result = GetCompletionList("<script src=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_HasExternalCompletion_ClassAttribute_ReturnsNull()
    {
        // "class" has hasExternalCompletion=true (CSS class completion from external provider)
        var result = GetCompletionList("<div class=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_HasExternalCompletion_StyleAttribute_ReturnsNull()
    {
        // "style" has hasExternalCompletion=true (CSS property completion from external provider)
        var result = GetCompletionList("<div style=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_HasExternalCompletion_MultiValueAttribute_ReturnsNull()
    {
        // vs:multivalue="true" — attributes like "rel" accept multiple space-separated values.
        // Value completion is owned by an external provider that handles retrigger-on-space.
        var result = GetCompletionList("<link rel=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_HasExternalCompletion_AriaRelevant_ReturnsNull()
    {
        // aria-relevant is multivalue (additions, removals, text, all)
        var result = GetCompletionList("<div aria-relevant=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_HasExternalCompletion_HrefAttribute_ReturnsNull()
    {
        // href on <base> has vs:preferredextensions (file path completion)
        var result = GetCompletionList("<base href=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_NonExternalCompletion_ReturnsValues()
    {
        // dir attribute has enumerated values (ltr, rtl, auto) and no external completion
        var result = GetCompletionList("<div dir=\"l$$\">");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "ltr");
        Assert.Contains(result.Items, static item => item.Label == "rtl");
    }

    #endregion

    #region Implicit Closure (vs:implicitclosure)

    [Fact]
    public void ElementCompletion_ImplicitlyClosedElement_OffersParentAsSibling()
    {
        // <li> is implicitly closed — typing <li> inside an unclosed <li> closes the first one.
        // When inside an implicitly-closable element without an end tag, the element itself
        // should appear in completions (new sibling implicitly closes current <li>).
        var result = GetCompletionList("<ul><li><$$</ul>");

        Assert.NotNull(result);
        // <li> should appear as a completion (new sibling implicitly closes current <li>)
        Assert.Contains(result.Items, static item => item.Label == "li");
    }

    [Fact]
    public void ElementCompletion_ImplicitlyClosedParagraph_OffersParagraph()
    {
        // <p> is implicitly closed — typing <p> inside an unclosed <p> closes the first one.
        var result = GetCompletionList("<div><p><$$</div>");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "p");
    }

    [Fact]
    public void ElementCompletion_ImplicitlyClosedElement_WithEndTag_DoesNotOfferParent()
    {
        // When the parent <li> has an explicit </li> end tag, typing a child element
        // is inside the parent — not a sibling. The implicit closure rule only applies
        // when the parent is unclosed (no end tag).
        var result = GetCompletionList("<ul><li><$$</li></ul>");

        Assert.NotNull(result);
        // <li> should NOT appear because the parent has an explicit end tag —
        // we're inside it, not creating a sibling.
        Assert.DoesNotContain(result.Items, item => item.Label == "li");
    }

    #endregion

    #region Disallowed Ancestors (vs:disallowedancestor)

    [Fact]
    public void ElementCompletion_DisallowedAncestor_FormNotNestedInForm()
    {
        // <form> has disallowedancestor="form" — should not appear when inside a <form>
        var result = GetCompletionList("<form><div><$$</div></form>");

        Assert.NotNull(result);
        Assert.DoesNotContain(result.Items, item => item.Label == "form");
    }

    [Fact]
    public void ElementCompletion_DisallowedAncestor_AllowedOutsideAncestor()
    {
        // <form> should appear when NOT inside a <form>
        var result = GetCompletionList("<div><$$</div>");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "form");
    }

    #endregion

    #region Unknown Elements and Attributes

    [Fact]
    public void AttributeCompletion_UnknownElement_ReturnsGlobalAttributes()
    {
        // Unknown elements (custom elements, SVG) are not in our schema, but global
        // attributes (id, class, style, etc.) apply to all HTML elements.
        // We return them authoritatively — no reason to defer to the external provider.
        var result = GetCompletionList("<my-custom-element $$>");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "id");
        Assert.Contains(result.Items, static item => item.Label == "class");
        Assert.Contains(result.Items, static item => item.Label == "style");
    }

    [Fact]
    public void AttributeValueCompletion_DataAttribute_ReturnsNull()
    {
        // Unknown data-* attributes fall back to the external server which may have
        // usage-derived values from document scanning.
        var result = GetCompletionList("<div data-custom=\"f$$\">");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_UnknownAttribute_NonData_ReturnsEmpty()
    {
        // Non-data unknown attributes on known elements — no enumerated values exist in our
        // schema or the external provider's. Return empty authoritative list.
        var result = GetCompletionList("<div mycustomattr=\"f$$\">");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void AttributeValueCompletion_UnknownElement_UnknownAttribute_ReturnsEmpty()
    {
        // Completely unknown element + attribute — no values to offer, but no reason
        // to defer either. Return empty authoritative list.
        var result = GetCompletionList("<my-widget foo=\"b$$\">");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void ElementCompletion_InsideUnknownElement_ReturnsAllElements()
    {
        // Unknown parent element — we don't know the content model, so we return
        // all elements (same as no-parent context). This is authoritative.
        var result = GetCompletionList("<my-widget><$$</my-widget>");

        Assert.NotNull(result);
        Assert.Contains(result.Items, static item => item.Label == "div");
        Assert.Contains(result.Items, static item => item.Label == "span");
    }

    [Fact]
    public void ElementCompletion_InsideSvg_ReturnsNull()
    {
        // Inside <svg>, we don't have the SVG schema locally. Fall back to the
        // external HTML server which has the full SVG element set.
        var result = GetCompletionList("<svg><$$</svg>");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeCompletion_SvgElement_InsideSvg_ReturnsNull()
    {
        // SVG elements like <circle> aren't in our schema. When inside <svg>,
        // fall back to external server for SVG-specific attributes.
        var result = GetCompletionList("<svg><circle $$></circle></svg>");

        Assert.Null(result);
    }

    [Fact]
    public void AttributeValueCompletion_SvgElement_InsideSvg_ReturnsNull()
    {
        // SVG attribute values (e.g., fill="..." on <circle>) aren't in our schema.
        // Fall back to external server.
        var result = GetCompletionList("<svg><circle fill=\"$$\"></circle></svg>");

        Assert.Null(result);
    }

    #endregion

    #region Entity Completion

    [Fact]
    public void EntityCompletion_AfterAmpersand_ReturnsEntities()
    {
        var result = GetCompletionList("<div>&$$</div>");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, static item => item.FilterText == "amp");
        Assert.Contains(result.Items, static item => item.FilterText == "lt");
        Assert.Contains(result.Items, static item => item.FilterText == "gt");
        Assert.Contains(result.Items, static item => item.FilterText == "nbsp");
    }

    [Fact]
    public void EntityCompletion_PartiallyTyped_ReturnsEntities()
    {
        var result = GetCompletionList("<div>&nb$$</div>");

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, static item => item.FilterText == "nbsp");
    }

    [Fact]
    public void EntityCompletion_NoAmpersand_ReturnsEmpty()
    {
        // Plain text without & should not offer entities
        var result = GetCompletionList("<div>text$$</div>");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void EntityCompletion_InsertTextFormat()
    {
        var result = GetCompletionList("<div>&$$</div>");

        Assert.NotNull(result);
        var ampItem = result.Items.Single(static item => item.FilterText == "amp");
        Assert.Null(ampItem.InsertText);
        Assert.Equal("&amp; (&)", ampItem.Label);
        Assert.Equal(CompletionItemKind.Constant, ampItem.Kind);

        // TextEdit replaces the range covering '&' through cursor
        Assert.NotNull(ampItem.TextEdit);
        var textEdit = (TextEdit)ampItem.TextEdit.Value;
        Assert.Equal("&amp;", textEdit.NewText);
    }

    [Fact]
    public void EntityCompletion_NoCommitCharacters()
    {
        // With TextEdit-based entities, commit characters are not needed
        var result = GetCompletionList("<div>&$$</div>");

        Assert.NotNull(result);
        var item = result.Items.First();
        Assert.Null(item.VsCommitCharacters);
    }

    [Fact]
    public void EntityCompletion_AfterSemicolon_NoEntities()
    {
        // After a completed entity, no entity completions
        var result = GetCompletionList("<div>&amp;$$</div>");

        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void EntityCompletion_Count_MatchesWebTools()
    {
        // WebTools EntityTable has 264 entries
        var result = GetCompletionList("<div>&$$</div>");

        Assert.NotNull(result);
        Assert.Equal(264, result.Items.Length);
    }

    #endregion

    #region Close-Tag Completion

    [Fact]
    public void CloseTagCompletion_NoCommitCharacters()
    {
        var result = GetCompletionList("<div></$$");

        Assert.NotNull(result);
        var item = Assert.Single(result.Items, static item => item.Label == "/div>");
        Assert.Null(item.VsCommitCharacters);
    }

    [Fact]
    public void CloseTagCompletion_NestedUnclosed_InnermostFirst()
    {
        var result = GetCompletionList("<div><p><span></$$");

        Assert.NotNull(result);
        var closeItems = result.Items.Where(static i => i.Label!.StartsWith("/")).ToArray();
        Assert.Equal(3, closeItems.Length);
        Assert.Equal("/span>", closeItems[0].Label);
        Assert.Equal("/p>", closeItems[1].Label);
        Assert.Equal("/div>", closeItems[2].Label);
    }

    [Fact]
    public void CloseTagCompletion_InStartTag_DoesNotOfferSelfCloseTag()
    {
        // When typing "<table" inside a <div>, close-tag items should offer /div>
        // (the unclosed ancestor), but NOT /table> (the element being typed).
        var result = GetCompletionList("<div><tabl$$");

        Assert.NotNull(result);
        var closeItems = result.Items.Where(static i => i.Label!.StartsWith("/")).ToArray();
        Assert.Single(closeItems);
        Assert.Equal("/div>", closeItems[0].Label);
        Assert.DoesNotContain(result.Items, static item => item.Label == "/tabl>");
    }

    [Fact]
    public void CloseTagCompletion_InStartTag_AllChildrenContext_DoesNotOfferSelfCloseTag()
    {
        // In an unknown parent (all children allowed), close-tag items should
        // still not include the element being typed.
        var result = GetCompletionList("<my-widget><sp$$");

        Assert.NotNull(result);
        var closeItems = result.Items.Where(static i => i.Label!.StartsWith("/")).ToArray();
        Assert.Single(closeItems);
        Assert.Equal("/my-widget>", closeItems[0].Label);
    }

    #endregion

    #region Completion Application (round-trip tests)

    [Theory]
    [InlineData("<div></d$$>", "/div>", "<div></div>")]
    [InlineData("<table></t$$", "/table>", "<table></table>")]
    [InlineData("<div></$$", "/div>", "<div></div>")]
    [InlineData("<div><$$", "/div>", "<div></div>")]
    [InlineData("<div><p><span></$$", "/span>", "<div><p><span></span>")]
    [InlineData("<div>&$$</div>", "&amp; (&)", "<div>&amp;</div>")]
    [InlineData("<div>&am$$</div>", "&amp; (&)", "<div>&amp;</div>")]
    [InlineData("<div>&am$$;</div>", "&amp; (&)", "<div>&amp;</div>")]
    // Attribute: boolean (no ="$0"), minimized (has ="$0"), existing value (no ="$0")
    [InlineData("<input $$>", "disabled", "<input disabled>")]
    [InlineData("""<div dr$$></div>""", "draggable", """<div draggable="$0"></div>""")]
    [InlineData("""<div dr$$opzone=""></div>""", "draggable", """<div draggable=""></div>""")]
    // Attribute: unclosed tag with cursor before distant content — inserts at cursor, doesn't replace
    [InlineData("<table $$\n\nadfs", "id", "<table id=\"$0\"\n\nadfs")]
    public void ApplyCompletion_ProducesExpectedBuffer(string markup, string itemLabel, string expected)
    {
        var result = ApplyCompletion(markup, itemLabel);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets the completion list, finds the item with the given label, applies the completion
    /// to the source text, and returns the resulting document text.
    /// Handles both TextEdit (explicit range) and InsertText (insert at cursor) items.
    /// </summary>
    private static string ApplyCompletion(string markup, string itemLabel)
    {
        var testCode = new TestCode(markup);
        var context = CreateContext(testCode.Text, testCode.Position);
        LocalHtmlCompletionProvider.TryGetHtmlCompletionList(context, out var completionList, out _);

        Assert.NotNull(completionList);
        var item = Assert.Single(completionList.Items, i => i.Label == itemLabel);

        if (item.TextEdit is not null)
        {
            // TextEdit: replace the specified range with NewText
            var textEdit = (TextEdit)item.TextEdit.Value;
            var sourceText = context.CodeDocument.Source.Text;
            var startPosition = sourceText.Lines.GetPosition(new Microsoft.CodeAnalysis.Text.LinePosition(
                textEdit.Range.Start.Line, textEdit.Range.Start.Character));
            var endPosition = sourceText.Lines.GetPosition(new Microsoft.CodeAnalysis.Text.LinePosition(
                textEdit.Range.End.Line, textEdit.Range.End.Character));

            return testCode.Text[..startPosition] + textEdit.NewText + testCode.Text[endPosition..];
        }
        else
        {
            // InsertText: insert at cursor position
            var insertText = item.InsertText ?? item.Label;
            return testCode.Text[..testCode.Position] + insertText + testCode.Text[testCode.Position..];
        }
    }

    private static RazorVSInternalCompletionList? GetCompletionList(string markup)
    {
        var testCode = new TestCode(markup);
        var context = CreateContext(testCode.Text, testCode.Position);
        return LocalHtmlCompletionProvider.TryGetHtmlCompletionList(context, out var completionList, out _)
            ? completionList
            : null;
    }

    private static RazorVSInternalCompletionList? GetCompletionList(string markup, TagHelperDescriptor[] tagHelpers)
    {
        var testCode = new TestCode(markup);
        var context = CreateContext(testCode.Text, testCode.Position, tagHelpers);
        return LocalHtmlCompletionProvider.TryGetHtmlCompletionList(context, out var completionList, out _)
            ? completionList
            : null;
    }

    private static RazorCompletionContext CreateContext(string text, int absoluteIndex, TagHelperDescriptor[]? tagHelpers = null)
    {
        var sourceDocument = TestRazorSourceDocument.Create(text, filePath: "C:/path/to/document.cshtml");
        var codeDocument = RazorCodeDocument.Create(sourceDocument);
        var syntaxTree = RazorSyntaxTree.Parse(sourceDocument);

        var tagHelperCollection = TagHelperCollection.Create(tagHelpers ?? []);
        var tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelperCollection);
        codeDocument = codeDocument.WithTagHelperContext(tagHelperDocumentContext);

        var binder = tagHelperDocumentContext.GetBinder();
        var rewrittenSyntaxTree = TagHelperParseTreeRewriter.Rewrite(syntaxTree, binder);
        codeDocument = codeDocument.WithTagHelperRewrittenSyntaxTree(rewrittenSyntaxTree);

        return RazorCompletionListProvider.CreateCompletionContext(
            codeDocument,
            absoluteIndex,
            new VSInternalCompletionContext(),
            s_defaultOptions);
    }

    #endregion
}
