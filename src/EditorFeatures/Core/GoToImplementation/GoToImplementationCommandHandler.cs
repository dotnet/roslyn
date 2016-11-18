// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToImplementation,
        ContentTypeNames.RoslynContentType)]
    internal sealed class GoToImplementationCommandHandler : ICommandHandler<GoToImplementationCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        public GoToImplementationCommandHandler(
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _waitIndicator = waitIndicator;
            _streamingPresenters = streamingPresenters;

            _asyncListener = new AggregateAsynchronousOperationListener(
                asyncListeners, FeatureAttribute.GoToImplementation);
        }

        public CommandState GetCommandState(GoToImplementationCommandArgs args, Func<CommandState> nextHandler)
        {
            // Because this is expensive to compute, we just always say yes
            return CommandState.Available;
        }

        public void ExecuteCommand(GoToImplementationCommandArgs args, Action nextHandler)
        {
            var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);

            if (caret.HasValue)
            {
                var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    ExecuteCommand(document, caret.Value);
                    return;
                }
            }

            nextHandler();
        }

        private void ExecuteCommand(Document document, int caretPosition)
        {
            var streamingService = document.GetLanguageService<IStreamingFindImplementationsService>();
            var synchronousService = document.GetLanguageService<IGoToImplementationService>();

            var streamingPresenter = GetStreamingPresenter();

            // See if we're running on a host that can provide streaming results.
            // We'll both need a FAR service that can stream results to us, and 
            // a presenter that can accept streamed results.
            var streamingEnabled = document.Project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.StreamingGoToImplementation, document.Project.Language);
            var canUseStreamingWindow = streamingEnabled && streamingService != null && streamingPresenter != null;
            var canUseSynchronousWindow = synchronousService != null;

            if (canUseStreamingWindow || canUseSynchronousWindow)
            {
                // We have all the cheap stuff, so let's do expensive stuff now
                string messageToShow = null;
                _waitIndicator.Wait(
                    EditorFeaturesResources.Go_To_Implementation,
                    EditorFeaturesResources.Locating_implementations,
                    allowCancel: true,
                    action: context =>
                    {
                        if (canUseStreamingWindow)
                        {
                            StreamingGoToImplementation(
                                document, caretPosition,
                                streamingService, streamingPresenter,
                                context.CancellationToken, out messageToShow);
                        }
                        else
                        {
                            synchronousService.TryGoToImplementation(
                                document, caretPosition, context.CancellationToken, out messageToShow);
                        }
                    });

                if (messageToShow != null)
                {
                    var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(messageToShow,
                        title: EditorFeaturesResources.Go_To_Implementation,
                        severity: NotificationSeverity.Information);
                }
            }
        }

        private void StreamingGoToImplementation(
            Document document, int caretPosition,
            IStreamingFindImplementationsService streamingService,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            out string messageToShow)
        {
            var goToImplContext = new GoToImplementationContext(cancellationToken);
            streamingService.FindImplementationsAsync(document, caretPosition, goToImplContext).Wait(cancellationToken);

            // If finding implementations reported a message, then just stop and show that 
            // message to the user.
            messageToShow = goToImplContext.Message;
            if (messageToShow != null)
            {
                return;
            }

            var allItems = goToImplContext.GetDefinitionItems();

            streamingPresenter.NavigateToOrPresentItemsAsync(
                EditorFeaturesResources.Go_To_Implementation, allItems).Wait(cancellationToken);
        }

        private IStreamingFindUsagesPresenter GetStreamingPresenter()
        {
            try
            {
                return _streamingPresenters.FirstOrDefault()?.Value;
            }
            catch
            {
                return null;
            }
        }

        private class GoToImplementationContext : FindUsagesContext
        {
            private readonly object _gate = new object();
            private readonly ImmutableArray<DefinitionItem>.Builder _definitionItems =
                ImmutableArray.CreateBuilder<DefinitionItem>();

            public override CancellationToken CancellationToken { get; }

            public GoToImplementationContext(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
            }

            public string Message { get; private set; }

            public override void ReportMessage(string message)
                => Message = message;

            public ImmutableArray<DefinitionItem> GetDefinitionItems()
            {
                lock (_gate)
                {
                    return _definitionItems.ToImmutableArray();
                }
            }

            public override Task OnDefinitionFoundAsync(DefinitionItem definition)
            {
                lock (_gate)
                {
                    _definitionItems.Add(definition);
                }

                return SpecializedTasks.EmptyTask;
            }
        }
    }
}