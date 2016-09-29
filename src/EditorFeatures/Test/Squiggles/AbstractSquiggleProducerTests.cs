// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
{
    public static class SquiggleUtilities
    {
        internal static async Task<Tuple<ImmutableArray<DiagnosticData>, List<ITagSpan<IErrorTag>>>> GetDiagnosticsAndErrorSpans(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null)
        {
            using (var wrapper = new DiagnosticTaggerWrapper(workspace, analyzerMap))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    await wrapper.WaitForTags();

                    var analyzerDiagnostics = await wrapper.AnalyzerService.GetDiagnosticsAsync(workspace.CurrentSolution);

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();

                    return Tuple.Create(analyzerDiagnostics, spans);
                }
            }
        }
    }

    public abstract class AbstractSquiggleProducerTests
    {
        internal static async Task<Tuple<ImmutableArray<DiagnosticData>, List<ITagSpan<IErrorTag>>>> GetDiagnosticsAndErrorSpans(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null)
        {
            return await SquiggleUtilities.GetDiagnosticsAndErrorSpans(workspace, analyzerMap);
        }

        internal static async Task<IList<ITagSpan<IErrorTag>>> GetErrorsFromUpdateSource(TestWorkspace workspace, TestHostDocument document, DiagnosticsUpdatedArgs updateArgs)
        {
            var source = new TestDiagnosticUpdateSource();
            using (var wrapper = new DiagnosticTaggerWrapper(workspace, source))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    source.RaiseDiagnosticsUpdated(updateArgs);

                    await wrapper.WaitForTags();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray();

                    return spans;
                }
            }
        }

        internal static DiagnosticData CreateDiagnosticData(TestWorkspace workspace, TestHostDocument document, TextSpan span)
        {
            return new DiagnosticData("test", "test", "test", "test", DiagnosticSeverity.Error, true, 0, workspace, document.Project.Id,
                new DiagnosticDataLocation(document.Id, span));
        }

        private class TestDiagnosticUpdateSource : IDiagnosticUpdateSource
        {
            private ImmutableArray<DiagnosticData> _diagnostics = ImmutableArray<DiagnosticData>.Empty;

            public void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
            {
                _diagnostics = args.Diagnostics;
                DiagnosticsUpdated?.Invoke(this, args);
            }

            public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

            public bool SupportGetDiagnostics => false;

            public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                return includeSuppressedDiagnostics ? _diagnostics : _diagnostics.WhereAsArray(d => !d.IsSuppressed);
            }
        }
    }
}
