// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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

            var indicatorFactory = document.Project.Solution.Workspace.Services.GetRequiredService<IBackgroundWorkIndicatorFactory>();
            using var backgroundIndicator = indicatorFactory.Create(textView, new SnapshotSpan(subjectBuffer.CurrentSnapshot, new Span(caretPos.Value, 0))

            var token = _listener.BeginAsyncOperation(nameof(ExecuteCommand));
            ExecuteCommandAsync(document, caretPos.Value, service, context).CompletesAsyncOperation(token);
            return true;
        }

        private void ExecuteCommand(Document document, int caretPosition, IGoToDefinitionService goToDefinitionService, CommandExecutionContext context)
        {
            context.OperationContext.TakeOwnership();

            ExecuteCommandAsync(document, caretPosition, goToDefinitionService, context).CompletesAsyncOperation(token);
        }

        private async Task<string?> ExecuteCommandAsync(
            Document document,
            int caretPosition,
            IGoToDefinitionService goToDefinitionService)
        {
            // 

            string? errorMessage = null;

            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Navigating_to_definition))
            {
                if (goToDefinitionService != null &&
                    goToDefinitionService.TryGoToDefinition(document, caretPosition, context.OperationContext.UserCancellationToken))
                {
                    return;
                }

                errorMessage = FeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret;
            }

            if (errorMessage != null)
            {
                // We are about to show a modal UI dialog so we should take over the command execution
                // wait context. That means the command system won't attempt to show its own wait dialog 
                // and also will take it into consideration when measuring command handling duration.
                context.OperationContext.TakeOwnership();
                var workspace = document.Project.Solution.Workspace;
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                notificationService.SendNotification(errorMessage, title: EditorFeaturesResources.Go_to_Definition, severity: NotificationSeverity.Information);
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

            public Task ExecuteCommandAsync(Document document, int caretPosition, IGoToDefinitionService goToDefinitionService, CommandExecutionContext context)
                => _handler.ExecuteCommandAsync(document, caretPosition, goToDefinitionService, context);
        }
    }
}
