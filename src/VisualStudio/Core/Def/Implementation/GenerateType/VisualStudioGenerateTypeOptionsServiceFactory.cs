// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.GenerateType
{
    [ExportWorkspaceServiceFactory(typeof(IGenerateTypeOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioGenerateTypeOptionsServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioGenerateTypeOptionsServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new VisualStudioGenerateTypeOptionsService();

        private class VisualStudioGenerateTypeOptionsService : IGenerateTypeOptionsService
        {
            private bool _isNewFile = false;
            private string _accessSelectString = "";
            private string _typeKindSelectString = "";

            public GenerateTypeOptionsResult GetGenerateTypeOptions(
                string typeName,
                GenerateTypeDialogOptions generateTypeDialogOptions,
                Document document,
                INotificationService notificationService,
                IProjectManagementService projectManagementService,
                ISyntaxFactsService syntaxFactsService)
            {
                var viewModel = new GenerateTypeDialogViewModel(
                    document,
                    notificationService,
                    projectManagementService,
                    syntaxFactsService,
                    generateTypeDialogOptions,
                    typeName,
                    document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb",
                    _isNewFile,
                    _accessSelectString,
                    _typeKindSelectString);

                var dialog = new GenerateTypeDialog(viewModel);
                var result = dialog.ShowModal();

                if (result.HasValue && result.Value)
                {
                    // Retain choice
                    _isNewFile = viewModel.IsNewFile;
                    _accessSelectString = viewModel.SelectedAccessibilityString;
                    _typeKindSelectString = viewModel.SelectedTypeKindString;

                    var defaultNamespace = projectManagementService.GetDefaultNamespace(viewModel.SelectedProject, viewModel.SelectedProject?.Solution.Workspace);

                    return new GenerateTypeOptionsResult(
                        accessibility: viewModel.SelectedAccessibility,
                        typeKind: viewModel.SelectedTypeKind,
                        typeName: viewModel.TypeName,
                        project: viewModel.SelectedProject,
                        isNewFile: viewModel.IsNewFile,
                        newFileName: viewModel.FileName.Trim(),
                        folders: viewModel.Folders,
                        fullFilePath: viewModel.FullFilePath,
                        existingDocument: viewModel.SelectedDocument,
                        defaultNamespace: defaultNamespace,
                        areFoldersValidIdentifiers: viewModel.AreFoldersValidIdentifiers);
                }
                else
                {
                    return GenerateTypeOptionsResult.Cancelled;
                }
            }
        }
    }
}
