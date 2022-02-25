// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToDefinition)]
    [Export(typeof(GoToDefinitionCommandHandler))]
    internal class GoToDefinitionCommandHandler :
        ICommandHandler<GoToDefinitionCommandArgs>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public GoToDefinitionCommandHandler(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.GoToBase);
        }

        public string DisplayName => EditorFeaturesResources.Go_to_Definition;

        private static (Document?, IGoToDefinitionService?) GetDocumentAndService(ITextSnapshot snapshot)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            return (document, document?.GetLanguageService<IGoToDefinitionService>());
        }

        public CommandState GetCommandState(GoToDefinitionCommandArgs args)
        {
            var (_, service) = GetDocumentAndService(args.SubjectBuffer.CurrentSnapshot);
            return service != null
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        public bool ExecuteCommand(GoToDefinitionCommandArgs args, CommandExecutionContext context)
        {
            var subjectBuffer = args.SubjectBuffer;
            var (document, service) = GetDocumentAndService(subjectBuffer.CurrentSnapshot);

            // In Live Share, typescript exports a gotodefinition service that returns no results and prevents the LSP client
            // from handling the request.  So prevent the local service from handling goto def commands in the remote workspace.
            // This can be removed once typescript implements LSP support for goto def.
            if (service == null || subjectBuffer.IsInLspEditorContext())
                return false;

            Contract.ThrowIfNull(document);
            var caretPos = args.TextView.GetCaretPoint(subjectBuffer);
            if (!caretPos.HasValue)
                return false;

            // We're showing our own UI, ensure the editor doesn't show anything itself.
            context.OperationContext.TakeOwnership();

            var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
            ExecuteCommandAsync(args, document, service, caretPos.Value).CompletesAsyncOperation(token);
            return true;
        }

        private async Task ExecuteCommandAsync(
            GoToDefinitionCommandArgs args, Document document, IGoToDefinitionService service, SnapshotPoint position)
        {
            var indicatorFactory = document.Project.Solution.Workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();

            bool succeeded;
            using (var backgroundIndicator = indicatorFactory.Create(
                args.TextView, new SnapshotSpan(args.SubjectBuffer.CurrentSnapshot, new Span(position, 0)),
                EditorFeaturesResources.Navigating_to_definition))
            {
                await Task.Delay(10000).ConfigureAwait(false);
                succeeded = await GoToDefinitionAsync(
                    document, position, service, backgroundIndicator.UserCancellationToken).ConfigureAwait(false);
            }

            if (!succeeded)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

                var workspace = document.Project.Solution.Workspace;
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(
                    FeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret, EditorFeaturesResources.Go_to_Definition, NotificationSeverity.Information);
            }
        }

        private async Task<bool> GoToDefinitionAsync(
            Document document,
            int position,
            IGoToDefinitionService service,
            CancellationToken cancellationToken)
        {
            if (service is IAsyncGoToDefinitionService asyncService)
            {
                return await asyncService.TryGoToDefinitionAsync(document, position, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return service.TryGoToDefinition(document, position, cancellationToken);
            }
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public struct TestAccessor
        {
            private readonly GoToDefinitionCommandHandler _handler;

            public TestAccessor(GoToDefinitionCommandHandler handler)
            {
                _handler = handler;
            }

            public Task ExecuteCommandAsync(Document document, int caretPosition, IGoToDefinitionService goToDefinitionService)
                => _handler.GoToDefinitionAsync(document, caretPosition, goToDefinitionService, CancellationToken.None);
        }
    }
}
