// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImportOnPaste;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract class AbstractAddImportsPasteCommandHandler(
    IThreadingContext threadingContext,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider) : IChainedCommandHandler<PasteCommandArgs>
{
    /// <summary>
    /// The command handler display name
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// The thread await dialog text shown to the user if the operation takes a long time
    /// </summary>
    protected abstract string DialogText { get; }

    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.AddImportsOnPaste);

    public CommandState GetCommandState(PasteCommandArgs args, Func<CommandState> nextCommandHandler)
        => nextCommandHandler();

    public void ExecuteCommand(PasteCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        var language = args.SubjectBuffer.GetLanguageName();

        // If the feature is not explicitly enabled we can exit early
        if (language is null || !_globalOptions.GetOption(AddImportOnPasteOptionsStorage.AddImportsOnPaste, language))
        {
            nextCommandHandler();
            return;
        }

        // Capture the pre-paste caret position
        var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer);
        if (!caretPosition.HasValue)
        {
            nextCommandHandler();
            return;
        }

        // Create a tracking span from the pre-paste caret position that will grow as text is inserted.
        var trackingSpan = caretPosition.Value.Snapshot.CreateTrackingSpan(caretPosition.Value.Position, 0, SpanTrackingMode.EdgeInclusive);

        // Perform the paste command before adding imports
        nextCommandHandler();

        if (executionContext.OperationContext.UserCancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            ExecuteCommandWorker(args, executionContext, trackingSpan);
        }
        catch (OperationCanceledException)
        {
            // According to Editor command handler API guidelines, it's best if we return early if cancellation
            // is requested instead of throwing. Otherwise, we could end up in an invalid state due to already
            // calling nextCommandHandler().
        }
    }

    private void ExecuteCommandWorker(
        PasteCommandArgs args,
        CommandExecutionContext executionContext,
        ITrackingSpan trackingSpan)
    {
        if (!args.SubjectBuffer.CanApplyChangeDocumentToWorkspace())
        {
            return;
        }

        // Don't perform work if we're inside the interactive window
        if (args.TextView.IsNotSurfaceBufferOfTextView(args.SubjectBuffer))
        {
            return;
        }

        // Applying the post-paste snapshot to the tracking span gives us the span of pasted text.
        var snapshotSpan = trackingSpan.GetSpan(args.SubjectBuffer.CurrentSnapshot);

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

        // We're showing our own UI, ensure the editor doesn't show anything itself.
        executionContext.OperationContext.TakeOwnership();

        var token = _listener.BeginAsyncOperation(nameof(ExecuteAsync));

        ExecuteAsync(document, snapshotSpan, args.TextView)
            .ReportNonFatalErrorAsync()
            .CompletesAsyncOperation(token);
    }

    private async Task ExecuteAsync(Document document, SnapshotSpan snapshotSpan, ITextView textView)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var indicatorFactory = document.Project.Solution.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
        using var backgroundWorkContext = indicatorFactory.Create(
            textView,
            snapshotSpan,
            DialogText,
            cancelOnEdit: true,
            cancelOnFocusLost: true);

        var cancellationToken = backgroundWorkContext.UserCancellationToken;

        // We're going to log the same thing on success or failure since this blocks the UI thread. This measurement is 
        // intended to tell us how long we're blocking the user from typing with this action. 
        using var blockLogger = Logger.LogBlock(FunctionId.CommandHandler_Paste_ImportsOnPaste, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken);

        await TaskScheduler.Default;

        var addMissingImportsService = document.GetRequiredLanguageService<IAddMissingImportsFeatureService>();
        var textSpan = snapshotSpan.Span.ToTextSpan();

        var updatedDocument = await addMissingImportsService.AddMissingImportsAsync(
            document, textSpan, backgroundWorkContext.GetCodeAnalysisProgress(), cancellationToken).ConfigureAwait(false);

        if (updatedDocument is null)
        {
            return;
        }

        // Required to switch back to the UI thread to call TryApplyChanges
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        document.Project.Solution.Workspace.TryApplyChanges(updatedDocument.Project.Solution);
    }
}
