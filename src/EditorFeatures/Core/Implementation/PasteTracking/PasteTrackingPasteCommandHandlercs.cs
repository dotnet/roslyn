// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.PasteTrackingPaste)]
    // By registering to run prior to FormatDocument and deferring until it has completed we
    // will be able to register the pasted text span after any formatting changes have been
    // applied. This is important because the PasteTrackingService will dismiss the registered
    // textspan when the textbuffer is changed.
    [Order(Before = PredefinedCommandHandlerNames.FormatDocument)]
    internal class PasteTrackingPasteCommandHandler : IChainedCommandHandler<PasteCommandArgs>
    {
        public string DisplayName => EditorFeaturesResources.Paste_Tracking;

        private readonly PasteTrackingService _pasteTrackingService;

        [ImportingConstructor]
        public PasteTrackingPasteCommandHandler(PasteTrackingService pasteTrackingService)
        {
            _pasteTrackingService = pasteTrackingService;
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Capture the pre-paste caret position
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);

            // Allow the pasted text to be inserted and formatted.
            nextCommandHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            // Create a tracking span from the pre-paste caret position that will grow as text is inserted.
            var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

            // Applying the post-paste snapshot to the tracking span gives us the span of pasted text.
            var snapshotSpan = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot);
            var textSpan = TextSpan.FromBounds(snapshotSpan.Start, snapshotSpan.End);

            _pasteTrackingService.RegisterPastedTextSpan(args.SubjectBuffer, textSpan);
        }
    }
}
