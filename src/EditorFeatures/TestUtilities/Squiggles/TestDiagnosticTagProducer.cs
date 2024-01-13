// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
{
    internal sealed class TestDiagnosticTagProducer<TProvider, TTag>
        where TProvider : AbstractDiagnosticsAdornmentTaggerProvider<TTag>
        where TTag : class, ITag
    {
        internal static Task<(ImmutableArray<DiagnosticData>, ImmutableArray<ITagSpan<TTag>>)> GetDiagnosticsAndErrorSpans(
            EditorTestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>>? analyzerMap = null)
        {
            return SquiggleUtilities.GetDiagnosticsAndErrorSpansAsync<TProvider, TTag>(workspace, analyzerMap);
        }

        internal static async Task<IList<ITagSpan<TTag>>> GetErrorsFromUpdateSource(EditorTestWorkspace workspace, DiagnosticsUpdatedArgs updateArgs, DiagnosticKind diagnosticKind)
        {
            var source = new TestDiagnosticUpdateSource();

            using var wrapper = new DiagnosticTaggerWrapper<TProvider, TTag>(workspace, updateSource: source);

            var firstDocument = workspace.Documents.First();
            var tagger = wrapper.TaggerProvider.CreateTagger<TTag>(firstDocument.GetTextBuffer());
            using var disposable = (IDisposable)tagger;

            var analyzerServer = (MockDiagnosticAnalyzerService)workspace.GetService<IDiagnosticAnalyzerService>();
            analyzerServer.AddDiagnostics(updateArgs.Diagnostics, diagnosticKind);

            source.RaiseDiagnosticsUpdated(ImmutableArray.Create(updateArgs));

            await wrapper.WaitForTags();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray();

            return spans;
        }

        internal static DiagnosticData CreateDiagnosticData(EditorTestHostDocument document, TextSpan span)
        {
            Contract.ThrowIfNull(document.FilePath);

            var sourceText = document.GetTextBuffer().CurrentSnapshot.AsText();
            var linePosSpan = sourceText.Lines.GetLinePositionSpan(span);
            return new DiagnosticData(
                id: "test",
                category: "test",
                message: "test",
                severity: DiagnosticSeverity.Error,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                warningLevel: 0,
                projectId: document.Project.Id,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string?>.Empty,
                location: new DiagnosticDataLocation(new FileLinePositionSpan(document.FilePath, linePosSpan), document.Id),
                language: document.Project.Language);
        }

        private class TestDiagnosticUpdateSource : IDiagnosticUpdateSource
        {
            private ImmutableArray<DiagnosticData> _diagnostics = ImmutableArray<DiagnosticData>.Empty;

            public void RaiseDiagnosticsUpdated(ImmutableArray<DiagnosticsUpdatedArgs> args)
            {
                _diagnostics = args.SelectManyAsArray(e => e.Diagnostics);
                DiagnosticsUpdated?.Invoke(this, args);
            }

            public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>? DiagnosticsUpdated;
            public event EventHandler DiagnosticsCleared { add { } remove { } }

            public bool SupportGetDiagnostics => false;

            public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
                => new(includeSuppressedDiagnostics ? _diagnostics : _diagnostics.WhereAsArray(d => !d.IsSuppressed));
        }
    }
}
