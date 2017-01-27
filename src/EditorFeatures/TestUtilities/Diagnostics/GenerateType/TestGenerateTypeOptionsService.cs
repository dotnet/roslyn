// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.ProjectManagement;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType
{
    [ExportWorkspaceService(typeof(IGenerateTypeOptionsService), ServiceLayer.Default), Shared]
    internal class TestGenerateTypeOptionsService : IGenerateTypeOptionsService
    {
        public Accessibility Accessibility = Accessibility.NotApplicable;
        public TypeKind TypeKind = TypeKind.Class;
        public string TypeName = null;
        public Project Project = null;
        public bool IsNewFile = false;
        public string NewFileName = null;
        public IList<string> Folders = null;
        public string FullFilePath = null;
        public Document ExistingDocument = null;
        public bool AreFoldersValidIdentifiers = true;
        public string DefaultNamespace = null;
        public bool IsCancelled = false;

        // Actual input
        public string ClassName { get; private set; }
        public GenerateTypeDialogOptions GenerateTypeDialogOptions { get; private set; }

        public GenerateTypeOptionsResult GetGenerateTypeOptions(
            string className,
            GenerateTypeDialogOptions generateTypeDialogOptions,
            Document document,
            INotificationService notificationService,
            IProjectManagementService projectManagementService,
            ISyntaxFactsService syntaxFactsService)
        {
            // Storing the actual values
            ClassName = className;
            GenerateTypeDialogOptions = generateTypeDialogOptions;
            if (DefaultNamespace == null)
            {
                DefaultNamespace = projectManagementService.GetDefaultNamespace(Project, Project?.Solution.Workspace);
            }
            return new GenerateTypeOptionsResult(
                accessibility: Accessibility,
                typeKind: TypeKind,
                typeName: TypeName,
                project: Project,
                isNewFile: IsNewFile,
                newFileName: NewFileName,
                folders: Folders,
                fullFilePath: FullFilePath,
                existingDocument: ExistingDocument,
                areFoldersValidIdentifiers: AreFoldersValidIdentifiers,
                defaultNamespace: DefaultNamespace,
                isCancelled: IsCancelled);
        }

        public void SetGenerateTypeOptions(
            Accessibility accessibility = Accessibility.NotApplicable,
            TypeKind typeKind = TypeKind.Class,
            string typeName = null,
            Project project = null,
            bool isNewFile = false,
            string newFileName = null,
            IList<string> folders = null,
            string fullFilePath = null,
            Document existingDocument = null,
            bool areFoldersValidIdentifiers = true,
            string defaultNamespace = null,
            bool isCancelled = false)
        {
            Accessibility = accessibility;
            TypeKind = typeKind;
            TypeName = typeName;
            Project = project;
            IsNewFile = isNewFile;
            NewFileName = newFileName;
            Folders = folders;
            FullFilePath = fullFilePath;
            ExistingDocument = existingDocument;
            AreFoldersValidIdentifiers = areFoldersValidIdentifiers;
            DefaultNamespace = defaultNamespace;
            IsCancelled = isCancelled;
        }
    }
}
