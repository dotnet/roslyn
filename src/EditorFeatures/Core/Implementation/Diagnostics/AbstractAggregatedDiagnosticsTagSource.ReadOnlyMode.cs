#if false
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract partial class AbstractAggregatedDiagnosticsTagSource<TTag>
    {
        private class ReadOnlyMode : Mode
        {
            private CancellationTokenSource _source;

            public ReadOnlyMode(AbstractAggregatedDiagnosticsTagSource<TTag> owner) : base(owner)
            {
                _source = new CancellationTokenSource();
                this.DiagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
            }

            public override void Disconnect()
            {
                this.DiagnosticService.DiagnosticsUpdated -= OnDiagnosticsUpdated;
            }

            public override IList<ITagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan)
            {
                if (this.SubjectBuffer != snapshotSpan.Snapshot.TextBuffer)
                {
                    // in venus case, buffer comes and goes and tag source might hold onto diagnostics that belong to
                    // old/new buffers which are different than current subject buffer.
                    return null;
                }

                // get document in right context
                var document = this.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                if (document == null)
                {
                    return null;
                }

                var diagnostics = this.DiagnosticService
                                      .GetDiagnostics(document.Project.Solution.Workspace, document.Project.Id, document.Id, id: null, cancellationToken: CancellationToken.None)
                                      .Where(this.Owner.ShouldInclude);

                List<ITagSpan<TTag>> result = null;

                var snapshot = snapshotSpan.Snapshot;
                var range = new TextSpan(snapshotSpan.Start, snapshotSpan.Length);

                foreach (var diagnostic in diagnostics)
                {
                    var span = diagnostic.GetExistingOrCalculatedTextSpan(snapshotSpan.Snapshot.AsText());
                    if (range.IntersectsWith(span))
                    {
                        var tagSpan = this.Owner.CreateTagSpan(AdjustSnapshotSpan(span.ToSnapshotSpan(snapshot), this.Owner.MinimumLength), diagnostic);
                        if (tagSpan != null)
                        {
                            result = result ?? new List<ITagSpan<TTag>>();
                            result.Add(tagSpan);
                        }
                    }
                }

                return result;
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                _source.Cancel();
                _source = new CancellationTokenSource();

                this.Owner.RegisterNotification(() => RefreshEntireBuffer(), TaggerConstants.ShortDelay, _source.Token);
            }
        }
    }
}
#endif