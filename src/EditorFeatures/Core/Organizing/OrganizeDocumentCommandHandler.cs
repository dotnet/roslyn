// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.Organizing;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Organizing
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [Name(PredefinedCommandHandlerNames.OrganizeDocument)]
    internal class OrganizeDocumentCommandHandler :
        ICommandHandler<OrganizeDocumentCommandArgs>,
        ICommandHandler<SortImportsCommandArgs>,
        ICommandHandler<SortAndRemoveUnnecessaryImportsCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public OrganizeDocumentCommandHandler(
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _globalOptions = globalOptions;
            _listener = listenerProvider.GetListener(FeatureAttribute.OrganizeDocument);
        }

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
                var organizeImportsService = workspace.Services.SolutionServices.GetProjectServices(args.SubjectBuffer).GetService<IOrganizeImportsService>();
                return new CommandState(isAvailable: true, displayText: descriptionString(organizeImportsService));
            }
            else
            {
                return CommandState.Unspecified;
            }
        }

        private static bool IsCommandSupported(EditorCommandArgs args, bool needsSemantics, out Workspace workspace)
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

        private void ExecuteCommandWorker(
            EditorCommandArgs commandArgs,
            CommandExecutionContext context,
            string dialogText,
            Func<ITextSnapshot, IUIThreadOperationContext, Task<Document>> getCurrentDocumentAsync,
            Func<Document, CancellationToken, Task<Document>> getChangedDocumentAsync)
        {
            // We're showing our own UI, ensure the editor doesn't show anything itself.
            context.OperationContext.TakeOwnership();

            var subjectBuffer = commandArgs.SubjectBuffer;
            var textView = commandArgs.TextView;

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (caretPoint is null)
                return;

            if (!subjectBuffer.TryGetWorkspace(out var workspace))
                return;

            var token = _listener.BeginAsyncOperation(nameof(ExecuteCommandWorker));

            var snapshotSpan = textView.GetTextElementSpan(caretPoint.Value);
            ExecuteAsync(workspace, commandArgs, snapshotSpan, dialogText, getCurrentDocumentAsync, getChangedDocumentAsync)
                .ReportNonFatalErrorAsync()
                .CompletesAsyncOperation(token);
        }

        private async Task ExecuteAsync(
            Workspace workspace,
            EditorCommandArgs commandArgs,
            SnapshotSpan snapshotSpan,
            string dialogText,
            Func<ITextSnapshot, IUIThreadOperationContext, Task<Document>> getCurrentDocumentAsync,
            Func<Document, CancellationToken, Task<Document>> getChangedDocumentAsync)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var indicatorFactory = workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
            using var backgroundWorkContext = indicatorFactory.Create(
                commandArgs.TextView,
                snapshotSpan,
                dialogText,
                cancelOnEdit: true,
                cancelOnFocusLost: true);

            var cancellationToken = backgroundWorkContext.UserCancellationToken;

            await TaskScheduler.Default;

            var currentDocument = await getCurrentDocumentAsync(snapshotSpan.Snapshot, backgroundWorkContext).ConfigureAwait(false);
            var newDocument = await getChangedDocumentAsync(currentDocument, cancellationToken).ConfigureAwait(false);

            if (currentDocument == newDocument)
                return;

            var changes = newDocument.GetTextChangesAsync(currentDocument, cancellationToken).WaitAndGetResult(cancellationToken);

            // Required to switch back to the UI thread to call TryApplyChanges
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // We're about to make an edit ourselves.  so disable the cancellation that happens on editing.
            backgroundWorkContext.CancelOnEdit = false;

            commandArgs.SubjectBuffer.ApplyChanges(changes);
        }

        public bool ExecuteCommand(OrganizeDocumentCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Organizing_document))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;
                var document = args.SubjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                    context.OperationContext, _threadingContext);
                if (document != null)
                {
                    var newDocument = OrganizingService.OrganizeAsync(document, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
                    if (document != newDocument)
                    {
                        var changes = newDocument.GetTextChangesAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                        args.SubjectBuffer.ApplyChanges(changes);
                    }
                }
            }

            return true;
        }

        public bool ExecuteCommand(SortImportsCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Organizing_document))
            {
                SortImports(args.SubjectBuffer, context.OperationContext);
            }

            return true;
        }

        public bool ExecuteCommand(SortAndRemoveUnnecessaryImportsCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Organizing_document))
            {
                this.SortAndRemoveUnusedImports(args.SubjectBuffer, context.OperationContext);
            }

            return true;
        }

        private void SortImports(ITextBuffer subjectBuffer, IUIThreadOperationContext operationContext)
        {
            var cancellationToken = operationContext.UserCancellationToken;
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document != null)
            {
                var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
                var options = document.GetOrganizeImportsOptionsAsync(_globalOptions, cancellationToken).AsTask().WaitAndGetResult(cancellationToken);
                var newDocument = organizeImportsService.OrganizeImportsAsync(document, options, cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    var changes = newDocument.GetTextChangesAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                    subjectBuffer.ApplyChanges(changes);
                }
            }
        }

        private void SortAndRemoveUnusedImports(ITextBuffer subjectBuffer, IUIThreadOperationContext operationContext)
        {
            var cancellationToken = operationContext.UserCancellationToken;
            var document = subjectBuffer.CurrentSnapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
                operationContext, _threadingContext);
            if (document != null)
            {
                var formattingOptions = document.SupportsSyntaxTree ? document.GetSyntaxFormattingOptionsAsync(_globalOptions, cancellationToken).AsTask().WaitAndGetResult(cancellationToken) : null;
                var newDocument = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>().RemoveUnnecessaryImportsAsync(document, formattingOptions, cancellationToken).WaitAndGetResult(cancellationToken);
                var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
                var options = document.GetOrganizeImportsOptionsAsync(_globalOptions, cancellationToken).AsTask().WaitAndGetResult(cancellationToken);
                newDocument = organizeImportsService.OrganizeImportsAsync(newDocument, options, cancellationToken).WaitAndGetResult(cancellationToken);
                if (document != newDocument)
                {
                    var changes = newDocument.GetTextChangesAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);
                    subjectBuffer.ApplyChanges(changes);
                }
            }
        }
    }
}
