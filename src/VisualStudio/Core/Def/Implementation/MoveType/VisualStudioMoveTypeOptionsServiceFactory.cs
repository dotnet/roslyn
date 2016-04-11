// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveType
{
    [ExportWorkspaceServiceFactory(typeof(IMoveTypeOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioMoveTypeOptionsServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new VisualStudioMoveTypeOptionsService();
        }

        private class VisualStudioMoveTypeOptionsService : IMoveTypeOptionsService
        {
            public MoveTypeOptionsResult GetMoveTypeOptions(
                string suggestedFileName,
                Document document,
                INotificationService notificationService,
                IProjectManagementService projectManagementService,
                ISyntaxFactsService syntaxFactsService)
            {
                var viewModel = new MoveTypeDialogViewModel(suggestedFileName, document, notificationService, projectManagementService, syntaxFactsService);
                var dialog = new MoveTypeDialog(viewModel);
                var result = dialog.ShowModal();

                if (result.HasValue && result.Value)
                {
                    var fileName = viewModel.FileName.Trim();
                    return new MoveTypeOptionsResult(fileName);
                }
                else
                {
                    return MoveTypeOptionsResult.Cancelled;
                }
            }
        }
    }
}
