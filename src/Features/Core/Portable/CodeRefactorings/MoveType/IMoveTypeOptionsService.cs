// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal interface IMoveTypeOptionsService : IWorkspaceService
    {
        MoveTypeOptionsResult GetMoveTypeOptions(
            string suggestedFileName,
            Document document,
            INotificationService notificationService,
            IProjectManagementService projectManagementService,
            ISyntaxFactsService syntaxFactsService);
    }
}
