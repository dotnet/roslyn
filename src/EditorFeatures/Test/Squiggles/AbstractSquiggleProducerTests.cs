// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
{
    public static class SquiggleUtilities
    {
        internal static List<ITagSpan<IErrorTag>> GetErrorSpans(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null)
        {
            using (var wrapper = new DiagnosticTaggerWrapper(workspace, analyzerMap))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    wrapper.WaitForTags();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();

                    return spans;
                }
            }
        }
    }

    public abstract class AbstractSquiggleProducerTests
    {
        protected static IEnumerable<ITagSpan<IErrorTag>> GetErrorSpans(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null)
        {
            return SquiggleUtilities.GetErrorSpans(workspace, analyzerMap);
        }

        internal static IList<ITagSpan<IErrorTag>> GetErrorsFromUpdateSource(TestWorkspace workspace, TestHostDocument document, DiagnosticsUpdatedArgs updateArgs)
        {
            var source = new TestDiagnosticUpdateSource();
            using (var wrapper = new DiagnosticTaggerWrapper(workspace, source))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    source.RaiseDiagnosticsUpdated(updateArgs);

                    wrapper.WaitForTags();

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
            private ImmutableArray<DiagnosticData> diagnostics = ImmutableArray<DiagnosticData>.Empty;

            public void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
            {
                this.diagnostics = args.Diagnostics;
                DiagnosticsUpdated?.Invoke(this, args);
            }

            public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

            public bool SupportGetDiagnostics => false;

            public ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                return includeSuppressedDiagnostics ? diagnostics : diagnostics.WhereAsArray(d => !d.IsSuppressed);
            }
        }
    }
}
