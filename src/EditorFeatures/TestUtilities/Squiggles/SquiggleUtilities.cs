// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
{
    public static class SquiggleUtilities
    {
        // Squiggle tests require solution crawler to run.
        internal static TestComposition CompositionWithSolutionCrawler = EditorTestCompositions.EditorFeatures
            .RemoveParts(typeof(MockWorkspaceEventListenerProvider));

        internal static TestComposition WpfCompositionWithSolutionCrawler = EditorTestCompositions.EditorFeaturesWpf
            .RemoveParts(typeof(MockWorkspaceEventListenerProvider));

        internal static async Task<ImmutableArray<TagSpan<TTag>>> GetTagSpansAsync<TProvider, TTag>(
            EditorTestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzerMap = null)
            where TProvider : AbstractDiagnosticsTaggerProvider<TTag>
            where TTag : class, ITag
        {
            var wrapper = new DiagnosticTaggerWrapper<TProvider, TTag>(workspace, analyzerMap);

            var firstDocument = workspace.Documents.First();
            var textBuffer = firstDocument.GetTextBuffer();
            using var tagger = wrapper.TaggerProvider.CreateTagger<TTag>(textBuffer);

            await wrapper.WaitForTags();

            var snapshot = textBuffer.CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray();

            return spans;
        }
    }
}
