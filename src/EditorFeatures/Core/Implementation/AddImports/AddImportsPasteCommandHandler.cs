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
    [ContentType(ContentTypeNames.RoslynContentType)]
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

        public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            // Capture the pre-paste caret position
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!caretPosition.HasValue)
            {
                return;
            }

            nextCommandHandler();

            if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
            {
                return;
            }

            if (!args.SubjectBuffer.GetFeatureOnOffOption(FeatureOnOffOptions.AddImportsOnPaste))
            {
                return;
            }

            // Create a tracking span from the pre-paste caret position that will grow as text is inserted.
            var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

            // Applying the post-paste snapshot to the tracking span gives us the span of pasted text.
            var snapshotSpan = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot);
            var textSpan = TextSpan.FromBounds(snapshotSpan.Start, snapshotSpan.End);

            AddMissingImportsForPaste(args, executionContext, textSpan);
        }

        public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        private void AddMissingImportsForPaste(PasteCommandArgs args, CommandExecutionContext executionContext, TextSpan textSpan)
        {
            var sourceTextContainer = args.SubjectBuffer.AsTextContainer();
            if (!Workspace.TryGetWorkspace(sourceTextContainer, out var workspace))
            {
                return;
            }

            var documentId = workspace.GetDocumentIdInCurrentContext(sourceTextContainer);
            var document = workspace.CurrentSolution.GetDocument(documentId);

            if (document is null)
            {
                return;
            }

            using var _ = executionContext.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Adding_missing_import_directives);
            var cancellationToken = executionContext.OperationContext.UserCancellationToken;

            var updatedProject = _threadingContext.JoinableTaskFactory.Run(() => AddMissingImportsAsync(document, textSpan, cancellationToken));
            if (updatedProject is null)
            {
                return;
            }

            updatedProject.Solution.Workspace.TryApplyChanges(updatedProject.Solution);
        }

        private static async Task<Project?> AddMissingImportsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var addMissingImportsService = document.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
            return await addMissingImportsService.AddMissingImportsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        }
    }
}
