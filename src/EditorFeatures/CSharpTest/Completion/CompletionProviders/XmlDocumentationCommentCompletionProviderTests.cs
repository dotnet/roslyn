// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders.XmlDocCommentCompletion;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class XmlDocumentationCommentCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override CompletionListProvider CreateCompletionProvider()
        {
            return new XmlDocCommentCompletionProvider();
        }

        private void VerifyItemsExist(string markup, params string[] items)
        {
            foreach (var item in items)
            {
                VerifyItemExists(markup, item);
            }
        }

        private void VerifyItemsAbsent(string markup, params string[] items)
        {
            foreach (var item in items)
            {
                VerifyItemIsAbsent(markup, item);
            }
        }

        protected override void VerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            // We don't need to try writing comments in from of items in doc comments.
            VerifyAtPosition(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            VerifyAtEndOfFile(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                VerifyAtPosition_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                VerifyAtEndOfFile_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AlwaysVisibleAtAnyLevelItems1()
        {
            VerifyItemsExist(@"
public class foo
{
    /// $$
    public void bar() { }
}", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AlwaysVisibleAtAnyLevelItems2()
        {
            VerifyItemsExist(@"
public class foo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "see", "seealso", "![CDATA[", "!--");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AlwaysVisibleNotTopLevelItems1()
        {
            VerifyItemsExist(@"
public class foo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "c", "code", "list", "para", "paramref", "typeparamref");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AlwaysVisibleNotTopLevelItems2()
        {
            VerifyItemsAbsent(@"
public class foo
{
    /// $$ 
    public void bar() { }
}", "c", "code", "list", "para", "paramref", "typeparamref");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AlwaysVisibleTopLevelOnlyItems1()
        {
            VerifyItemsExist(@"
public class foo
{
    /// $$ 
    public void bar() { }
}", "exception", "include", "permission");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AlwaysVisibleTopLevelOnlyItems2()
        {
            VerifyItemsAbsent(@"
public class foo
{
    /// <summary> $$ </summary>
    public void bar() { }
}", "exception", "include", "permission");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TopLevelSingleUseItems1()
        {
            VerifyItemsExist(@"
public class foo
{
    ///  $$
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TopLevelSingleUseItems2()
        {
            VerifyItemsAbsent(@"
public class foo
{
    ///  <summary> $$ </summary>
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TopLevelSingleUseItems3()
        {
            VerifyItemsAbsent(@"
public class foo
{
    ///  <summary> $$ </summary>
    /// <example></example>
    /// <remarks></remarks>
    
    public void bar() { }
}", "example", "remarks", "summary");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OnlyInListItems()
        {
            VerifyItemsAbsent(@"
public class foo
{
    ///  <summary> $$ </summary>
    /// <example></example>
    /// <remarks></remarks>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OnlyInListItems2()
        {
            VerifyItemsAbsent(@"
public class foo
{
    ///   $$ 
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OnlyInListItems3()
        {
            VerifyItemsExist(@"
public class foo
{
    ///   <list>$$</list>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OnlyInListItems4()
        {
            VerifyItemsExist(@"
public class foo
{
    ///   <list><$$</list>
    
    public void bar() { }
}", "listheader", "item", "term", "description");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ListHeaderItems()
        {
            VerifyItemsExist(@"
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
        public void VoidMethodDeclarationItems()
        {
            VerifyItemIsAbsent(@"
public class foo
{
    
    /// $$
    public void bar() { }
}", "returns");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodReturns()
        {
            VerifyItemExists(@"
public class foo
{
    
    /// $$
    public int bar() { }
}", "returns");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodParamTypeParam()
        {
            VerifyItemsExist(@"
public class foo<T>
{
    
    /// $$
    public int bar<T>(T green) { }
}", "typeparam name=\"T\"", "param name=\"green\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IndexerParamTypeParam()
        {
            VerifyItemsExist(@"
public class foo<T>
{

    /// $$
    public int this[T green] { get { } set { } }
}", "param name=\"green\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ClassTypeParam()
        {
            VerifyItemsExist(@"
/// $$
public class foo<T>
{
    public int bar<T>(T green) { }
}", "typeparam name=\"T\"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSummary()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSummaryOnTab()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSummaryOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitSummary()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitSummaryOnTab()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitSummaryOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitRemarksOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "remarks", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitRemarksOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "remarks", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitReturnOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "returns", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitReturnOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "returns", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitExampleOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "example", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitExampleOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "example", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitExceptionNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "exception", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitExceptionOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "exception", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitCommentNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "!--", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitCommentOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "!--", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitCdataNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "![CDATA[", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitCdataOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "![CDATA[", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitIncludeNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "include", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitIncludeOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "include", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitPermissionNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "permission", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitPermissionOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "permission", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSeeNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "see", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitSeeOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "see", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitSeealsoNoOpenAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "seealso", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitSeealsoOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "seealso", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitParam()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitParamOnTab()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitParamOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitParam()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitParamOnTab()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '\t');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitParamOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "param name=\"bar\"", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvokeWithOpenAngleCommitTypeparamOnCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "typeparam name=\"T\"", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitList()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "list", expectedCodeAfterCommit);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitListCloseAngle()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "list", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestTagCompletion1()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestTagCompletion2()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestTagCompletion3()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "summary", expectedCodeAfterCommit, commitChar: '>');
        }

        [WorkItem(623168)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoTrailingSpace()
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

            VerifyCustomCommitProvider(markupBeforeCommit, "see", expectedCodeAfterCommit, commitChar: ' ');
        }

        [WorkItem(638802)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TagsAfterSameLineClosedTag()
        {
            var text = @"/// <summary>
/// <foo></foo>$$
/// 
/// </summary>
";

            VerifyItemsExist(text, "!--", "![CDATA[", "c", "code", "list", "para", "paramref", "seealso", "see", "typeparamref");
        }

        [WorkItem(734825)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EnumMember()
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

            VerifyItemsExist(text);
        }

        [WorkItem(954679)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CompletionList()
        {
            VerifyItemExists(@"
/// $$
public class foo
{
}", "completionlist");
        }

        [WorkItem(775091)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ParamRefNames()
        {
            VerifyItemExists(@"
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
