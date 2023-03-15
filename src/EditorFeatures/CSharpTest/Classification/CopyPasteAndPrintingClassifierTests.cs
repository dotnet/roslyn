// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Classification)]
    public class CopyPasteAndPrintingClassifierTests
    {
        [WpfFact]
        public async Task TestGetTagsOnBufferTagger()
        {
            // don't crash
            using var workspace = TestWorkspace.CreateCSharp("class C { C c; }");
            var document = workspace.Documents.First();

            var listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            var provider = new CopyPasteAndPrintingClassificationBufferTaggerProvider(
                workspace.GetService<IThreadingContext>(),
                workspace.GetService<ClassificationTypeMap>(),
                listenerProvider,
                workspace.GlobalOptions);

            var tagger = provider.CreateTagger<IClassificationTag>(document.GetTextBuffer())!;
            using var disposable = (IDisposable)tagger;
            var waiter = listenerProvider.GetWaiter(FeatureAttribute.Classification);
            await waiter.ExpeditedWaitAsync();

            var tags = tagger.GetTags(document.GetTextBuffer().CurrentSnapshot.GetSnapshotSpanCollection());
            var allTags = tagger.GetAllTags(document.GetTextBuffer().CurrentSnapshot.GetSnapshotSpanCollection(), CancellationToken.None);

            Assert.Empty(tags);
            Assert.NotEmpty(allTags);

            Assert.Equal(1, allTags.Count());
        }
    }
}
