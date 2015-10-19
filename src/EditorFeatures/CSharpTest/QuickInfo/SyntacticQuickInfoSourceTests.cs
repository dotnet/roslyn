// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void Brackets_1()
        {
            TestInMethod(@"
            if (true)
            {
            }$$
",
            ExpectedContent("if (true)\r\n{"));
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
    }
}
