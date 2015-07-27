// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract class AbstractDiagnosticsTaggerProvider<TTag> : AsynchronousTaggerProvider<TTag> where TTag : ITag
    {
        private readonly IDiagnosticService _diagnosticService;

        protected AbstractDiagnosticsTaggerProvider(
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListener listener)
            : base(listener, notificationService)
        {
            _diagnosticService = diagnosticService;
        }

        protected abstract bool IsEnabled { get; }
        protected abstract bool IncludeDiagnostic(DiagnosticData data);
        protected abstract ITagSpan<TTag> CreateTagSpan(SnapshotSpan span, DiagnosticData data);

        protected override sealed ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer, TaggerDelay.Medium),
                TaggerEventSources.OnDiagnosticsChanged(subjectBuffer, _diagnosticService, TaggerDelay.Medium));
        }

        protected override sealed Task ProduceTagsAsync(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            ProduceTags(context, spanToTag);
            return SpecializedTasks.EmptyTask;
        }

        private void ProduceTags(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag)
        {
            if (!IsEnabled)
            {
                return;
            }

            var document = spanToTag.Document;
            if (document == null)
            {
                return;
            }

            var project = document.Project;
            var diagnostics = _diagnosticService.GetDiagnostics(
                project.Solution.Workspace, project.Id, document.Id, id: null, cancellationToken: context.CancellationToken);

            var snapshot = spanToTag.SnapshotSpan.Snapshot;
            var sourceText = snapshot.AsText();
            var requestedSpan = spanToTag.SnapshotSpan.Span.ToTextSpan();
            foreach (var diagnosticData in diagnostics)
            {
                if (IncludeDiagnostic(diagnosticData))
                {
                    var actualSpan = diagnosticData.GetExistingOrCalculatedTextSpan(sourceText);
                    if (actualSpan.IntersectsWith(requestedSpan))
                    {
                        var tagSpan = CreateTagSpan(actualSpan.ToSnapshotSpan(snapshot), diagnosticData);
                        if (tagSpan != null)
                        {
                            context.AddTag(tagSpan);
                        }
                    }
                }
            }
        }
    }
}