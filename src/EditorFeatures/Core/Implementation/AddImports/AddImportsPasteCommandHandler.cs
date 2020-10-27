// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AddImports
{
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Name(PredefinedCommandHandlerNames.AddImportsPaste)]
    // Order is important here, this command needs to execute before PasteTracking
    // since it may modify the pasted span. Paste tracking dismisses if 
    // the span is modified. It doesn't need to be before FormatDocument, but
    // this helps the order of execution be more constant in case there 
    // are problems that arise. This command will always execute the next
    // command before doing operations.
    [Order(After = PredefinedCommandHandlerNames.PasteTrackingPaste)]
    [Order(Before = PredefinedCommandHandlerNames.FormatDocument)]
    internal class AddImportsPasteCommandHandler : IChainedCommandHandler<PasteCommandArgs>
    {
        public string DisplayName => EditorFeaturesResources.Add_Missing_Imports_On_Paste;

        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AddImportsPasteCommandHandler(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Capture the pre-paste caret position
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPosition.HasValue)
            {
                return;
            }

            // Create a tracking span from the pre-paste caret position that will grow as text is inserted.
            var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

            // Perform the paste command before adding imports
            nextCommandHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            // Don't perform work if we're inside the interactive window
            if (args.TextView.IsBufferInInteractiveWindow(args.SubjectBuffer))
            {
                return;
            }

            if (!args.SubjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AddImportsOnPaste))
            {
                return;
            }

            // Applying the post-paste snapshot to the tracking span gives us the span of pasted text.
            var snapshotSpan = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot);
            var textSpan = snapshotSpan.Span.ToTextSpan();

            AddMissingImportsForPaste(args, executionContext, textSpan);
        }

        private void AddMissingImportsForPaste(PasteCommandArgs args, CommandExecutionContext executionContext, TextSpan textSpan)
        {
            var sourceTextContainer = args.SubjectBuffer.AsTextContainer();
            if (!Workspace.TryGetWorkspace(sourceTextContainer, out var workspace))
            {
                return;
            }

            var document = sourceTextContainer.GetOpenDocumentInCurrentContext();
            if (document is null)
            {
                return;
            }

            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();
            if (addMissingImportsService is null)
            {
                return;
            }

            using var _ = executionContext.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Adding_missing_import_directives);
            var cancellationToken = executionContext.OperationContext.UserCancellationToken;

#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            var updatedDocument = _threadingContext.JoinableTaskFactory.Run(() => addMissingImportsService.AddMissingImportsAsync(document, textSpan, cancellationToken));
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
            if (updatedDocument is null)
            {
                return;
            }

            updatedDocument.Project.Solution.Workspace.TryApplyChanges(updatedDocument.Project.Solution);
        }
    }
}
