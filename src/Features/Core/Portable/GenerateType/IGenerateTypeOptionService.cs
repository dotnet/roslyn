// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal interface IGenerateTypeOptionsService : IWorkspaceService
    {
        GenerateTypeOptionsResult GetGenerateTypeOptions(
            string className,
            GenerateTypeDialogOptions generateTypeDialogOptions,
            Document document,
            INotificationService notificationService,
            IProjectManagementService projectManagementService,
            ISyntaxFactsService syntaxFactsService);
    }
}
