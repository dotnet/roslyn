// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders.XmlDocCommentCompletion;
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

        internal override CompletionListProvider CreateCompletionProvider()
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

        protected override async Task VerifyWorkerAsync(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            // We don't need to try writing comments in from of items in doc comments.
            await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
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
}", "c", "code", "list", "para", "paramref", "typeparamref");
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodParamTypeParam()
        {
            await VerifyItemsExistAsync(@"
public class foo<T>
{
    
    /// $$
    public int bar<T>(T green) { }
}", "typeparam name=\"T\"", "param name=\"green\"");
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitSummary()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// summary$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitSummaryOnTab()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// summary$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '\t');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitSummaryOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// summary>$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitSummary()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <summary$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitSummaryOnTab()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <summary$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '\t');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitSummaryOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <summary>$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitRemarksOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// remarks>$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "remarks", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitRemarksOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <remarks>$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "remarks", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitReturnOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        int foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// returns>$$
        int foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "returns", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitReturnOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        int foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <returns>$$
        int foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "returns", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitExampleOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// example>$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "example", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitExampleOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <example>$$
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "example", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitExceptionNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <exception cref=""$$""
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "exception", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitExceptionOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <exception cref="">$$""
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "exception", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitCommentNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <!--$$-->
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "!--", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitCommentOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <!-->$$-->
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "!--", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitCdataNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <![CDATA[$$]]>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "![CDATA[", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitCdataOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <![CDATA[>$$]]>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "![CDATA[", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitIncludeNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <include file='$$' path='[@name=""""]'/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "include", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitIncludeOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <include file='>$$' path='[@name=""""]'/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "include", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitPermissionNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <permission cref=""$$""
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "permission", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitPermissionOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <permission cref="">$$""
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "permission", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitSeeNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <see cref=""$$""/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "see", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitSeeOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <see cref="">$$""/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "see", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitSeealsoNoOpenAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <seealso cref=""$$""/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "seealso", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitSeealsoOnCloseAngle()
        {
            var markupBeforeCommit = @"class c
{
        /// <$$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <seealso cref="">$$""/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "seealso", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitParam()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// $$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// param name=""bar""$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitParamOnTab()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// $$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// param name=""bar""$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '\t');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitParamOnCloseAngle()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// $$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// param name=""bar"">$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitParam()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <param name=""bar""$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitParamOnTab()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <param name=""bar""$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '\t');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitParamOnCloseAngle()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <param name=""bar"">$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvokeWithOpenAngleCommitTypeparamOnCloseAngle()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <typeparam name=""T"">$$
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "typeparam name=\"T\"", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitList()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <summary>
        /// $$
        /// </summary>
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <summary>
        /// <list type=""$$""
        /// </summary>
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "list", expectedCodeAfterCommit);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommitListCloseAngle()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <summary>
        /// $$
        /// </summary>
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <summary>
        /// <list type="">$$""
        /// </summary>
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "list", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTagCompletion1()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        /// </summary>
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <summary>$$
        /// </summary>
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTagCompletion2()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        /// <remarks></remarks>
        /// </summary>
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <summary>$$
        /// <remarks></remarks>
        /// </summary>
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestTagCompletion3()
        {
            var markupBeforeCommit = @"class c<T>
{
        /// <$$
        /// <remarks>
        /// </summary>
        void foo<T>(T bar) { }
}";

            var expectedCodeAfterCommit = @"class c<T>
{
        /// <summary>$$
        /// <remarks>
        /// </summary>
        void foo<T>(T bar) { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [WorkItem(623168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623168")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoTrailingSpace()
        {
            var markupBeforeCommit = @"class c
{
        /// $$
        void foo() { }
}";

            var expectedCodeAfterCommit = @"class c
{
        /// <see cref=""$$""/>
        void foo() { }
}";

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "see", expectedCodeAfterCommit, commitChar: ' ');
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

            await VerifyItemsExistAsync(text, "!--", "![CDATA[", "c", "code", "list", "para", "paramref", "seealso", "see", "typeparamref");
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
    }
}
