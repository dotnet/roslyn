// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public class SyntacticQuickInfoSourceTests : AbstractQuickInfoSourceTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void Brackets_0()
        {
            TestInMethodAndScript(@"
            switch (true)
            {
            }$$
",
            "switch (true)\r\n{");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void Brackets_1()
        {
            TestInClass("int Property { get; }$$ ", "int Property {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void Brackets_2()
        {
            TestInClass("void M()\r\n{ }$$ ", "void M()\r\n{");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void Brackets_3()
        {
            TestInMethodAndScript("var a = new int[] { }$$ ", "new int[] {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void Brackets_4()
        {
            TestInMethodAndScript(@"
            if (true)
            {
            }$$
",
            "if (true)\r\n{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_0()
        {
            TestInMethodAndScript(@"
            if (true)
            {
                {
                }$$
            }
",
            "{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_1()
        {
            TestInMethodAndScript(@"
            while (true)
            {
                // some
                // comment
                {
                }$$
            }
",
@"// some
// comment
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_2()
        {
            TestInMethodAndScript(@"
            do
            {
                /* comment */
                {
                }$$
            }
            while (true);
",
@"/* comment */
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_3()
        {
            TestInMethodAndScript(@"
            if (true)
            {
            }
            else
            {
                {
                    // some
                    // comment
                }$$
            }
",
@"{
    // some
    // comment");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_4()
        {
            TestInMethodAndScript(@"
            using (var x = new X())
            {
                {
                    /* comment */
                }$$
            }
",
@"{
    /* comment */");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_5()
        {
            TestInMethodAndScript(@"
            foreach (var x in xs)
            {
                // above
                {
                    /* below */
                }$$
            }
",
@"// above
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_6()
        {
            TestInMethodAndScript(@"
            for (;;)
            {
                /*************/

                // part 1

                // part 2
                {
                }$$
            }
",
@"/*************/

// part 1

// part 2
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_7()
        {
            TestInMethodAndScript(@"
            try
            {
                /*************/

                // part 1

                // part 2
                {
                }$$
            }
            catch { throw; }
",
@"/*************/

// part 1

// part 2
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_8()
        {
            TestInMethodAndScript(@"
            {
                /*************/

                // part 1

                // part 2
            }$$
",
@"{
    /*************/

    // part 1

    // part 2");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_9()
        {
            TestInClass(@"
            int Property
            {
                set
                {
                    {
                    }$$
                }
            }
",
            "{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public void ScopeBrackets_10()
        {
            TestInMethodAndScript(@"
            switch (true)
            {
                default:
                    // comment
                    {
                    }$$
                    break;
            }
",
@"// comment
{");
        }

        private IQuickInfoProvider CreateProvider(TestWorkspace workspace)
        {
            return new SyntacticQuickInfoProvider(
                workspace.GetService<ITextBufferFactoryService>(),
                workspace.GetService<IContentTypeRegistryService>(),
                workspace.GetService<IProjectionBufferFactoryService>(),
                workspace.GetService<IEditorOptionsFactoryService>(),
                workspace.GetService<ITextEditorFactoryService>(),
                workspace.GetService<IGlyphService>(),
                workspace.GetService<ClassificationTypeMap>());
        }

        protected override void AssertNoContent(
            TestWorkspace workspace,
            Document document,
            int position)
        {
            var provider = CreateProvider(workspace);
            Assert.Null(provider.GetItemAsync(document, position, CancellationToken.None).Result);
        }

        protected override void AssertContentIs(
            TestWorkspace workspace,
            Document document,
            int position,
            string expectedContent,
            string expectedDocumentationComment = null)
        {
            var provider = CreateProvider(workspace);
            var state = provider.GetItemAsync(document, position, cancellationToken: CancellationToken.None).Result;
            Assert.NotNull(state);

            var viewHostingControl = (ViewHostingControl)((ElisionBufferDeferredContent)state.Content).Create();
            try
            {
                var actualContent = viewHostingControl.ToString();
                Assert.Equal(expectedContent, actualContent);
            }
            finally
            {
                viewHostingControl.TextView_TestOnly.Close();
            }
        }

        protected override void TestInMethod(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            TestInClass("void M(){" + code + "}", expectedContent, expectedDocumentationComment);
        }

        protected override void TestInClass(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            Test("class C {" + code + "}", expectedContent, expectedDocumentationComment);
        }

        protected override void TestInScript(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            Test(code, expectedContent, expectedContent, Options.Script);
        }

        protected override void Test(
            string code,
            string expectedContent,
            string expectedDocumentationComment = null,
            CSharpParseOptions parseOptions = null)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code, parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var position = testDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.Projects.First().Documents.First();

                if (string.IsNullOrEmpty(expectedContent))
                {
                    AssertNoContent(workspace, document, position);
                }
                else
                {
                    AssertContentIs(workspace, document, position, expectedContent, expectedDocumentationComment);
                }
            }
        }
    }
}
