// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.GoToImplementation
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToImplementation,
        ContentTypeNames.RoslynContentType)]
    internal partial class GoToImplementationCommandHandler : ICommandHandler<GoToImplementationCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly IEnumerable<Lazy<IStreamingFindUsagesPresenter>> _streamingPresenters;

        [ImportingConstructor]
        public GoToImplementationCommandHandler(
            IWaitIndicator waitIndicator,
            [ImportMany] IEnumerable<Lazy<IStreamingFindUsagesPresenter>> streamingPresenters)
        {
            _waitIndicator = waitIndicator;
            _streamingPresenters = streamingPresenters;
        }

        private (Document, IGoToImplementationService, IFindUsagesService) GetDocumentAndServices(ITextSnapshot snapshot)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            return (document,
                    document?.GetLanguageService<IGoToImplementationService>(),
                    document?.GetLanguageService<IFindUsagesService>());
        }

        public CommandState GetCommandState(GoToImplementationCommandArgs args, Func<CommandState> nextHandler)
        {
            // Because this is expensive to compute, we just always say yes as long as the language allows it.
            var (document, implService, findUsagesService) = GetDocumentAndServices(args.SubjectBuffer.CurrentSnapshot);
            return implService != null || findUsagesService != null
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        public void ExecuteCommand(GoToImplementationCommandArgs args, Action nextHandler)
        {
            var (document, implService, findUsagesService) = GetDocumentAndServices(args.SubjectBuffer.CurrentSnapshot);
            if (implService != null || findUsagesService != null)
            {
                var caret = args.TextView.GetCaretPoint(args.SubjectBuffer);
                if (caret.HasValue)
                {
                    ExecuteCommand(document, caret.Value, implService, findUsagesService);
                    return;
                }
            }

            nextHandler();
        }

        private void ExecuteCommand(
            Document document, int caretPosition,
            IGoToImplementationService synchronousService,
            IFindUsagesService streamingService)
        {
            var streamingPresenter = GetStreamingPresenter();

            var streamingEnabled = document.Project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.StreamingGoToImplementation, document.Project.Language);
            var canUseStreamingWindow = streamingEnabled && streamingService != null;
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
            IFindUsagesService findUsagesService,
            IStreamingFindUsagesPresenter streamingPresenter,
            CancellationToken cancellationToken,
            out string messageToShow)
        {
            // We create our own context object, simply to capture all the definitions reported by 
            // the individual IFindUsagesService.  Once we get the results back we'll then decide 
            // what to do with them.  If we get only a single result back, then we'll just go 
            // directly to it.  Otherwise, we'll present the results in the IStreamingFindUsagesPresenter.
            var goToImplContext = new SimpleFindUsagesContext(cancellationToken);
            findUsagesService.FindImplementationsAsync(document, caretPosition, goToImplContext).Wait(cancellationToken);

            // If finding implementations reported a message, then just stop and show that 
            // message to the user.
            messageToShow = goToImplContext.Message;
            if (messageToShow != null)
            {
                return;
            }

            var definitionItems = goToImplContext.GetDefinitions();

            streamingPresenter.TryNavigateToOrPresentItemsAsync(
                document.Project.Solution.Workspace, goToImplContext.SearchTitle, definitionItems).Wait(cancellationToken);
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
    }
}
