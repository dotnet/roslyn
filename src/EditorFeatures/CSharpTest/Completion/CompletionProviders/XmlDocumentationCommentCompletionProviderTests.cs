// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class XmlDocumentationCommentCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(XmlDocCommentCompletionProvider);

    private async Task VerifyItemsExistAsync(string markup, params string[] items)
    {
        foreach (var item in items)
        {
            await VerifyItemExistsAsync(markup, item);
        }
    }

    private async Task VerifyItemsAbsentAsync(string markup, params string[] items)
    {
        foreach (var item in items)
        {
            await VerifyItemIsAbsentAsync(markup, item);
        }
    }

    private protected override async Task VerifyWorkerAsync(
        string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool checkForAbsence,
        Glyph? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, CompletionOptions options = null, bool skipSpeculation = false)
    {
        // We don't need to try writing comments in from of items in doc comments.
        await VerifyAtPositionAsync(
            code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
            isComplexTextEdit, matchingFilters, flags, options);

        await VerifyAtEndOfFileAsync(
            code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
            isComplexTextEdit, matchingFilters, flags, options);

        // Items cannot be partially written if we're checking for their absence,
        // or if we're verifying that the list will show up (without specifying an actual item)
        if (!checkForAbsence && expectedItemOrNull != null)
        {
            await VerifyAtPosition_ItemPartiallyWrittenAsync(
                code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options);

            await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
                code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options);
        }
    }

    [Fact]
    public Task AlwaysVisibleAtAnyLevelItems1()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// $$
                public void bar() { }
            }
            """, "inheritdoc", "see", "seealso", "![CDATA[", "!--");

    [Fact]
    public Task AlwaysVisibleAtAnyLevelItems2()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <summary> $$ </summary>
                public void bar() { }
            }
            """, "inheritdoc", "see", "seealso", "![CDATA[", "!--");

    [Fact]
    public Task AlwaysVisibleNotTopLevelItems1()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <summary> $$ </summary>
                public void bar() { }
            }
            """, "c", "code", "list", "para");

    [Fact]
    public Task AlwaysVisibleNotTopLevelItems2()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                /// $$ 
                public void bar() { }
            }
            """, "c", "code", "list", "para", "paramref", "typeparamref");

    [Fact]
    public Task CodeStyleElements_InsideSummary()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <summary> $$ </summary>
                public void bar() { }
            }
            """, "b", "em", "i", "strong", "tt");

    [Fact]
    public Task CodeStyleElements_NotAtTopLevel()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                /// $$ 
                public void bar() { }
            }
            """, "b", "em", "i", "strong", "tt");

    [Fact]
    public Task CodeStyleElements_InsidePara()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <summary>
                /// <para> $$ </para>
                /// </summary>
                public void bar() { }
            }
            """, "b", "em", "i", "strong", "tt");

    [Fact]
    public Task CodeStyleElements_InsideRemarks()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <remarks> $$ </remarks>
                public void bar() { }
            }
            """, "b", "em", "i", "strong", "tt");

    [Fact]
    public Task AlwaysVisibleTopLevelOnlyItems1()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// $$ 
                public void bar() { }
            }
            """, "exception", "include", "permission");

    [Fact]
    public Task AlwaysVisibleTopLevelOnlyItems2()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                /// <summary> $$ </summary>
                public void bar() { }
            }
            """, "exception", "include", "permission");

    [Fact]
    public Task TopLevelSingleUseItems1()
        => VerifyItemsExistAsync("""
            public class goo
            {
                ///  $$
                public void bar() { }
            }
            """, "example", "remarks", "summary");

    [Fact]
    public Task TopLevelSingleUseItems2()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                ///  <summary> $$ </summary>
                public void bar() { }
            }
            """, "example", "remarks", "summary");

    [Fact]
    public Task TopLevelSingleUseItems3()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                ///  <summary> $$ </summary>
                /// <example></example>
                /// <remarks></remarks>

                public void bar() { }
            }
            """, "example", "remarks", "summary");

    [Fact]
    public Task OnlyInListItems()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                ///  <summary> $$ </summary>
                /// <example></example>
                /// <remarks></remarks>

                public void bar() { }
            }
            """, "listheader", "item", "term", "description");

    [Fact]
    public Task OnlyInListItems2()
        => VerifyItemsAbsentAsync("""
            public class goo
            {
                ///   $$ 

                public void bar() { }
            }
            """, "listheader", "item", "term", "description");

    [Fact]
    public Task OnlyInListItems3()
        => VerifyItemsExistAsync("""
            public class goo
            {
                ///   <list>$$</list>

                public void bar() { }
            }
            """, "listheader", "item", "term", "description");

    [Fact]
    public Task OnlyInListItems4()
        => VerifyItemsExistAsync("""
            public class goo
            {
                ///   <list><$$</list>

                public void bar() { }
            }
            """, "listheader", "item", "term", "description");

    [Fact]
    public Task ListHeaderItems()
        => VerifyItemsExistAsync("""
            public class goo
            {
                ///  <summary>
                ///  <list><listheader> $$ </listheader></list>
                ///  </summary>
                /// <example></example>
                /// <remarks></remarks>

                public void bar() { }
            }
            """, "term", "description");

    [Fact]
    public Task VoidMethodDeclarationItems()
        => VerifyItemIsAbsentAsync("""
            public class goo
            {

                /// $$
                public void bar() { }
            }
            """, "returns");

    [Fact]
    public Task MethodReturns()
        => VerifyItemExistsAsync("""
            public class goo
            {

                /// $$
                public int bar() { }
            }
            """, "returns");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")]
    public Task ReadWritePropertyNoReturns()
        => VerifyItemIsAbsentAsync("""
            public class goo
            {

                /// $$
                public int bar { get; set; }
            }
            """, "returns");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")]
    public Task ReadWritePropertyValue()
        => VerifyItemExistsAsync("""
            public class goo
            {

                /// $$
                public int bar { get; set; }
            }
            """, "value");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")]
    public Task ReadOnlyPropertyNoReturns()
        => VerifyItemIsAbsentAsync("""
            public class goo
            {

                /// $$
                public int bar { get; }
            }
            """, "returns");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")]
    public Task ReadOnlyPropertyValue()
        => VerifyItemExistsAsync("""
            public class goo
            {

                /// $$
                public int bar { get; }
            }
            """, "value");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")]
    public Task WriteOnlyPropertyNoReturns()
        => VerifyItemIsAbsentAsync("""
            public class goo
            {

                /// $$
                public int bar { set; }
            }
            """, "returns");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8627")]
    public Task WriteOnlyPropertyValue()
        => VerifyItemExistsAsync("""
            public class goo
            {

                /// $$
                public int bar { set; }
            }
            """, "value");

    [Fact]
    public async Task MethodParamTypeParam()
    {
        var text = """
            public class goo<TGoo>
            {

                /// $$
                public int bar<TBar>(TBar green) { }
            }
            """;

        await VerifyItemsExistAsync(text, """
            typeparam name="TBar"
            """, """
            param name="green"
            """);
        await VerifyItemsAbsentAsync(text, """
            typeparam name="TGoo"
            """);
    }

    [Fact]
    public Task IndexerParamTypeParam()
        => VerifyItemsExistAsync("""
            public class goo<T>
            {

                /// $$
                public int this[T green] { get { } set { } }
            }
            """, """
            param name="green"
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17872")]
    public Task MethodParamRefName()
        => VerifyItemsExistAsync(
            """
            public class Outer<TOuter>
            {
                public class Inner<TInner>
                {
                    /// <summary>
                    /// $$
                    /// </summary>
                    public int Method<TMethod>(T green) { }
                }
            }
            """,
            """
            typeparamref name="TOuter"
            """,
            """
            typeparamref name="TInner"
            """,
            """
            typeparamref name="TMethod"
            """,
            """
            paramref name="green"
            """);

    [Fact]
    public Task ClassTypeParamRefName()
        => VerifyItemsExistAsync("""
            /// <summary>
            /// $$
            /// </summary>
            public class goo<T>
            {
                public int bar<T>(T green) { }
            }
            """, """
            typeparamref name="T"
            """);

    [Fact]
    public Task ClassTypeParam()
        => VerifyItemsExistAsync("""
            /// $$
            public class goo<T>
            {
                public int bar<T>(T green) { }
            }
            """, """
            typeparam name="T"
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638802")]
    public Task TagsAfterSameLineClosedTag()
        => VerifyItemsExistAsync("""
            /// <summary>
            /// <goo></goo>$$
            /// 
            /// </summary>
            """, "!--", "![CDATA[", "c", "code", "inheritdoc", "list", "para", "seealso", "see");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734825")]
    public Task EnumMember()
        => VerifyItemsExistAsync("""
            public enum z
            {
                /// <summary>
                /// 
                /// </summary>
                /// <$$
                a
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954679")]
    public Task CompletionList()
        => VerifyItemExistsAsync("""
            /// $$
            public class goo
            {
            }
            """, "completionlist");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775091")]
    public Task ParamRefNames()
        => VerifyItemIsAbsentAsync("""
            /// <summary>
            /// <paramref name="$$"/>
            /// </summary>
            static void Main(string[] args)
            {
            }
            """, "args", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775091")]
    public Task ParamRefNames_Interactive()
        => VerifyItemExistsAsync("""
            /// <summary>
            /// <paramref name="$$"/>
            /// </summary>
            static void Main(string[] args)
            {
            }
            """, "args", sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public Task ParamNamesInEmptyAttribute()
        => VerifyItemIsAbsentAsync("""
            /// <param name="$$"/>
            static void Goo(string str)
            {
            }
            """, "str", sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public Task ParamNamesInEmptyAttribute_Interactive()
        => VerifyItemExistsAsync("""
            /// <param name="$$"/>
            static void Goo(string str)
            {
            }
            """, "str", sourceCodeKind: SourceCodeKind.Script);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26713")]
    public Task DelegateParams()
        => VerifyItemExistsAsync("""
            /// $$
            delegate void D(object o);
            """, """
            param name="o"
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17872")]
    public Task TypeParamRefNamesInEmptyAttribute()
        => VerifyItemsExistAsync("""
            public class Outer<TOuter>
            {
                public class Inner<TInner>
                {
                    /// <summary>
                    /// <typeparamref name="$$"/>
                    /// </summary>
                    public int Method<TMethod>(T green) { }
                }
            }
            """, "TOuter", "TInner", "TMethod");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17872")]
    public Task TypeParamRefNamesPartiallyTyped()
        => VerifyItemsExistAsync("""
            public class Outer<TOuter>
            {
                public class Inner<TInner>
                {
                    /// <summary>
                    /// <typeparamref name="T$$"/>
                    /// </summary>
                    public int Method<TMethod>(T green) { }
                }
            }
            """, "TOuter", "TInner", "TMethod");

    [Fact]
    public async Task TypeParamNamesInEmptyAttribute()
    {
        var text = """
            public class Outer<TOuter>
            {
                public class Inner<TInner>
                {
                    /// <typeparam name="$$"/>
                    public int Method<TMethod>(T green) { }
                }
            }
            """;

        await VerifyItemsExistAsync(text, "TMethod");
        await VerifyItemsAbsentAsync(text, "TOuter", "TInner");
    }

    [Fact]
    public async Task TypeParamNamesInWrongScope()
    {
        var text = """
            public class Outer<TOuter>
            {
                public class Inner<TInner>
                {
                    /// <summary>
                    /// <typeparam name="$$"/>
                    /// </summary>
                    public int Method<TMethod>(T green) { }
                }
            }
            """;

        await VerifyItemsExistAsync(text, "TMethod");
        await VerifyItemsAbsentAsync(text, "TOuter", "TInner");
    }

    [Fact]
    public async Task TypeParamNamesPartiallyTyped()
    {
        var text = """
            public class Outer<TOuter>
            {
                public class Inner<TInner>
                {
                    /// <typeparam name="T$$"/>
                    public int Method<TMethod>(T green) { }
                }
            }
            """;

        await VerifyItemsExistAsync(text, "TMethod");
        await VerifyItemsAbsentAsync(text, "TOuter", "TInner");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8322")]
    public Task PartialTagCompletion()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <r$$
                public void bar() { }
            }
            """, "!--", "![CDATA[", "completionlist", "example", "exception", "include", "inheritdoc", "permission", "remarks", "see", "seealso", "summary");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8322")]
    public Task PartialTagCompletionNestedTags()
        => VerifyItemsExistAsync("""
            public class goo
            {
                /// <summary>
                /// <r$$
                /// </summary>
                public void bar() { }
            }
            """, "!--", "![CDATA[", "c", "code", "inheritdoc", "list", "para", "see", "seealso");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11487")]
    public Task TypeParamAtTopLevelOnly()
        => VerifyItemsAbsentAsync("""
            /// <summary>
            /// $$
            /// </summary>
            public class Goo<T>
            {
            }
            """, """
            typeparam name="T"
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11487")]
    public Task ParamAtTopLevelOnly()
        => VerifyItemsAbsentAsync("""
            /// <summary>
            /// $$
            /// </summary>
            static void Goo(string str)
            {
            }
            """, """
            param name="str"
            """);

    [Fact]
    public Task ListAttributeNames()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// <list $$></list>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "type");

    [Fact]
    public Task ListTypeAttributeValue()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// <list type="$$"></list>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "bullet", "number", "table");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37504")]
    [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11490")]
    public Task SeeAttributeNames()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// <see $$/>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "cref", "langword", "href");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72259")]
    public Task SeeAttributeNames2()
        => VerifyProviderCommitAsync("""
            class C
            {
                /// <summary>
                /// <see l$$=""/>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "langword", """
            class C
            {
                /// <summary>
                /// <see langword=""/>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, null);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37504")]
    public Task SeeAlsoAttributeNames()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// <seealso $$/>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "cref", "href");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")]
    public Task LangwordCompletionInPlainText()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// Some text $$
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "null", "sealed", "true", "false", "await");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")]
    public Task LangwordCompletionAfterAngleBracket1()
        => VerifyItemsAbsentAsync("""
            class C
            {
                /// <summary>
                /// Some text <$$
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "null", "sealed", "true", "false", "await");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")]
    public Task LangwordCompletionAfterAngleBracket2()
        => VerifyItemsAbsentAsync("""
            class C
            {
                /// <summary>
                /// Some text <s$$
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "null", "sealed", "true", "false", "await");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22789")]
    public Task LangwordCompletionAfterAngleBracket3()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// Some text < $$
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "null", "sealed", "true", "false", "await");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11490")]
    public async Task SeeLangwordAttributeValue()
    {
        var source = """
            class C
            {
                /// <summary>
                /// <see langword="$$"/>
                /// </summary>
                static void Goo()
                {
                }
            }
            """;

        foreach (var keywordKind in SyntaxFacts.GetKeywordKinds())
        {
            var keywordText = SyntaxFacts.GetText(keywordKind);

            if (keywordText[0] == '_')
            {
                await VerifyItemIsAbsentAsync(source, keywordText);
            }
            else
            {
                await VerifyItemExistsAsync(source, keywordText, glyph: Glyph.Keyword);
            }
        }
    }

    [Fact]
    public Task InheritdocAttributes1()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <summary>
                /// <inheritdoc $$/>
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "cref", "path");

    [Fact]
    public Task InheritdocAttributes2()
        => VerifyItemsExistAsync("""
            class C
            {
                /// <inheritdoc $$/>
                static void Goo()
                {
                }
            }
            """, "cref", "path");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterTagNameInIncompleteTag()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <exception $$
                static void Goo()
                {
                }
            }
            """, "cref", usePreviousCharAsTrigger: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterTagNameInElementStartTag()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <exception $$>
                void Goo() { }
            }
            """, "cref");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterTagNameInEmptyElement()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <see $$/>
                void Goo() { }
            }
            """, "cref");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterTagNamePartiallyTyped()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <exception c$$
                void Goo() { }
            }
            """, "cref");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterSpecialCrefAttribute()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <list cref="String" $$
                /// </summary>
                void Goo() { }
            }
            """, "type");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterSpecialNameAttribute()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <list name="goo" $$
                /// </summary>
                void Goo() { }
            }
            """, "type");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameAfterTextAttribute()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <list goo="" $$
                /// </summary>
                void Goo() { }
            }
            """, "type");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameInWrongTagTypeEmptyElement()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <list $$/>
                /// </summary>
                void Goo() { }
            }
            """, "type");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeNameInWrongTagTypeElementStartTag()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <see $$>
                /// </summary>
                void Goo() { }
            }
            """, "langword");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/11489")]
    public Task AttributeValueOnQuote()
        => VerifyItemExistsAsync("""
            class C
            {
                /// <summary>
                /// <see langword="$$
                /// </summary>
                static void Goo()
                {
                }
            }
            """, "await", usePreviousCharAsTrigger: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/757")]
    public async Task TermAndDescriptionInsideItem()
    {
        var text = """
            class C
            {
                /// <summary>
                ///     <list type="table">
                ///         <item>
                ///             $$
                ///         </item>
                ///     </list>
                /// </summary>
                static void Goo()
                {
                }
            }
            """;
        await VerifyItemExistsAsync(text, "term");
        await VerifyItemExistsAsync(text, "description");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52738")]
    public Task RecordParam()
        => VerifyItemsExistAsync("""
            /// $$
            public record Goo<T>(string MyParameter);
            """, """
            param name="MyParameter"
            """, """
            typeparam name="T"
            """);

    [Fact]
    public Task PrimaryConstructor_Class_Param()
        => VerifyItemsExistAsync("""
            /// $$
            public class Goo<T>(string MyParameter);
            """, """
            param name="MyParameter"
            """, """
            typeparam name="T"
            """);

    [Fact]
    public Task PrimaryConstructor_Struct_Param()
        => VerifyItemsExistAsync("""
            /// $$
            public struct Goo<T>(string MyParameter);
            """, """
            param name="MyParameter"
            """, """
            typeparam name="T"
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69293")]
    public Task DelegateParamRef()
        => VerifyItemsExistAsync("""
            /// <summary>
            /// $$
            /// <summary>
            public delegate void Goo<T>(string myParameter);
            """, """
            paramref name="myParameter"
            """, """
            typeparamref name="T"
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52738")]
    public Task RecordParamRef()
        => VerifyItemsExistAsync("""
            /// <summary>
            /// $$
            /// <summary>
            public record Goo<T>(string MyParameter);
            """, """
            paramref name="MyParameter"
            """, """
            typeparamref name="T"
            """);

    [Fact]
    public Task PrimaryConstructor_Class_ParamRef()
        => VerifyItemsExistAsync("""
            /// <summary>
            /// $$
            /// <summary>
            public class Goo<T>(string MyParameter);
            """, """
            paramref name="MyParameter"
            """, """
            typeparamref name="T"
            """);

    [Fact]
    public Task PrimaryConstructor_Struct_ParamRef()
        => VerifyItemsExistAsync("""
            /// <summary>
            /// $$
            /// <summary>
            public struct Goo<T>(string MyParameter);
            """, """
            paramref name="MyParameter"
            """, """
            typeparamref name="T"
            """);

    [Fact]
    public async Task TriggerOnDeletion_DeleteInsideAttributeName()
    {
        TriggerOnDeletion = true;

        await VerifyItemExistsAsync("""
            /// <see lang$$ord="true" />
            """,
            "langword",
            deletedCharTrigger: 'w');
    }

    [Fact]
    public async Task TriggerOnDeletion_DeleteInsideAttributeValue()
    {
        TriggerOnDeletion = true;

        await VerifyItemExistsAsync("""
            /// <see langword="t$$ue" />
            """,
            "true",
            deletedCharTrigger: 'r');
    }

    [Fact]
    public async Task TriggerOnDeletion_DeleteInsideTagName_DoesntSuggest()
    {
        TriggerOnDeletion = true;

        await VerifyItemIsAbsentAsync("""
            /// <summ$$ry>
            /// 
            /// </summary>
            """,
            "see",
            deletedCharTrigger: 'a');
    }

    [Fact]
    public async Task TriggerOnDeletion_DeleteInXmlText_DoesntSuggest()
    {
        TriggerOnDeletion = true;

        await VerifyItemIsAbsentAsync("""
            /// <summary>
            /// a$$c
            /// </summary>
            """,
            "see",
            deletedCharTrigger: 'b');
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78770")]
    public Task ExtensionBlock_01()
        => VerifyItemsExistAsync("""
            public static class E
            {
                /// $$ 
                extension<T>(int i) { }
            }
            """, """
            typeparam name="T"
            """, """
            param name="i"
            """,
            "summary");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78770")]
    public Task ExtensionBlock_02()
        => VerifyItemsExistAsync("""
            public static class E
            {
                /// <summary> $$ </summary>
                extension<T>(int i)
                {
                } 
            }
            """, """
            paramref name="i"
            """, """
            typeparamref name="T"
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78770")]
    public Task ExtensionBlock_03()
        => VerifyItemsExistAsync("""
            public static class E
            {
                extension<T>(int i)
                {
                    /// <summary> $$ </summary>
                    void M() { }
                } 
            }
            """, """
            paramref name="i"
            """, """
            typeparamref name="T"
            """);
}
