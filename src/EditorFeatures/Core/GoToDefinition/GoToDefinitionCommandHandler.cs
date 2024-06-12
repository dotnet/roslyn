// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToDefinition)]
    [method: ImportingConstructor]
    [method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    internal class GoToDefinitionCommandHandler(
        IGlobalOptionService globalOptionService,
        IThreadingContext threadingContext,
        IUIThreadOperationExecutor executor,
        IAsynchronousOperationListenerProvider listenerProvider) :
        ICommandHandler<GoToDefinitionCommandArgs>
    {
        private readonly IGlobalOptionService _globalOptionService = globalOptionService;
        private readonly IThreadingContext _threadingContext = threadingContext;
        private readonly IUIThreadOperationExecutor _executor = executor;
        private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.GoToDefinition);

        public string DisplayName => EditorFeaturesResources.Go_to_Definition;

        private static (Document?, IGoToDefinitionService?, IAsyncGoToDefinitionService?) GetDocumentAndService(ITextSnapshot snapshot)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            return (document, document?.GetLanguageService<IGoToDefinitionService>(), document?.GetLanguageService<IAsyncGoToDefinitionService>());
        }

        public CommandState GetCommandState(GoToDefinitionCommandArgs args)
        {
            var (_, service, asyncService) = GetDocumentAndService(args.SubjectBuffer.CurrentSnapshot);
            return service != null || asyncService != null
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        public bool ExecuteCommand(GoToDefinitionCommandArgs args, CommandExecutionContext context)
        {
            var subjectBuffer = args.SubjectBuffer;
            var (document, service, asyncService) = GetDocumentAndService(subjectBuffer.CurrentSnapshot);

            if (service == null && asyncService == null)
                return false;

            Contract.ThrowIfNull(document);

            // In Live Share, typescript exports a gotodefinition service that returns no results and prevents the LSP
            // client from handling the request.  So prevent the local service from handling goto def commands in the
            // remote workspace. This can be removed once typescript implements LSP support for goto def.
            if (subjectBuffer.IsInLspEditorContext())
                return false;

            // If the file is empty, there's nothing to be on that we can goto-def on.  This also ensures that we can
            // create an appropriate non-empty tracking span later on.
            var currentSnapshot = subjectBuffer.CurrentSnapshot;
            if (currentSnapshot.Length == 0)
                return false;

            // If there's a selection, use the starting point of the selection as the invocation point. Otherwise, just
            // pick wherever the caret is exactly at.
            var caretPos =
                args.TextView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer).FirstOrNull()?.Start ??
                args.TextView.GetCaretPoint(subjectBuffer);

            if (!caretPos.HasValue)
                return false;

            if (asyncService != null && _globalOptionService.GetOption(FeatureOnOffOptions.NavigateAsynchronously))
            {
                // We're showing our own UI, ensure the editor doesn't show anything itself.
                context.OperationContext.TakeOwnership();
                var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
                ExecuteAsynchronouslyAsync(args, document, asyncService, caretPos.Value)
                    .ReportNonFatalErrorAsync()
                    .CompletesAsyncOperation(token);
            }
            else
            {
                // The language either doesn't support async goto-def, or the option is disabled to navigate
                // asynchronously.  So fall back to normal synchronous navigation.
                var succeeded = ExecuteSynchronously(document, service, asyncService, caretPos.Value, context);

                if (!succeeded)
                {
                    // Dismiss any context dialog that is up before showing our own notification.
                    context.OperationContext.TakeOwnership();
                    ReportFailure(document);
                }
            }

            return true;
        }

        private bool ExecuteSynchronously(
            Document document,
            IGoToDefinitionService? service,
            IAsyncGoToDefinitionService? asyncService,
            int position,
            CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Navigating_to_definition))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;
                if (asyncService != null)
                {
                    return _threadingContext.JoinableTaskFactory.Run(async () =>
                    {
                        // determine the location first.
                        var (location, _) = await asyncService.FindDefinitionLocationAsync(
                            document, position, includeType: true, cancellationToken).ConfigureAwait(false);
                        return await location.TryNavigateToAsync(
                            _threadingContext, NavigationOptions.Default, cancellationToken).ConfigureAwait(false);
                    });
                }
                else if (service != null)
                {
                    return service.TryGoToDefinition(document, position, cancellationToken);
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }
        }

        private static void ReportFailure(Document document)
        {
            var notificationService = document.Project.Solution.Services.GetRequiredService<INotificationService>();
            notificationService.SendNotification(
                FeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret, EditorFeaturesResources.Go_to_Definition, NotificationSeverity.Information);
        }

        private async Task ExecuteAsynchronouslyAsync(
            GoToDefinitionCommandArgs args, Document document, IAsyncGoToDefinitionService service, SnapshotPoint position)
        {
            bool succeeded;

            var indicatorFactory = document.Project.Solution.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();

            // TODO: prior logic was to get a tracking span of length 1 here.  Preserving that, though it's unclear if
            // that is necessary for the BWI to work properly.
            Contract.ThrowIfTrue(position.Snapshot.Length == 0);
            var applicableToSpan = position < position.Snapshot.Length
                ? new SnapshotSpan(position, position + 1)
                : new SnapshotSpan(position - 1, position);

            using (var backgroundIndicator = indicatorFactory.Create(
                args.TextView, applicableToSpan,
                EditorFeaturesResources.Navigating_to_definition))
            {
                var cancellationToken = backgroundIndicator.UserCancellationToken;

                // determine the location first.
                var (location, _) = await service.FindDefinitionLocationAsync(
                    document, position, includeType: true, cancellationToken).ConfigureAwait(false);

                // make sure that if our background indicator got canceled, that we do not still perform the navigation.
                if (backgroundIndicator.UserCancellationToken.IsCancellationRequested)
                    return;

                // we're about to navigate.  so disable cancellation on focus-lost in our indicator so we don't end up
                // causing ourselves to self-cancel.
                backgroundIndicator.CancelOnFocusLost = false;
                succeeded = await location.TryNavigateToAsync(
                    _threadingContext, new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true), cancellationToken).ConfigureAwait(false);
            }

            if (!succeeded)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                ReportFailure(document);
            }
        }
    }
}
