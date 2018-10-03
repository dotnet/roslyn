// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [Export]
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.PasteTrackingPaste)]
    [Order(After = PredefinedCommandHandlerNames.FormatDocument)]
    [Order(Before = PredefinedCommandHandlerNames.Completion)]
    internal partial class PasteTrackingPasteCommandHandler : IChainedCommandHandler<PasteCommandArgs>
    {
        public string DisplayName => EditorFeaturesResources.Paste_Tracking;

        private readonly IPasteTrackingService _pasteTrackingService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal PasteTrackingPasteCommandHandler(IPasteTrackingService pasteTrackingService)
        {
            _pasteTrackingService = pasteTrackingService;
        }

        public VSCommanding.CommandState GetCommandState(PasteCommandArgs args, Func<VSCommanding.CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);

            nextCommandHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var snapshotSpan = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot);
            var textSpan = TextSpan.FromBounds(snapshotSpan.Start, snapshotSpan.End);

            _pasteTrackingService.RegisterPastedTextSpan(document, textSpan);
        }
    }
}
