// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class XmlDocumentationCommentCompletionProviderTests : AbstractCSharpCompletionProviderTests
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
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
            string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
            List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null, bool skipSpeculation = false)
        {
            // We don't need to try writing comments in from of items in doc comments.
            await VerifyAtPositionAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags);

            await VerifyAtEndOfFileAsync(
                code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
                checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription,
                isComplexTextEdit, matchingFilters, flags);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                    displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags);

                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(
                    code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull,
                    sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
                    displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleAtAnyLevelItems1()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// $$
    public void bar() { }
}", "inheritdoc", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleAtAnyLevelItems2()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "inheritdoc", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleNotTopLevelItems1()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "c", "code", "list", "para");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleNotTopLevelItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class goo
{
    /// $$ 
    public void bar() { }
}", "c", "code", "list", "para", "paramref", "typeparamref");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleTopLevelOnlyItems1()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// $$ 
    public void bar() { }
}", "exception", "include", "permission");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleTopLevelOnlyItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class goo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "exception", "include", "permission");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevelSingleUseItems1()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    ///  $$
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevelSingleUseItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class goo
{
    ///  <summary> $$ </summary>
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevelSingleUseItems3()
        {
            await VerifyItemsAbsentAsync(@"
public class goo
{
    ///  <summary> $$ </summary>
    /// <example></example>
    /// <remarks></remarks>
    
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyInListItems()
        {
            await VerifyItemsAbsentAsync(@"
public class goo
{
    ///  <summary> $$ </summary>
    /// <example></example>
    /// <remarks></remarks>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyInListItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class goo
{
    ///   $$ 
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyInListItems3()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    ///   <list>$$</list>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyInListItems4()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    ///   <list><$$</list>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ListHeaderItems()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    ///  <summary>
    ///  <list><listheader> $$ </listheader></list>
    ///  </summary>
    /// <example></example>
    /// <remarks></remarks>
    
    public void bar() { }
}", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VoidMethodDeclarationItems()
        {
            await VerifyItemIsAbsentAsync(@"
public class goo
{
    
    /// $$
    public void bar() { }
}", "returns");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodReturns()
        {
            await VerifyItemExistsAsync(@"
public class goo
{
    
    /// $$
    public int bar() { }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadWritePropertyNoReturns()
        {
            await VerifyItemIsAbsentAsync(@"
public class goo
{
    
    /// $$
    public int bar { get; set; }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadWritePropertyValue()
        {
            await VerifyItemExistsAsync(@"
public class goo
{
    
    /// $$
    public int bar { get; set; }
}", "value");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyPropertyNoReturns()
        {
            await VerifyItemIsAbsentAsync(@"
public class goo
{
    
    /// $$
    public int bar { get; }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyPropertyValue()
        {
            await VerifyItemExistsAsync(@"
public class goo
{
    
    /// $$
    public int bar { get; }
}", "value");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WriteOnlyPropertyNoReturns()
        {
            await VerifyItemIsAbsentAsync(@"
public class goo
{
    
    /// $$
    public int bar { set; }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WriteOnlyPropertyValue()
        {
            await VerifyItemExistsAsync(@"
public class goo
{
    
    /// $$
    public int bar { set; }
}", "value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodParamTypeParam()
        {
            var text = @"
public class goo<TGoo>
{
    
    /// $$
    public int bar<TBar>(TBar green) { }
}";

            await VerifyItemsExistAsync(text, "typeparam name=\"TBar\"", "param name=\"green\"");
            await VerifyItemsAbsentAsync(text, "typeparam name=\"TGoo\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IndexerParamTypeParam()
        {
            await VerifyItemsExistAsync(@"
public class goo<T>
{

    /// $$
    public int this[T green] { get { } set { } }
}", "param name=\"green\"");
        }

        [WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodParamRefName()
        {
            var text = @"
public class Outer<TOuter>
{
    public class Inner<TInner>
    {
        /// <summary>
        /// $$
        /// </summary>
        public int Method<TMethod>(T green) { }
    }
}";
            await VerifyItemsExistAsync(
                text,
                "typeparamref name=\"TOuter\"",
                "typeparamref name=\"TInner\"",
                "typeparamref name=\"TMethod\"",
                "paramref name=\"green\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ClassTypeParamRefName()
        {
            await VerifyItemsExistAsync(@"
/// <summary>
/// $$
/// </summary>
public class goo<T>
{
    public int bar<T>(T green) { }
}", "typeparamref name=\"T\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ClassTypeParam()
        {
            await VerifyItemsExistAsync(@"
/// $$
public class goo<T>
{
    public int bar<T>(T green) { }
}", "typeparam name=\"T\"");
        }

        [WorkItem(638802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638802")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TagsAfterSameLineClosedTag()
        {
            var text = @"/// <summary>
/// <goo></goo>$$
/// 
/// </summary>
";

            await VerifyItemsExistAsync(text, "!--", "![CDATA[", "c", "code", "inheritdoc", "list", "para", "seealso", "see");
        }

        [WorkItem(734825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734825")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnumMember()
        {
            var text = @"public enum z
{
    /// <summary>
    /// 
    /// </summary>
    /// <$$
    a
}
";

            await VerifyItemsExistAsync(text);
        }

        [WorkItem(954679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954679")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionList()
        {
            await VerifyItemExistsAsync(@"
/// $$
public class goo
{
}", "completionlist");
        }

        [WorkItem(775091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775091")]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParamRefNames()
        {
            // Local functions do not support documentation comments
            await VerifyItemIsAbsentAsync(@"
/// <summary>
/// <paramref name=""$$""/>
/// </summary>
static void Main(string[] args)
{
}
", "args", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(775091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775091")]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParamRefNames_Interactive()
        {
            await VerifyItemExistsAsync(@"
/// <summary>
/// <paramref name=""$$""/>
/// </summary>
static void Main(string[] args)
{
}
", "args", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public async Task ParamNamesInEmptyAttribute()
        {
            // Local functions do not support documentation comments
            await VerifyItemIsAbsentAsync(@"
/// <param name=""$$""/>
static void Goo(string str)
{
}
", "str", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public async Task ParamNamesInEmptyAttribute_Interactive()
        {
            await VerifyItemExistsAsync(@"
/// <param name=""$$""/>
static void Goo(string str)
{
}
", "str", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(26713, "https://github.com/dotnet/roslyn/issues/26713")]
        public async Task DelegateParams()
        {
            await VerifyItemExistsAsync(@"
/// $$
delegate void D(object o);
", "param name=\"o\"");
        }

        [WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamRefNamesInEmptyAttribute()
        {
            var text = @"
public class Outer<TOuter>
{
    public class Inner<TInner>
    {
        /// <summary>
        /// <typeparamref name=""$$""/>
        /// </summary>
        public int Method<TMethod>(T green) { }
    }
}";

            await VerifyItemsExistAsync(text, "TOuter", "TInner", "TMethod");
        }

        [WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamRefNamesPartiallyTyped()
        {
            var text = @"
public class Outer<TOuter>
{
    public class Inner<TInner>
    {
        /// <summary>
        /// <typeparamref name=""T$$""/>
        /// </summary>
        public int Method<TMethod>(T green) { }
    }
}";

            await VerifyItemsExistAsync(text, "TOuter", "TInner", "TMethod");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamNamesInEmptyAttribute()
        {
            var text = @"
public class Outer<TOuter>
{
    public class Inner<TInner>
    {
        /// <typeparam name=""$$""/>
        public int Method<TMethod>(T green) { }
    }
}";

            await VerifyItemsExistAsync(text, "TMethod");
            await VerifyItemsAbsentAsync(text, "TOuter", "TInner");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamNamesInWrongScope()
        {
            var text = @"
public class Outer<TOuter>
{
    public class Inner<TInner>
    {
        /// <summary>
        /// <typeparam name=""$$""/>
        /// </summary>
        public int Method<TMethod>(T green) { }
    }
}";

            await VerifyItemsExistAsync(text, "TMethod");
            await VerifyItemsAbsentAsync(text, "TOuter", "TInner");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamNamesPartiallyTyped()
        {
            var text = @"
public class Outer<TOuter>
{
    public class Inner<TInner>
    {
        /// <typeparam name=""T$$""/>
        public int Method<TMethod>(T green) { }
    }
}";

            await VerifyItemsExistAsync(text, "TMethod");
            await VerifyItemsAbsentAsync(text, "TOuter", "TInner");
        }

        [WorkItem(8322, "https://github.com/dotnet/roslyn/issues/8322")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialTagCompletion()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// <r$$
    public void bar() { }
}", "!--", "![CDATA[", "completionlist", "example", "exception", "include", "inheritdoc", "permission", "remarks", "see", "seealso", "summary");
        }

        [WorkItem(8322, "https://github.com/dotnet/roslyn/issues/8322")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartialTagCompletionNestedTags()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// <summary>
    /// <r$$
    /// </summary>
    public void bar() { }
}", "!--", "![CDATA[", "c", "code", "inheritdoc", "list", "para", "see", "seealso");
        }

        [WorkItem(11487, "https://github.com/dotnet/roslyn/issues/11487")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamAtTopLevelOnly()
        {
            await VerifyItemsAbsentAsync(@"
/// <summary>
/// $$
/// </summary>
public class Goo<T>
{
}", "typeparam name=\"T\"");
        }

        [WorkItem(11487, "https://github.com/dotnet/roslyn/issues/11487")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParamAtTopLevelOnly()
        {
            await VerifyItemsAbsentAsync(@"
/// <summary>
/// $$
/// </summary>
static void Goo(string str)
{
}", "param name=\"str\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ListAttributeNames()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <list $$></list>
    /// </summary>
    static void Goo()
    {
    }
}", "type");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ListTypeAttributeValue()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <list type=""$$""></list>
    /// </summary>
    static void Goo()
    {
    }
}", "bullet", "number", "table");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11490")]
        [WorkItem(37504, "https://github.com/dotnet/roslyn/issues/37504")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeAttributeNames()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <see $$/>
    /// </summary>
    static void Goo()
    {
    }
}", "cref", "langword", "href");
        }

        [WorkItem(37504, "https://github.com/dotnet/roslyn/issues/37504")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeAlsoAttributeNames()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <seealso $$/>
    /// </summary>
    static void Goo()
    {
    }
}", "cref", "href");
        }

        [WorkItem(22789, "https://github.com/dotnet/roslyn/issues/22789")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LangwordCompletionInPlainText()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// Some text $$
    /// </summary>
    static void Goo()
    {
    }
}", "null", "sealed", "true", "false", "await");
        }

        [WorkItem(22789, "https://github.com/dotnet/roslyn/issues/22789")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LangwordCompletionAfterAngleBracket1()
        {
            await VerifyItemsAbsentAsync(@"
class C
{
    /// <summary>
    /// Some text <$$
    /// </summary>
    static void Goo()
    {
    }
}", "null", "sealed", "true", "false", "await");
        }

        [WorkItem(22789, "https://github.com/dotnet/roslyn/issues/22789")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LangwordCompletionAfterAngleBracket2()
        {
            await VerifyItemsAbsentAsync(@"
class C
{
    /// <summary>
    /// Some text <s$$
    /// </summary>
    static void Goo()
    {
    }
}", "null", "sealed", "true", "false", "await");
        }

        [WorkItem(22789, "https://github.com/dotnet/roslyn/issues/22789")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LangwordCompletionAfterAngleBracket3()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// Some text < $$
    /// </summary>
    static void Goo()
    {
    }
}", "null", "sealed", "true", "false", "await");
        }

        [WorkItem(11490, "https://github.com/dotnet/roslyn/issues/11490")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeLangwordAttributeValue()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <see langword=""$$""/>
    /// </summary>
    static void Goo()
    {
    }
}", "null", "true", "false", "await");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritdocAttributes1()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <inheritdoc $$/>
    /// </summary>
    static void Goo()
    {
    }
}", "cref", "path");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritdocAttributes2()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <inheritdoc $$/>
    static void Goo()
    {
    }
}", "cref", "path");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterTagNameInIncompleteTag()
        {
            var text = @"
class C
{
    /// <exception $$
    static void Goo()
    {
    }
}";
            await VerifyItemExistsAsync(text, "cref", usePreviousCharAsTrigger: true);
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterTagNameInElementStartTag()
        {
            var text = @"
class C
{
    /// <exception $$>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "cref");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterTagNameInEmptyElement()
        {
            var text = @"
class C
{
    /// <see $$/>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "cref");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterTagNamePartiallyTyped()
        {
            var text = @"
class C
{
    /// <exception c$$
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "cref");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterSpecialCrefAttribute()
        {
            var text = @"
class C
{
    /// <summary>
    /// <list cref=""String"" $$
    /// </summary>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "type");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterSpecialNameAttribute()
        {
            var text = @"
class C
{
    /// <summary>
    /// <list name=""goo"" $$
    /// </summary>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "type");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterTextAttribute()
        {
            var text = @"
class C
{
    /// <summary>
    /// <list goo="""" $$
    /// </summary>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "type");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameInWrongTagTypeEmptyElement()
        {
            var text = @"
class C
{
    /// <summary>
    /// <list $$/>
    /// </summary>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "type");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameInWrongTagTypeElementStartTag()
        {
            var text = @"
class C
{
    /// <summary>
    /// <see $$>
    /// </summary>
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "langword");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeValueOnQuote()
        {
            var text = @"
class C
{
    /// <summary>
    /// <see langword=""$$
    /// </summary>
    static void Goo()
    {
    }
}";
            await VerifyItemExistsAsync(text, "await", usePreviousCharAsTrigger: true);
        }

        [WorkItem(757, "https://github.com/dotnet/roslyn/issues/757")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TermAndDescriptionInsideItem()
        {
            var text = @"
class C
{
    /// <summary>
    ///     <list type=""table"">
    ///         <item>
    ///             $$
    ///         </item>
    ///     </list>
    /// </summary>
    static void Goo()
    {
    }
}";
            await VerifyItemExistsAsync(text, "term");
            await VerifyItemExistsAsync(text, "description");
        }

        [WorkItem(52738, "https://github.com/dotnet/roslyn/issues/52738")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RecordParam()
        {
            await VerifyItemsExistAsync(@"
/// $$
public record Goo<T>(string MyParameter);
", "param name=\"MyParameter\"", "typeparam name=\"T\"");
        }

        [WorkItem(52738, "https://github.com/dotnet/roslyn/issues/52738")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RecordParamRef()
        {
            await VerifyItemsExistAsync(@"
/// <summary>
/// $$
/// <summary>
public record Goo<T>(string MyParameter);
", "paramref name=\"MyParameter\"", "typeparamref name=\"T\"");
        }
    }
}
