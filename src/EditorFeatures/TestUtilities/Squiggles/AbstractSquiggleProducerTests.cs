// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
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
        internal static async Task<(ImmutableArray<DiagnosticData>, ImmutableArray<ITagSpan<IErrorTag>>)> GetDiagnosticsAndErrorSpansAsync<TProvider>(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null)
            where TProvider : AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
        {
            using (var wrapper = new DiagnosticTaggerWrapper<TProvider>(workspace, analyzerMap))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    await wrapper.WaitForTags();

                    var analyzerDiagnostics = await wrapper.AnalyzerService.GetDiagnosticsAsync(workspace.CurrentSolution);

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray();

                    return (analyzerDiagnostics, spans);
                }
            }
        }
    }

    internal sealed class DiagnosticTagProducer<TProvider>
        where TProvider : AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
    {
        internal Task<(ImmutableArray<DiagnosticData>, ImmutableArray<ITagSpan<IErrorTag>>)> GetDiagnosticsAndErrorSpans(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null)
        {
            return SquiggleUtilities.GetDiagnosticsAndErrorSpansAsync<TProvider>(workspace, analyzerMap);
        }

        internal async Task<IList<ITagSpan<IErrorTag>>> GetErrorsFromUpdateSource(TestWorkspace workspace, TestHostDocument document, DiagnosticsUpdatedArgs updateArgs)
        {
            var source = new TestDiagnosticUpdateSource();
            using (var wrapper = new DiagnosticTaggerWrapper<TProvider>(workspace, source))
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

        internal DiagnosticData CreateDiagnosticData(TestHostDocument document, TextSpan span)
        {
            return new DiagnosticData(
                id: "test",
                category: "test",
                message: "test",
                enuMessageForBingSearch: "test",
                severity: DiagnosticSeverity.Error,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                warningLevel: 0,
                projectId: document.Project.Id,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string>.Empty,
                location: new DiagnosticDataLocation(document.Id, span),
                language: document.Project.Language);
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
            public event EventHandler DiagnosticsCleared { add { } remove { } }

            public bool SupportGetDiagnostics => false;

            public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
            {
                return includeSuppressedDiagnostics ? _diagnostics : _diagnostics.WhereAsArray(d => !d.IsSuppressed);
            }
        }
    }
}
