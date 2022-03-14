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
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        internal static async Task<(ImmutableArray<DiagnosticData>, ImmutableArray<ITagSpan<TTag>>)> GetDiagnosticsAndErrorSpansAsync<TProvider, TTag>(
            TestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzerMap = null)
            where TProvider : AbstractDiagnosticsAdornmentTaggerProvider<TTag>
            where TTag : class, ITag
        {
            using var wrapper = new DiagnosticTaggerWrapper<TProvider, TTag>(workspace, analyzerMap);
            var tagger = wrapper.TaggerProvider.CreateTagger<TTag>(workspace.Documents.First().GetTextBuffer());

            using var disposable = tagger as IDisposable;
            await wrapper.WaitForTags();

            var analyzerDiagnostics = await wrapper.AnalyzerService.GetDiagnosticsAsync(workspace.CurrentSolution);

            var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray();

            return (analyzerDiagnostics, spans);
        }
    }
}
