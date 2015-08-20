// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToImplementation
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToDefinition,
        ContentTypeNames.RoslynContentType)]
    internal sealed class GoToImplementationCommandHandler : ICommandHandler<GoToImplementationCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _navigatableItemPresenters;

        [ImportingConstructor]
        public GoToImplementationCommandHandler(
            [ImportMany] IEnumerable<Lazy<INavigableItemsPresenter>> navigatableItemPresenters,
            IWaitIndicator waitIndicator)
        {
            _navigatableItemPresenters = navigatableItemPresenters;
            _waitIndicator = waitIndicator;
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
                    // We have all the cheap stuff, so let's do expensive stuff now
                    string messageToShow = null;
                    _waitIndicator.Wait(
                        EditorFeaturesResources.GoToImplementationTitle,
                        EditorFeaturesResources.GoToImplementationMessage,
                        allowCancel: true,
                        action: context => messageToShow = ExecuteCommandCoreAndReturnMessage(document, caret.Value, context.CancellationToken));

                    if (messageToShow != null)
                    {
                        var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                        notificationService.SendNotification(messageToShow,
                            title: EditorFeaturesResources.GoToImplementationTitle,
                            severity: NotificationSeverity.Information);
                    }

                    return;
                }
            }

            nextHandler();
        }

        private string ExecuteCommandCoreAndReturnMessage(Document document, int position, CancellationToken cancellationToken)
        {
            var symbol = SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);

            if (symbol != null)
            {
                var solution = document.Project.Solution;
                var implementations =
                    SymbolFinder.FindImplementationsAsync(
                        symbol,
                        solution,
                        cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken).ToList();

                if (implementations.Count == 0)
                {
                    return EditorFeaturesResources.SymbolHasNoImplementations;
                }
                else if (implementations.Count == 1)
                {
                    GoToDefinition.GoToDefinitionHelpers.TryGoToDefinition(implementations.Single(), document.Project, _navigatableItemPresenters, cancellationToken);
                }
                else
                {
                    // We have multiple symbols, so we'll build a list of all preferred locations for all the symbols
                    var navigableItems = implementations.SelectMany(implementation =>
                        NavigableItemFactory.GetItemsfromPreferredSourceLocations(solution, implementation));

                    var presenter = _navigatableItemPresenters.First();
                    presenter.Value.DisplayResult(navigableItems);
                }

                return null;
            }
            else
            {
                return EditorFeaturesResources.CannotNavigateToTheSymbol;
            }
        }
    }
}
