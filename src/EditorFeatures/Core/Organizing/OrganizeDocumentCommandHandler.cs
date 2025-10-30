// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Organizing;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[Name(PredefinedCommandHandlerNames.OrganizeDocument)]
[method: ImportingConstructor]
[SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class OrganizeDocumentCommandHandler(
    IThreadingContext threadingContext,
    IAsynchronousOperationListenerProvider listenerProvider) :
    ICommandHandler<OrganizeDocumentCommandArgs>,
    ICommandHandler<SortImportsCommandArgs>,
    ICommandHandler<SortAndRemoveUnnecessaryImportsCommandArgs>
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.OrganizeDocument);

    public string DisplayName => EditorFeaturesResources.Organize_Document;

    public CommandState GetCommandState(OrganizeDocumentCommandArgs args)
        => GetCommandState(args, _ => EditorFeaturesResources.Organize_Document, needsSemantics: true);

    public CommandState GetCommandState(SortImportsCommandArgs args)
        => GetCommandState(args, o => o.SortImportsDisplayStringWithAccelerator, needsSemantics: false);

    public CommandState GetCommandState(SortAndRemoveUnnecessaryImportsCommandArgs args)
        => GetCommandState(args, o => o.SortAndRemoveUnusedImportsDisplayStringWithAccelerator, needsSemantics: true);

    private static CommandState GetCommandState(EditorCommandArgs args, Func<IOrganizeImportsService, string> descriptionString, bool needsSemantics)
    {
        if (IsCommandSupported(args, needsSemantics, out var workspace))
        {
            var organizeImportsService = workspace.Services.SolutionServices.GetProjectServices(args.SubjectBuffer)!.GetRequiredService<IOrganizeImportsService>();
            return new CommandState(isAvailable: true, displayText: descriptionString(organizeImportsService));
        }
        else
        {
            return CommandState.Unspecified;
        }
    }

    private static bool IsCommandSupported(EditorCommandArgs args, bool needsSemantics, [NotNullWhen(true)] out Workspace? workspace)
    {
        workspace = null;
        if (args.SubjectBuffer.TryGetWorkspace(out var retrievedWorkspace))
        {
            workspace = retrievedWorkspace;
            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return false;
            }

            if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return !needsSemantics;
            }

            return args.SubjectBuffer.SupportsRefactorings();
        }

        return false;
    }

    private bool ExecuteCommand(
        EditorCommandArgs commandArgs,
        CommandExecutionContext context,
        Func<ITextSnapshot, IUIThreadOperationContext, Task<Document?>> getCurrentDocumentAsync,
        Func<Document, CancellationToken, Task<Document>> getChangedDocumentAsync)
    {
        // We're showing our own UI, ensure the editor doesn't show anything itself.
        context.OperationContext.TakeOwnership();

        var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
        _ = ExecuteAsync(commandArgs, getCurrentDocumentAsync, getChangedDocumentAsync)
            .ReportNonFatalErrorAsync()
            .CompletesAsyncOperation(token);

        return true;
    }

    private async Task ExecuteAsync(
        EditorCommandArgs commandArgs,
        Func<ITextSnapshot, IUIThreadOperationContext, Task<Document?>> getCurrentDocumentAsync,
        Func<Document, CancellationToken, Task<Document>> getChangedDocumentAsync)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

        var subjectBuffer = commandArgs.SubjectBuffer;
        var textView = commandArgs.TextView;

        var caretPoint = textView.GetCaretPoint(subjectBuffer);
        if (caretPoint is null)
            return;

        if (!subjectBuffer.TryGetWorkspace(out var workspace))
            return;

        var snapshotSpan = textView.GetTextElementSpan(textView.Caret.Position.BufferPosition);

        var indicatorFactory = workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
        using var backgroundWorkContext = indicatorFactory.Create(
            commandArgs.TextView,
            snapshotSpan,
            EditorFeaturesResources.Organizing_document,
            cancelOnEdit: true,
            cancelOnFocusLost: false);

        var cancellationToken = backgroundWorkContext.UserCancellationToken;

        await TaskScheduler.Default;

        var currentDocument = await getCurrentDocumentAsync(caretPoint.Value.Snapshot, backgroundWorkContext).ConfigureAwait(false);
        if (currentDocument is null)
            return;

        var newDocument = await getChangedDocumentAsync(currentDocument, cancellationToken).ConfigureAwait(false);
        if (currentDocument == newDocument)
            return;

        var changes = await newDocument.GetTextChangesAsync(currentDocument, cancellationToken).ConfigureAwait(false);

        // Required to switch back to the UI thread to call TryApplyChanges
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // We're about to make an edit ourselves.  so disable the cancellation that happens on editing.
        var disposable = await backgroundWorkContext.SuppressAutoCancelAsync().ConfigureAwait(true);
        await using var _ = disposable.ConfigureAwait(true);

        commandArgs.SubjectBuffer.ApplyChanges(changes);
    }

    public bool ExecuteCommand(OrganizeDocumentCommandArgs args, CommandExecutionContext context)
        => ExecuteCommand(
            args, context,
            // Need full semantics for this operation.
            (snapshot, context) => snapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(context),
            (document, cancellationToken) => OrganizingService.OrganizeAsync(document, cancellationToken: cancellationToken));

    public bool ExecuteCommand(SortImportsCommandArgs args, CommandExecutionContext context)
        => ExecuteCommand(
            args, context,
            // Only need syntax for this operation.
            (snapshot, context) => Task.FromResult(snapshot.GetOpenDocumentInCurrentContextWithChanges()),
            async (document, cancellationToken) =>
            {
                var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
                var options = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
                return await organizeImportsService.OrganizeImportsAsync(document, options, cancellationToken).ConfigureAwait(false);
            });

    public bool ExecuteCommand(SortAndRemoveUnnecessaryImportsCommandArgs args, CommandExecutionContext context)
        => ExecuteCommand(
            args, context,
            // Need full semantics for this operation.
            (snapshot, context) => snapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(context),
            async (document, cancellationToken) =>
            {
                var formattingOptions = document.SupportsSyntaxTree
                    ? await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false)
                    : null;

                var removeImportsService = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();
                var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();

                var newDocument = await removeImportsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
                var options = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);
                return await organizeImportsService.OrganizeImportsAsync(newDocument, options, cancellationToken).ConfigureAwait(false);
            });
}
