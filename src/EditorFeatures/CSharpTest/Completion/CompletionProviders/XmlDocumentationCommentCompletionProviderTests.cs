// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class XmlDocumentationCommentCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public XmlDocumentationCommentCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new XmlDocCommentCompletionProvider();
        }

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

        protected override async Task VerifyWorkerAsync(
            string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull,
            SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence,
            int? glyph, int? matchPriority, bool? hasSuggestionItem)
        {
            // We don't need to try writing comments in from of items in doc comments.
            await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem);
            await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem);
                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem);
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
}", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleAtAnyLevelItems2()
        {
            await VerifyItemsExistAsync(@"
public class goo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "see", "seealso", "![CDATA[", "!--");
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

            await VerifyItemsExistAsync(text, "!--", "![CDATA[", "c", "code", "list", "para", "seealso", "see");
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
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParamRefNames()
        {
            await VerifyItemExistsAsync(@"
/// <summary>
/// <paramref name=""$$""/>
/// </summary>
static void Main(string[] args)
{
}
", "args");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParamNames()
        {
            await VerifyItemExistsAsync(@"
/// <param name=""$$""/>
static void Goo(string str)
{
}
", "str");
        }

        [WorkItem(17872, "https://github.com/dotnet/roslyn/issues/17872")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamRefNames()
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
        public async Task TypeParamNames()
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
        /// <summary>
        /// <typeparam name=""T$$""/>
        /// </summary>
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
}", "!--", "![CDATA[", "completionlist", "example", "exception", "include", "permission", "remarks", "see", "seealso", "summary");
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
}", "!--", "![CDATA[", "c", "code", "list", "para", "see", "seealso");
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
        public async Task ListTypes()
        {
            await VerifyItemsExistAsync(@"
/// <summary>
/// <list type=""$$""
/// </summary>
static void Goo()
{
}
", "bullet", "number", "table");
        }

        [WorkItem(11490, "https://github.com/dotnet/roslyn/issues/11490")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeLangwordValues()
        {
            await VerifyItemsExistAsync(@"
/// <summary>
/// <see langword=""$$""
/// </summary>
static void Goo()
{
}
", "await", "class");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeAttributesOnSpace()
        {
            var text = @"
/// <summary>
/// <see $$
/// </summary>
static void Goo()
{
}";
            await VerifyItemExistsAsync(text, "langword", usePreviousCharAsTrigger: true);
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeLangwordValuesOnQuote()
        {
            var text = @"
/// <summary>
/// <see langword=""$$
/// </summary>
static void Goo()
{
}";
            await VerifyItemExistsAsync(text, "await", usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterElementName()
        {
            var text = @"
class C
{
    /// <exception $$
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "cref");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PartiallyTypedAttributeName()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterAttribute()
        {
            var text = @"
class C
{
    /// <exception name="""" $$
    void Goo() { }
}
";
            await VerifyItemExistsAsync(text, "cref");
        }
    }
}
