﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [Name("CallHierarchy")]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class CallHierarchyCommandHandler : VSCommanding.ICommandHandler<ViewCallHierarchyCommandArgs>
    {
        private readonly ICallHierarchyPresenter _presenter;
        private readonly CallHierarchyProvider _provider;

        public string DisplayName => EditorFeaturesResources.Call_Hierarchy;

        [ImportingConstructor]
        public CallHierarchyCommandHandler([ImportMany] IEnumerable<ICallHierarchyPresenter> presenters, CallHierarchyProvider provider)
        {
            _presenter = presenters.FirstOrDefault();
            _provider = provider;
        }

        public bool ExecuteCommand(ViewCallHierarchyCommandArgs args, CommandExecutionContext context)
        {
            AddRootNode(args, context);
            return true;
        }

        private void AddRootNode(ViewCallHierarchyCommandArgs args, CommandExecutionContext context)
        {
            using (var waitScope = context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Computing_Call_Hierarchy_Information))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;
                var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return;
                }

                var workspace = document.Project.Solution.Workspace;
                var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var caretPosition = args.TextView.Caret.Position.BufferPosition.Position;
                var symbolUnderCaret = SymbolFinder.FindSymbolAtPositionAsync(semanticModel, caretPosition, workspace, cancellationToken)
                    .WaitAndGetResult(cancellationToken);

                if (symbolUnderCaret != null)
                {
                    // Map symbols so that Call Hierarchy works from metadata-as-source
                    var mappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();
                    var mapping = mappingService.MapSymbolAsync(document, symbolUnderCaret, cancellationToken).WaitAndGetResult(cancellationToken);

                    if (mapping.Symbol != null)
                    {
                        var node = _provider.CreateItemAsync(mapping.Symbol, mapping.Project, SpecializedCollections.EmptyEnumerable<Location>(), cancellationToken).WaitAndGetResult(cancellationToken);
                        if (node != null)
                        {
                            _presenter.PresentRoot((CallHierarchyItem)node);
                        }
                    }
                }
                else
                {
                    // We are about to show a modal UI dialog so we should take over the command execution
                    // wait context. That means the command system won't attempt to show its own wait dialog 
                    // and also will take it into consideration when measuring command handling duration.
                    waitScope.Context.TakeOwnership();
                    var notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(EditorFeaturesResources.Cursor_must_be_on_a_member_name, severity: NotificationSeverity.Information);
                }
            }
        }

        public VSCommanding.CommandState GetCommandState(ViewCallHierarchyCommandArgs args)
        {
            return VSCommanding.CommandState.Available;
        }
    }
}
