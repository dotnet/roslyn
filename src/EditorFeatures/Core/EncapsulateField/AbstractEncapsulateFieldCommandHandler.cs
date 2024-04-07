// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField;

internal abstract class AbstractEncapsulateFieldCommandHandler(
    IThreadingContext threadingContext,
    ITextBufferUndoManagerProvider undoManager,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider) : ICommandHandler<EncapsulateFieldCommandArgs>
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly ITextBufferUndoManagerProvider _undoManager = undoManager;
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.EncapsulateField);

    public string DisplayName => EditorFeaturesResources.Encapsulate_Field;

    public CommandState GetCommandState(EncapsulateFieldCommandArgs args)
        => args.SubjectBuffer.SupportsRefactorings() ? CommandState.Available : CommandState.Unspecified;

    public bool ExecuteCommand(EncapsulateFieldCommandArgs args, CommandExecutionContext context)
    {
        var textBuffer = args.SubjectBuffer;
        if (!textBuffer.SupportsRefactorings())
            return false;

        var spans = args.TextView.Selection.GetSnapshotSpansOnBuffer(textBuffer);
        if (spans.Count != 1)
            return false;

        var document = args.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
        if (document == null)
            return false;

        // Fire and forget
        var token = _listener.BeginAsyncOperation(FeatureAttribute.EncapsulateField);
        _ = ExecuteAsync(args, document, spans.Single()).CompletesAsyncOperation(token);
        return true;
    }

    private async Task ExecuteAsync(
        EncapsulateFieldCommandArgs args,
        Document initialDocument,
        SnapshotSpan span)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var subjectBuffer = args.SubjectBuffer;
        var workspace = initialDocument.Project.Solution.Workspace;

        var indicatorFactory = workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
        using var context = indicatorFactory.Create(
            args.TextView, span, EditorFeaturesResources.Computing_Encapsulate_Field_information,
            cancelOnEdit: true, cancelOnFocusLost: true);

        var cancellationToken = context.UserCancellationToken;
        var document = await subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(context).ConfigureAwait(false);
        Contract.ThrowIfNull(document);

        var service = document.GetRequiredLanguageService<AbstractEncapsulateFieldService>();

        var result = await service.EncapsulateFieldsInSpanAsync(
            document, span.Span.ToTextSpan(), _globalOptions.CreateProvider(), useDefaultBehavior: true, cancellationToken).ConfigureAwait(false);

        if (result == null)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // We are about to show a modal UI dialog so we should take over the command execution
            // wait context. That means the command system won't attempt to show its own wait dialog 
            // and also will take it into consideration when measuring command handling duration.
            context.TakeOwnership();

            var notificationService = workspace.Services.GetRequiredService<INotificationService>();
            notificationService.SendNotification(EditorFeaturesResources.Please_select_the_definition_of_the_field_to_encapsulate, severity: NotificationSeverity.Error);
            return;
        }

        await ApplyChangeAsync(subjectBuffer, document, result, cancellationToken).ConfigureAwait(false);
    }

    private async Task ApplyChangeAsync(
        ITextBuffer subjectBuffer,
        Document document,
        EncapsulateFieldResult result, CancellationToken cancellationToken)
    {
        var finalSolution = await result.GetSolutionAsync(cancellationToken).ConfigureAwait(false);

        var solution = document.Project.Solution;
        var workspace = solution.Workspace;
        var previewService = workspace.Services.GetService<IPreviewDialogService>();
        if (previewService != null)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            finalSolution = previewService.PreviewChanges(
                string.Format(EditorFeaturesResources.Preview_Changes_0, EditorFeaturesResources.Encapsulate_Field),
                 "vs.csharp.refactoring.preview",
                EditorFeaturesResources.Encapsulate_Field_colon,
                result.Name,
                result.Glyph,
                finalSolution,
                solution);
        }

        if (finalSolution == null)
        {
            // User clicked cancel.
            return;
        }

        using var undoTransaction = _undoManager.GetTextBufferUndoManager(subjectBuffer).TextBufferUndoHistory.CreateTransaction(EditorFeaturesResources.Encapsulate_Field);

        if (workspace.TryApplyChanges(finalSolution))
        {
            undoTransaction.Complete();
        }
        else
        {
            undoTransaction.Cancel();
        }
    }
}
