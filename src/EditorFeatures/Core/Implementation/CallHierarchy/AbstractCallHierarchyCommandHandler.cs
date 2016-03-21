// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    internal class AbstractCallHierarchyCommandHandler : ICommandHandler<ViewCallHierarchyCommandArgs>
    {
        private readonly ICallHierarchyPresenter _presenter;
        private readonly CallHierarchyProvider _provider;
        private readonly IWaitIndicator _waitIndicator;

        protected AbstractCallHierarchyCommandHandler(IEnumerable<ICallHierarchyPresenter> presenters, CallHierarchyProvider provider, IWaitIndicator waitIndicator)
        {
            _presenter = presenters.FirstOrDefault();
            _provider = provider;
            _waitIndicator = waitIndicator;
        }

        public void ExecuteCommand(ViewCallHierarchyCommandArgs args, Action nextHandler)
        {
            AddRootNode(args);
        }

        private void AddRootNode(ViewCallHierarchyCommandArgs args)
        {
            _waitIndicator.Wait(EditorFeaturesResources.CallHierarchy, EditorFeaturesResources.ComputingCallHierarchyInformation, allowCancel: true, action: waitcontext =>
                {
                    var cancellationToken = waitcontext.CancellationToken;
                    var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        return;
                    }

                    var workspace = document.Project.Solution.Workspace;
                    var semanticModel = document.GetSemanticModelAsync(waitcontext.CancellationToken).WaitAndGetResult(cancellationToken);

                    var caretPosition = args.TextView.Caret.Position.BufferPosition.Position;
                    var symbolUnderCaret = SymbolFinder.FindSymbolAtPositionAsync(semanticModel, caretPosition, workspace, cancellationToken)
                        .WaitAndGetResult(cancellationToken);

                    if (symbolUnderCaret != null)
                    {
                        // Map symbols so that Call Hierarchy works from metadata-as-source
                        var mappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();
                        var mapping = mappingService.MapSymbolAsync(document, symbolUnderCaret, waitcontext.CancellationToken).WaitAndGetResult(cancellationToken);

                        if (mapping.Symbol != null)
                        {
                            var node = _provider.CreateItem(mapping.Symbol, mapping.Project, SpecializedCollections.EmptyEnumerable<Location>(), cancellationToken).WaitAndGetResult(cancellationToken);
                            if (node != null)
                            {
                                _presenter.PresentRoot((CallHierarchyItem)node);
                            }
                        }
                    }
                    else
                    {
                        var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                        notificationService.SendNotification(EditorFeaturesResources.CursorMustBeOnAMemberName, severity: NotificationSeverity.Information);
                    }
                });
        }

        public CommandState GetCommandState(ViewCallHierarchyCommandArgs args, Func<CommandState> nextHandler)
        {
            return CommandState.Available;
        }
    }
}
