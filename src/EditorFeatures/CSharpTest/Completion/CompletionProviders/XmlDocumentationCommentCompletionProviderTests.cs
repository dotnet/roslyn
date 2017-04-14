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
            int? glyph, int? matchPriority)
        {
            // We don't need to try writing comments in from of items in doc comments.
            await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
            await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleAtAnyLevelItems1()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    /// $$
    public void bar() { }
}", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleAtAnyLevelItems2()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleNotTopLevelItems1()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "c", "code", "list", "para");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleNotTopLevelItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class foo
{
    /// $$ 
    public void bar() { }
}", "c", "code", "list", "para", "paramref", "typeparamref");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleTopLevelOnlyItems1()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    /// $$ 
    public void bar() { }
}", "exception", "include", "permission");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AlwaysVisibleTopLevelOnlyItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class foo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "exception", "include", "permission");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevelSingleUseItems1()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    ///  $$
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevelSingleUseItems2()
        {
            await VerifyItemsAbsentAsync(@"
public class foo
{
    ///  <summary> $$ </summary>
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevelSingleUseItems3()
        {
            await VerifyItemsAbsentAsync(@"
public class foo
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
public class foo
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
public class foo
{
    ///   $$ 
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyInListItems3()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    ///   <list>$$</list>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyInListItems4()
        {
            await VerifyItemsExistAsync(@"
public class foo
{
    ///   <list><$$</list>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ListHeaderItems()
        {
            await VerifyItemsExistAsync(@"
public class foo
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
public class foo
{
    
    /// $$
    public void bar() { }
}", "returns");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodReturns()
        {
            await VerifyItemExistsAsync(@"
public class foo
{
    
    /// $$
    public int bar() { }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadWritePropertyReturns()
        {
            await VerifyItemExistsAsync(@"
public class foo
{
    
    /// $$
    public int bar { get; set; }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReadOnlyPropertyReturns()
        {
            await VerifyItemExistsAsync(@"
public class foo
{
    
    /// $$
    public int bar { get; }
}", "returns");
        }

        [WorkItem(8627, "https://github.com/dotnet/roslyn/issues/8627")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WriteOnlyPropertyNoReturns()
        {
            await VerifyItemIsAbsentAsync(@"
public class foo
{
    
    /// $$
    public int bar { set; }
}", "returns");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodParamTypeParam()
        {
            var text = @"
public class foo<TFoo>
{
    
    /// $$
    public int bar<TBar>(TBar green) { }
}";

            await VerifyItemsExistAsync(text, "typeparam name=\"TBar\"", "param name=\"green\"");
            await VerifyItemsAbsentAsync(text, "typeparam name=\"TFoo\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IndexerParamTypeParam()
        {
            await VerifyItemsExistAsync(@"
public class foo<T>
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
public class foo<T>
{
    public int bar<T>(T green) { }
}", "typeparamref name=\"T\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ClassTypeParam()
        {
            await VerifyItemsExistAsync(@"
/// $$
public class foo<T>
{
    public int bar<T>(T green) { }
}", "typeparam name=\"T\"");
        }

        [WorkItem(638802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638802")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TagsAfterSameLineClosedTag()
        {
            var text = @"/// <summary>
/// <foo></foo>$$
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
public class foo
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
        public async Task ParamNamesInEmptyAttribute()
        {
            await VerifyItemExistsAsync(@"
/// <param name=""$$""/>
static void Foo(string str)
{
}
", "str");
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
public class foo
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
public class foo
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
public class Foo<T>
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
static void Foo(string str)
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
    static void Foo()
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
    static void Foo()
    {
    }
}", "bullet", "number", "table");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11490")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SeeAttributeNames()
        {
            await VerifyItemsExistAsync(@"
class C
{
    /// <summary>
    /// <see $$/>
    /// </summary>
    static void Foo()
    {
    }
}", "cref", "langword");
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
    static void Foo()
    {
    }
}", "await", "class");
        }

        [WorkItem(11489, "https://github.com/dotnet/roslyn/issues/11489")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterTagNameInIncompleteTag()
        {
            var text = @"
class C
{
    /// <exception $$
    static void Foo()
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
    void Foo() { }
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
    void Foo() { }
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
    void Foo() { }
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
    void Foo() { }
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
    /// <list name=""foo"" $$
    /// </summary>
    void Foo() { }
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
    /// <list foo="""" $$
    /// </summary>
    void Foo() { }
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
    void Foo() { }
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
    void Foo() { }
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
    static void Foo()
    {
    }
}";
            await VerifyItemExistsAsync(text, "await", usePreviousCharAsTrigger: true);
        }
    }
}
