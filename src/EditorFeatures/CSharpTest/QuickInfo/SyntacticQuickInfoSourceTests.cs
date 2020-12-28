﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public class SyntacticQuickInfoSourceTests : AbstractQuickInfoSourceTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Brackets_0()
        {
            await TestInMethodAndScriptAsync(
@"
switch (true)
{
}$$
",
@"switch (true)
{");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Brackets_1()
            => await TestInClassAsync("int Property { get; }$$ ", "int Property {");

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Brackets_2()
            => await TestInClassAsync("void M()\r\n{ }$$ ", "void M()\r\n{");

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Brackets_3()
            => await TestInMethodAndScriptAsync("var a = new int[] { }$$ ", "new int[] {");

        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task Brackets_4()
        {
            await TestInMethodAndScriptAsync(
@"
if (true)
{
}$$
",
@"if (true)
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_0()
        {
            await TestInMethodAndScriptAsync(
@"if (true)
            {
                {
                }$$
            }",
            "{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_1()
        {
            await TestInMethodAndScriptAsync(
@"while (true)
            {
                // some
                // comment
                {
                }$$
            }",
@"// some
// comment
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_2()
        {
            await TestInMethodAndScriptAsync(
@"do
            {
                /* comment */
                {
                }$$
            }
            while (true);",
@"/* comment */
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_3()
        {
            await TestInMethodAndScriptAsync(
@"if (true)
            {
            }
            else
            {
                {
                    // some
                    // comment
                }$$
            }",
@"{
    // some
    // comment");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_4()
        {
            await TestInMethodAndScriptAsync(
@"using (var x = new X())
            {
                {
                    /* comment */
                }$$
            }",
@"{
    /* comment */");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_5()
        {
            await TestInMethodAndScriptAsync(
@"foreach (var x in xs)
            {
                // above
                {
                    /* below */
                }$$
            }",
@"// above
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_6()
        {
            await TestInMethodAndScriptAsync(
@"for (;;)
            {
                /*************/

                // part 1

                // part 2
                {
                }$$
            }",
@"/*************/

// part 1

// part 2
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_7()
        {
            await TestInMethodAndScriptAsync(
@"try
            {
                /*************/

                // part 1

                // part 2
                {
                }$$
            }
            catch { throw; }",
@"/*************/

// part 1

// part 2
{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_8()
        {
            await TestInMethodAndScriptAsync(
@"
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
        public async Task ScopeBrackets_9()
        {
            await TestInClassAsync(
@"int Property
{
    set
    {
        {
        }$$
    }
}",
            "{");
        }

        [WorkItem(325, "https://github.com/dotnet/roslyn/issues/325")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)]
        public async Task ScopeBrackets_10()
        {
            await TestInMethodAndScriptAsync(
@"switch (true)
            {
                default:
                    // comment
                    {
                    }$$
                    break;
            }",
@"// comment
{");
        }

        private static QuickInfoProvider CreateProvider()
            => new CSharpSyntacticQuickInfoProvider();

        protected override async Task AssertNoContentAsync(
            TestWorkspace workspace,
            Document document,
            int position)
        {
            var provider = CreateProvider();
            Assert.Null(await provider.GetQuickInfoAsync(new QuickInfoContext(document, position, CancellationToken.None)));
        }

        protected override async Task AssertContentIsAsync(
            TestWorkspace workspace,
            Document document,
            int position,
            string expectedContent,
            string expectedDocumentationComment = null)
        {
            var provider = CreateProvider();
            var info = await provider.GetQuickInfoAsync(new QuickInfoContext(document, position, CancellationToken.None));
            Assert.NotNull(info);
            Assert.NotEqual(0, info.RelatedSpans.Length);

            var trackingSpan = new Mock<ITrackingSpan>(MockBehavior.Strict);
            var threadingContext = workspace.ExportProvider.GetExportedValue<IThreadingContext>();
            var streamingPresenter = workspace.ExportProvider.GetExport<IStreamingFindUsagesPresenter>();
            var quickInfoItem = await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, info, document, threadingContext, streamingPresenter, CancellationToken.None);
            var containerElement = quickInfoItem.Item as ContainerElement;

            var textElements = containerElement.Elements.OfType<ClassifiedTextElement>();
            Assert.NotEmpty(textElements);

            var textElement = textElements.First();
            var actualText = string.Concat(textElement.Runs.Select(r => r.Text));
            Assert.Equal(expectedContent, actualText);
        }

        protected override Task TestInMethodAsync(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            return TestInClassAsync(
@"void M()
{" + code + "}", expectedContent, expectedDocumentationComment);
        }

        protected override Task TestInClassAsync(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            return TestAsync(
@"class C
{" + code + "}", expectedContent, expectedDocumentationComment);
        }

        protected override Task TestInScriptAsync(string code, string expectedContent, string expectedDocumentationComment = null)
            => TestAsync(code, expectedContent, expectedContent, Options.Script);

        protected override async Task TestAsync(
            string code,
            string expectedContent,
            string expectedDocumentationComment = null,
            CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, parseOptions);
            var testDocument = workspace.Documents.Single();
            var position = testDocument.CursorPosition.Value;
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            if (string.IsNullOrEmpty(expectedContent))
            {
                await AssertNoContentAsync(workspace, document, position);
            }
            else
            {
                await AssertContentIsAsync(workspace, document, position, expectedContent, expectedDocumentationComment);
            }
        }
    }
}
