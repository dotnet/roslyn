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
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
{
    internal sealed class TestDiagnosticTagProducer<TProvider>
        where TProvider : AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
    {
        internal static Task<(ImmutableArray<DiagnosticData>, ImmutableArray<ITagSpan<IErrorTag>>)> GetDiagnosticsAndErrorSpans(
            TestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzerMap = null)
        {
            return SquiggleUtilities.GetDiagnosticsAndErrorSpansAsync<TProvider>(workspace, analyzerMap);
        }

        internal static async Task<IList<ITagSpan<IErrorTag>>> GetErrorsFromUpdateSource(TestWorkspace workspace, DiagnosticsUpdatedArgs updateArgs)
        {
            var source = new TestDiagnosticUpdateSource();
            using (var wrapper = new DiagnosticTaggerWrapper<TProvider, IErrorTag>(workspace, updateSource: source))
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

        internal static DiagnosticData CreateDiagnosticData(TestHostDocument document, TextSpan span)
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
                => includeSuppressedDiagnostics ? _diagnostics : _diagnostics.WhereAsArray(d => !d.IsSuppressed);
        }
    }
}
